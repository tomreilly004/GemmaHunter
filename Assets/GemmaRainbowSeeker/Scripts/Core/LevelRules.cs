using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// All tunable rules for a single level: colour sequence, scoring values,
    /// combo parameters, and star-rating thresholds. Create one asset per level.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelRules",
        menuName = "GemmaRainbowSeeker/Level Rules",
        order = 0)]
    public sealed class LevelRules : ScriptableObject
    {
        // ── Sequence ─────────────────────────────────────────────────────────
        [Header("Colour Sequence")]
        [Tooltip("The ordered rainbow colours Gemma must collect, from first to last.")]
        [SerializeField] private RainbowColour[] _colourSequence = new[]
        {
            RainbowColour.Red,
            RainbowColour.Orange,
            RainbowColour.Yellow,
            RainbowColour.Green,
            RainbowColour.Blue,
            RainbowColour.Indigo,
            RainbowColour.Violet
        };

        // ── Timing ────────────────────────────────────────────────────────────
        [Header("Timing")]
        [Tooltip("Par time in seconds. Finishing faster earns a time bonus.")]
        [Min(1f)]
        [SerializeField] private float _parTimeSeconds = 180f;

        // ── Base Scoring ──────────────────────────────────────────────────────
        [Header("Base Scoring")]
        [Tooltip("Points awarded for collecting a correct gem (before combo multiplier).")]
        [Min(0)]
        [SerializeField] private int _correctGemBasePoints = 100;

        [Tooltip("Points awarded for dash-breaking a breakable hazard.")]
        [Min(0)]
        [SerializeField] private int _hazardBreakPoints = 50;

        [Tooltip("Points awarded the first time a Rainbow Rest is activated.")]
        [Min(0)]
        [SerializeField] private int _rainbowRestFirstActivationPoints = 100;

        [Tooltip("Bonus points per remaining health pip on level completion.")]
        [Min(0)]
        [SerializeField] private int _completionHealthBonusPerPip = 150;

        [Tooltip("Bonus points per whole second under par time on level completion.")]
        [Min(0)]
        [SerializeField] private int _timeBonusPerSecondUnderPar = 5;

        // ── Combo ─────────────────────────────────────────────────────────────
        [Header("Combo Multiplier")]
        [Tooltip("Starting combo multiplier at the beginning of a run.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _comboStart = 1.0f;

        [Tooltip("How much the combo multiplier increases per correct collection.")]
        [Range(0.01f, 2f)]
        [SerializeField] private float _comboIncrement = 0.25f;

        [Tooltip("Maximum combo multiplier cap.")]
        [Range(1f, 10f)]
        [SerializeField] private float _comboMax = 2.5f;

        [Tooltip("How much the combo multiplier decreases after a wrong attempt.")]
        [Range(0f, 5f)]
        [SerializeField] private float _comboWrongPenalty = 0.5f;

        [Tooltip("Minimum combo multiplier floor (never drops below this).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _comboMin = 1.0f;

        // ── Penalties ─────────────────────────────────────────────────────────
        [Header("Penalties")]
        [Tooltip("Score deducted when restarting from a Rainbow Rest checkpoint.")]
        [Min(0)]
        [SerializeField] private int _restartFromRestPenalty = 200;

        // ── Star Thresholds ───────────────────────────────────────────────────
        [Header("Star Rating Thresholds")]
        [Tooltip("Minimum score required for two stars (completion always earns one star).")]
        [Min(0)]
        [SerializeField] private int _twoStarThreshold = 1600;

        [Tooltip("Minimum score required for three stars.")]
        [Min(0)]
        [SerializeField] private int _threeStarThreshold = 2200;

        // ── Public read-only accessors ────────────────────────────────────────

        /// <summary>A copy of the ordered colour sequence for this level.</summary>
        public RainbowColour[] ColourSequence
        {
            get
            {
                if (_colourSequence == null || _colourSequence.Length == 0)
                {
                    _colourSequence = new[]
                    {
                        RainbowColour.Red,
                        RainbowColour.Orange,
                        RainbowColour.Yellow,
                        RainbowColour.Green,
                        RainbowColour.Blue,
                        RainbowColour.Indigo,
                        RainbowColour.Violet
                    };
                }

                // Return a copy to prevent external mutation.
                var copy = new RainbowColour[_colourSequence.Length];
                _colourSequence.CopyTo(copy, 0);
                return copy;
            }
        }

        public float ParTimeSeconds              => _parTimeSeconds;
        public int   CorrectGemBasePoints        => _correctGemBasePoints;
        public int   HazardBreakPoints           => _hazardBreakPoints;
        public int   RainbowRestFirstActivationPoints => _rainbowRestFirstActivationPoints;
        public int   CompletionHealthBonusPerPip => _completionHealthBonusPerPip;
        public int   TimeBonusPerSecondUnderPar  => _timeBonusPerSecondUnderPar;

        public float ComboStart        => _comboStart;
        public float ComboIncrement    => _comboIncrement;
        public float ComboMax          => _comboMax;
        public float ComboWrongPenalty => _comboWrongPenalty;
        public float ComboMin          => _comboMin;

        public int RestartFromRestPenalty => _restartFromRestPenalty;

        public int TwoStarThreshold   => _twoStarThreshold;
        public int ThreeStarThreshold => _threeStarThreshold;

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the star rating (1, 2, or 3) for a final score.
        /// Assumes the level was completed (completion is the gate for 1 star).
        /// </summary>
        public int ComputeStarRating(int finalScore)
        {
            if (finalScore >= _threeStarThreshold) return 3;
            if (finalScore >= _twoStarThreshold)   return 2;
            return 1;
        }
    }
}
