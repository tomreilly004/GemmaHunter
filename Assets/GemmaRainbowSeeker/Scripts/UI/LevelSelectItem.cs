using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Renders a single level card (Levels 1-10) within the Level Select Screen:
    /// - Level Number & Title
    /// - Locked / Unlocked badge
    /// - Best Score
    /// - Best Star Rating (★ ★ ★)
    /// - Selection state
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI starsText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private UnityEngine.UI.Button selectButton;
        [SerializeField] private UnityEngine.UI.Image cardBackground;

        private int _levelNumber;
        private bool _isUnlocked;
        private Action<int> _onSelectedCallback;

        public int LevelNumber => _levelNumber;
        public bool IsUnlocked => _isUnlocked;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(HandleClicked);
            }
        }

        public void Setup(int levelNum, bool isUnlocked, int bestScore, int bestStars, bool isSelected, Action<int> onSelected)
        {
            _levelNumber = levelNum;
            _isUnlocked = isUnlocked;
            _onSelectedCallback = onSelected;

            if (levelTitleText != null)
            {
                levelTitleText.text = $"LEVEL {levelNum}";
            }

            if (lockIcon != null)
            {
                lockIcon.SetActive(!isUnlocked);
            }

            if (scoreText != null)
            {
                scoreText.text = isUnlocked ? (bestScore > 0 ? $"Best: {bestScore:N0}" : "Best: ---") : "Locked";
                scoreText.color = isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f);
            }

            if (starsText != null)
            {
                if (!isUnlocked)
                {
                    starsText.text = "";
                }
                else
                {
                    string stars = "";
                    for (int s = 1; s <= 3; s++)
                    {
                        stars += (s <= bestStars) ? "<color=#FFE640>*</color> " : "<color=#4A5060>-</color> ";
                    }
                    starsText.text = stars.TrimEnd();
                }
            }

            if (selectButton != null)
            {
                selectButton.interactable = isUnlocked;
            }

            SetSelected(isSelected);

            if (cardBackground != null)
            {
                cardBackground.color = isUnlocked
                    ? new Color(0.18f, 0.22f, 0.35f, 0.95f)
                    : new Color(0.12f, 0.14f, 0.20f, 0.85f);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(isSelected);
            }
        }

        private void HandleClicked()
        {
            if (_isUnlocked)
            {
                _onSelectedCallback?.Invoke(_levelNumber);
            }
        }
    }
}
