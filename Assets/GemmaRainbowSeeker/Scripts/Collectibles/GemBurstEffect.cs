using System.Collections;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Spawns or triggers a visual burst effect when a gem is collected.
    /// Scales up rapidly while fading out alpha, then cleans itself up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GemBurstEffect : MonoBehaviour
    {
        [Tooltip("SpriteRenderer used for the burst ring/sparkle.")]
        [SerializeField] private SpriteRenderer burstRenderer;

        [Tooltip("Total duration of the burst animation in seconds.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float duration = 0.45f;

        [Tooltip("Final scale multiplier relative to initial scale.")]
        [Range(1.5f, 5f)]
        [SerializeField] private float targetScaleMultiplier = 2.8f;

        public void Play(Color burstColor, System.Action onComplete = null)
        {
            if (burstRenderer == null) burstRenderer = GetComponent<SpriteRenderer>();
            gameObject.SetActive(true);
            StartCoroutine(BurstRoutine(burstColor, onComplete));
        }

        private IEnumerator BurstRoutine(Color color, System.Action onComplete)
        {
            Vector3 startScale = Vector3.one * 0.4f;
            Vector3 endScale = Vector3.one * targetScaleMultiplier;

            if (burstRenderer != null)
            {
                burstRenderer.sortingLayerName = "Collectibles";
                burstRenderer.sortingOrder = 5;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out scale
                float scaleEase = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, endScale, scaleEase);

                // Fade alpha out
                if (burstRenderer != null)
                {
                    Color c = color;
                    c.a = Mathf.Lerp(1.0f, 0f, t);
                    burstRenderer.color = c;
                }

                yield return null;
            }

            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}
