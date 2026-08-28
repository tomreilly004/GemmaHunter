using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Displays data-driven tutorial cards with support for:
    /// - Gameplay pause (suspending Rainbow Rush without resetting)
    /// - UI Target highlighting
    /// - Touch, keyboard, mouse and gamepad dismissal
    /// - Queued step processing
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialBanner : MonoBehaviour
    {
        [Serializable]
        public struct TutorialMessage
        {
            public string title;
            public string body;
            public string controlsHint;
            public string highlightTarget;
            public bool pauseGameplay;
            public float autoDismissDuration; // 0 = wait for dismiss button / input
            public Action onDismissed;

            public TutorialMessage(string title, string body, string hint = "", float duration = 0f)
            {
                this.title = title;
                this.body = body;
                this.controlsHint = hint;
                this.highlightTarget = "";
                this.pauseGameplay = false;
                this.autoDismissDuration = duration;
                this.onDismissed = null;
            }

            public TutorialMessage(string title, string body, string hint, string highlight, bool pause, float duration, Action onDismiss = null)
            {
                this.title = title;
                this.body = body;
                this.controlsHint = hint;
                this.highlightTarget = highlight;
                this.pauseGameplay = pause;
                this.autoDismissDuration = duration;
                this.onDismissed = onDismiss;
            }
        }

        [Header("UI References")]
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI controlsHintText;
        [SerializeField] private UnityEngine.UI.Button dismissButton;

        [Header("Highlight Overlay")]
        [SerializeField] private RectTransform highlightBox;

        [Header("Animation")]
        [Range(0.1f, 1f)]
        [SerializeField] private float transitionDuration = 0.25f;

        private readonly Queue<TutorialMessage> _messageQueue = new Queue<TutorialMessage>();
        private bool _isShowingMessage;
        private Coroutine _displayRoutine;
        private bool _dismissCurrentRequested;

        public bool IsShowingMessage => _isShowingMessage;

        private void Awake()
        {
            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
                bannerCanvasGroup.blocksRaycasts = false;
            }

            if (dismissButton != null)
            {
                dismissButton.onClick.AddListener(DismissCurrent);
            }

            if (highlightBox != null)
            {
                highlightBox.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (_isShowingMessage)
            {
                // Keyboard, Gamepad or touch dismiss
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
                {
                    DismissCurrent();
                }
                else if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
                {
                    DismissCurrent();
                }
                else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    DismissCurrent();
                }
            }
        }

        public void QueueMessage(TutorialMessage msg)
        {
            _messageQueue.Enqueue(msg);
            if (!_isShowingMessage)
            {
                ProcessNextMessage();
            }
        }

        public void DismissCurrent()
        {
            _dismissCurrentRequested = true;
        }

        private void ProcessNextMessage()
        {
            if (_messageQueue.Count == 0)
            {
                _isShowingMessage = false;
                if (GameSession.Active != null)
                {
                    GameSession.Active.IsTutorialBlocking = false;
                }
                return;
            }

            var nextMsg = _messageQueue.Dequeue();
            if (_displayRoutine != null)
            {
                StopCoroutine(_displayRoutine);
            }
            _displayRoutine = StartCoroutine(ShowMessageRoutine(nextMsg));
        }

        private IEnumerator ShowMessageRoutine(TutorialMessage msg)
        {
            _isShowingMessage = true;
            _dismissCurrentRequested = false;

            if (titleText != null) titleText.text = msg.title;
            if (bodyText != null) bodyText.text = msg.body;
            if (controlsHintText != null) controlsHintText.text = msg.controlsHint;

            // Handle gameplay pause
            if (msg.pauseGameplay)
            {
                if (GameSession.Active != null)
                {
                    GameSession.Active.IsTutorialBlocking = true;
                }
            }

            // Handle UI highlight
            if (!string.IsNullOrEmpty(msg.highlightTarget) && highlightBox != null)
            {
                var targetObj = GameObject.Find(msg.highlightTarget);
                if (targetObj != null && targetObj.transform is RectTransform targetRect)
                {
                    highlightBox.position = targetRect.position;
                    highlightBox.sizeDelta = targetRect.sizeDelta + new Vector2(20f, 20f);
                    highlightBox.gameObject.SetActive(true);
                }
            }

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.blocksRaycasts = true;
            }

            // Slide & Fade In
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0f, 150f);
            Vector2 targetPos = Vector2.zero;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = ease;
                if (bannerRect != null) bannerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                yield return null;
            }

            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f;
            if (bannerRect != null) bannerRect.anchoredPosition = targetPos;

            // Wait until duration elapses or dismissed
            float showTimer = 0f;
            float maxWait = msg.autoDismissDuration > 0f ? msg.autoDismissDuration : (msg.pauseGameplay ? float.MaxValue : 8.0f);

            while (showTimer < maxWait && !_dismissCurrentRequested)
            {
                showTimer += Time.unscaledDeltaTime;
                yield return null;
            }

            // Slide & Fade Out
            elapsed = 0f;
            Vector2 endPos = new Vector2(0f, 150f);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float ease = t * t;

                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f - ease;
                if (bannerRect != null) bannerRect.anchoredPosition = Vector2.Lerp(targetPos, endPos, ease);
                yield return null;
            }

            if (bannerCanvasGroup != null)
            {
                bannerCanvasGroup.alpha = 0f;
                bannerCanvasGroup.blocksRaycasts = false;
            }

            if (highlightBox != null)
            {
                highlightBox.gameObject.SetActive(false);
            }

            if (msg.pauseGameplay && GameSession.Active != null)
            {
                GameSession.Active.IsTutorialBlocking = false;
            }

            msg.onDismissed?.Invoke();

            _displayRoutine = null;
            ProcessNextMessage();
        }
    }
}

