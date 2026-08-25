using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Displays the end-of-level Results Screen showing final score, time bonus,
    /// health bonus, detailed statistics (mistakes, damage, restarts), and 1-3 star rating.
    /// Supports mouse, keyboard, and gamepad button navigation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultsModal : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup resultsCanvasGroup;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI timeStatsText;
        [SerializeField] private TextMeshProUGUI healthStatsText;
        [SerializeField] private TextMeshProUGUI gemsStatsText;
        [SerializeField] private TextMeshProUGUI mistakesStatsText;
        [SerializeField] private TextMeshProUGUI damageStatsText;
        [SerializeField] private TextMeshProUGUI restartsStatsText;
        [SerializeField] private TextMeshProUGUI starRatingText;
        [SerializeField] private TextMeshProUGUI statusNoticeText;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button retryLevelButton;
        [SerializeField] private UnityEngine.UI.Button continueButton;

        private void Awake()
        {
            if (resultsCanvasGroup != null)
            {
                resultsCanvasGroup.alpha = 0f;
                resultsCanvasGroup.blocksRaycasts = false;
                resultsCanvasGroup.interactable = false;
            }

            if (retryLevelButton != null)
            {
                retryLevelButton.onClick.AddListener(OnRetryLevelClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (statusNoticeText != null)
            {
                statusNoticeText.text = "";
            }
        }

        private void Start()
        {
            if (GameSession.Active != null)
            {
                GameSession.Active.OnLevelCompleted += DisplayResults;
            }
        }

        private void OnDestroy()
        {
            if (GameSession.Active != null)
            {
                GameSession.Active.OnLevelCompleted -= DisplayResults;
            }
        }

        public void DisplayResults(LevelCompletionData data)
        {
            if (resultsCanvasGroup != null)
            {
                resultsCanvasGroup.alpha = 1f;
                resultsCanvasGroup.blocksRaycasts = true;
                resultsCanvasGroup.interactable = true;
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"FINAL SCORE: {data.finalScore:N0}";
            }

            int mins = (int)(data.completionTime / 60f);
            int secs = (int)(data.completionTime % 60f);
            if (timeStatsText != null)
            {
                timeStatsText.text = $"Time: {mins:00}:{secs:00}  (+{data.timeBonus:N0} Bonus)";
            }

            if (healthStatsText != null)
            {
                healthStatsText.text = $"Health Left: {data.remainingHealth}/3  (+{data.healthBonus:N0} Bonus)";
            }

            if (gemsStatsText != null)
            {
                gemsStatsText.text = $"Gems Collected: {data.correctGems} / 7";
            }

            if (mistakesStatsText != null)
            {
                mistakesStatsText.text = $"Wrong Attempts: {data.wrongAttempts}";
            }

            if (damageStatsText != null)
            {
                damageStatsText.text = $"Damage Taken: {data.damageTaken}";
            }

            if (restartsStatsText != null)
            {
                restartsStatsText.text = $"Checkpoint Restarts: {data.checkpointRestarts}";
            }

            if (starRatingText != null)
            {
                string starsString = "";
                for (int i = 0; i < 3; i++)
                {
                    if (i < data.starRating)
                        starsString += "<color=#FFD700>★</color> ";
                    else
                        starsString += "<color=#555C6E>☆</color> ";
                }
                starRatingText.text = $"STAR RATING: {starsString}";
            }

            if (statusNoticeText != null)
            {
                statusNoticeText.text = "";
            }

            // Focused selection for gamepad/keyboard
            if (EventSystem.current != null && continueButton != null)
            {
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            }
        }

        private void OnRetryLevelClicked()
        {
            if (GameSession.Active != null)
            {
                GameSession.Active.ResetLevelProgress();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnContinueClicked()
        {
            if (statusNoticeText != null)
            {
                statusNoticeText.text = "<color=#36A7FF>Level 2 is currently in development! You can Retry Level to aim for 3 stars.</color>";
            }
        }
    }
}
