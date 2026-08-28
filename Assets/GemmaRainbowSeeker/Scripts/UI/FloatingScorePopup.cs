using System.Collections;
using TMPro;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Displays a floating score number (e.g. "+125") that pops in,
    /// travels upward toward the HUD, and fades out smoothly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingScorePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private float travelDistance = 1.6f;

        public static FloatingScorePopup Spawn(Vector3 worldPosition, int points, Color textColor, int multiplier = 1)
        {
            if (!Application.isPlaying) return null;

            var go = new GameObject("ScorePopup");
            go.transform.position = worldPosition;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 5.5f;
            tmp.sortingLayerID = SortingLayer.NameToID("Foreground");
            tmp.sortingOrder = 50;

            string text = multiplier > 1 ? $"+{points:N0} <size=75%>(x{multiplier})</size>" : $"+{points:N0}";
            tmp.text = text;
            tmp.color = textColor;

            var popup = go.AddComponent<FloatingScorePopup>();
            popup.textMesh = tmp;
            popup.Play(textColor);
            return popup;
        }

        public static FloatingScorePopup Spawn(Vector3 worldPosition, int points, Color textColor, float multiplier)
        {
            return Spawn(worldPosition, points, textColor, (int)Mathf.Round(multiplier));
        }

        public void Play(Color color)
        {
            StartCoroutine(AnimateRoutine(color));
        }

        private IEnumerator AnimateRoutine(Color color)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + new Vector3(0.5f, travelDistance, 0f);
            Vector3 startScale = Vector3.one * 0.4f;
            Vector3 peakScale = Vector3.one * 1.15f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Movement ease-out
                float moveT = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = Vector3.Lerp(startPos, endPos, moveT);

                // Scale pop then settle
                float scaleT = t < 0.25f ? t / 0.25f : 1f - (t - 0.25f) / 0.75f;
                transform.localScale = t < 0.25f ? Vector3.Lerp(startScale, peakScale, scaleT) : Vector3.Lerp(peakScale, Vector3.one, (t - 0.25f) / 0.75f);

                // Fade alpha out during second half
                if (textMesh != null)
                {
                    float alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                    Color c = color;
                    c.a = alpha;
                    textMesh.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
