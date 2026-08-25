using System;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Tracks the player's score and combo multiplier.
    /// Pure C# — no MonoBehaviour or Unity dependency.
    /// </summary>
    public sealed class ScoreManager
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly LevelRules _rules;
        private int   _score;
        private float _combo;

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised whenever the score changes. Carries the new score.</summary>
        public event Action<int> ScoreChanged;

        /// <summary>Raised whenever the combo multiplier changes. Carries the new combo value.</summary>
        public event Action<float> ComboChanged;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <param name="rules">Level rules providing combo and scoring parameters.</param>
        public ScoreManager(LevelRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _score = 0;
            _combo = _rules.ComboStart;
        }

        // ── Read-only Properties ──────────────────────────────────────────────

        /// <summary>Current score. Never negative.</summary>
        public int Score => _score;

        /// <summary>Current combo multiplier.</summary>
        public float Combo => _combo;

        // ── Scoring Operations ────────────────────────────────────────────────

        /// <summary>
        /// Awards points for a correct gem collection: base points × current combo (floored to int),
        /// then increases combo by the configured increment (clamped to ComboMax).
        /// </summary>
        public void RegisterCorrectCollection()
        {
            int points = (int)Math.Floor(_rules.CorrectGemBasePoints * _combo);
            AddPoints(points);

            float newCombo = Math.Min(_combo + _rules.ComboIncrement, _rules.ComboMax);
            SetCombo(newCombo);
        }

        /// <summary>
        /// Applies a wrong-attempt combo penalty (does NOT change the score).
        /// Combo is reduced by ComboWrongPenalty, clamped to ComboMin.
        /// </summary>
        public void RegisterWrongAttempt()
        {
            float newCombo = Math.Max(_combo - _rules.ComboWrongPenalty, _rules.ComboMin);
            SetCombo(newCombo);
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

        /// <summary>Resets combo multiplier to the configured starting value.</summary>
        public void ResetCombo()
        {
            SetCombo(_rules.ComboStart);
        }

        // ── Completion Bonus Helpers ──────────────────────────────────────────

        /// <summary>
        /// Awards the health completion bonus for the given remaining health pip count.
        /// </summary>
        public void AddCompletionHealthBonus(int remainingHealthPips)
        {
            if (remainingHealthPips <= 0) return;
            AddPoints(_rules.CompletionHealthBonusPerPip * remainingHealthPips);
        }

        /// <summary>
        /// Awards the time bonus based on elapsed time vs. par time.
        /// Only positive when elapsedSeconds &lt; par time.
        /// </summary>
        public void AddTimeBonusForElapsedTime(float elapsedSeconds)
        {
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

        private void SetCombo(float newCombo)
        {
            // Clamp within [min, max] as a safety net.
            newCombo = Math.Max(_rules.ComboMin, Math.Min(_rules.ComboMax, newCombo));
            if (Math.Abs(newCombo - _combo) < 0.0001f) return;
            _combo = newCombo;
            ComboChanged?.Invoke(_combo);
        }
    }
}
