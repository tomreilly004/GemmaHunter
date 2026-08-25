using System;
using System.Collections;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Manages Gemma's health, damage handling, knockback impulse,
    /// temporary invulnerability with flashing feedback, and knockout state.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Health Settings")]
        [Tooltip("Maximum health pips for Gemma.")]
        [Range(1, 10)]
        [SerializeField] private int maxHealth = 3;

        [Header("Damage & Invulnerability")]
        [Tooltip("Duration in seconds of post-damage invulnerability.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float invulnerabilityDuration = 1.1f;

        [Tooltip("Knockback impulse velocity magnitude applied upon taking damage.")]
        [Range(1f, 30f)]
        [SerializeField] private float knockbackForce = 9.5f;

        [Tooltip("Flashing rate (cycles per second) during invulnerability.")]
        [Range(2f, 25f)]
        [SerializeField] private float flashFrequency = 12f;

        // ── State ─────────────────────────────────────────────────────────────
        private int _currentHealth = 3;
        private bool _isInvulnerable;
        private bool _isKnockedOut;
        private Rigidbody2D _rb;
        private GemmaDash _dash;
        private GemmaMotor2D _motor;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _invulnerabilityRoutine;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<int, int> OnHealthChanged;
        public event Action<int, Vector2> OnDamaged;
        public event Action<int> OnHealed;
        public event Action OnKnockedOut;
        public event Action<bool> OnInvulnerabilityChanged;

        // ── Properties ────────────────────────────────────────────────────────
        public int CurrentHealth => _currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvulnerable => _isInvulnerable;
        public bool IsKnockedOut => _isKnockedOut;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureComponents();
            _currentHealth = maxHealth;
            _isKnockedOut = false;
        }

        private void EnsureComponents()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_dash == null) _dash = GetComponent<GemmaDash>();
            if (_motor == null) _motor = GetComponent<GemmaMotor2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        // ── Damage Handling ───────────────────────────────────────────────────

        /// <summary>
        /// Attempts to inflict damage on Gemma.
        /// Returns true if damage was successfully dealt.
        /// Ignores damage if Gemma is dashing, already invulnerable, or knocked out.
        /// </summary>
        public bool TakeDamage(int amount, Vector2 knockbackDirection)
        {
            if (_isKnockedOut || _isInvulnerable || amount <= 0)
            {
                return false;
            }

            EnsureComponents();

            // Dashing grants temporary hazard immunity
            if (_dash != null && _dash.IsDashing)
            {
                return false;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Record damage in LevelSessionStats via GameSession
            GameSession.Active?.SessionStats?.RecordDamageTaken();

            // Apply readable knockback
            ApplyKnockback(knockbackDirection);

            OnDamaged?.Invoke(amount, knockbackDirection);

            if (_currentHealth <= 0)
            {
                TriggerKnockout();
            }
            else
            {
                StartInvulnerability(invulnerabilityDuration);
            }

            return true;
        }

        private void ApplyKnockback(Vector2 direction)
        {
            if (_rb == null) return;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.left; // Default push left if direction is undefined
            }
            direction.Normalize();

            _rb.linearVelocity = direction * knockbackForce;
        }

        // ── Invulnerability & Flashing ────────────────────────────────────────

        public void StartInvulnerability(float duration)
        {
            if (duration <= 0f)
            {
                if (_invulnerabilityRoutine != null)
                {
                    StopCoroutine(_invulnerabilityRoutine);
                    _invulnerabilityRoutine = null;
                }
                _isInvulnerable = false;
                OnInvulnerabilityChanged?.Invoke(false);
                return;
            }

            if (isActiveAndEnabled && Application.isPlaying)
            {
                if (_invulnerabilityRoutine != null)
                {
                    StopCoroutine(_invulnerabilityRoutine);
                }
                _invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(duration));
            }
            else
            {
                _isInvulnerable = true;
                OnInvulnerabilityChanged?.Invoke(true);
            }
        }

        private IEnumerator InvulnerabilityRoutine(float duration)
        {
            _isInvulnerable = true;
            OnInvulnerabilityChanged?.Invoke(true);

            float elapsed = 0f;
            Color originalColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (_spriteRenderer != null)
                {
                    // Flash alpha between 0.25 and 1.0
                    float alpha = (Mathf.Sin(elapsed * flashFrequency * Mathf.PI * 2f) > 0f) ? 1f : 0.25f;
                    Color c = originalColor;
                    c.a = alpha;
                    _spriteRenderer.color = c;
                }

                yield return null;
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = originalColor;
            }

            _isInvulnerable = false;
            _invulnerabilityRoutine = null;
            OnInvulnerabilityChanged?.Invoke(false);
        }

        // ── Healing & Restoration ─────────────────────────────────────────────

        /// <summary>
        /// Restores health up to maxHealth.
        /// </summary>
        public void Heal(int amount)
        {
            if (_isKnockedOut || amount <= 0 || _currentHealth >= maxHealth)
            {
                return;
            }

            int prev = _currentHealth;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            int gained = _currentHealth - prev;

            if (gained > 0)
            {
                OnHealthChanged?.Invoke(_currentHealth, maxHealth);
                OnHealed?.Invoke(gained);
            }
        }

        /// <summary>
        /// Fully restores health to maxHealth and clears knockout/invulnerability states.
        /// </summary>
        public void RestoreFullHealth()
        {
            _currentHealth = maxHealth;
            _isKnockedOut = false;

            if (_motor != null)
            {
                _motor.InputEnabled = true;
            }

            if (_invulnerabilityRoutine != null)
            {
                StopCoroutine(_invulnerabilityRoutine);
                _invulnerabilityRoutine = null;
            }

            _isInvulnerable = false;
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = 1f;
                _spriteRenderer.color = c;
            }

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            OnInvulnerabilityChanged?.Invoke(false);
        }

        private void TriggerKnockout()
        {
            _isKnockedOut = true;
            if (_motor != null)
            {
                _motor.InputEnabled = false;
            }
            if (_dash != null)
            {
                _dash.CancelDash();
            }

            OnKnockedOut?.Invoke();
        }
    }
}
