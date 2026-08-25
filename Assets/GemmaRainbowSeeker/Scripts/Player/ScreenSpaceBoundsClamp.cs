using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Prevents Gemma from leaving the camera viewport or escaping the playable world bounds.
    /// Clamps position in LateUpdate after camera and physics updates have completed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenSpaceBoundsClamp : MonoBehaviour
    {
        [Header("Camera Viewport Protection")]
        [Tooltip("Whether to clamp Gemma within the visible camera viewport.")]
        [SerializeField] private bool clampToViewport = true;

        [Tooltip("Viewport margin (0 to 0.5) from camera screen edges.")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float viewportMargin = 0.04f;

        [Header("World Play Area Bounds (Optional)")]
        [Tooltip("Optional collider defining the outer boundaries of the level.")]
        [SerializeField] private Collider2D worldBoundsCollider;

        private Camera _cachedCamera;
        private Rigidbody2D _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            Vector3 pos = transform.position;

            // 1. Clamp to Camera Viewport
            if (clampToViewport)
            {
                if (_cachedCamera == null)
                {
                    _cachedCamera = Camera.main;
                }

                if (_cachedCamera != null && _cachedCamera.orthographic)
                {
                    Vector3 vp = _cachedCamera.WorldToViewportPoint(pos);
                    float clampedX = Mathf.Clamp(vp.x, viewportMargin, 1f - viewportMargin);
                    float clampedY = Mathf.Clamp(vp.y, viewportMargin, 1f - viewportMargin);

                    if (Mathf.Abs(vp.x - clampedX) > 0.0001f || Mathf.Abs(vp.y - clampedY) > 0.0001f)
                    {
                        vp.x = clampedX;
                        vp.y = clampedY;
                        Vector3 worldPos = _cachedCamera.ViewportToWorldPoint(vp);
                        worldPos.z = pos.z;
                        pos = worldPos;

                        if (_rb != null)
                        {
                            _rb.position = pos;
                        }
                        else
                        {
                            transform.position = pos;
                        }
                    }
                }
            }

            // 2. Clamp to World Bounds Collider (if provided)
            if (worldBoundsCollider != null)
            {
                Bounds b = worldBoundsCollider.bounds;
                float clampedX = Mathf.Clamp(pos.x, b.min.x, b.max.x);
                float clampedY = Mathf.Clamp(pos.y, b.min.y, b.max.y);

                if (Mathf.Abs(pos.x - clampedX) > 0.001f || Mathf.Abs(pos.y - clampedY) > 0.001f)
                {
                    pos.x = clampedX;
                    pos.y = clampedY;
                    if (_rb != null)
                    {
                        _rb.position = pos;
                    }
                    else
                    {
                        transform.position = pos;
                    }
                }
            }
        }
    }
}
