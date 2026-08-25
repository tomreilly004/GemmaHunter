using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Displays non-blocking, queued tutorial message cards.
    /// Shows keyboard and gamepad controls, dismisses upon input or gameplay trigger,
    /// and never permanently blocks player control.
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
            public float autoDismissDuration; // 0 = wait for input / event

            public TutorialMessage(string title, string body, string hint = "", float duration = 0f)
            {
                this.title = title;
                this.body = body;
                this.controlsHint = hint;
                this.autoDismissDuration = duration;
            }
        }

        [Header("UI References")]
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private RectTransform bannerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI controlsHintText;

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
        }

        private void Start()
        {
            // Initial default welcome tutorial message
            QueueMessage(new TutorialMessage(
                "HOW TO PLAY",
                "Swim through the sky and collect the 7 rainbow gems IN EXACT ORDER (Red to Violet).\nAvoid dark storm clouds or dash through cracked ones!",
                "MOVE: [WASD / Arrows / Left Stick]   DASH: [Space / Button South]   PAUSE: [Esc]",
                5.0f
            ));
        }

        private void Update()
        {
            if (_isShowingMessage)
            {
                // Check if player pressed Move, Dash, or any key to dismiss early
                if (Keyboard.current != null && (Keyboard.current.anyKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                {
                    DismissCurrent();
                }
                else if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.2f))
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

            // Slide & Fade In
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0f, 150f);
            Vector2 targetPos = Vector2.zero;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
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
            float maxWait = msg.autoDismissDuration > 0f ? msg.autoDismissDuration : 8.0f;

            while (showTimer < maxWait && !_dismissCurrentRequested)
            {
                showTimer += Time.deltaTime;
                yield return null;
            }

            // Slide & Fade Out
            elapsed = 0f;
            Vector2 endPos = new Vector2(0f, 150f);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float ease = t * t;

                if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 1f - ease;
                if (bannerRect != null) bannerRect.anchoredPosition = Vector2.Lerp(targetPos, endPos, ease);
                yield return null;
            }

            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;

            _displayRoutine = null;
            ProcessNextMessage();
        }
    }
}
