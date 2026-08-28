using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Event-driven controller for the in-game HUD:
    /// - Dynamic Sequence Rainbow Meter along the bottom
    /// - Current required colour pulsing banner ("NEXT: ORANGE")
    /// - 3-Heart Health display
    /// - Score and Large x1–x5 Rainbow Rush Multiplier with countdown meter
    /// - Elapsed time
    /// - Transient feedback messages and "RUSH LOST" alerts
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudController : MonoBehaviour
    {
        [Header("Rainbow Meter")]
        [SerializeField] private RectTransform slotsContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private RainbowMeterSlot[] meterSlots;
        [SerializeField] private TextMeshProUGUI nextTargetText;
        [SerializeField] private TextMeshProUGUI objectiveText;

        public RectTransform SlotsContainer
        {
            get => slotsContainer;
            set => slotsContainer = value;
        }

        public GameObject SlotPrefab
        {
            get => slotPrefab;
            set => slotPrefab = value;
        }

        public TextMeshProUGUI ObjectiveText
        {
            get => objectiveText;
            set => objectiveText = value;
        }

        private readonly List<RainbowMeterSlot> _dynamicSlots = new List<RainbowMeterSlot>();
        private GameObject _cachedSlotTemplate;

        [Header("Health Display")]
        [SerializeField] private TextMeshProUGUI[] heartIcons;
        [SerializeField] private Color fullHeartColor = new Color(1f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color emptyHeartColor = new Color(0.35f, 0.38f, 0.45f, 0.5f);

        [Header("Score & Rainbow Rush Multiplier")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private RectTransform multiplierContainer;
        [SerializeField] private Image rushTimerMeter;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Feedback Banner")]
        [SerializeField] private CanvasGroup feedbackCanvasGroup;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private UnityEngine.UI.Image feedbackBg;

        private Coroutine _feedbackRoutine;
        private Coroutine _multiplierEnergyRoutine;
        private Coroutine _multiplierShakeRoutine;
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
                _session.OnMultiplierChanged     += UpdateMultiplier;
                _session.OnRushTimerUpdated      += UpdateRushTimer;
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
                }
                if (_session.RushController != null)
                {
                    UpdateMultiplier(_session.RushController.Multiplier);
                    UpdateRushTimer(_session.RushController.RemainingTime, _session.RushController.RushWindow);
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
            RefreshHudVisibility();

            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
            }
        }

        public void RefreshHudVisibility()
        {
            var session = GameSession.Active;
            var levelDef = session != null ? session.LevelDefinition : null;

            // 1. Health Display: only visible if HealthEnabled is true
            bool healthEnabled = levelDef != null ? levelDef.HealthEnabled : true;
            if (heartIcons != null && heartIcons.Length > 0 && heartIcons[0] != null)
            {
                var healthContainer = heartIcons[0].transform.parent?.gameObject;
                if (healthContainer != null && healthContainer.name.Contains("Health"))
                {
                    healthContainer.SetActive(healthEnabled);
                }
                else
                {
                    foreach (var h in heartIcons)
                    {
                        if (h != null) h.gameObject.SetActive(healthEnabled);
                    }
                }
            }

            // 2. Rainbow Rush Display: hidden until introduced (Level >= 2 and RainbowRushTimeWindow > 0)
            bool rushEnabled = levelDef != null && levelDef.LevelNumber >= 2 && levelDef.RainbowRushTimeWindow > 0f;
            if (multiplierContainer != null)
            {
                multiplierContainer.gameObject.SetActive(rushEnabled);
            }
            else if (multiplierText != null)
            {
                multiplierText.gameObject.SetActive(rushEnabled);
            }

            if (rushTimerMeter != null)
            {
                rushTimerMeter.gameObject.SetActive(rushEnabled);
            }
        }

        public void UnbindEvents()
        {
            if (_session != null)
            {
                _session.OnScoreChanged          -= UpdateScore;
                _session.OnMultiplierChanged     -= UpdateMultiplier;
                _session.OnRushTimerUpdated      -= UpdateRushTimer;
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

        public void BuildSequenceMeter(RainbowColour[] sequence)
        {
            if (sequence == null || sequence.Length == 0) return;

            // Resolve slotsContainer if not assigned
            if (slotsContainer == null)
            {
                if (meterSlots != null && meterSlots.Length > 0 && meterSlots[0] != null)
                {
                    slotsContainer = meterSlots[0].transform.parent as RectTransform;
                }
                else
                {
                    slotsContainer = (transform.Find("RainbowMeterPanel/SlotsContainer") as RectTransform)
                                  ?? (transform.Find("SlotsContainer") as RectTransform)
                                  ?? (GetComponentInChildren<HorizontalLayoutGroup>()?.transform as RectTransform);
                }
            }

            if (slotsContainer == null) return;

            // Cache slot template if needed
            if (slotPrefab == null && _cachedSlotTemplate == null)
            {
                if (meterSlots != null && meterSlots.Length > 0 && meterSlots[0] != null)
                {
                    _cachedSlotTemplate = meterSlots[0].gameObject;
                }
                else if (slotsContainer.childCount > 0)
                {
                    _cachedSlotTemplate = slotsContainer.GetChild(0).gameObject;
                }
            }

            // Clear existing slots
            _dynamicSlots.Clear();
            for (int i = slotsContainer.childCount - 1; i >= 0; i--)
            {
                var child = slotsContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // Sizing based on sequence length (1-10 gems)
            int count = sequence.Length;
            float slotSize = count <= 5 ? 64f : (count <= 7 ? 60f : (count <= 8 ? 56f : (count <= 9 ? 52f : 48f)));
            float fontSize = count <= 5 ? 32f : (count <= 7 ? 30f : (count <= 8 ? 28f : (count <= 9 ? 26f : 24f)));
            float spacing  = count <= 6 ? 14f : (count <= 8 ? 10f : 6f);

            var hlg = slotsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = spacing;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            // Instantiate dynamic slots
            for (int i = 0; i < count; i++)
            {
                GameObject slotObj;
                if (slotPrefab != null)
                {
                    slotObj = Instantiate(slotPrefab, slotsContainer);
                }
                else if (_cachedSlotTemplate != null)
                {
                    slotObj = Instantiate(_cachedSlotTemplate, slotsContainer);
                }
                else
                {
                    slotObj = new GameObject($"Slot_{i}_{sequence[i]}", typeof(RectTransform));
                    slotObj.transform.SetParent(slotsContainer, false);
                    slotObj.AddComponent<RainbowMeterSlot>();
                }

                slotObj.name = $"Slot_{i}_{sequence[i]}";
                slotObj.SetActive(true);

                var slot = slotObj.GetComponent<RainbowMeterSlot>();
                if (slot != null)
                {
                    slot.Initialize(sequence[i]);
                    slot.SetSize(new Vector2(slotSize, slotSize), fontSize);
                    _dynamicSlots.Add(slot);
                }
            }
        }

        public void RefreshRainbowMeter()
        {
            if (_session == null)
            {
                _session = GameSession.Active;
            }

            var progress = _session != null ? _session.RainbowProgress : null;
            string objDescription = _session != null && _session.LevelDefinition != null 
                ? _session.LevelDefinition.ObjectiveDescription 
                : "Collect 1 red gem";

            if (objectiveText != null)
            {
                objectiveText.text = objDescription;
            }

            if (progress == null)
            {
                for (int i = 0; i < _dynamicSlots.Count; i++)
                {
                    _dynamicSlots[i]?.SetState(RainbowMeterSlot.SlotState.Empty);
                }
                if (nextTargetText != null)
                {
                    nextTargetText.text = objectiveText != null ? "NEXT: RED" : $"{objDescription} | NEXT: RED";
                }
                return;
            }

            // Check if slots need to be rebuilt
            if (_dynamicSlots.Count != progress.TotalCount)
            {
                BuildSequenceMeter(progress.Sequence);
            }
            else
            {
                for (int i = 0; i < progress.TotalCount; i++)
                {
                    if (_dynamicSlots[i] == null || _dynamicSlots[i].Colour != progress.GetColourAt(i))
                    {
                        BuildSequenceMeter(progress.Sequence);
                        break;
                    }
                }
            }

            int collected = progress.CollectedCount;
            int banked = progress.BankedCount;
            bool isComplete = progress.IsComplete;
            RainbowColour? target = progress.CurrentTarget;

            for (int i = 0; i < _dynamicSlots.Count; i++)
            {
                var slot = _dynamicSlots[i];
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
                    nextTargetText.text = "<color=#FFE666>ALL GEMS COLLECTED! ENTER GATE!</color>";
                }
                else if (target.HasValue)
                {
                    string name = target.Value.ToString().ToUpper();
                    string hex = RainbowColourHelper.GetHex(target.Value);
                    if (objectiveText != null)
                    {
                        nextTargetText.text = $"NEXT: <color={hex}>{name}</color> ({collected + 1}/{progress.TotalCount})";
                    }
                    else
                    {
                        nextTargetText.text = $"<b>{objDescription}</b> | NEXT: <color={hex}>{name}</color> ({collected + 1}/{progress.TotalCount})";
                    }
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

        // ── Score & Multiplier ────────────────────────────────────────────────

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score:N0}";
            }
        }

        public void UpdateMultiplier(int multiplier)
        {
            var txt = multiplierText;
            if (txt == null)
            {
                // Fallback lookup if not assigned
                txt = transform.Find("TopBar/ScoreComboDisplay/ComboText")?.GetComponent<TextMeshProUGUI>();
            }

            if (txt != null)
            {
                if (multiplier >= 5)
                {
                    txt.text = "<color=#FFE640>MAX RUSH x5</color>";
                }
                else if (multiplier > 1)
                {
                    string colorHex = multiplier switch
                    {
                        2 => "#4FFFA4",
                        3 => "#52E5FF",
                        4 => "#FFB240",
                        _ => "#FFE640"
                    };
                    txt.text = $"<color={colorHex}>RUSH x{multiplier}</color>";
                }
                else
                {
                    txt.text = "RUSH: x1";
                    txt.color = new Color(0.75f, 0.8f, 0.9f, 0.8f);
                }

                if (multiplierContainer == null)
                {
                    multiplierContainer = txt.transform.parent as RectTransform;
                }

                if (multiplier > 1 && isActiveAndEnabled && Application.isPlaying)
                {
                    if (_multiplierEnergyRoutine != null) StopCoroutine(_multiplierEnergyRoutine);
                    _multiplierEnergyRoutine = StartCoroutine(MultiplierBounceRoutine(multiplier));
                }
                else if (multiplierContainer != null)
                {
                    multiplierContainer.localScale = Vector3.one;
                }
            }
        }

        public void UpdateRushTimer(float remainingTime, float totalWindow)
        {
            if (rushTimerMeter != null)
            {
                if (totalWindow > 0.001f && remainingTime > 0f)
                {
                    rushTimerMeter.fillAmount = Mathf.Clamp01(remainingTime / totalWindow);
                    rushTimerMeter.gameObject.SetActive(true);
                }
                else
                {
                    rushTimerMeter.fillAmount = 0f;
                    rushTimerMeter.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator MultiplierBounceRoutine(int multiplier)
        {
            if (multiplierContainer == null) yield break;

            float intensity = multiplier >= 5 ? 1.45f : 1.15f + (multiplier - 1) * 0.08f;
            multiplierContainer.localScale = Vector3.one * intensity;

            float elapsed = 0f;
            float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                multiplierContainer.localScale = Vector3.Lerp(Vector3.one * intensity, Vector3.one, elapsed / duration);
                yield return null;
            }
            multiplierContainer.localScale = Vector3.one;
            _multiplierEnergyRoutine = null;
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

        public void ShakeMultiplierDisplay()
        {
            if (multiplierContainer == null && multiplierText != null)
            {
                multiplierContainer = multiplierText.transform.parent as RectTransform;
            }
            if (multiplierContainer == null || !isActiveAndEnabled || !Application.isPlaying) return;

            if (_multiplierShakeRoutine != null)
            {
                StopCoroutine(_multiplierShakeRoutine);
            }
            _multiplierShakeRoutine = StartCoroutine(MultiplierShakeRoutine());
        }

        private IEnumerator MultiplierShakeRoutine()
        {
            Vector3 originalLocalPos = multiplierContainer.localPosition;
            float duration = 0.25f;
            float elapsed = 0f;
            float magnitude = 8.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float damp = 1f - progress;
                float xOffset = Mathf.Sin(progress * Mathf.PI * 8f) * magnitude * damp;
                multiplierContainer.localPosition = originalLocalPos + new Vector3(xOffset, 0f, 0f);
                yield return null;
            }

            multiplierContainer.localPosition = originalLocalPos;
            _multiplierShakeRoutine = null;
        }

        private void HandleCorrectGem(RainbowColour col) => RefreshRainbowMeter();
        private void HandleWrongGem(RainbowColour col)
        {
            RefreshRainbowMeter();
            ShakeMultiplierDisplay();
        }
        private void HandleProgressBanked() => RefreshRainbowMeter();
        private void HandleProgressRestored() => RefreshRainbowMeter();
        private void HandleProgressReset() => RefreshRainbowMeter();
        private void HandleDashRecharged() => ShowFeedbackMessage("DASH READY! [SPACE]", new Color(0.4f, 1.0f, 0.85f, 1f));
    }
}

