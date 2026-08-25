using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Coordinates event-driven tutorials throughout Level 1:
    /// - Movement & goal (at start)
    /// - First correct gem collection
    /// - First wrong attempt
    /// - First hazard damage
    /// - Dash ability & breakable obstacles
    /// - Rainbow Rest banking
    /// - Final colour (Violet)
    /// - Rainbow Gate unlocking
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialCoordinator : MonoBehaviour
    {
        private bool _shownFirstCorrect;
        private bool _shownFirstWrong;
        private bool _shownFirstHazard;
        private bool _shownFirstDash;
        private bool _shownFirstBank;
        private bool _shownFinalColour;
        private bool _shownGateUnlocked;

        private TutorialBanner _banner;
        private PlayerHealth _playerHealth;
        private GemmaDash _playerDash;

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

        private void ShowBanner(string title, string body, string hint = "", float duration = 5.0f)
        {
            if (_banner == null)
            {
                _banner = Object.FindFirstObjectByType<TutorialBanner>();
            }

            if (_banner != null)
            {
                _banner.QueueMessage(new TutorialBanner.TutorialMessage(title, body, hint, duration));
            }
        }

        private void HandleCorrectGem(RainbowColour colour)
        {
            if (!_shownFirstCorrect)
            {
                _shownFirstCorrect = true;
                ShowBanner(
                    "FIRST COLOUR COLLECTED!",
                    "You collected RED! The rainbow meter shows your next required colour (ORANGE).\nCollecting gems in sequence builds your score and combo multiplier!",
                    "Look at the meter at the bottom for the NEXT colour."
                );
            }

            if (colour == RainbowColour.Indigo && !_shownFinalColour)
            {
                _shownFinalColour = true;
                ShowBanner(
                    "FINAL COLOUR: VIOLET!",
                    "Only one colour remains! Collect the VIOLET gem ahead to unlock the Rainbow Gate.",
                    "Look ahead for Violet [V]."
                );
            }
        }

        private void HandleWrongGem(RainbowColour colour)
        {
            if (!_shownFirstWrong)
            {
                _shownFirstWrong = true;
                ShowBanner(
                    "WRONG GEM OUT OF ORDER",
                    "Touching a gem out of rainbow order reduces your combo multiplier by 0.5.\nYou do NOT lose health! Check the Rainbow Meter for the pulsing required colour.",
                    "Always collect in rainbow sequence: R -> O -> Y -> G -> B -> I -> V"
                );
            }
        }

        private void HandlePlayerDamaged(int amount, Vector2 dir)
        {
            if (!_shownFirstHazard)
            {
                _shownFirstHazard = true;
                ShowBanner(
                    "HAZARD DAMAGE",
                    "Storm clouds deal 1 health damage and knock you back.\nYou gain 1.1s of flashing invulnerability. Avoid dark storm clouds!",
                    "Dashing grants temporary immunity to hazards!"
                );
            }
        }

        private void HandleDashStarted(Vector2 direction)
        {
            if (!_shownFirstDash)
            {
                _shownFirstDash = true;
                ShowBanner(
                    "DASH ABILITY",
                    "Dashing grants temporary invulnerability against hazards and can break cracked purple clouds for +50 bonus points!",
                    "DASH: [Space] or [South Button] (0.85s cooldown)"
                );
            }
        }

        private void HandleProgressBanked()
        {
            if (!_shownFirstBank)
            {
                _shownFirstBank = true;
                ShowBanner(
                    "RAINBOW REST ACTIVATED",
                    "Your collected colours are permanently banked! (Look for 🔒 on the meter).\nIf knocked out, restarting from a Rest restores full health and preserves banked progress.",
                    "Rainbow Rests also heal 1 health and award 100 bonus points on first activation."
                );
            }
        }

        private void HandleRainbowCompleted()
        {
            if (!_shownGateUnlocked)
            {
                _shownGateUnlocked = true;
                ShowBanner(
                    "RAINBOW GATE UNLOCKED!",
                    "All 7 colours collected! Swim to the Rainbow Gate at the end of the course to bank final progress and complete the level!",
                    "Swim through the radiant Rainbow Gate to view your final score and star rating!"
                );
            }
        }
    }
}
