using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Base hazard component. Deals damage and knockback on contact with Gemma.
    /// Respects Gemma's dash immunity.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public class Hazard : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Amount of damage dealt to Gemma on contact.")]
        [Range(1, 5)]
        [SerializeField] private int damage = 1;

        [Tooltip("Additional knockback multiplier specific to this hazard.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float knockbackMultiplier = 1.0f;

        [Header("Visual Feedback")]
        [Tooltip("Whether this hazard performs an ambient danger pulse.")]
        [SerializeField] private bool enableDangerPulse = true;

        [Tooltip("Danger pulse color (ominous red).")]
        [SerializeField] private Color pulseDangerColor = new Color(1.0f, 0.28f, 0.35f, 1f);

        [Tooltip("Base ambient color of the hazard.")]
        [SerializeField] private Color baseHazardColor = new Color(0.9f, 0.35f, 0.45f, 0.95f);

        [Tooltip("Frequency of danger pulsation.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float pulseFrequency = 2.4f;

        protected Collider2D _collider;
        protected SpriteRenderer _spriteRenderer;
        protected float _timeOffset;

        public int Damage => damage;

        protected virtual void Awake()
        {
            EnsureComponents();
            _timeOffset = Random.Range(0f, 5f);

            int hazardLayer = LayerMask.NameToLayer("Hazard");
            if (hazardLayer >= 0) gameObject.layer = hazardLayer;
        }

        protected void EnsureComponents()
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
                if (_collider != null) _collider.isTrigger = true;
            }

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        protected virtual void Update()
        {
            if (enableDangerPulse && _spriteRenderer != null)
            {
                float t = 0.5f + 0.5f * Mathf.Sin((Time.time + _timeOffset) * pulseFrequency);
                _spriteRenderer.color = Color.Lerp(baseHazardColor, pulseDangerColor, t);
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            HandleContact(other);
        }

        protected virtual void HandleContact(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponentInChildren<PlayerHealth>();
            if (health == null || health.IsKnockedOut || health.IsInvulnerable)
            {
                return;
            }

            var dash = other.GetComponentInParent<GemmaDash>() ?? other.GetComponentInChildren<GemmaDash>();
            if (dash != null && dash.IsDashing)
            {
                // Dashing grants temporary hazard immunity
                return;
            }

            Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
            if (knockbackDir.sqrMagnitude < 0.001f) knockbackDir = Vector2.up;

            health.TakeDamage(damage, knockbackDir * knockbackMultiplier);
        }
    }
}
