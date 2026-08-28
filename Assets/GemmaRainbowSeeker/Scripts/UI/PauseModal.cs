using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Modal overlay displayed when the game is paused:
    /// - Resume
    /// - Replay Tutorial (calls TutorialCoordinator.ReplayTutorial())
    /// - Level Select
    /// - Restart Level
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseModal : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private CanvasGroup modalCanvasGroup;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button replayTutorialButton;
        [SerializeField] private UnityEngine.UI.Button levelSelectButton;
        [SerializeField] private UnityEngine.UI.Button restartLevelButton;

        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ClosePauseMenu);
            if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(HandleReplayTutorial);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(HandleOpenLevelSelect);
            if (restartLevelButton != null) restartLevelButton.onClick.AddListener(HandleRestartLevel);

            HideModalImmediate();
        }

        public void OpenPauseMenu()
        {
            _isOpen = true;
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 1f;
                modalCanvasGroup.blocksRaycasts = true;
                modalCanvasGroup.interactable = true;
            }
            gameObject.SetActive(true);

            if (GameSession.Active != null)
            {
                GameSession.Active.IsPaused = true;
            }
            Time.timeScale = 0f;

            if (resumeButton != null)
            {
                resumeButton.Select();
            }
        }

        public void ClosePauseMenu()
        {
            _isOpen = false;
            HideModalImmediate();

            if (GameSession.Active != null)
            {
                GameSession.Active.IsPaused = false;
            }
            Time.timeScale = 1f;
        }

        private void HideModalImmediate()
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 0f;
                modalCanvasGroup.blocksRaycasts = false;
                modalCanvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        private void HandleReplayTutorial()
        {
            ClosePauseMenu();
            var coordinator = Object.FindFirstObjectByType<TutorialCoordinator>();
            if (coordinator != null)
            {
                coordinator.ReplayTutorial();
            }
        }

        private void HandleOpenLevelSelect()
        {
            ClosePauseMenu();
            var ui = UIManager.Instance;
            if (ui != null && ui.LevelSelect != null)
            {
                ui.LevelSelect.OpenLevelSelect();
            }
        }

        private void HandleRestartLevel()
        {
            ClosePauseMenu();
            var session = GameSession.Active;
            if (session != null)
            {
                session.ResetLevelProgress();
            }

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                var checkpointMgr = Object.FindFirstObjectByType<CheckpointManager>();
                if (checkpointMgr != null)
                {
                    checkpointMgr.RestartFromRainbowRest(gemma);
                }
            }
        }
    }
}
