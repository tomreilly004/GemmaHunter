using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Coordinates all UI views in the scene:
    /// - In-game HUD (meter, score, combo, health, timer, feedback)
    /// - Tutorial Banner
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
        [SerializeField] private KnockoutModal knockoutModal;
        [SerializeField] private ResultsModal resultsModal;

        public HudController HUD => hudController;
        public TutorialBanner Tutorial => tutorialBanner;
        public KnockoutModal Knockout => knockoutModal;
        public ResultsModal Results => resultsModal;

        private void Awake()
        {
            Instance = this;

            if (hudController == null) hudController = GetComponentInChildren<HudController>(true);
            if (tutorialBanner == null) tutorialBanner = GetComponentInChildren<TutorialBanner>(true);
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
