using System;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Tracks the player's score and applies completion bonuses.
    /// Pure C# — no MonoBehaviour or Unity dependency.
    /// </summary>
    public sealed class ScoreManager
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly LevelDefinition _rules;
        private int _score;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the score changes. Carries the new score.</summary>
        public event Action<int> ScoreChanged;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <param name="rules">Level definition providing scoring parameters.</param>
        public ScoreManager(LevelDefinition rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _score = 0;
        }

        // ── Read-only Properties ──────────────────────────────────────────────

        /// <summary>Current score. Never negative.</summary>
        public int Score => _score;

        // ── Scoring Operations ────────────────────────────────────────────────

        /// <summary>
        /// Awards points for a correct gem collection using the given Rush multiplier:
        /// points = base points × multiplier.
        /// </summary>
        public void RegisterCorrectCollection(int multiplier = 1)
        {
            int basePoints = _rules != null ? _rules.CorrectGemBasePoints : 100;
            int points = basePoints * Math.Max(1, multiplier);
            AddPoints(points);
        }

        /// <summary>
        /// Adds a flat number of points. Clamped so score cannot go below zero.
        /// </summary>
        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            SetScore(_score + amount);
        }

        /// <summary>
        /// Subtracts a flat number of points. Score is clamped to a minimum of 0.
        /// </summary>
        public void SubtractPoints(int amount)
        {
            if (amount <= 0) return;
            SetScore(Math.Max(0, _score - amount));
        }

        // ── Completion Bonus Helpers ──────────────────────────────────────────

        /// <summary>
        /// Awards the health completion bonus for the given remaining health pip count.
        /// </summary>
        public void AddCompletionHealthBonus(int remainingHealthPips)
        {
            if (remainingHealthPips <= 0 || _rules == null) return;
            AddPoints(_rules.CompletionHealthBonusPerPip * remainingHealthPips);
        }

        /// <summary>
        /// Awards the time bonus based on elapsed time vs. par time.
        /// Only positive when elapsedSeconds &lt; par time.
        /// </summary>
        public void AddTimeBonusForElapsedTime(float elapsedSeconds)
        {
            if (_rules == null) return;
            float parTime = _rules.ParTimeSeconds;
            if (elapsedSeconds >= parTime) return;

            int wholeSecondsUnderPar = (int)(parTime - elapsedSeconds);
            AddPoints(wholeSecondsUnderPar * _rules.TimeBonusPerSecondUnderPar);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        private void SetScore(int newScore)
        {
            newScore = Math.Max(0, newScore);
            if (newScore == _score) return;
            _score = newScore;
            ScoreChanged?.Invoke(_score);
        }
    }
}

