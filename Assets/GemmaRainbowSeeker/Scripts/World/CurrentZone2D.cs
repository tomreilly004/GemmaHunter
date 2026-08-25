using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// A magical current zone that applies a continuous, smoothly blended directional force
    /// to Gemma while she is inside without stripping player input control.
    /// Supports Rightward, Upward-Right, Downward-Right, and Custom flow vectors.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class CurrentZone2D : MonoBehaviour
    {
        public enum FlowDirection
        {
            Right,
            UpRight,
            DownRight,
            Custom
        }

        [Header("Current Configuration")]
        [Tooltip("Preset flow direction.")]
        [SerializeField] private FlowDirection directionPreset = FlowDirection.Right;

        [Tooltip("Custom normalized direction vector when directionPreset is set to Custom.")]
        [SerializeField] private Vector2 customDirection = Vector2.right;

        [Tooltip("Force magnitude applied to bodies inside the current in units/sec^2.")]
        [Range(1f, 30f)]
        [SerializeField] private float currentStrength = 9.5f;

        [Tooltip("Maximum velocity boost the current will contribute along its flow axis.")]
        [Range(1f, 25f)]
        [SerializeField] private float maxCurrentVelocity = 8f;

        [Header("Visual Flow Animation")]
        [Tooltip("Container holding arrow visual sprites to scroll/animate along the current vector.")]
        [SerializeField] private Transform arrowsContainer;

        [Tooltip("Scroll speed of arrows inside the zone.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float arrowScrollSpeed = 2.5f;

        [Tooltip("Zone width along the local flow axis for wrapping arrow positions.")]
        [SerializeField] private float wrapDistance = 6f;

        private Collider2D _collider;
        private readonly List<Rigidbody2D> _trackedBodies = new List<Rigidbody2D>();
        private Vector2 _effectiveDirection;

        public Vector2 EffectiveDirection => _effectiveDirection;
        public float CurrentStrength => currentStrength;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null) _collider.isTrigger = true;

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer >= 0) gameObject.layer = triggerLayer;

            UpdateEffectiveDirection();
            OrientVisuals();
        }

        private void OnValidate()
        {
            UpdateEffectiveDirection();
            OrientVisuals();
        }

        public void SetDirection(FlowDirection preset, Vector2 custom = default)
        {
            directionPreset = preset;
            if (preset == FlowDirection.Custom && custom.sqrMagnitude > 0.001f)
            {
                customDirection = custom.normalized;
            }
            UpdateEffectiveDirection();
            OrientVisuals();
        }

        private void UpdateEffectiveDirection()
        {
            switch (directionPreset)
            {
                case FlowDirection.Right:
                    _effectiveDirection = Vector2.right;
                    break;
                case FlowDirection.UpRight:
                    _effectiveDirection = new Vector2(0.7071068f, 0.7071068f);
                    break;
                case FlowDirection.DownRight:
                    _effectiveDirection = new Vector2(0.7071068f, -0.7071068f);
                    break;
                case FlowDirection.Custom:
                    _effectiveDirection = customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector2.right;
                    break;
            }
        }

        private void OrientVisuals()
        {
            if (arrowsContainer != null && _effectiveDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(_effectiveDirection.y, _effectiveDirection.x) * Mathf.Rad2Deg;
                arrowsContainer.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void Update()
        {
            // Animate arrow visuals scrolling along local X axis
            if (arrowsContainer != null)
            {
                for (int i = 0; i < arrowsContainer.childCount; i++)
                {
                    Transform child = arrowsContainer.GetChild(i);
                    Vector3 lp = child.localPosition;
                    lp.x += arrowScrollSpeed * Time.deltaTime;

                    if (lp.x > wrapDistance * 0.5f)
                    {
                        lp.x -= wrapDistance;
                    }
                    child.localPosition = lp;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null && !_trackedBodies.Contains(rb))
            {
                _trackedBodies.Add(rb);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                _trackedBodies.Remove(rb);
            }
        }

        private void FixedUpdate()
        {
            // Smoothly apply directional force to all bodies currently inside
            for (int i = _trackedBodies.Count - 1; i >= 0; i--)
            {
                var rb = _trackedBodies[i];
                if (rb == null)
                {
                    _trackedBodies.RemoveAt(i);
                    continue;
                }

                // Smooth continuous force blending
                float velAlongFlow = Vector2.Dot(rb.linearVelocity, _effectiveDirection);
                if (velAlongFlow < maxCurrentVelocity)
                {
                    // Apply acceleration smoothly without snapping
                    rb.AddForce(_effectiveDirection * currentStrength, ForceMode2D.Force);
                }
            }
        }

        private void OnDrawGizmos()
        {
            UpdateEffectiveDirection();
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
            Gizmos.DrawRay(transform.position, (Vector3)_effectiveDirection * 2f);
        }
    }
}
