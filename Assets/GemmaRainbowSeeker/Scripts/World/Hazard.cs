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

        protected Collider2D _collider;

        public int Damage => damage;

        protected virtual void Awake()
        {
            EnsureComponents();

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
