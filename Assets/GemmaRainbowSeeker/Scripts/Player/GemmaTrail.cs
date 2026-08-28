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

        [Header("Accessibility & Tuning")]
        [Tooltip("Global trail intensity multiplier (set lower for reduced visual motion).")]
        [Range(0.1f, 2f)]
        [SerializeField] private float trailIntensityMultiplier = 1.0f;

        private TrailRenderer _trail;
        private GemmaDash _dash;
        private Coroutine _flashRoutine;

        private int _currentRushTier = 1;

        public TrailRenderer TrailRenderer => _trail;
        public Color CurrentColor => defaultColor;
        public int CurrentRushTier => _currentRushTier;
        public float TrailIntensityMultiplier
        {
            get => trailIntensityMultiplier;
            set
            {
                trailIntensityMultiplier = Mathf.Clamp(value, 0.1f, 2f);
                ConfigureDefaultTrail();
            }
        }

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            _dash = GetComponentInParent<GemmaDash>();
            ConfigureDefaultTrail();
        }

        private void OnEnable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted += HandleDashStarted;
                _dash.OnDashEnded += HandleDashEnded;
            }
        }

        private void OnDisable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted -= HandleDashStarted;
                _dash.OnDashEnded -= HandleDashEnded;
            }
        }

        private void Update()
        {
            // Sync with active GameSession Rush tier
            var session = GameSession.Active;
            int tier = (session != null && session.RushController != null) ? session.RushController.Multiplier : 1;
            if (tier != _currentRushTier)
            {
                SetRushTier(tier);
            }
        }

        /// <summary>
        /// Adjusts trail intensity and appearance according to the active Rush tier:
        /// - x1: Normal width and subtle color.
        /// - x2: 1.25x width.
        /// - x3: 1.50x width + speed streak brightness.
        /// - x4: 1.75x width.
        /// - x5: 2.00x width + vibrant radiant trail.
        /// </summary>
        public void SetRushTier(int tier)
        {
            _currentRushTier = Mathf.Clamp(tier, 1, 5);
            if (_trail == null || (_dash != null && _dash.IsDashing)) return;

            float tierScale = 1f + (_currentRushTier - 1) * 0.25f;
            _trail.startWidth = startWidth * tierScale * trailIntensityMultiplier;
            _trail.time = trailTime * (1f + (_currentRushTier - 1) * 0.1f);

            if (_currentRushTier >= 5)
            {
                // Radiant max rush
                Color maxCol = new Color(1f, 0.9f, 0.35f, 0.95f);
                SetColorGradient(maxCol, new Color(0.8f, 0.4f, 1f, 0f));
            }
            else if (_currentRushTier >= 3)
            {
                // Speed streak tier
                Color streakCol = new Color(0.6f, 0.95f, 1f, 0.85f);
                SetColorGradient(streakCol, new Color(0.3f, 0.7f, 1f, 0f));
            }
            else
            {
                SetColorGradient(defaultColor, tailColor);
            }
        }

        private void HandleDashStarted(Vector2 dir)
        {
            if (_trail == null) return;
            // Stronger, wider dash trail
            _trail.startWidth = startWidth * 1.6f * trailIntensityMultiplier;
            _trail.time = trailTime * 1.4f;
            Color boostCol = new Color(0.7f, 0.95f, 1f, 0.95f);
            SetColorGradient(boostCol, new Color(0.3f, 0.7f, 1f, 0f));
        }

        private void HandleDashEnded()
        {
            if (_trail == null) return;
            _trail.startWidth = startWidth * trailIntensityMultiplier;
            _trail.time = trailTime;
            SetColorGradient(defaultColor, tailColor);
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
