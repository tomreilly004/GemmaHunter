using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Handles Gemma's burst dash ability.
    /// Propels the player at dash speed in the current input direction (or last facing)
    /// for a fixed duration, with cooldown tracking and dash-state events.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(GemmaMotor2D))]
    [DisallowMultipleComponent]
    public sealed class GemmaDash : MonoBehaviour
    {
        [Header("Dash Tuning")]
        [Tooltip("Burst velocity applied during the dash in units per second.")]
        [Range(5f, 30f)]
        [SerializeField] private float dashSpeed = 13.5f;

        [Tooltip("Active duration of the dash impulse in seconds.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float dashDuration = 0.22f;

        [Tooltip("Cooldown period before another dash can be initiated.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float dashCooldown = 0.85f;

        [Header("State")]
        [Tooltip("Whether the dash ability is currently allowed.")]
        [SerializeField] private bool dashEnabled = true;

        private Rigidbody2D _rb;
        private GemmaMotor2D _motor;
        private bool _isDashing;
        private float _cooldownTimer;
        private Vector2 _dashDirection;
        private Coroutine _dashRoutine;

        public event Action<Vector2> OnDashStarted;
        public event Action OnDashEnded;
        public event Action OnDashRecharged;

        public bool IsDashing => _isDashing;
        public bool IsOnCooldown => _cooldownTimer > 0f;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);
        public float CooldownTotal => dashCooldown;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;

        public bool DashEnabled
        {
            get => dashEnabled;
            set => dashEnabled = value;
        }

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_motor == null) _motor = GetComponent<GemmaMotor2D>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    OnDashRecharged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Input System message handler from PlayerInput (Send Messages mode).
        /// </summary>
        public void OnDash(InputValue value)
        {
            if (value.isPressed)
            {
                TryDash();
            }
        }

        /// <summary>
        /// Attempts to trigger a dash. Returns true if the dash successfully started.
        /// </summary>
        public bool TryDash()
        {
            if (!dashEnabled || _isDashing || _cooldownTimer > 0f)
            {
                return false;
            }

            EnsureComponents();

            // Determine dash direction: current move input if non-zero, otherwise last non-zero direction
            Vector2 dir = _motor != null ? _motor.MoveInput : Vector2.zero;
            if (dir.sqrMagnitude > 0.01f)
            {
                _dashDirection = dir.normalized;
            }
            else if (_motor != null && _motor.LastNonZeroDirection.sqrMagnitude > 0.01f)
            {
                _dashDirection = _motor.LastNonZeroDirection.normalized;
            }
            else
            {
                _dashDirection = Vector2.right;
            }

            if (isActiveAndEnabled && Application.isPlaying)
            {
                if (_dashRoutine != null)
                {
                    StopCoroutine(_dashRoutine);
                }
                _dashRoutine = StartCoroutine(DashRoutine());
            }
            else
            {
                _isDashing = true;
                _cooldownTimer = dashCooldown;
                if (_rb != null)
                {
                    _rb.linearVelocity = _dashDirection * dashSpeed;
                }
                OnDashStarted?.Invoke(_dashDirection);
            }

            return true;
        }

        private IEnumerator DashRoutine()
        {
            _isDashing = true;
            _cooldownTimer = dashCooldown;
            OnDashStarted?.Invoke(_dashDirection);

            float elapsed = 0f;
            while (elapsed < dashDuration)
            {
                _rb.linearVelocity = _dashDirection * dashSpeed;
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            _isDashing = false;
            _dashRoutine = null;
            OnDashEnded?.Invoke();
        }

        public void ResetCooldown()
        {
            _cooldownTimer = 0f;
        }

        public void CancelDash()
        {
            if (_isDashing)
            {
                if (_dashRoutine != null)
                {
                    StopCoroutine(_dashRoutine);
                    _dashRoutine = null;
                }
                _isDashing = false;
                OnDashEnded?.Invoke();
            }
        }
    }
}
