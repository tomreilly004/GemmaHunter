using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Displays a single colour slot in the 7-slot Rainbow Meter.
    /// Handles Empty (muted), Target/Next (pulsing highlight), Collected (filled),
    /// and Banked (filled + lock icon) visual states.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RainbowMeterSlot : MonoBehaviour
    {
        public enum SlotState
        {
            Empty,
            NextRequired,
            Collected,
            Banked
        }

        [Header("Slot Identity")]
        [SerializeField] private RainbowColour colour = RainbowColour.Red;

        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Image slotBackground;
        [SerializeField] private UnityEngine.UI.Image slotOutline;
        [SerializeField] private TextMeshProUGUI letterText;
        [SerializeField] private TextMeshProUGUI lockText;
        [SerializeField] private RectTransform containerTransform;

        [Header("Visual Styling")]
        [SerializeField] private Color emptyBgColor = new Color(0.18f, 0.20f, 0.26f, 0.9f);
        [SerializeField] private Color emptyOutlineColor = new Color(0.4f, 0.44f, 0.52f, 0.6f);
        [SerializeField] private Color emptyTextColor = new Color(0.6f, 0.65f, 0.75f, 0.5f);
        [SerializeField] private Color highlightOutlineColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        private SlotState _currentState = SlotState.Empty;
        private Color _vibrantColor;
        private Vector3 _baseScale;
        private Coroutine _pulseRoutine;

        public RainbowColour Colour => colour;
        public SlotState State => _currentState;

        private void Awake()
        {
            if (containerTransform == null) containerTransform = GetComponent<RectTransform>();
            _baseScale = containerTransform != null ? containerTransform.localScale : Vector3.one;
            _vibrantColor = RainbowColourHelper.GetColor(colour);

            if (letterText != null)
            {
                letterText.text = RainbowColourHelper.GetMarkerString(colour);
            }

            SetState(SlotState.Empty);
        }

        public void Initialize(RainbowColour col)
        {
            colour = col;
            _vibrantColor = RainbowColourHelper.GetColor(colour);
            if (letterText != null)
            {
                letterText.text = RainbowColourHelper.GetMarkerString(colour);
            }
            SetState(_currentState);
        }

        public void SetSize(Vector2 size, float fontSize = 0f)
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = size;
            }
            if (letterText != null && fontSize > 0f)
            {
                letterText.fontSize = fontSize;
            }
        }

        public void SetState(SlotState newState)
        {
            SlotState prevState = _currentState;
            _currentState = newState;

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            if (containerTransform != null)
            {
                containerTransform.localScale = _baseScale;
            }

            switch (_currentState)
            {
                case SlotState.Empty:
                    if (slotBackground != null) slotBackground.color = emptyBgColor;
                    if (slotOutline != null) slotOutline.color = emptyOutlineColor;
                    if (letterText != null) letterText.color = emptyTextColor;
                    if (lockText != null) lockText.gameObject.SetActive(false);
                    break;

                case SlotState.NextRequired:
                    if (slotBackground != null) slotBackground.color = Color.Lerp(emptyBgColor, _vibrantColor, 0.25f);
                    if (slotOutline != null) slotOutline.color = highlightOutlineColor;
                    if (letterText != null) letterText.color = Color.white;
                    if (lockText != null) lockText.gameObject.SetActive(false);
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        _pulseRoutine = StartCoroutine(PulseRoutine());
                    }
                    break;

                case SlotState.Collected:
                    if (slotBackground != null) slotBackground.color = _vibrantColor;
                    if (slotOutline != null) slotOutline.color = Color.white;
                    if (letterText != null) letterText.color = Color.white;
                    if (lockText != null) lockText.gameObject.SetActive(false);
                    if (prevState != SlotState.Collected && isActiveAndEnabled && Application.isPlaying)
                    {
                        StartCoroutine(BounceRoutine());
                    }
                    break;

                case SlotState.Banked:
                    if (slotBackground != null) slotBackground.color = _vibrantColor;
                    if (slotOutline != null) slotOutline.color = new Color(1.0f, 0.95f, 0.4f, 1.0f); // Gold outline
                    if (letterText != null) letterText.color = Color.white;
                    if (lockText != null)
                    {
                        lockText.gameObject.SetActive(true);
                        lockText.text = "L";
                    }
                    if (prevState != SlotState.Banked && isActiveAndEnabled && Application.isPlaying)
                    {
                        StartCoroutine(BounceRoutine());
                    }
                    break;
            }
        }

        private IEnumerator BounceRoutine()
        {
            if (containerTransform == null) yield break;

            Vector3 peakScale = _baseScale * 1.35f;
            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Elastic bounce curve: sin(t * pi) with settle
                float curve = Mathf.Sin(t * Mathf.PI);
                containerTransform.localScale = Vector3.Lerp(_baseScale, peakScale, curve);
                yield return null;
            }

            containerTransform.localScale = _baseScale;
        }

        private IEnumerator PulseRoutine()
        {
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime * 3.5f;
                float pulse = 1f + 0.12f * Mathf.Sin(timer);
                if (containerTransform != null)
                {
                    containerTransform.localScale = _baseScale * pulse;
                }

                if (slotOutline != null)
                {
                    float glowAlpha = 0.6f + 0.4f * Mathf.Sin(timer);
                    slotOutline.color = new Color(1f, 1f, 1f, glowAlpha);
                }

                yield return null;
            }
        }
    }
}
