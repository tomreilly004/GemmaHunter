using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Coordinates data-driven per-level tutorials:
    /// - Evaluates the TutorialSequence from LevelDefinition
    /// - Respects "Show Once" persistence via SaveManager
    /// - Triggers steps on gameplay events (Start, Gem, Wrong, Hazard, Dash, Bank, RainbowComplete)
    /// - Supports highlighting UI/world targets and optional gameplay pausing (suspending Rainbow Rush)
    /// - Supports manual tutorial replay from the pause menu
    /// - Never replays automatically on respawn
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialCoordinator : MonoBehaviour
    {
        private TutorialBanner _banner;
        private PlayerHealth _playerHealth;
        private GemmaDash _playerDash;
        private readonly HashSet<string> _completedStepIds = new HashSet<string>();
        private bool _forceReplay = false;

        private void Start()
        {
            _banner = Object.FindFirstObjectByType<TutorialBanner>();

            var session = GameSession.Active;
            if (session != null)
            {
                session.OnCorrectGemCollected += HandleCorrectGem;
                session.OnWrongGemAttempted   += HandleWrongGem;
                session.OnProgressBanked      += HandleProgressBanked;
                session.OnRainbowCompleted    += HandleRainbowCompleted;
                session.OnRushReset           += HandleRushReset;
            }

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                _playerHealth = gemma.GetComponent<PlayerHealth>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnDamaged += HandlePlayerDamaged;
                }

                _playerDash = gemma.GetComponent<GemmaDash>();
                if (_playerDash != null)
                {
                    _playerDash.OnDashStarted += HandleDashStarted;
                }
            }

            // Trigger OnLevelStart tutorial step if appropriate
            CheckTrigger(TutorialTriggerEvent.OnLevelStart);
        }

        private void OnDestroy()
        {
            var session = GameSession.Active;
            if (session != null)
            {
                session.OnCorrectGemCollected -= HandleCorrectGem;
                session.OnWrongGemAttempted   -= HandleWrongGem;
                session.OnProgressBanked      -= HandleProgressBanked;
                session.OnRainbowCompleted    -= HandleRainbowCompleted;
                session.OnRushReset           -= HandleRushReset;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnDamaged -= HandlePlayerDamaged;
            }

            if (_playerDash != null)
            {
                _playerDash.OnDashStarted -= HandleDashStarted;
            }
        }

        /// <summary>
        /// Manually replays the tutorial sequence for the current level (from the Pause menu).
        /// </summary>
        public void ReplayTutorial()
        {
            _forceReplay = true;
            _completedStepIds.Clear();
            CheckTrigger(TutorialTriggerEvent.OnLevelStart);
        }

        private void CheckTrigger(TutorialTriggerEvent trigger)
        {
            var session = GameSession.Active;
            if (session == null || session.LevelDefinition == null) return;

            var levelDef = session.LevelDefinition;
            int levelNum = levelDef.LevelNumber;

            // If not forcing replay and tutorial already viewed for this level, skip show-once steps
            bool alreadyViewed = SaveManager.HasTutorialBeenViewed(levelNum);
            if (alreadyViewed && !_forceReplay)
            {
                return;
            }

            var sequence = levelDef.TutorialSequence;
            if (sequence != null && sequence.Steps != null && sequence.Steps.Count > 0)
            {
                foreach (var step in sequence.Steps)
                {
                    if (step.triggerEvent == trigger && !_completedStepIds.Contains(step.stepId))
                    {
                        ExecuteStep(step, levelNum);
                    }
                }
            }
            else
            {
                // Fallback default tutorials if no custom sequence asset is assigned
                ExecuteFallback(trigger, levelNum);
            }
        }

        private void ExecuteStep(TutorialSequence.TutorialStep step, int levelNumber)
        {
            _completedStepIds.Add(step.stepId);

            if (_banner == null)
            {
                _banner = Object.FindFirstObjectByType<TutorialBanner>();
            }

            if (_banner != null)
            {
                _banner.QueueMessage(new TutorialBanner.TutorialMessage(
                    step.title,
                    step.body,
                    step.controlsHint,
                    step.highlightTargetName,
                    step.pauseGameplay,
                    step.displayDuration,
                    () =>
                    {
                        SaveManager.MarkTutorialViewed(levelNumber);
                    }
                ));
            }
        }

        private void ExecuteFallback(TutorialTriggerEvent trigger, int levelNumber)
        {
            string id = $"fallback_{trigger}";
            if (_completedStepIds.Contains(id)) return;
            _completedStepIds.Add(id);

            switch (trigger)
            {
                case TutorialTriggerEvent.OnLevelStart:
                    ShowBanner("HOW TO PLAY", "Swim through the sky to collect the rainbow gems in exact order.\nAvoid dark storm clouds!", "MOVE: Joystick / WASD / D-Pad", 5.0f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnFirstCorrectGem:
                    ShowBanner("RAINBOW RUSH ACTIVATED!", "Collecting gems in sequence triggers RAINBOW RUSH to multiply your score and increase swim speed!", "Keep moving to sustain your multiplier!", 4.5f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnFirstWrongGem:
                    ShowBanner("WRONG GEM", "Touching a gem out of rainbow order resets your Rainbow Rush to x1.\nYou do NOT lose health. Follow the Rainbow Meter!", "Collect in sequence: R -> O -> Y -> G -> B -> I -> V", 4.5f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnFirstHazard:
                    ShowBanner("HAZARD DAMAGE", "Storm clouds deal 1 damage and knock you back.\nYou gain 1.1s of flashing invulnerability. Avoid storm hazards!", "", 4.0f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnFirstDash:
                    ShowBanner("DASH ABILITY", "Dashing gives temporary invulnerability against hazards and breaks cracked purple clouds for +50 bonus points!", "DASH: Dash Button / Space / South Button", 4.5f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnFirstBank:
                    ShowBanner("PROGRESS BANKED", "Rainbow Rests bank your gems permanently. If knocked out, restarting from a Rest keeps your banked colours!", "", 4.5f, levelNumber);
                    break;
                case TutorialTriggerEvent.OnRainbowComplete:
                    ShowBanner("RAINBOW COMPLETE!", "All gems collected! Swim into the radiant Rainbow Gate to finish the level!", "Enter the Rainbow Gate ahead!", 5.0f, levelNumber);
                    break;
            }
        }

        private void ShowBanner(string title, string body, string hint, float duration, int levelNumber)
        {
            if (_banner == null)
            {
                _banner = Object.FindFirstObjectByType<TutorialBanner>();
            }

            if (_banner != null)
            {
                _banner.QueueMessage(new TutorialBanner.TutorialMessage(title, body, hint, duration));
                SaveManager.MarkTutorialViewed(levelNumber);
            }
        }

        private void HandleCorrectGem(RainbowColour colour) => CheckTrigger(TutorialTriggerEvent.OnFirstCorrectGem);
        private void HandleWrongGem(RainbowColour colour) => CheckTrigger(TutorialTriggerEvent.OnFirstWrongGem);
        private void HandlePlayerDamaged(int amount, Vector2 dir) => CheckTrigger(TutorialTriggerEvent.OnFirstHazard);
        private void HandleDashStarted(Vector2 dir) => CheckTrigger(TutorialTriggerEvent.OnFirstDash);
        private void HandleProgressBanked() => CheckTrigger(TutorialTriggerEvent.OnFirstBank);
        private void HandleRainbowCompleted() => CheckTrigger(TutorialTriggerEvent.OnRainbowComplete);
        private void HandleRushReset(RushResetReason reason) => CheckTrigger(TutorialTriggerEvent.OnRushBroken);
    }
}

