using System;
using System.Collections;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Coordinates checkpoint tracking, respawning at the active Rainbow Rest,
    /// penalty deductions, health restoration, and level progress recovery.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Spawn Defaults")]
        [Tooltip("Default initial spawn point if no Rainbow Rest has been activated yet.")]
        [SerializeField] private Transform defaultSpawnTransform;

        [Header("Respawn Transition")]
        [Tooltip("Fade out / in duration in seconds when respawning.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float fadeDuration = 0.25f;

        [Tooltip("Optional CanvasGroup used for screen fading during respawn.")]
        [SerializeField] private CanvasGroup screenFadeCanvasGroup;

        // ── State ─────────────────────────────────────────────────────────────
        private Vector3 _initialSpawnPosition;
        private RainbowRest _activeRest;
        private Coroutine _restartRoutine;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<RainbowRest> OnActiveCheckpointChanged;
        public event Action OnCheckpointRestartStarted;
        public event Action OnCheckpointRestartCompleted;

        // ── Properties ────────────────────────────────────────────────────────
        public RainbowRest ActiveRest => _activeRest;
        public Vector3 CurrentSpawnPosition => _activeRest != null ? _activeRest.SpawnPosition : _initialSpawnPosition;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;

            if (defaultSpawnTransform != null)
            {
                _initialSpawnPosition = defaultSpawnTransform.position;
            }
            else
            {
                var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
                if (gemma != null)
                {
                    _initialSpawnPosition = gemma.transform.position;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ── Checkpoint Registration ───────────────────────────────────────────

        public void SetActiveCheckpoint(RainbowRest rest)
        {
            if (rest == null) return;

            _activeRest = rest;
            OnActiveCheckpointChanged?.Invoke(_activeRest);
        }

        public void SetInitialSpawnPosition(Vector3 position)
        {
            _initialSpawnPosition = position;
        }

        // ── Restart from Rainbow Rest ─────────────────────────────────────────

        /// <summary>
        /// Executes a restart from the active Rainbow Rest checkpoint:
        /// fades screen, restores full health, deducts score penalty (min 0),
        /// resets combo to x1, restores unbanked gems & hazards, and relocates Gemma.
        /// </summary>
        public void RestartFromRainbowRest(GameObject player = null, Action onComplete = null)
        {
            if (player == null)
            {
                player = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            }

            if (Application.isPlaying && isActiveAndEnabled)
            {
                if (_restartRoutine != null)
                {
                    StopCoroutine(_restartRoutine);
                }
                _restartRoutine = StartCoroutine(RestartFromRestRoutine(player, onComplete));
            }
            else
            {
                // Immediate synchronous execution for EditMode / testing
                ExecuteRestartImmediate(player);
                onComplete?.Invoke();
            }
        }

        public void ExecuteRestartImmediate(GameObject player)
        {
            OnCheckpointRestartStarted?.Invoke();

            Vector3 spawnPos = CurrentSpawnPosition;
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = spawnPos;
                    rb.linearVelocity = Vector2.zero;
                }
                player.transform.position = spawnPos;

                var health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.RestoreFullHealth();
                }
            }

            var session = GameSession.Active;
            if (session != null)
            {
                int penalty = session.LevelRules != null ? session.LevelRules.RestartFromRestPenalty : 200;
                session.ScoreManager?.SubtractPoints(penalty);
                session.RushController?.ResetRush(RushResetReason.Restart);
                session.RestoreBankedProgress();
                session.SessionStats?.RecordCheckpointRestart();
            }

            OnCheckpointRestartCompleted?.Invoke();
        }

        private IEnumerator RestartFromRestRoutine(GameObject player, Action onComplete)
        {
            OnCheckpointRestartStarted?.Invoke();

            if (player == null)
            {
                player = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            }

            // 1. Fade out screen
            yield return StartCoroutine(FadeScreen(0f, 1f, fadeDuration));

            // 2. Relocate Gemma and zero out velocity
            Vector3 spawnPos = CurrentSpawnPosition;
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = spawnPos;
                    rb.linearVelocity = Vector2.zero;
                }
                player.transform.position = spawnPos;

                // 3. Restore full health
                var health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.RestoreFullHealth();
                }
            }

            // 4. Session penalties & state recovery
            var session = GameSession.Active;
            if (session != null)
            {
                // Remove score penalty (200 pts, clamped >= 0)
                int penalty = session.LevelRules != null ? session.LevelRules.RestartFromRestPenalty : 200;
                session.ScoreManager?.SubtractPoints(penalty);

                // Reset Rainbow Rush multiplier back to x1
                session.RushController?.ResetRush(RushResetReason.Restart);

                // Restore rainbow progress to banked count (reactivates unbanked gems & broken hazards)
                session.RestoreBankedProgress();

                // Record checkpoint restart in session statistics
                session.SessionStats?.RecordCheckpointRestart();
            }

            // Brief wait at black
            yield return new WaitForSeconds(0.1f);

            // 5. Fade in screen
            yield return StartCoroutine(FadeScreen(1f, 0f, fadeDuration));

            _restartRoutine = null;
            OnCheckpointRestartCompleted?.Invoke();
            onComplete?.Invoke();
        }

        private IEnumerator FadeScreen(float fromAlpha, float toAlpha, float duration)
        {
            if (screenFadeCanvasGroup == null)
            {
                yield break;
            }

            float elapsed = 0f;
            screenFadeCanvasGroup.alpha = fromAlpha;
            screenFadeCanvasGroup.blocksRaycasts = (toAlpha > 0.5f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                screenFadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                yield return null;
            }

            screenFadeCanvasGroup.alpha = toAlpha;
            screenFadeCanvasGroup.blocksRaycasts = (toAlpha > 0.5f);
        }
    }
}
