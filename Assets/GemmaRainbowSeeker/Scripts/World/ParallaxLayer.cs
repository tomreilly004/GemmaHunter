using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Offsets a background element or layer based on camera movement,
    /// creating depth and a 2.5D parallax effect.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Parallax movement factor. 0 = static in world, 1 = moves completely with camera (infinite distance).")]
        [SerializeField] private Vector2 parallaxFactor = new Vector2(0.7f, 0.3f);

        [Tooltip("Optional reference camera. Defaults to Camera.main if null.")]
        [SerializeField] private Camera targetCamera;

        private Vector3 _startPosition;
        private Vector3 _startCameraPosition;
        private bool _isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            _startPosition = transform.position;
            if (targetCamera != null)
            {
                _startCameraPosition = targetCamera.transform.position;
                _isInitialized = true;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
                _startCameraPosition = targetCamera.transform.position;
                _isInitialized = true;
            }

            if (!_isInitialized)
            {
                Initialize();
            }

            Vector3 camDelta = targetCamera.transform.position - _startCameraPosition;
            Vector3 targetPos = new Vector3(
                _startPosition.x + camDelta.x * parallaxFactor.x,
                _startPosition.y + camDelta.y * parallaxFactor.y,
                _startPosition.z
            );

            transform.position = targetPos;
        }
    }
}
