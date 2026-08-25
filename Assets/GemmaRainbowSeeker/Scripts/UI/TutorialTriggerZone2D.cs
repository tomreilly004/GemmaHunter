using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Trigger volume that queues a tutorial banner when Gemma enters the area.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class TutorialTriggerZone2D : MonoBehaviour
    {
        [Header("Tutorial Message")]
        [SerializeField] private string title = "TUTORIAL";
        [TextArea(2, 4)]
        [SerializeField] private string body = "Message text here.";
        [SerializeField] private string controlsHint = "";
        [SerializeField] private float autoDismissDuration = 6.0f;
        [SerializeField] private bool triggerOnce = true;

        private bool _hasTriggered;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer >= 0) gameObject.layer = triggerLayer;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerOnce && _hasTriggered) return;

            var motor = other.GetComponentInParent<GemmaMotor2D>() ?? other.GetComponentInChildren<GemmaMotor2D>();
            if (motor == null && !other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                return;
            }

            _hasTriggered = true;

            var banner = Object.FindFirstObjectByType<TutorialBanner>();
            if (banner != null)
            {
                banner.QueueMessage(new TutorialBanner.TutorialMessage(title, body, controlsHint, autoDismissDuration));
            }
        }
    }
}
