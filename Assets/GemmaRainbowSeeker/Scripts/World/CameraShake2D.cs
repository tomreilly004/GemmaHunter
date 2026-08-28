using System.Collections;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Provides subtle, non-intrusive camera impulses and shakes.
    /// Exposes a global intensity multiplier for reduced-motion accessibility settings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake2D : MonoBehaviour
    {
        public static CameraShake2D Instance { get; private set; }

        [Header("Settings & Accessibility")]
        [Tooltip("Global multiplier for all camera shakes (set to 0 for reduced motion).")]
        [Range(0f, 2f)]
        [SerializeField] private float shakeIntensityMultiplier = 1.0f;

        private Vector3 _shakeOffset;
        private Coroutine _shakeRoutine;
        private Transform _targetTransform;

        public float ShakeIntensityMultiplier
        {
            get => shakeIntensityMultiplier;
            set => shakeIntensityMultiplier = Mathf.Clamp(value, 0f, 2f);
        }

        public Vector3 CurrentOffset => _shakeOffset;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            _targetTransform = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Triggers a brief, subtle camera impulse.
        /// </summary>
        public void TriggerShake(float intensity = 0.15f, float duration = 0.15f)
        {
            if (shakeIntensityMultiplier <= 0.001f || duration <= 0f) return;
            if (!isActiveAndEnabled || !Application.isPlaying) return;

            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
            }
            _shakeRoutine = StartCoroutine(ShakeRoutine(intensity * shakeIntensityMultiplier, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 originalLocalPos = _targetTransform.localPosition - _shakeOffset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float currentIntensity = Mathf.Lerp(intensity, 0f, progress);

                // Perlin noise jitter
                float seed = Time.time * 25f;
                float x = (Mathf.PerlinNoise(seed, 0f) * 2f - 1f) * currentIntensity;
                float y = (Mathf.PerlinNoise(0f, seed) * 2f - 1f) * currentIntensity;

                _shakeOffset = new Vector3(x, y, 0f);
                _targetTransform.localPosition = originalLocalPos + _shakeOffset;

                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _targetTransform.localPosition = originalLocalPos;
            _shakeRoutine = null;
        }
    }
}
