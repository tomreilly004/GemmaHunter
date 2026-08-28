using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Coordinates all UI views in the scene:
    /// - In-game HUD (meter, score, rush multiplier, health, timer, feedback)
    /// - Tutorial Banner
    /// - Mobile Touch Controls (virtual joystick & dash button)
    /// - Pause Modal
    /// - Level Select Modal
    /// - Knockout Modal
    /// - Results Modal
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Views")]
        [SerializeField] private HudController hudController;
        [SerializeField] private TutorialBanner tutorialBanner;
        [SerializeField] private MobileControlsView mobileControls;
        [SerializeField] private PauseModal pauseModal;
        [SerializeField] private LevelSelectModal levelSelectModal;
        [SerializeField] private KnockoutModal knockoutModal;
        [SerializeField] private ResultsModal resultsModal;

        public HudController HUD => hudController;
        public TutorialBanner Tutorial => tutorialBanner;
        public MobileControlsView MobileControls => mobileControls;
        public PauseModal Pause => pauseModal;
        public LevelSelectModal LevelSelect => levelSelectModal;
        public KnockoutModal Knockout => knockoutModal;
        public ResultsModal Results => resultsModal;

        private void Awake()
        {
            Instance = this;

            if (hudController == null) hudController = GetComponentInChildren<HudController>(true);
            if (tutorialBanner == null) tutorialBanner = GetComponentInChildren<TutorialBanner>(true);
            if (mobileControls == null) mobileControls = GetComponentInChildren<MobileControlsView>(true);
            if (pauseModal == null) pauseModal = GetComponentInChildren<PauseModal>(true);
            if (levelSelectModal == null) levelSelectModal = GetComponentInChildren<LevelSelectModal>(true);
            if (knockoutModal == null) knockoutModal = GetComponentInChildren<KnockoutModal>(true);
            if (resultsModal == null) resultsModal = GetComponentInChildren<ResultsModal>(true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

