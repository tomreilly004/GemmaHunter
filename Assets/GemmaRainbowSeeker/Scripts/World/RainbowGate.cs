using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// The Rainbow Gate exit portal at the end of the level.
    /// Remains locked until all 7 rainbow colours are gathered in order.
    /// Displays missing colour feedback if touched early.
    /// Unlocks with a visual opening animation when RainbowProgress is complete.
    /// On entry: banks final progress, disables player control, stops the timer,
    /// and invokes level completion.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class RainbowGate : MonoBehaviour
    {
        [Header("Visual Components")]
        [Tooltip("Arch/gate framework sprite renderer.")]
        [SerializeField] private SpriteRenderer archRenderer;

        [Tooltip("Locked barrier / crystal sprite renderer in gate center.")]
        [SerializeField] private SpriteRenderer barrierRenderer;

        [Tooltip("Radiant rainbow portal glow sprite renderer.")]
        [SerializeField] private SpriteRenderer portalGlowRenderer;

        [Tooltip("Optional gate label text.")]
        [SerializeField] private TextMeshPro gateLabelText;

        [Header("Gate Styling")]
        [SerializeField] private Color lockedBarrierColor = new Color(0.4f, 0.45f, 0.55f, 0.85f);
        [SerializeField] private Color unlockedPortalColor = new Color(1.0f, 0.95f, 0.6f, 0.95f);

        private Collider2D _trigger;
        private bool _isUnlocked;
        private bool _isEntered;
        private Coroutine _openRoutine;
        private float _earlyEntryCooldownTimer;

        public bool IsUnlocked => _isUnlocked;
        public bool IsEntered => _isEntered;

        public event Action OnGateUnlocked;
        public event Action OnGateEntered;

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            if (_trigger != null) _trigger.isTrigger = true;

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer >= 0) gameObject.layer = triggerLayer;

            SetLockedStateVisuals();
        }

        private void Start()
        {
            if (GameSession.Active != null)
            {
                GameSession.Active.OnRainbowCompleted += UnlockGate;
                if (GameSession.Active.RainbowProgress != null && GameSession.Active.RainbowProgress.IsComplete)
                {
                    UnlockGate();
                }
            }
        }

        private void OnDestroy()
        {
            if (GameSession.Active != null)
            {
                GameSession.Active.OnRainbowCompleted -= UnlockGate;
            }
        }

        private void Update()
        {
            if (_earlyEntryCooldownTimer > 0f)
            {
                _earlyEntryCooldownTimer -= Time.deltaTime;
            }

            if (_isUnlocked && portalGlowRenderer != null)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 4f);
                Color c = unlockedPortalColor;
                c.a *= pulse;
                portalGlowRenderer.color = c;
            }
        }

        private void SetLockedStateVisuals()
        {
            _isUnlocked = false;
            _isEntered = false;

            if (barrierRenderer != null)
            {
                barrierRenderer.gameObject.SetActive(true);
                barrierRenderer.color = lockedBarrierColor;
            }

            if (portalGlowRenderer != null)
            {
                portalGlowRenderer.gameObject.SetActive(false);
            }

            if (gateLabelText != null)
            {
                gateLabelText.text = "LOCKED";
                gateLabelText.color = new Color(0.8f, 0.4f, 0.4f, 1f);
            }
        }

        public void UnlockGate()
        {
            if (_isUnlocked) return;
            _isUnlocked = true;

            if (isActiveAndEnabled && Application.isPlaying)
            {
                if (_openRoutine != null) StopCoroutine(_openRoutine);
                _openRoutine = StartCoroutine(OpenAnimationRoutine());
            }
            else
            {
                SetUnlockedVisualsImmediate();
            }

            OnGateUnlocked?.Invoke();
        }

        private void SetUnlockedVisualsImmediate()
        {
            if (barrierRenderer != null) barrierRenderer.gameObject.SetActive(false);
            if (portalGlowRenderer != null)
            {
                portalGlowRenderer.gameObject.SetActive(true);
                portalGlowRenderer.color = unlockedPortalColor;
            }
            if (gateLabelText != null)
            {
                gateLabelText.text = "OPEN!";
                gateLabelText.color = new Color(1f, 0.95f, 0.4f, 1f);
            }
        }

        private IEnumerator OpenAnimationRoutine()
        {
            if (gateLabelText != null)
            {
                gateLabelText.text = "OPENING...";
                gateLabelText.color = new Color(0.4f, 0.9f, 1f, 1f);
            }

            // Flash barrier and fade out
            float elapsed = 0f;
            float duration = 0.6f;

            if (portalGlowRenderer != null)
            {
                portalGlowRenderer.gameObject.SetActive(true);
                portalGlowRenderer.transform.localScale = Vector3.zero;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (barrierRenderer != null)
                {
                    Color c = lockedBarrierColor;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    barrierRenderer.color = c;
                }

                if (portalGlowRenderer != null)
                {
                    portalGlowRenderer.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.5f, t);
                    Color pc = unlockedPortalColor;
                    pc.a = t;
                    portalGlowRenderer.color = pc;
                }

                yield return null;
            }

            SetUnlockedVisualsImmediate();
            _openRoutine = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider2D other)
        {
            if (_isEntered) return;

            var motor = other.GetComponentInParent<GemmaMotor2D>() ?? other.GetComponentInChildren<GemmaMotor2D>();
            if (motor == null && !other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                return;
            }

            var session = GameSession.Active;
            bool isComplete = session != null && session.RainbowProgress != null && session.RainbowProgress.IsComplete;

            if (!_isUnlocked || !isComplete)
            {
                // Early entry while locked: display missing colour feedback
                if (_earlyEntryCooldownTimer <= 0f)
                {
                    _earlyEntryCooldownTimer = 1.2f;
                    if (session != null && session.RainbowProgress != null && session.RainbowProgress.CurrentTarget.HasValue)
                    {
                        var target = session.RainbowProgress.CurrentTarget.Value;
                        string hex = RainbowColourHelper.GetHex(target);
                        session.PostFeedbackMessage($"GATE LOCKED! MISSING: <color={hex}>{target.ToString().ToUpper()}</color>", new Color(1f, 0.5f, 0.5f));
                    }
                    else
                    {
                        session?.PostFeedbackMessage("GATE LOCKED! COLLECT ALL 7 GEMS IN ORDER", new Color(1f, 0.5f, 0.5f));
                    }
                }
                return;
            }

            // Gate is unlocked -> Enter and complete level!
            EnterGate(other.gameObject);
        }

        public void EnterGate(GameObject playerObj)
        {
            if (_isEntered) return;
            _isEntered = true;

            // 1. Bank final progress
            GameSession.Active?.BankProgress();

            // 2. Disable player control
            if (playerObj != null)
            {
                var motor = playerObj.GetComponentInParent<GemmaMotor2D>() ?? playerObj.GetComponentInChildren<GemmaMotor2D>();
                if (motor != null) motor.InputEnabled = false;

                var dash = playerObj.GetComponentInParent<GemmaDash>() ?? playerObj.GetComponentInChildren<GemmaDash>();
                if (dash != null) dash.DashEnabled = false;

                var rb = playerObj.GetComponentInParent<Rigidbody2D>() ?? playerObj.GetComponentInChildren<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            // 3. Stop timer & calculate final score & open results screen
            int remainingHealth = 3;
            if (playerObj != null)
            {
                var health = playerObj.GetComponentInParent<PlayerHealth>() ?? playerObj.GetComponentInChildren<PlayerHealth>();
                if (health != null) remainingHealth = health.CurrentHealth;
            }

            GameSession.Active?.CompleteLevel(remainingHealth);
            OnGateEntered?.Invoke();
        }
    }
}
