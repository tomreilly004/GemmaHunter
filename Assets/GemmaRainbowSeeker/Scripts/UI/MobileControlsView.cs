using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Manages mobile touch on-screen controls:
    /// - Virtual joystick in the lower-left for swimming movement
    /// - Large Dash button in the lower-right (hidden until Level 8 introduces it)
    /// - Top-bar Pause button
    /// - Automatically configures visibility based on LevelDefinition.DashEnabled
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileControlsView : MonoBehaviour
    {
        [Header("Virtual Joystick (Lower Left)")]
        [SerializeField] private GameObject joystickRoot;
        [SerializeField] private OnScreenStick onScreenStick;

        [Header("Dash Button (Lower Right)")]
        [SerializeField] private GameObject dashButtonRoot;
        [SerializeField] private OnScreenButton onScreenDashButton;
        [SerializeField] private UnityEngine.UI.Image dashCooldownOverlay;

        [Header("Pause Button (Top Bar)")]
        [SerializeField] private GameObject pauseButtonRoot;
        [SerializeField] private UnityEngine.UI.Button pauseButton;

        private GemmaDash _playerDash;

        public GameObject JoystickRoot => joystickRoot;
        public GameObject DashButtonRoot => dashButtonRoot;
        public GameObject PauseButtonRoot => pauseButtonRoot;

        private void Awake()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(HandlePauseClicked);
            }
        }

        private void Start()
        {
            RefreshControlsVisibility();

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                _playerDash = gemma.GetComponent<GemmaDash>();
            }
        }

        private void Update()
        {
            if (dashCooldownOverlay != null && _playerDash != null)
            {
                dashCooldownOverlay.fillAmount = _playerDash.CooldownNormalized;
            }
        }

        public void RefreshControlsVisibility()
        {
            var session = GameSession.Active;
            bool dashEnabled = true;

            if (session != null && session.LevelDefinition != null)
            {
                dashEnabled = session.LevelDefinition.DashEnabled;
            }

            if (dashButtonRoot != null)
            {
                dashButtonRoot.SetActive(dashEnabled);
            }
        }

        private void HandlePauseClicked()
        {
            var ui = UIManager.Instance;
            if (ui != null && ui.Pause != null)
            {
                ui.Pause.OpenPauseMenu();
            }
            else
            {
                var session = GameSession.Active;
                if (session != null)
                {
                    session.IsPaused = !session.IsPaused;
                    Time.timeScale = session.IsPaused ? 0f : 1f;
                }
            }
        }
    }
}
