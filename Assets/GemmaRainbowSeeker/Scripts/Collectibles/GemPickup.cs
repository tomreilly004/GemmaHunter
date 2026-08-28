using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Represents a collectible rainbow gem pickup in the world.
    /// Manages trigger interaction, collection attempt through RainbowProgress / GameSession,
    /// disable/reactivate lifecycle upon checkpoint restore, and rejection feedback on mismatch.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class GemPickup : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Gem Identity")]
        [Tooltip("The rainbow colour of this gem.")]
        [SerializeField] private RainbowColour colour = RainbowColour.Red;

        [Header("Components")]
        [Tooltip("Visual component handling animation, tinting and marker display.")]
        [SerializeField] private GemVisual visual;

        [Tooltip("Trigger collider on this pickup.")]
        [SerializeField] private Collider2D triggerCollider;

        [Tooltip("Optional burst effect prefab spawned upon correct collection.")]
        [SerializeField] private GameObject burstEffectPrefab;

        [Header("Tuning")]
        [Tooltip("Cooldown period in seconds before a wrong attempt penalty can be triggered again on this gem.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float wrongAttemptCooldown = 1.0f;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _isCollected;
        private bool _wasBanked;
        private float _rejectionCooldownTimer;
        private GameSession _session;
        private RainbowProgress _injectedProgress;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<GemPickup> OnCollected;
        public event Action<GemPickup> OnWrongAttempt;

        // ── Properties ────────────────────────────────────────────────────────
        public RainbowColour Colour
        {
            get => colour;
            set
            {
                colour = value;
                if (visual != null) visual.ApplyColour(colour);
            }
        }

        public bool IsCollected => _isCollected;
        public bool WasBanked => _wasBanked;
        public bool IsOnRejectionCooldown => _rejectionCooldownTimer > 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureComponents();

            int gemLayer = LayerMask.NameToLayer("Gem");
            if (gemLayer >= 0) gameObject.layer = gemLayer;

            if (visual != null) visual.ApplyColour(colour);
        }

        private void EnsureComponents()
        {
            if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
            if (visual == null) visual = GetComponentInChildren<GemVisual>();
        }

        private void Start()
        {
            ConnectToSession(GameSession.Active);
        }

        private void OnDestroy()
        {
            DisconnectFromSession();
        }

        private void Update()
        {
            if (_rejectionCooldownTimer > 0f)
            {
                _rejectionCooldownTimer -= Time.deltaTime;
            }
        }

        // ── Setup & Wiring ────────────────────────────────────────────────────

        public void Initialize(RainbowColour newColour, GameSession session = null, RainbowProgress progress = null)
        {
            Colour = newColour;
            _injectedProgress = progress;
            if (session != null)
            {
                ConnectToSession(session);
            }
            Reactivate();
        }

        public void ConnectToSession(GameSession session)
        {
            if (_session != null) DisconnectFromSession();

            _session = session;
            if (_session != null)
            {
                _session.OnProgressBanked   += HandleProgressBanked;
                _session.OnProgressRestored += HandleProgressRestored;
                _session.OnProgressReset    += HandleProgressReset;
            }
        }

        private void DisconnectFromSession()
        {
            if (_session != null)
            {
                _session.OnProgressBanked   -= HandleProgressBanked;
                _session.OnProgressRestored -= HandleProgressRestored;
                _session.OnProgressReset    -= HandleProgressReset;
                _session = null;
            }
        }

        // ── Trigger Overlap ───────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleOverlap(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleOverlap(other);
        }

        private void HandleOverlap(Collider2D other)
        {
            if (_isCollected || _rejectionCooldownTimer > 0f) return;

            // Check if collider belongs to Gemma (Player tag or layer)
            if (!other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                if (other.GetComponentInParent<GemmaMotor2D>() == null)
                    return;
            }

            Vector2 pushDir = (transform.position - other.transform.position).normalized;
            if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector2.up;

            AttemptPickup(other.gameObject, pushDir);
        }

        /// <summary>
        /// Executes a pickup attempt by the given player object.
        /// Returns true if the collection was correct and successful.
        /// </summary>
        public bool AttemptPickup(GameObject playerObj, Vector2 pushDir)
        {
            if (_isCollected || _rejectionCooldownTimer > 0f) return false;

            EnsureComponents();

            // Resolve GameSession or injected RainbowProgress
            GameSession session = _session ?? GameSession.Active;
            bool success = false;

            if (session != null)
            {
                success = session.TryCollectGem(colour);
            }
            else if (_injectedProgress != null)
            {
                success = _injectedProgress.TryCollect(colour);
            }

            if (success)
            {
                // Correct collection
                _isCollected = true;
                _wasBanked = false;

                // Disable trigger immediately to prevent double collection
                if (triggerCollider != null) triggerCollider.enabled = false;

                // 1. Quick gem scale-up before disappearing
                if (visual != null)
                {
                    visual.PlayCollectAnimation(null);
                }

                // 2. Gemma trail bright flash in this gem's colour for 0.6 seconds
                Color gemColor = RainbowColourHelper.GetColor(colour);
                if (playerObj != null)
                {
                    var trail = playerObj.GetComponentInParent<GemmaTrail>() ?? playerObj.GetComponentInChildren<GemmaTrail>();
                    if (trail != null)
                    {
                        trail.SetTrailColour(gemColor, 0.6f);
                    }
                }

                // 3. Spawn colour-matched particle burst
                SpawnBurstEffect();

                // 4. Brief, subtle camera impulse
                CameraShake2D.Instance?.TriggerShake(0.08f, 0.12f);

                // 5. Score number popup that travels toward HUD
                int pts = 100;
                int multiplier = 1;
                if (session != null)
                {
                    int basePts = session.LevelDefinition != null ? session.LevelDefinition.CorrectGemBasePoints : 100;
                    multiplier = session.RushController != null ? session.RushController.Multiplier : 1;
                    pts = basePts * multiplier;
                }
                FloatingScorePopup.Spawn(transform.position, pts, gemColor, multiplier);

                OnCollected?.Invoke(this);
                return true;
            }
            else
            {
                // Wrong attempt: start rejection cooldown to prevent repeated penalties
                _rejectionCooldownTimer = wrongAttemptCooldown;

                // Visual feedback: pale grey flash, recoil
                if (visual != null)
                {
                    visual.PlayWrongAttemptFeedback(pushDir);
                }

                // Specific "Wrong colour — find [TARGET]" message
                if (session != null && session.RainbowProgress != null && session.RainbowProgress.CurrentTarget.HasValue)
                {
                    var target = session.RainbowProgress.CurrentTarget.Value;
                    string targetHex = RainbowColourHelper.GetHex(target);
                    session.PostFeedbackMessage($"Wrong colour — find <color={targetHex}>{target.ToString().ToUpper()}</color>", new Color(1f, 0.45f, 0.45f, 1f));
                }

                OnWrongAttempt?.Invoke(this);
                return false;
            }
        }

        private void SpawnBurstEffect()
        {
            if (burstEffectPrefab != null && Application.isPlaying)
            {
                var burstObj = Instantiate(burstEffectPrefab, transform.position, Quaternion.identity);
                var burstComp = burstObj.GetComponent<GemBurstEffect>();
                if (burstComp != null)
                {
                    burstComp.Play(RainbowColourHelper.GetColor(colour));
                }
            }
        }

        // ── Bank / Restore Progress Event Handlers ────────────────────────────

        public void HandleProgressBanked()
        {
            if (_isCollected)
            {
                _wasBanked = true;
            }
        }

        public void HandleProgressRestored()
        {
            // If collected AFTER the last bank, restore and reactivate this gem
            if (_isCollected && !_wasBanked)
            {
                Reactivate();
            }
        }

        public void HandleProgressReset()
        {
            Reactivate();
        }

        /// <summary>
        /// Reactivates this gem pickup (enables collider and visual).
        /// </summary>
        public void Reactivate()
        {
            EnsureComponents();

            _isCollected = false;
            _wasBanked = false;
            _rejectionCooldownTimer = 0f;

            if (triggerCollider != null) triggerCollider.enabled = true;
            if (visual != null)
            {
                visual.SetVisibility(true);
                visual.ApplyColour(colour);
            }
        }
    }
}
