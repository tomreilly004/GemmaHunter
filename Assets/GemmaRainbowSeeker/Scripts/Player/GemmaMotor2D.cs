using UnityEngine;
using UnityEngine.InputSystem;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Handles 2D momentum-based swimming physics for Gemma.
    /// Movement is applied via Rigidbody2D in FixedUpdate with acceleration,
    /// deceleration, directional responsiveness and diagonal normalisation.
    /// Integrates with RainbowRushController to apply speed bonuses (x1=0%, x2=6%, x3=12%, x4=18%, x5=24%)
    /// and smoothly return to normal speed over 0.2 seconds when Rush resets.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class GemmaMotor2D : MonoBehaviour
    {
        [Header("Swim Tuning")]
        [Tooltip("Maximum base swimming velocity in units per second.")]
        [Range(1f, 20f)]
        [SerializeField] private float maxSpeed = 5.8f;

        [Tooltip("Rate of base acceleration when applying movement input.")]
        [Range(1f, 50f)]
        [SerializeField] private float acceleration = 22f;

        [Tooltip("Rate of deceleration (drag) when releasing input.")]
        [Range(1f, 50f)]
        [SerializeField] private float deceleration = 14f;

        [Tooltip("Extra responsiveness factor applied when rapidly turning or reversing direction.")]
        [Range(1f, 50f)]
        [SerializeField] private float directionChangeResponsiveness = 18f;

        [Header("State")]
        [Tooltip("Whether swimming movement input is currently enabled.")]
        [SerializeField] private bool inputEnabled = true;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Vector2 _lastNonZeroDirection = Vector2.right;
        private GemmaDash _dash;

        // Rainbow Rush speed bonus blending
        private float _currentSpeedBonus = 0f;
        private const float ResetSmoothReturnDuration = 0.2f;

        public Vector2 MoveInput => _moveInput;
        public Vector2 LastNonZeroDirection => _lastNonZeroDirection;
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
        public float BaseMaxSpeed => maxSpeed;
        public float BaseAcceleration => acceleration;
        public float MaxSpeed => EffectiveMaxSpeed;
        public float CurrentSpeedBonus => _currentSpeedBonus;
        public float EffectiveMaxSpeed => maxSpeed * (1f + _currentSpeedBonus);
        public float EffectiveAcceleration => acceleration * (1f + _currentSpeedBonus);

        public bool InputEnabled
        {
            get => inputEnabled;
            set => inputEnabled = value;
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
                if (_rb != null)
                {
                    _rb.gravityScale = 0f;
                    _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                    _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }
            }
            if (_dash == null)
            {
                _dash = GetComponent<GemmaDash>();
            }
        }

        /// <summary>
        /// Input System message handler from PlayerInput (Send Messages mode).
        /// </summary>
        public void OnMove(InputValue value)
        {
            SetMoveInput(value.Get<Vector2>());
        }

        public void SetMoveInput(Vector2 input)
        {
            if (!inputEnabled)
            {
                _moveInput = Vector2.zero;
                return;
            }

            // Normalise diagonal input so diagonal speed does not exceed 1.0
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            _moveInput = input;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastNonZeroDirection = _moveInput.normalized;
            }
        }

        private void Update()
        {
            // Query RainbowRushController speed bonus
            float targetBonus = 0f;
            var session = GameSession.Active;
            if (session != null && session.RushController != null)
            {
                targetBonus = session.RushController.CurrentSpeedBonus;
            }

            if (targetBonus >= _currentSpeedBonus)
            {
                // Immediate response on tier up
                _currentSpeedBonus = targetBonus;
            }
            else
            {
                // Smooth decay back to base speed over 0.2s when Rush resets
                float maxChange = (1f / ResetSmoothReturnDuration) * Time.deltaTime;
                _currentSpeedBonus = Mathf.MoveTowards(_currentSpeedBonus, targetBonus, maxChange);
            }
        }

        private void FixedUpdate()
        {
            EnsureComponents();
            if (_rb == null) return;

            // If dashing, dash logic controls the rigidbody velocity (dash speed is not scaled)
            if (_dash != null && _dash.IsDashing)
            {
                return;
            }

            float effectiveMax = maxSpeed * (1f + _currentSpeedBonus);
            float effectiveAccel = acceleration * (1f + _currentSpeedBonus);

            Vector2 currentVel = _rb.linearVelocity;
            Vector2 targetVel = _moveInput * effectiveMax;

            if (_moveInput.sqrMagnitude > 0.001f)
            {
                // Accelerating or turning
                float currentAccel = effectiveAccel;

                // If steering away from current velocity, increase responsiveness
                if (currentVel.sqrMagnitude > 0.1f)
                {
                    float dot = Vector2.Dot(currentVel.normalized, _moveInput.normalized);
                    if (dot < 0.9f)
                    {
                        // Blend towards higher turning responsiveness based on how sharp the turn is
                        float turnFactor = Mathf.Clamp01((1f - dot) * 0.5f);
                        currentAccel = Mathf.Lerp(effectiveAccel, effectiveAccel + directionChangeResponsiveness, turnFactor);
                    }
                }

                currentVel = Vector2.MoveTowards(currentVel, targetVel, currentAccel * Time.fixedDeltaTime);
            }
            else
            {
                // Decelerating smoothly to stop (water drag feel)
                currentVel = Vector2.MoveTowards(currentVel, Vector2.zero, deceleration * Time.fixedDeltaTime);
            }

            _rb.linearVelocity = currentVel;
        }
    }
}

