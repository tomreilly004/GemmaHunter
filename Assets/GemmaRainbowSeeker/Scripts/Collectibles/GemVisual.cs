using System.Collections;
using TMPro;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Handles the visual presentation of a GemPickup:
    /// idle bobbing, gentle scale pulsing, slight rotation oscillation,
    /// colour tinting, text marker (R, O, Y, G, B, I, V), and wrong-attempt recoil/flash animations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GemVisual : MonoBehaviour
    {
        [Header("Sprites & Renderers")]
        [Tooltip("Main gem shape sprite renderer.")]
        [SerializeField] private SpriteRenderer mainRenderer;

        [Tooltip("Optional outer glow or rim sprite renderer.")]
        [SerializeField] private SpriteRenderer glowRenderer;

        [Tooltip("TextMeshPro component displaying the colour marker (R, O, Y, G, B, I, V).")]
        [SerializeField] private TextMeshPro markerText;

        [Header("Idle Animation Tuning")]
        [Tooltip("Frequency of the vertical bobbing wave.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float bobFrequency = 2.2f;

        [Tooltip("Amplitude of the vertical bobbing wave.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float bobAmplitude = 0.12f;

        [Tooltip("Frequency of gentle scale pulsing.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float pulseFrequency = 3.0f;

        [Tooltip("Amplitude of scale pulsing (0.05 = 5% scale change).")]
        [Range(0.01f, 0.3f)]
        [SerializeField] private float pulseAmplitude = 0.08f;

        [Tooltip("Frequency of rotation oscillation.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float rotateFrequency = 1.8f;

        [Tooltip("Maximum angle of rotation oscillation in degrees.")]
        [Range(0f, 45f)]
        [SerializeField] private float rotateAngle = 12f;

        [Header("Feedback Tuning")]
        [Tooltip("Colour flashed when Gemma touches this gem out of order.")]
        [SerializeField] private Color wrongAttemptFlashColor = new Color(0.78f, 0.78f, 0.82f, 1.0f); // Pale grey

        [Tooltip("Duration of the wrong-attempt flash in seconds.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float flashDuration = 0.35f;

        [Tooltip("Recoil displacement magnitude when a wrong attempt occurs.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float recoilMagnitude = 0.25f;

        private RainbowColour _colour;
        private Color _baseColor;
        private Vector3 _initialLocalPos;
        private Vector3 _baseScale;
        private Coroutine _recoilRoutine;
        private Coroutine _flashRoutine;
        private float _timeOffset;

        public RainbowColour Colour => _colour;
        public Color BaseColor => _baseColor;

        private void Awake()
        {
            _initialLocalPos = transform.localPosition;
            _baseScale = transform.localScale;
            _timeOffset = Random.Range(0f, 10f); // Desynchronize gems in level

            if (mainRenderer == null) mainRenderer = GetComponent<SpriteRenderer>();
            if (markerText == null) markerText = GetComponentInChildren<TextMeshPro>();
        }

        public void ApplyColour(RainbowColour colour)
        {
            _colour = colour;
            _baseColor = RainbowColourHelper.GetColor(colour);

            if (mainRenderer != null)
            {
                mainRenderer.color = _baseColor;
                mainRenderer.sortingLayerName = "Collectibles";
                mainRenderer.sortingOrder = 0;
            }

            if (glowRenderer != null)
            {
                Color glowCol = _baseColor;
                glowCol.a = 0.45f;
                glowRenderer.color = glowCol;
                glowRenderer.sortingLayerName = "Collectibles";
                glowRenderer.sortingOrder = -1;
            }

            if (markerText != null)
            {
                markerText.text = RainbowColourHelper.GetMarkerString(colour);
                markerText.color = Color.white;
                markerText.sortingLayerID = SortingLayer.NameToID("Collectibles");
                markerText.sortingOrder = 1;
            }
        }

        private void Update()
        {
            float t = Time.time + _timeOffset;

            // 1. Vertical Bobbing
            float bobY = Mathf.Sin(t * bobFrequency) * bobAmplitude;
            transform.localPosition = _initialLocalPos + new Vector3(0f, bobY, 0f);

            // 2. Scale Pulsing
            float pulse = 1f + Mathf.Sin(t * pulseFrequency) * pulseAmplitude;
            transform.localScale = _baseScale * pulse;

            // 3. Gentle Rotation Oscillation
            float rotZ = Mathf.Sin(t * rotateFrequency) * rotateAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        /// <summary>
        /// Plays the wrong-attempt feedback: pale grey flash, recoil shake, and recovers.
        /// </summary>
        public void PlayWrongAttemptFeedback(Vector2 recoilDirection)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            if (_recoilRoutine != null) StopCoroutine(_recoilRoutine);

            _flashRoutine = StartCoroutine(FlashPaleGreyRoutine());
            _recoilRoutine = StartCoroutine(RecoilRoutine(recoilDirection));
        }

        private IEnumerator FlashPaleGreyRoutine()
        {
            if (mainRenderer != null)
            {
                mainRenderer.color = wrongAttemptFlashColor;
            }

            if (markerText != null)
            {
                markerText.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            }

            yield return new WaitForSeconds(flashDuration);

            if (mainRenderer != null)
            {
                mainRenderer.color = _baseColor;
            }

            if (markerText != null)
            {
                markerText.color = Color.white;
            }

            _flashRoutine = null;
        }

        private IEnumerator RecoilRoutine(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;
            dir.Normalize();

            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + (Vector3)(dir * recoilMagnitude);

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                // Spring back with elastic overshoot
                float curve = Mathf.Sin(progress * Mathf.PI);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, curve);
                yield return null;
            }

            transform.localPosition = startPos;
            _recoilRoutine = null;
        }

        public void SetVisibility(bool visible)
        {
            if (mainRenderer != null) mainRenderer.enabled = visible;
            if (glowRenderer != null) glowRenderer.enabled = visible;
            if (markerText != null) markerText.enabled = visible;
        }
    }
}
