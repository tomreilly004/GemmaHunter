using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Pursuing enemy for Level 10 of Gemma Beaker: Rainbow Seeker.
    /// Activates when Gemma enters its clearly displayed detection radius.
    /// Chases more slowly than Gemma's base swim speed (3.4 u/s vs 5.8 u/s).
    /// Gives up after limited duration/distance, and cannot enter Rainbow Rest or Gate safe zones.
    /// Contact removes one heart and resets Rainbow Rush.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class StormChaser : Hazard
    {
        public enum ChaserState
        {
            Idle,
            Chasing,
            Returning
        }

        [Header("Detection & Speed")]
        [Tooltip("Radius at which the Storm Chaser detects Gemma.")]
        [Range(3f, 15f)]
        [SerializeField] private float detectionRadius = 7.0f;

        [Tooltip("Chase movement speed in units per second (slower than Gemma's base max 5.8 u/s).")]
        [Range(1f, 5.5f)]
        [SerializeField] private float chaseSpeed = 3.4f;

        [Tooltip("Return speed when retreating to home position.")]
        [Range(1f, 6f)]
        [SerializeField] private float returnSpeed = 4.0f;

        [Header("Leash & Limits")]
        [Tooltip("Maximum duration in seconds the Storm Chaser will pursue before giving up.")]
        [Range(2f, 12f)]
        [SerializeField] private float maxChaseDuration = 5.0f;

        [Tooltip("Maximum distance from home position before giving up chase.")]
        [Range(5f, 25f)]
        [SerializeField] private float maxLeashDistance = 14.0f;

        [Tooltip("Safe zone clearance distance from Rainbow Rests and Rainbow Gates.")]
        [Range(3f, 10f)]
        [SerializeField] private float safeZoneClearance = 6.0f;

        [Header("Visual Feedback")]
        [Tooltip("LineRenderer used to display the detection boundary aura.")]
        [SerializeField] private LineRenderer detectionRing;

        [Tooltip("Color of the detection ring when idle.")]
        [SerializeField] private Color ringIdleColor = new Color(0.3f, 0.7f, 1.0f, 0.35f);

        [Tooltip("Color of the detection ring when chasing.")]
        [SerializeField] private Color ringChaseColor = new Color(1.0f, 0.3f, 0.4f, 0.6f);

        [Tooltip("Base tint of the Storm Chaser.")]
        [SerializeField] private Color chaserColor = new Color(0.2f, 0.35f, 0.65f, 1f);

        private Vector3 _homePosition;
        private ChaserState _state = ChaserState.Idle;
        private float _chaseTimer;
        private Transform _targetPlayer;
        private RainbowRest[] _cachedRests;
        private RainbowGate _cachedGate;

        public ChaserState State => _state;
        public float DetectionRadius => detectionRadius;
        public float ChaseSpeed => chaseSpeed;

        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = chaserColor;
                _spriteRenderer.sortingLayerName = "Gameplay";
                _spriteRenderer.sortingOrder = 3;
            }

            SetupDetectionRing();
        }

        private void SetupDetectionRing()
        {
            if (detectionRing == null)
            {
                detectionRing = GetComponent<LineRenderer>();
            }

            if (detectionRing != null)
            {
                int segments = 40;
                detectionRing.positionCount = segments + 1;
                detectionRing.useWorldSpace = false;
                detectionRing.startWidth = 0.07f;
                detectionRing.endWidth = 0.07f;
                detectionRing.startColor = ringIdleColor;
                detectionRing.endColor = ringIdleColor;
                detectionRing.sortingLayerName = "GameplayBack";
                detectionRing.sortingOrder = -1;

                float angleStep = 360f / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float rad = Mathf.Deg2Rad * (i * angleStep);
                    float x = Mathf.Cos(rad) * detectionRadius;
                    float y = Mathf.Sin(rad) * detectionRadius;
                    detectionRing.SetPosition(i, new Vector3(x, y, 0f));
                }
            }
        }

        private void Start()
        {
            FindPlayer();
            _cachedRests = UnityEngine.Object.FindObjectsByType<RainbowRest>(FindObjectsSortMode.None);
            _cachedGate = UnityEngine.Object.FindFirstObjectByType<RainbowGate>();
        }

        private void FindPlayer()
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                _targetPlayer = gemma.transform;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_targetPlayer == null)
            {
                FindPlayer();
                if (_targetPlayer == null) return;
            }

            float distToPlayer = Vector3.Distance(transform.position, _targetPlayer.position);
            float distToHome = Vector3.Distance(transform.position, _homePosition);

            switch (_state)
            {
                case ChaserState.Idle:
                    // Gentle ambient hover
                    float hoverY = _homePosition.y + Mathf.Sin(Time.time * 2.5f) * 0.4f;
                    transform.position = new Vector3(_homePosition.x, hoverY, _homePosition.z);

                    if (distToPlayer <= detectionRadius && !IsInSafeZone(_targetPlayer.position))
                    {
                        _state = ChaserState.Chasing;
                        _chaseTimer = 0f;
                        SetRingColor(ringChaseColor);
                    }
                    break;

                case ChaserState.Chasing:
                    _chaseTimer += Time.deltaTime;

                    // Check give-up conditions
                    bool exceededTime = _chaseTimer >= maxChaseDuration;
                    bool exceededLeash = distToHome >= maxLeashDistance;
                    bool playerTooFar = distToPlayer > (detectionRadius + 3.5f);
                    bool enteredSafeZone = IsNearSafeZone(transform.position);

                    if (exceededTime || exceededLeash || playerTooFar || enteredSafeZone)
                    {
                        _state = ChaserState.Returning;
                        SetRingColor(ringIdleColor);
                        break;
                    }

                    // Move toward player
                    Vector3 moveDir = (_targetPlayer.position - transform.position).normalized;
                    transform.position += moveDir * chaseSpeed * Time.deltaTime;

                    // Visual tilt
                    if (_spriteRenderer != null)
                    {
                        float tilt = Mathf.Sin(Time.time * 8f) * 6f;
                        _spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
                    }
                    break;

                case ChaserState.Returning:
                    Vector3 returnDir = (_homePosition - transform.position).normalized;
                    transform.position += returnDir * returnSpeed * Time.deltaTime;

                    if (Vector3.Distance(transform.position, _homePosition) < 0.2f)
                    {
                        transform.position = _homePosition;
                        _state = ChaserState.Idle;
                        _chaseTimer = 0f;
                    }
                    break;
            }
        }

        private bool IsNearSafeZone(Vector3 pos)
        {
            if (_cachedRests != null)
            {
                foreach (var r in _cachedRests)
                {
                    if (r != null && Vector3.Distance(pos, r.transform.position) < safeZoneClearance)
                    {
                        return true;
                    }
                }
            }

            if (_cachedGate != null && Vector3.Distance(pos, _cachedGate.transform.position) < safeZoneClearance)
            {
                return true;
            }

            return false;
        }

        private bool IsInSafeZone(Vector3 pos)
        {
            return IsNearSafeZone(pos);
        }

        private void SetRingColor(Color c)
        {
            if (detectionRing != null)
            {
                detectionRing.startColor = c;
                detectionRing.endColor = c;
            }
        }

        public void ResetToHome()
        {
            transform.position = _homePosition;
            _state = ChaserState.Idle;
            _chaseTimer = 0f;
            SetRingColor(ringIdleColor);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _homePosition : transform.position;
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireSphere(origin, detectionRadius);

            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(origin, maxLeashDistance);
        }
    }
}
