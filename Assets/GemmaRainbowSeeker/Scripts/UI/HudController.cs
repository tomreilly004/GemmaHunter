using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Event-driven controller for the in-game HUD:
    /// - 7-slot Rainbow Meter along the bottom
    /// - Current required colour pulsing banner ("NEXT: ORANGE")
    /// - 3-Heart Health display
    /// - Score and energetic Combo multiplier
    /// - Elapsed time
    /// - Transient feedback messages
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        [Header("Rainbow Meter")]
        [SerializeField] private RainbowMeterSlot[] meterSlots;
        [SerializeField] private TextMeshProUGUI nextTargetText;

        [Header("Health Display")]
        [SerializeField] private TextMeshProUGUI[] heartIcons;
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color emptyHeartColor = new Color(0.35f, 0.38f, 0.45f, 0.5f);

        [Header("Score & Combo")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private RectTransform comboContainer;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Feedback Banner")]
        [SerializeField] private CanvasGroup feedbackCanvasGroup;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private UnityEngine.UI.Image feedbackBg;

        private Coroutine _feedbackRoutine;
        private Coroutine _comboEnergyRoutine;
        private GameSession _session;
        private PlayerHealth _playerHealth;
        private GemmaDash _playerDash;

        private void Start()
        {
            BindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        public void BindEvents()
        {
            UnbindEvents();

            _session = GameSession.Active;
            if (_session != null)
            {
                _session.OnScoreChanged          += UpdateScore;
                _session.OnComboChanged          += UpdateCombo;
                _session.OnTimeUpdated           += UpdateTimer;
                _session.OnCorrectGemCollected   += HandleCorrectGem;
                _session.OnWrongGemAttempted     += HandleWrongGem;
                _session.OnProgressBanked        += HandleProgressBanked;
                _session.OnProgressRestored      += HandleProgressRestored;
                _session.OnProgressReset         += HandleProgressReset;
                _session.OnFeedbackMessage       += ShowFeedbackMessage;

                if (_session.RainbowProgress != null)
                {
                    _session.RainbowProgress.ProgressChanged += RefreshRainbowMeter;
                    _session.RainbowProgress.TargetChanged   += RefreshRainbowMeter;
                }

                // Initial state
                if (_session.ScoreManager != null)
                {
                    UpdateScore(_session.ScoreManager.Score);
                    UpdateCombo(_session.ScoreManager.Combo);
                }
                if (_session.SessionStats != null)
                {
                    UpdateTimer(_session.SessionStats.ElapsedSeconds);
                }
            }

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                _playerHealth = gemma.GetComponent<PlayerHealth>();
                if (_playerHealth != null)
                {
                    _playerHealth.OnHealthChanged += UpdateHealth;
                    UpdateHealth(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                }

                _playerDash = gemma.GetComponent<GemmaDash>();
                if (_playerDash != null)
                {
                    _playerDash.OnDashRecharged += HandleDashRecharged;
                }
            }

            RefreshRainbowMeter();

            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
            }
        }

        public void UnbindEvents()
        {
            if (_session != null)
            {
                _session.OnScoreChanged          -= UpdateScore;
                _session.OnComboChanged          -= UpdateCombo;
                _session.OnTimeUpdated           -= UpdateTimer;
                _session.OnCorrectGemCollected   -= HandleCorrectGem;
                _session.OnWrongGemAttempted     -= HandleWrongGem;
                _session.OnProgressBanked        -= HandleProgressBanked;
                _session.OnProgressRestored      -= HandleProgressRestored;
                _session.OnProgressReset         -= HandleProgressReset;
                _session.OnFeedbackMessage       -= ShowFeedbackMessage;

                if (_session.RainbowProgress != null)
                {
                    _session.RainbowProgress.ProgressChanged -= RefreshRainbowMeter;
                    _session.RainbowProgress.TargetChanged   -= RefreshRainbowMeter;
                }
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= UpdateHealth;
            }

            if (_playerDash != null)
            {
                _playerDash.OnDashRecharged -= HandleDashRecharged;
            }
        }

        // ── Rainbow Meter Refresh ─────────────────────────────────────────────

        public void RefreshRainbowMeter()
        {
            if (meterSlots == null || meterSlots.Length == 0) return;

            var progress = _session != null ? _session.RainbowProgress : null;
            if (progress == null)
            {
                for (int i = 0; i < meterSlots.Length; i++)
                {
                    meterSlots[i]?.SetState(RainbowMeterSlot.SlotState.Empty);
                }
                if (nextTargetText != null) nextTargetText.text = "NEXT: RED";
                return;
            }

            int collected = progress.CollectedCount;
            int banked = progress.BankedCount;
            bool isComplete = progress.IsComplete;
            RainbowColour? target = progress.CurrentTarget;

            for (int i = 0; i < meterSlots.Length; i++)
            {
                var slot = meterSlots[i];
                if (slot == null) continue;

                if (i < banked)
                {
                    slot.SetState(RainbowMeterSlot.SlotState.Banked);
                }
                else if (i < collected)
                {
                    slot.SetState(RainbowMeterSlot.SlotState.Collected);
                }
                else if (i == collected && !isComplete)
                {
                    slot.SetState(RainbowMeterSlot.SlotState.NextRequired);
                }
                else
                {
                    slot.SetState(RainbowMeterSlot.SlotState.Empty);
                }
            }

            if (nextTargetText != null)
            {
                if (isComplete)
                {
                    nextTargetText.text = "<color=#FFE666>★ ALL COLOURS COLLECTED! ENTER GATE! ★</color>";
                }
                else if (target.HasValue)
                {
                    string name = target.Value.ToString().ToUpper();
                    string hex = RainbowColourHelper.GetHex(target.Value);
                    nextTargetText.text = $"NEXT: <color={hex}>{name}</color>";
                }
            }
        }

        // ── Health Display ────────────────────────────────────────────────────

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (heartIcons == null) return;

            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] == null) continue;

                if (i < currentHealth)
                {
                    heartIcons[i].text = "♥";
                    heartIcons[i].color = fullHeartColor;
                }
                else
                {
                    heartIcons[i].text = "♡";
                    heartIcons[i].color = emptyHeartColor;
                }
            }
        }

        // ── Score & Combo ─────────────────────────────────────────────────────

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:N0}";
            }
        }

        public void UpdateCombo(float combo)
        {
            if (comboText != null)
            {
                comboText.text = $"COMBO: x{combo:F2}";

                if (combo > 1.0f)
                {
                    // Vibrant yellow/gold energetic display
                    comboText.color = new Color(1.0f, 0.88f, 0.2f, 1f);
                    if (isActiveAndEnabled)
                    {
                        if (_comboEnergyRoutine != null) StopCoroutine(_comboEnergyRoutine);
                        _comboEnergyRoutine = StartCoroutine(ComboBounceRoutine(combo));
                    }
                }
                else
                {
                    comboText.color = new Color(0.75f, 0.8f, 0.9f, 0.8f);
                    if (comboContainer != null) comboContainer.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator ComboBounceRoutine(float combo)
        {
            if (comboContainer == null) yield break;

            float intensity = Mathf.Lerp(1.15f, 1.35f, (combo - 1f) / 1.5f);
            comboContainer.localScale = Vector3.one * intensity;

            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                comboContainer.localScale = Vector3.Lerp(Vector3.one * intensity, Vector3.one, elapsed / duration);
                yield return null;
            }
            comboContainer.localScale = Vector3.one;
            _comboEnergyRoutine = null;
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        public void UpdateTimer(float elapsedSeconds)
        {
            if (timerText != null)
            {
                int minutes = (int)(elapsedSeconds / 60f);
                int seconds = (int)(elapsedSeconds % 60f);
                int frac = (int)((elapsedSeconds * 10f) % 10f);
                timerText.text = $"TIME: {minutes:00}:{seconds:00}.{frac}";
            }
        }

        // ── Feedback Banner ───────────────────────────────────────────────────

        public void ShowFeedbackMessage(string message, Color color)
        {
            if (feedbackCanvasGroup == null || feedbackText == null) return;

            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
            }
            _feedbackRoutine = StartCoroutine(FeedbackRoutine(message, color));
        }

        private IEnumerator FeedbackRoutine(string message, Color color)
        {
            feedbackText.text = message;
            feedbackText.color = color;
            if (feedbackBg != null)
            {
                Color bgCol = color * 0.25f;
                bgCol.a = 0.85f;
                feedbackBg.color = bgCol;
            }

            // Quick Fade In
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                feedbackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.15f);
                yield return null;
            }
            feedbackCanvasGroup.alpha = 1f;

            // Hold on screen
            yield return new WaitForSeconds(1.1f);

            // Smooth Fade Out
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                feedbackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.35f);
                yield return null;
            }
            feedbackCanvasGroup.alpha = 0f;
            _feedbackRoutine = null;
        }

        private void HandleCorrectGem(RainbowColour col) => RefreshRainbowMeter();
        private void HandleWrongGem(RainbowColour col) => RefreshRainbowMeter();
        private void HandleProgressBanked() => RefreshRainbowMeter();
        private void HandleProgressRestored() => RefreshRainbowMeter();
        private void HandleProgressReset() => RefreshRainbowMeter();
        private void HandleDashRecharged() => ShowFeedbackMessage("DASH READY! [SPACE]", new Color(0.4f, 1.0f, 0.85f, 1f));
    }
}
