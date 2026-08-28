using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Level selection screen for Levels 1–10:
    /// - Displays locked and unlocked states
    /// - Displays best score and star rating per level
    /// - Selects active level with Play button
    /// - Development reset progress button
    /// - Supports touch, mouse, and gamepad/keyboard navigation
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelSelectModal : MonoBehaviour
    {
        [Header("UI Containers")]
        [SerializeField] private CanvasGroup modalCanvasGroup;
        [SerializeField] private RectTransform itemsContainer;
        [SerializeField] private GameObject itemPrefab;

        [Header("Controls")]
        [SerializeField] private TextMeshProUGUI selectedLevelInfoText;
        [SerializeField] private UnityEngine.UI.Button playButton;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button devResetButton;

        private readonly List<LevelSelectItem> _items = new List<LevelSelectItem>();
        private int _selectedLevel = 1;
        private bool _isOpen = false;

        public int SelectedLevel => _selectedLevel;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(HandlePlayClicked);
            if (closeButton != null) closeButton.onClick.AddListener(CloseLevelSelect);
            if (devResetButton != null) devResetButton.onClick.AddListener(HandleDevResetClicked);

            HideImmediate();
        }

        public void OpenLevelSelect()
        {
            _isOpen = true;
            SaveManager.Load();
            _selectedLevel = Mathf.Clamp(_selectedLevel, 1, SaveManager.HighestUnlockedLevel);

            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 1f;
                modalCanvasGroup.blocksRaycasts = true;
                modalCanvasGroup.interactable = true;
            }
            gameObject.SetActive(true);

            PopulateLevelGrid();
            UpdateSelectedInfo();

            if (playButton != null)
            {
                playButton.Select();
            }
        }

        public void CloseLevelSelect()
        {
            _isOpen = false;
            HideImmediate();
        }

        private void HideImmediate()
        {
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.alpha = 0f;
                modalCanvasGroup.blocksRaycasts = false;
                modalCanvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        public void PopulateLevelGrid()
        {
            if (itemsContainer == null) return;

            // Clear old items if dynamic
            if (_items.Count == 0 && itemPrefab != null)
            {
                for (int i = 1; i <= 10; i++)
                {
                    var go = Instantiate(itemPrefab, itemsContainer);
                    go.name = $"LevelItem_{i}";
                    var item = go.GetComponent<LevelSelectItem>() ?? go.AddComponent<LevelSelectItem>();
                    _items.Add(item);
                }
            }
            else if (_items.Count == 0)
            {
                var existing = itemsContainer.GetComponentsInChildren<LevelSelectItem>(true);
                _items.AddRange(existing);
            }

            for (int i = 0; i < _items.Count && i < 10; i++)
            {
                int lvlNum = i + 1;
                bool isUnlocked = SaveManager.IsLevelUnlocked(lvlNum);
                var record = SaveManager.GetLevelRecord(lvlNum);

                _items[i].Setup(
                    lvlNum,
                    isUnlocked,
                    record.bestScore,
                    record.bestStars,
                    lvlNum == _selectedLevel,
                    SelectLevel
                );
            }
        }

        public void SelectLevel(int levelNumber)
        {
            if (!SaveManager.IsLevelUnlocked(levelNumber)) return;

            _selectedLevel = levelNumber;
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].SetSelected(_items[i].LevelNumber == _selectedLevel);
            }

            UpdateSelectedInfo();
        }

        private void UpdateSelectedInfo()
        {
            if (selectedLevelInfoText != null)
            {
                var record = SaveManager.GetLevelRecord(_selectedLevel);
                string scoreStr = record.bestScore > 0 ? $"{record.bestScore:N0} pts" : "No score yet";
                string starsStr = record.bestStars > 0 ? $"{record.bestStars}/3 Stars" : "0/3 Stars";
                selectedLevelInfoText.text = $"<b>LEVEL {_selectedLevel}</b>\nBest: {scoreStr}   ({starsStr})";
            }

            if (playButton != null)
            {
                playButton.interactable = SaveManager.IsLevelUnlocked(_selectedLevel);
            }
        }

        private void HandlePlayClicked()
        {
            CloseLevelSelect();

            string sceneName = $"Level{_selectedLevel:D2}";
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName)
                {
                    var session = GameSession.Active;
                    if (session != null)
                    {
                        session.ResetLevelProgress();
                    }
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    return;
                }
            }
            else
            {
                var session = GameSession.Active;
                if (session != null)
                {
                    var levelDef = LevelDefinition.CreateRuntimeInstance(
                        levelNumber: _selectedLevel,
                        sequence: new[] { RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo, RainbowColour.Violet },
                        dash: _selectedLevel >= 8
                    );
                    session.LoadLevel(levelDef);
                    session.ResetLevelProgress();
                }
            }

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma != null)
            {
                var checkpointMgr = UnityEngine.Object.FindFirstObjectByType<CheckpointManager>();
                if (checkpointMgr != null)
                {
                    checkpointMgr.RestartFromRainbowRest(gemma);
                }
            }

            Time.timeScale = 1f;
        }

        private void HandleDevResetClicked()
        {
            SaveManager.ResetProgress();
            _selectedLevel = 1;
            PopulateLevelGrid();
            UpdateSelectedInfo();
            Debug.Log("[LevelSelectModal] Player progress has been reset for development.");
        }
    }
}
