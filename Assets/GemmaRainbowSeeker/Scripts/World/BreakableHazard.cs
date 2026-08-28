using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// A cracked magical hazard that damages Gemma on normal contact,
    /// but can be destroyed when dashed through, awarding score and participating
    /// in checkpoint restoration.
    /// </summary>
    public sealed class BreakableHazard : Hazard
    {
        [Header("Breakable Tuning")]
        [Tooltip("Points awarded when destroyed by a dash.")]
        [SerializeField] private int scoreAward = 50;

        [Tooltip("Optional break effect prefab instantiated when broken.")]
        [SerializeField] private GameObject breakEffectPrefab;

        [Header("Visual Components")]
        [Tooltip("Main sprite renderer representing the hazard.")]
        [SerializeField] private SpriteRenderer mainRenderer;

        [Header("Breakable Visuals")]
        [Tooltip("Color of the glowing cracked cloud.")]
        [SerializeField] private Color crackedBaseColor = new Color(0.65f, 0.35f, 0.85f, 0.95f);

        [Tooltip("Glow color of the active cracks.")]
        [SerializeField] private Color crackGlowColor = new Color(0.95f, 0.55f, 1.0f, 1.0f);

        [Tooltip("Frequency of crack glow pulsing.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float crackPulseSpeed = 3.2f;

        private bool _isBroken;
        private bool _wasBanked;
        private GameSession _session;

        public bool IsBroken => _isBroken;
        public bool WasBanked => _wasBanked;

        public event Action<BreakableHazard> OnHazardBroken;
        public event Action<BreakableHazard> OnHazardRestored;

        protected override void Awake()
        {
            base.Awake();
            EnsureBreakableComponents();
            _timeOffset = UnityEngine.Random.Range(0f, 5f);
        }

        protected override void Update()
        {
            if (!_isBroken && mainRenderer != null)
            {
                // Pulsing glowing purple/magenta cracks
                float t = 0.5f + 0.5f * Mathf.Sin((Time.time + _timeOffset) * crackPulseSpeed);
                mainRenderer.color = Color.Lerp(crackedBaseColor, crackGlowColor, t);
            }
        }

        private void EnsureBreakableComponents()
        {
            EnsureComponents();
            if (mainRenderer == null) mainRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            ConnectToSession(GameSession.Active);
        }

        private void OnDestroy()
        {
            DisconnectFromSession();
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

        protected override void HandleContact(Collider2D other)
        {
            if (_isBroken) return;

            var dash = other.GetComponentInParent<GemmaDash>() ?? other.GetComponentInChildren<GemmaDash>();
            if (dash != null && dash.IsDashing)
            {
                // Dashed into cracked hazard -> Break it!
                BreakHazard();
                return;
            }

            // Normal contact without dashing -> Deal damage
            base.HandleContact(other);
        }

        /// <summary>
        /// Breaks this hazard: awards score, increments stats, disables visuals/collider.
        /// </summary>
        public void BreakHazard()
        {
            if (_isBroken) return;

            EnsureBreakableComponents();

            _isBroken = true;
            _wasBanked = false;

            // Award points
            var session = _session ?? GameSession.Active;
            if (session != null)
            {
                int pts = session.LevelRules != null ? session.LevelRules.HazardBreakPoints : scoreAward;
                session.ScoreManager?.AddPoints(pts);
                session.SessionStats?.RecordHazardBroken();
            }

            // Disable collider and visuals
            if (_collider != null) _collider.enabled = false;
            if (mainRenderer != null) mainRenderer.enabled = false;

            // Spawn break effect
            if (breakEffectPrefab != null && Application.isPlaying)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            OnHazardBroken?.Invoke(this);
        }

        /// <summary>
        /// Restores and reactivates this hazard.
        /// </summary>
        public void RestoreHazard()
        {
            EnsureBreakableComponents();

            _isBroken = false;
            _wasBanked = false;

            if (_collider != null) _collider.enabled = true;
            if (mainRenderer != null) mainRenderer.enabled = true;

            OnHazardRestored?.Invoke(this);
        }

        private void HandleProgressBanked()
        {
            if (_isBroken)
            {
                _wasBanked = true;
            }
        }

        private void HandleProgressRestored()
        {
            // If broken AFTER the last bank, restore it
            if (_isBroken && !_wasBanked)
            {
                RestoreHazard();
            }
        }

        private void HandleProgressReset()
        {
            RestoreHazard();
        }
    }
}
