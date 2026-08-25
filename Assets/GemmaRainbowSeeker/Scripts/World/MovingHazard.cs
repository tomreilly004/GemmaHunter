using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Moves a hazard back and forth between two waypoints with smooth easing.
    /// Supports local offset or absolute world waypoints.
    /// </summary>
    [RequireComponent(typeof(Hazard))]
    [DisallowMultipleComponent]
    public sealed class MovingHazard : MonoBehaviour
    {
        public enum EasingType
        {
            SmoothStep,
            Sinusoidal,
            Linear
        }

        [Header("Movement Path")]
        [Tooltip("Starting offset relative to initial position.")]
        [SerializeField] private Vector2 offsetA = Vector2.zero;

        [Tooltip("Ending offset relative to initial position.")]
        [SerializeField] private Vector2 offsetB = new Vector2(0f, 4f);

        [Header("Timing")]
        [Tooltip("Time in seconds to travel from Point A to Point B.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float travelDuration = 2.5f;

        [Tooltip("Pause duration in seconds at each endpoint.")]
        [Range(0f, 2f)]
        [SerializeField] private float pauseDuration = 0.2f;

        [Tooltip("Easing interpolation curve.")]
        [SerializeField] private EasingType easing = EasingType.SmoothStep;

        private Vector3 _startWorldPos;
        private Vector3 _pointA;
        private Vector3 _pointB;
        private float _cycleTime;
        private bool _movingToB = true;
        private float _pauseTimer;

        private void Awake()
        {
            _startWorldPos = transform.position;
            _pointA = _startWorldPos + (Vector3)offsetA;
            _pointB = _startWorldPos + (Vector3)offsetB;
        }

        private void Update()
        {
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            _cycleTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_cycleTime / travelDuration);

            float easedT = EvaluateEasing(progress, easing);

            Vector3 from = _movingToB ? _pointA : _pointB;
            Vector3 to   = _movingToB ? _pointB : _pointA;

            transform.position = Vector3.Lerp(from, to, easedT);

            if (progress >= 1f)
            {
                _movingToB = !_movingToB;
                _cycleTime = 0f;
                _pauseTimer = pauseDuration;
            }
        }

        private float EvaluateEasing(float t, EasingType type)
        {
            switch (type)
            {
                case EasingType.SmoothStep:
                    return Mathf.SmoothStep(0f, 1f, t);
                case EasingType.Sinusoidal:
                    return 0.5f * (1f - Mathf.Cos(t * Mathf.PI));
                case EasingType.Linear:
                default:
                    return t;
            }
        }

        public void ResetToStart()
        {
            _cycleTime = 0f;
            _pauseTimer = 0f;
            _movingToB = true;
            transform.position = _pointA;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _startWorldPos : transform.position;
            Vector3 a = origin + (Vector3)offsetA;
            Vector3 b = origin + (Vector3)offsetB;

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.3f);
            Gizmos.DrawWireSphere(b, 0.3f);
        }
    }
}
