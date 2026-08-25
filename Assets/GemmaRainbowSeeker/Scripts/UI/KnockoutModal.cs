using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Modal overlay displayed upon Gemma reaching 0 health.
    /// Provides "Restart from Rainbow Rest" (disabled if no Rest banked) and "Restart Level" options.
    /// Supports gamepad, keyboard, and mouse navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KnockoutModal : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup modalCanvasGroup;
        [SerializeField] private UnityEngine.UI.Button restartFromRestButton;
        [SerializeField] private UnityEngine.UI.Button restartLevelButton;

        private PlayerHealth _playerHealth;

        private void Awake()
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 0f;
                modalCanvasGroup.blocksRaycasts = false;
                modalCanvasGroup.interactable = false;
            }

            if (restartFromRestButton != null)
            {
                restartFromRestButton.onClick.AddListener(OnRestartFromRestClicked);
            }

            if (restartLevelButton != null)
            {
                restartLevelButton.onClick.AddListener(OnRestartLevelClicked);
            }
        }

        private void Start()
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                _playerHealth = gemma.GetComponent<PlayerHealth>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnKnockedOut += Show;
                }
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnKnockedOut -= Show;
            }
        }

        public void Show()
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 1f;
                modalCanvasGroup.blocksRaycasts = true;
                modalCanvasGroup.interactable = true;
            }

            // Check if any Rainbow Rest has been activated / banked
            bool hasRest = (CheckpointManager.Instance != null && CheckpointManager.Instance.ActiveRest != null) ||
                           (GameSession.Active != null && GameSession.Active.RainbowProgress != null && GameSession.Active.RainbowProgress.BankedCount > 0);

            if (restartFromRestButton != null)
            {
                restartFromRestButton.interactable = hasRest;
            }

            // Set default focused selection for gamepad/keyboard navigation
            if (EventSystem.current != null)
            {
                var selectTarget = (hasRest && restartFromRestButton != null) ? restartFromRestButton.gameObject : restartLevelButton.gameObject;
                EventSystem.current.SetSelectedGameObject(selectTarget);
            }
        }

        public void Hide()
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 0f;
                modalCanvasGroup.blocksRaycasts = false;
                modalCanvasGroup.interactable = false;
            }
        }

        private void OnRestartFromRestClicked()
        {
            Hide();
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RestartFromRainbowRest();
            }
        }

        private void OnRestartLevelClicked()
        {
            Hide();
            // Reload current active scene or reset session
            if (GameSession.Active != null)
            {
                GameSession.Active.ResetLevelProgress();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
