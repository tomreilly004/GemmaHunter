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
    }

    /// <summary>
    /// Scene-level composition root for Gemma Beaker: Rainbow Seeker.
    /// Owns and coordinates the core game systems (RainbowProgress, ScoreManager,
    /// LevelSessionStats) and broadcasts session-level gameplay events.
    /// One instance is expected per gameplay scene, placed under the "Systems" root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        // ── Scene Access ───────────────────────────────────────────────────────
        public static GameSession Active { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Composition Root")]
        [Tooltip("Marks this object as the central composition root for the level.")]
        [SerializeField] private bool isCompositionRoot = true;

        [Header("Level Configuration")]
        [Tooltip("ScriptableObject containing all tunable rules for this level " +
                 "(colour sequence, scoring, combo parameters, star thresholds).")]
        [SerializeField] private LevelRules _levelRules;

        // ── Owned Systems ─────────────────────────────────────────────────────
        private RainbowProgress   _rainbowProgress;
        private ScoreManager      _scoreManager;
        private LevelSessionStats _sessionStats;
        private bool _isTimerRunning = true;
        private bool _isLevelCompleted = false;

        // ── Events ────────────────────────────────────────────────────
        /// <summary>Broadcast when score changes.</summary>
        public event Action<int> OnScoreChanged;

        /// <summary>Broadcast when combo changes.</summary>
        public event Action<float> OnComboChanged;

        /// <summary>Broadcast when elapsed time updates.</summary>
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

        /// <summary>Broadcast when all 7 colours are collected in order.</summary>
        public event Action OnRainbowCompleted;

        /// <summary>Broadcast when the level is completed and final stats calculated.</summary>
        public event Action<LevelCompletionData> OnLevelCompleted;

        /// <summary>Broadcast when a transient feedback message should appear on the HUD.</summary>
        public event Action<string, Color> OnFeedbackMessage;

        // ── Public Properties ─────────────────────────────────────────────────

        public bool IsCompositionRoot => isCompositionRoot;
        public bool IsTimerRunning => _isTimerRunning;
        public bool IsLevelCompleted => _isLevelCompleted;

        /// <summary>The level-rule asset assigned for this scene.</summary>
        public LevelRules LevelRules => _levelRules;

        /// <summary>Tracks which rainbow colours have been collected and banked.</summary>
        public RainbowProgress RainbowProgress => _rainbowProgress;

        /// <summary>Manages score and combo multiplier.</summary>
        public ScoreManager ScoreManager => _scoreManager;

        /// <summary>Accumulates per-run statistics (time, mistakes, restarts, etc.).</summary>
        public LevelSessionStats SessionStats => _sessionStats;

        // ── MonoBehaviour Lifecycle ───────────────────────────────────────────

        private void Awake()
        {
            Active = this;

            if (_levelRules != null)
            {
                InitializeSystems(_levelRules);
            }
            else
            {
                Debug.LogError(
                    "[GameSession] LevelRules asset is not assigned. " +
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
            if (_isTimerRunning && !_isLevelCompleted && _sessionStats != null)
            {
                _sessionStats.Tick(Time.deltaTime);
                OnTimeUpdated?.Invoke(_sessionStats.ElapsedSeconds);
            }
        }

        // ── Initialization & Wiring ───────────────────────────────────────────

        public void InitializeSystems(LevelRules rules)
        {
            UnsubscribeSystems();

            _levelRules = rules;
            _rainbowProgress = new RainbowProgress(_levelRules.ColourSequence);
            _scoreManager    = new ScoreManager(_levelRules);
            _sessionStats    = new LevelSessionStats();
            _isTimerRunning  = true;
            _isLevelCompleted = false;

            _rainbowProgress.CorrectColourCollected   += HandleCorrectColourCollected;
            _rainbowProgress.IncorrectColourAttempted += HandleIncorrectColourAttempted;
            _rainbowProgress.ProgressBanked           += HandleProgressBanked;
            _rainbowProgress.RainbowCompleted         += HandleRainbowCompleted;

            _scoreManager.ScoreChanged += HandleScoreChanged;
            _scoreManager.ComboChanged += HandleComboChanged;
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
                _scoreManager.ComboChanged -= HandleComboChanged;
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void HandleScoreChanged(int newScore)
        {
            OnScoreChanged?.Invoke(newScore);
        }

        private void HandleComboChanged(float newCombo)
        {
            OnComboChanged?.Invoke(newCombo);
        }

        private void HandleCorrectColourCollected(RainbowColour colour)
        {
            _scoreManager?.RegisterCorrectCollection();
            _sessionStats?.RecordCorrectCollection();
            OnCorrectGemCollected?.Invoke(colour);

            string marker = RainbowColourHelper.GetMarkerString(colour);
            Color col = RainbowColourHelper.GetColor(colour);
            PostFeedbackMessage($"COLLECTED {marker}! ({colour.ToString().ToUpper()})", col);
        }

        private void HandleIncorrectColourAttempted(RainbowColour colour)
        {
            _scoreManager?.RegisterWrongAttempt();
            _sessionStats?.RecordWrongAttempt();
            OnWrongGemAttempted?.Invoke(colour);

            PostFeedbackMessage("WRONG GEM! (COMBO -0.5)", new Color(1f, 0.4f, 0.4f, 1f));
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
        /// Updates RainbowProgress, ScoreManager and SessionStats, and raises events.
        /// </summary>
        public bool TryCollectGem(RainbowColour colour)
        {
            if (_rainbowProgress == null) return false;
            return _rainbowProgress.TryCollect(colour);
        }

        /// <summary>
        /// Banks current collected progress (e.g. at a Rainbow Rest).
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
            _scoreManager?.ResetCombo();
            _sessionStats?.Reset();
            _isTimerRunning = true;
            _isLevelCompleted = false;
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

            if (_levelRules != null && _scoreManager != null)
            {
                // Completion Health Bonus
                healthBonus = remainingHealth * _levelRules.CompletionHealthBonusPerPip;
                _scoreManager.AddPoints(healthBonus);

                // Time Bonus: 5 pts per second under par
                float parTime = _levelRules.ParTimeSeconds;
                if (elapsed < parTime)
                {
                    int secUnderPar = (int)(parTime - elapsed);
                    timeBonus = secUnderPar * _levelRules.TimeBonusPerSecondUnderPar;
                    _scoreManager.AddPoints(timeBonus);
                }
            }

            int finalScore = _scoreManager != null ? _scoreManager.Score : 0;
            int stars = _levelRules != null ? _levelRules.ComputeStarRating(finalScore) : 1;

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
                starRating = stars
            };

            OnLevelCompleted?.Invoke(data);
            return data;
        }
    }
}


