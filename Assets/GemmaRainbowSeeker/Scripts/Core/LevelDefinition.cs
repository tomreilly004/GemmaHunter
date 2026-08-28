using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Data-driven definition for any level in Gemma Beaker: Rainbow Seeker.
    /// Supports dynamic color sequence lengths (1-10 gems), duplicate colors,
    /// per-level star thresholds, target times, Rainbow Rush windows, mechanics flags,
    /// and tutorial sequences.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelDefinition",
        menuName = "GemmaRainbowSeeker/Level Definition",
        order = 0)]
    public class LevelDefinition : ScriptableObject
    {
        // ── Level Metadata ──
        [Header("Level Information")]
        [Tooltip("The level number displayed in menus (1, 2, 3...).")]
        [SerializeField] private int _levelNumber = 1;

        [Tooltip("Display name for the level (e.g. 'Meadow Skies', 'Twilight Current').")]
        [SerializeField] private string _displayName = "Level 01";

        [Tooltip("Name of the scene asset to load for this level.")]
        [SerializeField] private string _sceneName = "Level01";

        [Tooltip("Authored objective text for this level (e.g. 'Collect 1 red gem', 'Collect Red, Red, Orange', etc.).")]
        [SerializeField] private string _objectiveDescription = "Collect 1 red gem";

        // ── Colour Sequence ──
        [Header("Colour Sequence")]
        [Tooltip("The ordered rainbow colours Gemma must collect. Supports repeated colours and lengths from 1 to 10.")]
        [SerializeField] private List<RainbowColour> _colourSequence = new List<RainbowColour>
        {
            RainbowColour.Red,
            RainbowColour.Orange,
            RainbowColour.Yellow,
            RainbowColour.Green,
            RainbowColour.Blue,
            RainbowColour.Indigo,
            RainbowColour.Violet
        };

        // ── Timing ──
        [Header("Timing")]
        [Tooltip("Target completion time (par time) in seconds.")]
        [SerializeField] private float _targetCompletionTime = 180f;

        [Tooltip("Rainbow Rush time window in seconds.")]
        [SerializeField] private float _rainbowRushTimeWindow = 30f;

        // ── Star Thresholds ──
        [Header("Star Rating Thresholds")]
        [Tooltip("Minimum score required for two stars (level completion always awards at least one star).")]
        [SerializeField] private int _twoStarThreshold = 1600;

        [Tooltip("Minimum score required for three stars.")]
        [SerializeField] private int _threeStarThreshold = 2200;

        // ── Available Mechanics Flags ──
        [Header("Mechanics Flags")]
        [Tooltip("Whether the dash ability is enabled for Gemma in this level.")]
        [SerializeField] private bool _dashEnabled = true;

        [Tooltip("Whether health and damage taking are enabled.")]
        [SerializeField] private bool _healthEnabled = true;

        [Tooltip("Whether magical current zones are enabled.")]
        [SerializeField] private bool _currentsEnabled = true;

        [Tooltip("Whether solid cloud obstacles are enabled.")]
        [SerializeField] private bool _solidObstaclesEnabled = true;

        [Tooltip("Whether dangerous storm hazards are enabled.")]
        [SerializeField] private bool _dangerousHazardsEnabled = true;

        [Tooltip("Whether enemy entities are enabled.")]
        [SerializeField] private bool _enemiesEnabled = false;

        [Tooltip("Whether Rainbow Rest checkpoints are enabled.")]
        [SerializeField] private bool _rainbowRestsEnabled = true;

        // ── Tutorials ──
        [Header("Tutorial Sequence")]
        [Tooltip("Optional data asset defining the tutorial steps for this level.")]
        [SerializeField] private TutorialSequence _tutorialSequence;

        // ── Scoring Values ──
        [Header("Scoring Values")]
        [Tooltip("Base score for collecting a correct gem (before combo multiplier).")]
        [SerializeField] private int _correctGemBasePoints = 100;

        [Tooltip("Score awarded for breaking a dash-breakable hazard.")]
        [SerializeField] private int _hazardBreakPoints = 50;

        [Tooltip("One-time bonus score for activating a Rainbow Rest.")]
        [SerializeField] private int _rainbowRestFirstActivationPoints = 100;

        [Tooltip("Bonus score awarded per remaining health pip at level completion.")]
        [SerializeField] private int _completionHealthBonusPerPip = 150;

        [Tooltip("Bonus score per whole second below par time on completion.")]
        [SerializeField] private int _timeBonusPerSecondUnderPar = 5;

        // ── Combo Rules ──
        [Header("Combo Rules")]
        [Tooltip("Initial combo multiplier.")]
        [SerializeField] private float _comboStart = 1.0f;

        [Tooltip("Multiplier increase per correct gem collection.")]
        [SerializeField] private float _comboIncrement = 0.25f;

        [Tooltip("Maximum combo multiplier achievable.")]
        [SerializeField] private float _comboMax = 2.5f;

        [Tooltip("Multiplier deduction on a wrong-colour gem attempt.")]
        [SerializeField] private float _comboWrongPenalty = 0.5f;

        [Tooltip("Minimum combo floor.")]
        [SerializeField] private float _comboMin = 1.0f;

        // ── Checkpoint Penalties ──
        [Header("Penalties")]
        [Tooltip("Score deducted on restart from a Rainbow Rest (floored at zero).")]
        [SerializeField] private int _restartFromRestPenalty = 200;

        // ── Read-only Public Properties ──
        public int LevelNumber => _levelNumber;
        public string DisplayName => _displayName;
        public string SceneName => _sceneName;
        public string ObjectiveDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(_objectiveDescription)) return _objectiveDescription;
                return GetDefaultObjectiveText();
            }
        }

        public string GetDefaultObjectiveText()
        {
            if (_colourSequence == null || _colourSequence.Count == 0)
                return "Collect gems to unlock gate";
            if (_colourSequence.Count == 1)
                return $"Collect 1 {_colourSequence[0].ToString().ToLower()} gem";
            if (_colourSequence.Count == 7 && _colourSequence[0] == RainbowColour.Red && _colourSequence[6] == RainbowColour.Violet)
                return "Collect all 7 rainbow colours in order";

            return "Collect " + string.Join(", ", _colourSequence);
        }

        public RainbowColour[] ColourSequence => _colourSequence != null ? _colourSequence.ToArray() : Array.Empty<RainbowColour>();
        public IReadOnlyList<RainbowColour> ColourSequenceList => _colourSequence;
        public int RequiredGemCount => _colourSequence != null ? _colourSequence.Count : 0;

        public float TargetCompletionTime => _targetCompletionTime;
        public float ParTimeSeconds => _targetCompletionTime; // Compatibility alias
        public float RainbowRushTimeWindow => _rainbowRushTimeWindow;

        public int TwoStarThreshold => _twoStarThreshold;
        public int ThreeStarThreshold => _threeStarThreshold;

        public bool DashEnabled => _dashEnabled;
        public bool HealthEnabled => _healthEnabled;
        public bool CurrentsEnabled => _currentsEnabled;
        public bool SolidObstaclesEnabled => _solidObstaclesEnabled;
        public bool DangerousHazardsEnabled => _dangerousHazardsEnabled;
        public bool EnemiesEnabled => _enemiesEnabled;
        public bool RainbowRestsEnabled => _rainbowRestsEnabled;

        public TutorialSequence TutorialSequence => _tutorialSequence;

        public int CorrectGemBasePoints => _correctGemBasePoints;
        public int HazardBreakPoints => _hazardBreakPoints;
        public int RainbowRestFirstActivationPoints => _rainbowRestFirstActivationPoints;
        public int CompletionHealthBonusPerPip => _completionHealthBonusPerPip;
        public int TimeBonusPerSecondUnderPar => _timeBonusPerSecondUnderPar;

        public float ComboStart => _comboStart;
        public float ComboIncrement => _comboIncrement;
        public float ComboMax => _comboMax;
        public float ComboWrongPenalty => _comboWrongPenalty;
        public float ComboMin => _comboMin;

        public int RestartFromRestPenalty => _restartFromRestPenalty;

        // ── Star Rating Computation ──
        /// <summary>
        /// Computes star rating for a final score. Completing a level always awards at least 1 star.
        /// </summary>
        public int ComputeStarRating(int finalScore)
        {
            if (finalScore >= _threeStarThreshold) return 3;
            if (finalScore >= _twoStarThreshold)   return 2;
            return 1;
        }

        // ── Runtime / Test Helper ──
        public static LevelDefinition CreateRuntimeInstance(
            int levelNumber = 1,
            IEnumerable<RainbowColour> sequence = null,
            string objective = null,
            float parTime = 180f,
            int twoStar = 1600,
            int threeStar = 2200,
            bool dash = true,
            bool health = true,
            bool rests = true)
        {
            var def = CreateInstance<LevelDefinition>();
            def._levelNumber = levelNumber;
            def._displayName = $"Level {levelNumber:D2}";
            def._sceneName = $"Level{levelNumber:D2}";
            def._targetCompletionTime = parTime;
            def._twoStarThreshold = twoStar;
            def._threeStarThreshold = threeStar;
            def._dashEnabled = dash;
            def._healthEnabled = health;
            def._rainbowRestsEnabled = rests;
            def._objectiveDescription = objective ?? "";

            if (sequence != null)
            {
                def._colourSequence = new List<RainbowColour>(sequence);
            }
            return def;
        }
    }
}
