using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Controls Gemma's visual presentation on a child transform.
    /// Handles banking/rotation toward movement velocity, squash & stretch,
    /// and visual dash reactions without altering the physics root orientation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GemmaVisual : MonoBehaviour
    {
        [Header("Tilt & Rotation")]
        [Tooltip("Maximum tilt angle in degrees when moving up or down.")]
        [Range(0f, 60f)]
        [SerializeField] private float maxTiltAngle = 30f;

        [Tooltip("Smooth rotation interpolation speed.")]
        [Range(1f, 30f)]
        [SerializeField] private float rotationSmoothSpeed = 12f;

        [Header("Squash & Stretch")]
        [Tooltip("Base scale of the visual child object.")]
        [SerializeField] private Vector3 baseScale = Vector3.one;

        [Tooltip("Maximum stretch factor along the movement axis during high speed / dash.")]
        [Range(1f, 2f)]
        [SerializeField] private float maxStretch = 1.35f;

        [Tooltip("Maximum squash factor perpendicular to movement axis during high speed / dash.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float minSquash = 0.75f;

        [Tooltip("Speed at which squash/stretch returns to base scale.")]
        [Range(1f, 30f)]
        [SerializeField] private float scaleRecoverySpeed = 15f;

        [Header("Buoyant Bobbing")]
        [Tooltip("Frequency of subtle idle breathing/bobbing oscillation.")]
        [Range(0f, 10f)]
        [SerializeField] private float bobFrequency = 2.5f;

        [Tooltip("Amplitude of subtle idle breathing/bobbing oscillation.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float bobAmplitude = 0.04f;

        private GemmaMotor2D _motor;
        private GemmaDash _dash;
        private Vector3 _currentScale;
        private float _currentTiltAngle;
        private bool _facingRight = true;

        public bool FacingRight => _facingRight;

        private void Awake()
        {
            _motor = GetComponentInParent<GemmaMotor2D>();
            _dash = GetComponentInParent<GemmaDash>();
            _currentScale = baseScale;
        }

        private void OnEnable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted += HandleDashStarted;
            }
        }

        private void OnDisable()
        {
            if (_dash != null)
            {
                _dash.OnDashStarted -= HandleDashStarted;
            }
        }

        private void HandleDashStarted(Vector2 dir)
        {
            // Instant burst stretch on dash start
            _currentScale = new Vector3(maxStretch * 1.1f, minSquash * 0.9f, 1f);
        }

        private void Update()
        {
            Vector2 vel = _motor != null ? _motor.Velocity : Vector2.zero;
            float speed = vel.magnitude;
            float maxSpeed = _motor != null ? _motor.MaxSpeed : 5.8f;

            // 1. Facing and Tilt
            if (Mathf.Abs(vel.x) > 0.1f)
            {
                _facingRight = vel.x > 0f;
            }

            // Target tilt based on vertical movement
            float targetTilt = 0f;
            if (speed > 0.2f)
            {
                float normalizedY = Mathf.Clamp(vel.y / Mathf.Max(1f, speed), -1f, 1f);
                targetTilt = normalizedY * maxTiltAngle;
                if (!_facingRight)
                {
                    targetTilt = -targetTilt; // Invert tilt for left-facing orientation
                }
            }

            _currentTiltAngle = Mathf.Lerp(_currentTiltAngle, targetTilt, rotationSmoothSpeed * Time.deltaTime);

            // Apply rotation with facing flip
            float yRotation = _facingRight ? 0f : 180f;
            transform.localRotation = Quaternion.Euler(0f, yRotation, _currentTiltAngle);

            // 2. Squash & Stretch target
            Vector3 targetScale = baseScale;

            if (_dash != null && _dash.IsDashing)
            {
                targetScale = new Vector3(maxStretch, minSquash, 1f);
            }
            else if (speed > 0.5f)
            {
                float speedFactor = Mathf.Clamp01(speed / maxSpeed);
                float stretchX = Mathf.Lerp(1f, maxStretch, speedFactor * 0.5f);
                float squashY = Mathf.Lerp(1f, minSquash, speedFactor * 0.5f);
                targetScale = new Vector3(stretchX, squashY, 1f);
            }
            else
            {
                // Idle buoyancy wave
                float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                targetScale = baseScale + new Vector3(bob * 0.5f, bob, 0f);
            }

            _currentScale = Vector3.Lerp(_currentScale, targetScale, scaleRecoverySpeed * Time.deltaTime);
            transform.localScale = _currentScale;
        }
    }
}
