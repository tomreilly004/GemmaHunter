using System.Collections;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Manages Gemma's movement trail renderer.
    /// Provides methods to update trail colors and trigger brightness flashes
    /// when collecting rainbow gems or dashing.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    [DisallowMultipleComponent]
    public sealed class GemmaTrail : MonoBehaviour
    {
        [Header("Default Trail Settings")]
        [Tooltip("Base color of the trail when no color flash is active.")]
        [SerializeField] private Color defaultColor = new Color(0.9f, 0.95f, 1f, 0.6f);

        [Tooltip("Color applied to the end of the trail.")]
        [SerializeField] private Color tailColor = new Color(0.5f, 0.8f, 1f, 0f);

        [Tooltip("Base width at the start of the trail.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float startWidth = 0.45f;

        [Tooltip("Default trail lifetime in seconds.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float trailTime = 0.4f;

        private TrailRenderer _trail;
        private Coroutine _flashRoutine;

        public TrailRenderer TrailRenderer => _trail;
        public Color CurrentColor => defaultColor;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            ConfigureDefaultTrail();
        }

        private void ConfigureDefaultTrail()
        {
            if (_trail == null) return;

            _trail.time = trailTime;
            _trail.startWidth = startWidth;
            _trail.endWidth = 0.05f;
            _trail.autodestruct = false;
            _trail.emitting = true;
            _trail.sortingLayerName = "Player";
            _trail.sortingOrder = -1;

            SetColorGradient(defaultColor, tailColor);
        }

        public void SetTrailColour(Color color, float boostDuration = 0f)
        {
            SetTrailColor(color, boostDuration);
        }

        public void SetTrailColor(Color color, float boostDuration = 0f)
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            if (boostDuration > 0f)
            {
                _flashRoutine = StartCoroutine(ColorFlashRoutine(color, boostDuration));
            }
            else
            {
                defaultColor = color;
                SetColorGradient(color, new Color(color.r, color.g, color.b, 0f));
            }
        }

        private IEnumerator ColorFlashRoutine(Color boostColor, float duration)
        {
            Color brightBoost = boostColor * 1.3f;
            brightBoost.a = 0.9f;
            SetColorGradient(brightBoost, new Color(boostColor.r, boostColor.g, boostColor.b, 0f));

            if (_trail != null)
            {
                _trail.startWidth = startWidth * 1.3f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetColorGradient(defaultColor, tailColor);
            if (_trail != null)
            {
                _trail.startWidth = startWidth;
            }
            _flashRoutine = null;
        }

        private void SetColorGradient(Color start, Color end)
        {
            if (_trail == null) return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) }
            );
            _trail.colorGradient = gradient;
        }
    }
}
