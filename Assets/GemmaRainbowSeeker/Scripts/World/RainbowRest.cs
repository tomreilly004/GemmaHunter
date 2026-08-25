using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Rainbow Rest checkpoint altar. Automatically activates when Gemma enters its trigger,
    /// banks collected colours, updates the active checkpoint, and awards a one-time
    /// +1 HP heal and 100-point bonus on first visit.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class RainbowRest : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Checkpoint Spawn")]
        [Tooltip("Local offset from this object where Gemma will appear when restarting.")]
        [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0f);

        [Header("Visual Elements")]
        [Tooltip("Main shrine or pedestal sprite renderer.")]
        [SerializeField] private SpriteRenderer shrineRenderer;

        [Tooltip("Glow / halo aura sprite renderer that brightens when activated.")]
        [SerializeField] private SpriteRenderer auraRenderer;

        [Tooltip("Color of the aura when unactivated / dim.")]
        [SerializeField] private Color dimAuraColor = new Color(0.4f, 0.45f, 0.6f, 0.35f);

        [Tooltip("Color of the aura when fully illuminated.")]
        [SerializeField] private Color activeAuraColor = new Color(1.0f, 0.95f, 0.7f, 0.95f);

        [Tooltip("Pulsing light/glow intensity oscillation frequency.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float pulseSpeed = 2.5f;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _isActivated;
        private bool _hasAwardedFirstBonus;
        private Collider2D _trigger;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<RainbowRest> OnRestActivated;
        public event Action<RainbowRest> OnFirstActivationBonusAwarded;

        // ── Properties ────────────────────────────────────────────────────────
        public bool IsActivated => _isActivated;
        public bool HasAwardedFirstBonus => _hasAwardedFirstBonus;
        public Vector3 SpawnPosition => transform.position + (Vector3)spawnOffset;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null) _trigger.isTrigger = true;

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer >= 0) gameObject.layer = triggerLayer;

            UpdateVisuals();
        }

        private void Update()
        {
            if (_isActivated && auraRenderer != null)
            {
                // Gentle breathing pulsation on active aura
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * pulseSpeed);
                Color c = activeAuraColor;
                c.a *= pulse;
                auraRenderer.color = c;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<PlayerHealth>() ?? other.GetComponentInChildren<PlayerHealth>();
            if (health == null && !other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                return;
            }

            ActivateRest(health);
        }

        /// <summary>
        /// Activates this Rainbow Rest: banks progress, updates checkpoint, and grants first-time bonuses.
        /// </summary>
        public void ActivateRest(PlayerHealth health = null)
        {
            _isActivated = true;

            // 1. Update Checkpoint Manager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetActiveCheckpoint(this);
            }

            // 2. Bank rainbow progress
            GameSession.Active?.BankProgress();

            // 3. One-time first activation bonus (Heal 1 HP + 100 points)
            if (!_hasAwardedFirstBonus)
            {
                _hasAwardedFirstBonus = true;

                // Heal 1 health
                if (health != null)
                {
                    health.Heal(1);
                }
                else
                {
                    var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Gemma");
                    playerObj?.GetComponent<PlayerHealth>()?.Heal(1);
                }

                // Award points and record stats
                if (GameSession.Active != null)
                {
                    int pts = GameSession.Active.LevelRules != null ?
                              GameSession.Active.LevelRules.RainbowRestFirstActivationPoints : 100;
                    GameSession.Active.ScoreManager?.AddPoints(pts);
                    GameSession.Active.SessionStats?.RecordRainbowRestActivated();
                }

                OnFirstActivationBonusAwarded?.Invoke(this);
            }

            UpdateVisuals();
            OnRestActivated?.Invoke(this);
        }

        private void UpdateVisuals()
        {
            if (auraRenderer != null)
            {
                auraRenderer.color = _isActivated ? activeAuraColor : dimAuraColor;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.4f);
            Gizmos.DrawLine(transform.position, SpawnPosition);
        }
    }
}
