using System;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Predictable patrolling enemy for Gemma Beaker: Rainbow Seeker.
    /// Moves back and forth between two waypoints, pausing at endpoints,
    /// with a visible patrol path. Contact damages Gemma and resets Rainbow Rush.
    /// Does not collect or block gems.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class Gloomling : Hazard
    {
        [Header("Patrol Path")]
        [Tooltip("Starting patrol offset relative to spawn position.")]
        [SerializeField] private Vector2 patrolOffsetA = Vector2.zero;

        [Tooltip("Ending patrol offset relative to spawn position.")]
        [SerializeField] private Vector2 patrolOffsetB = new Vector2(0f, 5f);

        [Header("Patrol Timing")]
        [Tooltip("Time in seconds to travel between endpoints.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float travelDuration = 2.8f;

        [Tooltip("Pause duration in seconds at each endpoint.")]
        [Range(0.0f, 3f)]
        [SerializeField] private float pauseDuration = 0.45f;

        [Header("Visual Styling")]
        [Tooltip("Gloomling tint color.")]
        [SerializeField] private Color gloomColor = new Color(0.55f, 0.2f, 0.75f, 1f);

        [Tooltip("Optional LineRenderer used to render the patrol route.")]
        [SerializeField] private LineRenderer routeRenderer;

        private Vector3 _startWorldPos;
        private Vector3 _worldPointA;
        private Vector3 _worldPointB;
        private float _cycleTime;
        private float _pauseTimer;
        private bool _movingToB = true;

        public Vector3 WorldPointA => _worldPointA;
        public Vector3 WorldPointB => _worldPointB;

        protected override void Awake()
        {
            base.Awake();
            _startWorldPos = transform.position;
            _worldPointA = _startWorldPos + (Vector3)patrolOffsetA;
            _worldPointB = _startWorldPos + (Vector3)patrolOffsetB;

            SetupVisuals();
            SetupRouteRenderer();
        }

        private void SetupVisuals()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = gloomColor;
                _spriteRenderer.sortingLayerName = "Gameplay";
                _spriteRenderer.sortingOrder = 2;
            }
        }

        private void SetupRouteRenderer()
        {
            if (routeRenderer == null)
            {
                routeRenderer = GetComponent<LineRenderer>();
            }

            if (routeRenderer != null)
            {
                routeRenderer.positionCount = 2;
                routeRenderer.useWorldSpace = true;
                routeRenderer.SetPosition(0, _worldPointA);
                routeRenderer.SetPosition(1, _worldPointB);
                routeRenderer.startWidth = 0.08f;
                routeRenderer.endWidth = 0.08f;
                routeRenderer.startColor = new Color(0.6f, 0.3f, 0.9f, 0.35f);
                routeRenderer.endColor = new Color(0.6f, 0.3f, 0.9f, 0.35f);
                routeRenderer.sortingLayerName = "GameplayBack";
                routeRenderer.sortingOrder = -2;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            _cycleTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_cycleTime / travelDuration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 from = _movingToB ? _worldPointA : _worldPointB;
            Vector3 to   = _movingToB ? _worldPointB : _worldPointA;

            transform.position = Vector3.Lerp(from, to, eased);

            // Subtle bob / squish
            if (_spriteRenderer != null)
            {
                float squish = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
                _spriteRenderer.transform.localScale = new Vector3(squish, 2f - squish, 1f);
            }

            if (progress >= 1f)
            {
                _movingToB = !_movingToB;
                _cycleTime = 0f;
                _pauseTimer = pauseDuration;
            }
        }

        public void ConfigurePatrol(Vector2 offsetA, Vector2 offsetB, float duration = 2.8f, float pause = 0.45f)
        {
            patrolOffsetA = offsetA;
            patrolOffsetB = offsetB;
            travelDuration = duration;
            pauseDuration = pause;

            _startWorldPos = transform.position;
            _worldPointA = _startWorldPos + (Vector3)patrolOffsetA;
            _worldPointB = _startWorldPos + (Vector3)patrolOffsetB;

            SetupRouteRenderer();
        }

        public void ResetToStart()
        {
            _cycleTime = 0f;
            _pauseTimer = 0f;
            _movingToB = true;
            transform.position = _worldPointA;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = Application.isPlaying ? _startWorldPos : transform.position;
            Vector3 a = origin + (Vector3)patrolOffsetA;
            Vector3 b = origin + (Vector3)patrolOffsetB;

            Gizmos.color = new Color(0.8f, 0.3f, 1f, 0.85f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.4f);
            Gizmos.DrawWireSphere(b, 0.4f);
        }
    }
}
