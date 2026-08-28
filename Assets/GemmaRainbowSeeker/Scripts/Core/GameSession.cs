using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    [Serializable]
    public struct LevelCompletionData
    {
        public int finalScore;
        public float completionTime;
        public int timeBonus;
        public int healthBonus;
        public int correctGems;
        public int wrongAttempts;
        public int damageTaken;
        public int checkpointRestarts;
        public int remainingHealth;
        public int starRating;
        public int highestMultiplier;
        public float longestRushDuration;
        public int rushBreaks;
    }

    /// <summary>
    /// Scene-level composition root for Gemma Beaker: Rainbow Seeker.
    /// Owns and coordinates the core game systems:
    /// - RainbowProgress
    /// - ScoreManager
    /// - RainbowRushController (single multiplier system)
    /// - LevelSessionStats
    /// Broadcasts session-level gameplay events and manages timer & pause states.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        // ── Scene Access ───────────────────────────────────────────────────────
        public static GameSession Active { get; set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Composition Root")]
        [Tooltip("Marks this object as the central composition root for the level.")]
        [SerializeField] private bool isCompositionRoot = true;

        [Header("Level Configuration")]
        [Tooltip("ScriptableObject containing all tunable rules for this level " +
                 "(colour sequence, scoring, rush parameters, star thresholds, mechanics).")]
        [SerializeField] private LevelDefinition _levelDefinition;

        // ── Owned Systems ─────────────────────────────────────────────────────
        private RainbowProgress        _rainbowProgress;
        private ScoreManager           _scoreManager;
        private RainbowRushController  _rushController;
        private LevelSessionStats      _sessionStats;

        private bool _isTimerRunning = true;
        private bool _isLevelCompleted = false;
        private bool _isPaused = false;
        private bool _isTutorialBlocking = false;

        private GemmaMotor2D _playerMotor;

        // ── Events ────────────────────────────────────────────────────
        /// <summary>Broadcast when score changes.</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Broadcast when the Rainbow Rush multiplier tier changes (1..5).</summary>
        public event Action<int> OnMultiplierChanged;

        /// <summary>Broadcast when the Rainbow Rush timer ticks (remainingTime, totalWindow).</summary>
        public event Action<float, float> OnRushTimerUpdated;

        /// <summary>Broadcast when Rainbow Rush is reset back to x1. Carries reset reason.</summary>
        public event Action<RushResetReason> OnRushReset;

        /// <summary>Broadcast when elapsed level time updates.</summary>
        public event Action<float> OnTimeUpdated;

        /// <summary>Broadcast when a correct gem is collected in sequence.</summary>
        public event Action<RainbowColour> OnCorrectGemCollected;

        /// <summary>Broadcast when a wrong colour gem is touched.</summary>
        public event Action<RainbowColour> OnWrongGemAttempted;

        /// <summary>Broadcast when progress is banked at a Rainbow Rest.</summary>
        public event Action OnProgressBanked;

        /// <summary>Broadcast when progress is restored to the banked count.</summary>
        public event Action OnProgressRestored;

        /// <summary>Broadcast when level progress is completely reset.</summary>
        public event Action OnProgressReset;

        /// <summary>Broadcast when all required colours are collected in order.</summary>
        public event Action OnRainbowCompleted;

        /// <summary>Broadcast when the level is completed and final stats calculated.</summary>
        public event Action<LevelCompletionData> OnLevelCompleted;

        /// <summary>Broadcast when a transient feedback message should appear on the HUD.</summary>
        public event Action<string, Color> OnFeedbackMessage;

        // ── Public Properties ─────────────────────────────────────────────────

        public bool IsCompositionRoot => isCompositionRoot;
        public bool IsTimerRunning => _isTimerRunning;
        public bool IsLevelCompleted => _isLevelCompleted;
        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }
        public bool IsTutorialBlocking
        {
            get => _isTutorialBlocking;
            set => _isTutorialBlocking = value;
        }

        /// <summary>The level definition asset assigned for this scene.</summary>
        public LevelDefinition LevelDefinition => _levelDefinition;

        /// <summary>Backward compatibility accessor for legacy LevelRules.</summary>
        public LevelRules LevelRules => _levelDefinition as LevelRules;

        /// <summary>Tracks which rainbow colours have been collected and banked.</summary>
        public RainbowProgress RainbowProgress => _rainbowProgress;

        /// <summary>Manages score awarding and completion bonuses.</summary>
        public ScoreManager ScoreManager => _scoreManager;

        /// <summary>The single Rainbow Rush multiplier and timer controller.</summary>
        public RainbowRushController RushController => _rushController;

        /// <summary>Accumulates per-run statistics (time, mistakes, restarts, rush stats, etc.).</summary>
        public LevelSessionStats SessionStats => _sessionStats;

        // ── MonoBehaviour Lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            Active = this;

            if (_levelDefinition != null)
            {
                InitializeSystems(_levelDefinition);
            }
            else
            {
                Debug.LogError(
                    "[GameSession] LevelDefinition asset is not assigned. " +
                    "Assign it in the Inspector on the GameSession object.",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }

            UnsubscribeSystems();
        }

        private void Update()
        {
            bool isSuspended = _isPaused || _isTutorialBlocking || _isLevelCompleted || !_isTimerRunning;

            if (!isSuspended && _sessionStats != null)
            {
                _sessionStats.Tick(Time.deltaTime);
                OnTimeUpdated?.Invoke(_sessionStats.ElapsedSeconds);
            }

            // Tick RainbowRushController
            if (_rushController != null)
            {
                if (_playerMotor == null)
                {
                    var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
                    if (gemma != null) _playerMotor = gemma.GetComponent<GemmaMotor2D>();
                }

                Vector2 input = _playerMotor != null ? _playerMotor.MoveInput : Vector2.zero;
                Vector2 vel   = _playerMotor != null ? _playerMotor.Velocity  : Vector2.zero;

                _rushController.Tick(Time.deltaTime, input, vel, isSuspended);
            }
        }

        // ── Initialization & Wiring ───────────────────────────────────────────

        /// <summary>
        /// Reusable level-loading flow: loads a LevelDefinition, initializes core systems,
        /// applies mechanics flags (Dash, Health, etc.) and notifies UI.
        /// </summary>
        public void LoadLevel(LevelDefinition definition)
        {
            InitializeSystems(definition);
        }

        public void InitializeSystems(LevelDefinition rules)
        {
            Active = this;
            UnsubscribeSystems();

            _levelDefinition = rules;
            if (_levelDefinition != null)
            {
                _rainbowProgress = new RainbowProgress(_levelDefinition.ColourSequence);
                _scoreManager    = new ScoreManager(_levelDefinition);
                _rushController  = new RainbowRushController(_levelDefinition);
                _sessionStats    = new LevelSessionStats();
                _isTimerRunning  = true;
                _isLevelCompleted = false;
                _isPaused        = false;
                _isTutorialBlocking = false;

                _rainbowProgress.CorrectColourCollected   += HandleCorrectColourCollected;
                _rainbowProgress.IncorrectColourAttempted += HandleIncorrectColourAttempted;
                _rainbowProgress.ProgressBanked           += HandleProgressBanked;
                _rainbowProgress.RainbowCompleted         += HandleRainbowCompleted;

                _scoreManager.ScoreChanged += HandleScoreChanged;

                _rushController.OnMultiplierChanged += HandleMultiplierChanged;
                _rushController.OnTimerUpdated      += HandleRushTimerUpdated;
                _rushController.OnRushReset         += HandleRushReset;

                ApplyMechanicsFlags();
            }
        }

        private void ApplyMechanicsFlags()
        {
            if (_levelDefinition == null) return;

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                var dash = gemma.GetComponent<GemmaDash>();
                if (dash != null)
                {
                    dash.DashEnabled = _levelDefinition.DashEnabled;
                }
                _playerMotor = gemma.GetComponent<GemmaMotor2D>();
            }

            var hud = FindFirstObjectByType<HudController>();
            if (hud != null)
            {
                hud.RefreshRainbowMeter();
            }
        }

        private void UnsubscribeSystems()
        {
            if (_rainbowProgress != null)
            {
                _rainbowProgress.CorrectColourCollected   -= HandleCorrectColourCollected;
                _rainbowProgress.IncorrectColourAttempted -= HandleIncorrectColourAttempted;
                _rainbowProgress.ProgressBanked           -= HandleProgressBanked;
                _rainbowProgress.RainbowCompleted         -= HandleRainbowCompleted;
            }

            if (_scoreManager != null)
            {
                _scoreManager.ScoreChanged -= HandleScoreChanged;
            }

            if (_rushController != null)
            {
                _rushController.OnMultiplierChanged -= HandleMultiplierChanged;
                _rushController.OnTimerUpdated      -= HandleRushTimerUpdated;
                _rushController.OnRushReset         -= HandleRushReset;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }

        private void HandleMultiplierChanged(int newMultiplier)
        {
            OnMultiplierChanged?.Invoke(newMultiplier);
        }

        private void HandleRushTimerUpdated(float remaining, float total)
        {
            OnRushTimerUpdated?.Invoke(remaining, total);
        }

        private void HandleRushReset(RushResetReason reason)
        {
            OnRushReset?.Invoke(reason);
            string reasonStr = RainbowRushController.GetResetReasonDescription(reason);
            PostFeedbackMessage($"RUSH LOST: {reasonStr.ToUpper()}", new Color(1f, 0.45f, 0.45f, 1f));
        }

        private void HandleCorrectColourCollected(RainbowColour colour)
        {
            int scoringMultiplier = _rushController != null ? _rushController.RegisterCorrectCollection() : 1;
            _scoreManager?.RegisterCorrectCollection(scoringMultiplier);
            _sessionStats?.RecordCorrectCollection();
            OnCorrectGemCollected?.Invoke(colour);

            string marker = RainbowColourHelper.GetMarkerString(colour);
            Color col = RainbowColourHelper.GetColor(colour);
            int newMul = _rushController != null ? _rushController.Multiplier : 1;
            string rushTag = newMul > 1 ? $" (RUSH x{newMul}!)" : "";
            PostFeedbackMessage($"COLLECTED {marker}! ({colour.ToString().ToUpper()}){rushTag}", col);
        }

        private void HandleIncorrectColourAttempted(RainbowColour colour)
        {
            _rushController?.ResetRush(RushResetReason.WrongColour);
            _sessionStats?.RecordWrongAttempt();
            OnWrongGemAttempted?.Invoke(colour);
        }

        private void HandleProgressBanked()
        {
            OnProgressBanked?.Invoke();
            PostFeedbackMessage("PROGRESS BANKED! 🔒", new Color(0.4f, 0.9f, 1f, 1f));
        }

        private void HandleRainbowCompleted()
        {
            OnRainbowCompleted?.Invoke();
            PostFeedbackMessage("RAINBOW COMPLETE! GATE UNLOCKED!", new Color(1f, 0.95f, 0.4f, 1f));
        }

        // ── Public Gameplay Operations ────────────────────────────────────────

        public void PostFeedbackMessage(string message, Color color)
        {
            OnFeedbackMessage?.Invoke(message, color);
        }

        public void PauseTimer()
        {
            _isTimerRunning = false;
        }

        public void ResumeTimer()
        {
            if (!_isLevelCompleted)
            {
                _isTimerRunning = true;
            }
        }

        /// <summary>
        /// Attempts to collect a gem of the given colour.
        /// Updates RainbowProgress, RainbowRushController, ScoreManager and SessionStats, and raises events.
        /// </summary>
        public bool TryCollectGem(RainbowColour colour)
        {
            if (_rainbowProgress == null) return false;
            return _rainbowProgress.TryCollect(colour);
        }

        /// <summary>
        /// Banks current collected progress (e.g. at a Rainbow Rest).
        /// Note: Passing through a Rainbow Rest does NOT reset Rush if Gemma swims through without stopping.
        /// </summary>
        public void BankProgress()
        {
            if (_rainbowProgress == null) return;
            _rainbowProgress.BankCurrentProgress();
        }

        /// <summary>
        /// Restores collected count to the last banked checkpoint count.
        /// </summary>
        public void RestoreBankedProgress()
        {
            if (_rainbowProgress == null) return;
            _rainbowProgress.RestoreBankedProgress();
            _rushController?.ResetRush(RushResetReason.Restart);
            OnProgressRestored?.Invoke();
            PostFeedbackMessage("RESTORED FROM CHECKPOINT", new Color(0.7f, 0.8f, 1f, 1f));
        }

        /// <summary>
        /// Resets all progress back to 0 (full level restart).
        /// </summary>
        public void ResetLevelProgress()
        {
            if (_rainbowProgress == null) return;
            _rainbowProgress.ResetLevelProgress();
            _rushController?.ResetStats();
            _sessionStats?.Reset();
            _isTimerRunning = true;
            _isLevelCompleted = false;
            _isPaused = false;
            _isTutorialBlocking = false;
            OnProgressReset?.Invoke();
        }

        /// <summary>
        /// Finalizes level completion, awards health and time bonuses, stops the timer,
        /// and broadcasts the completion data.
        /// </summary>
        public LevelCompletionData CompleteLevel(int remainingHealth)
        {
            _isLevelCompleted = true;
            _isTimerRunning = false;

            float elapsed = _sessionStats != null ? _sessionStats.ElapsedSeconds : 0f;
            int healthBonus = 0;
            int timeBonus = 0;

            if (_levelDefinition != null && _scoreManager != null)
            {
                // Completion Health Bonus (only if health mechanics are enabled)
                if (_levelDefinition.HealthEnabled)
                {
                    healthBonus = remainingHealth * _levelDefinition.CompletionHealthBonusPerPip;
                    _scoreManager.AddPoints(healthBonus);
                }
                else
                {
                    healthBonus = 0;
                }

                // Time Bonus: 5 pts per second under par
                float parTime = _levelDefinition.ParTimeSeconds;
                if (elapsed < parTime)
                {
                    int secUnderPar = (int)(parTime - elapsed);
                    timeBonus = secUnderPar * _levelDefinition.TimeBonusPerSecondUnderPar;
                    _scoreManager.AddPoints(timeBonus);
                }
            }

            int finalScore = _scoreManager != null ? _scoreManager.Score : 0;
            int stars = _levelDefinition != null ? _levelDefinition.ComputeStarRating(finalScore) : 1;

            if (_rushController != null && _sessionStats != null)
            {
                _sessionStats.UpdateRushStats(
                    _rushController.HighestMultiplierAchieved,
                    _rushController.LongestRushDuration,
                    _rushController.RushBreakCount
                );
            }

            var data = new LevelCompletionData
            {
                finalScore = finalScore,
                completionTime = elapsed,
                timeBonus = timeBonus,
                healthBonus = healthBonus,
                correctGems = _sessionStats != null ? _sessionStats.CorrectCollections : 0,
                wrongAttempts = _sessionStats != null ? _sessionStats.WrongAttempts : 0,
                damageTaken = _sessionStats != null ? _sessionStats.DamageTaken : 0,
                checkpointRestarts = _sessionStats != null ? _sessionStats.CheckpointRestarts : 0,
                remainingHealth = remainingHealth,
                starRating = stars,
                highestMultiplier = _rushController != null ? _rushController.HighestMultiplierAchieved : 1,
                longestRushDuration = _rushController != null ? _rushController.LongestRushDuration : 0f,
                rushBreaks = _rushController != null ? _rushController.RushBreakCount : 0
            };

            OnLevelCompleted?.Invoke(data);
            return data;
        }
    }
}



