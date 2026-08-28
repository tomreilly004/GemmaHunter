using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemmaRainbowSeeker
{
    [Serializable]
    public struct LevelRecordData
    {
        public int levelNumber;
        public int bestScore;
        public int bestStars;
        public bool tutorialViewed;
    }

    [Serializable]
    public class PlayerProfileData
    {
        public int version = 1;
        public int highestUnlockedLevel = 1;
        public List<LevelRecordData> levelRecords = new List<LevelRecordData>();
    }

    /// <summary>
    /// Manages versioned local player profile persistence:
    /// - Highest unlocked level
    /// - Best score and star ratings per level (1-10)
    /// - Whether each level's tutorial has been viewed
    /// - Development reset functionality
    /// </summary>
    public static class SaveManager
    {
        private const string SaveKey = "GemmaRainbowSeeker_SaveData_v1";
        private const int MaxLevels = 10;
        private static PlayerProfileData _currentData;

        public static PlayerProfileData CurrentProfile
        {
            get
            {
                if (_currentData == null)
                {
                    Load();
                }
                return _currentData;
            }
        }

        public static int HighestUnlockedLevel => CurrentProfile.highestUnlockedLevel;

        public static void Load()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SaveKey);
                    _currentData = JsonUtility.FromJson<PlayerProfileData>(json);
                    if (_currentData != null)
                    {
                        EnsureRecords();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Failed to parse save data JSON, creating fresh profile: {ex.Message}");
                }
            }

            _currentData = new PlayerProfileData
            {
                version = 1,
                highestUnlockedLevel = 1,
                levelRecords = new List<LevelRecordData>()
            };
            EnsureRecords();
            Save();
        }

        public static void Save()
        {
            if (_currentData == null)
            {
                _currentData = new PlayerProfileData
                {
                    version = 1,
                    highestUnlockedLevel = 1,
                    levelRecords = new List<LevelRecordData>()
                };
            }
            EnsureRecords();

            string json = JsonUtility.ToJson(_currentData, false);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private static void EnsureRecords()
        {
            if (_currentData == null)
            {
                _currentData = new PlayerProfileData
                {
                    version = 1,
                    highestUnlockedLevel = 1,
                    levelRecords = new List<LevelRecordData>()
                };
            }

            if (_currentData.levelRecords == null)
            {
                _currentData.levelRecords = new List<LevelRecordData>();
            }

            for (int lvl = 1; lvl <= MaxLevels; lvl++)
            {
                int index = _currentData.levelRecords.FindIndex(r => r.levelNumber == lvl);
                if (index < 0)
                {
                    _currentData.levelRecords.Add(new LevelRecordData
                    {
                        levelNumber = lvl,
                        bestScore = 0,
                        bestStars = 0,
                        tutorialViewed = false
                    });
                }
            }
        }

        public static bool IsLevelUnlocked(int levelNumber)
        {
            if (levelNumber <= 1) return true;
            return levelNumber <= CurrentProfile.highestUnlockedLevel;
        }

        public static LevelRecordData GetLevelRecord(int levelNumber)
        {
            EnsureLoaded();
            int index = _currentData.levelRecords.FindIndex(r => r.levelNumber == levelNumber);
            if (index >= 0)
            {
                return _currentData.levelRecords[index];
            }
            return new LevelRecordData { levelNumber = levelNumber, bestScore = 0, bestStars = 0, tutorialViewed = false };
        }

        public static void RecordLevelResult(int levelNumber, int score, int stars)
        {
            EnsureLoaded();
            int index = _currentData.levelRecords.FindIndex(r => r.levelNumber == levelNumber);
            if (index >= 0)
            {
                var record = _currentData.levelRecords[index];
                if (score > record.bestScore) record.bestScore = score;
                if (stars > record.bestStars) record.bestStars = stars;
                _currentData.levelRecords[index] = record;
            }

            // Completing a level unlocks the next level
            int nextLevel = levelNumber + 1;
            if (nextLevel <= MaxLevels && nextLevel > _currentData.highestUnlockedLevel)
            {
                _currentData.highestUnlockedLevel = nextLevel;
            }

            Save();
        }

        public static bool HasTutorialBeenViewed(int levelNumber)
        {
            return GetLevelRecord(levelNumber).tutorialViewed;
        }

        public static void MarkTutorialViewed(int levelNumber)
        {
            EnsureLoaded();
            int index = _currentData.levelRecords.FindIndex(r => r.levelNumber == levelNumber);
            if (index >= 0)
            {
                var record = _currentData.levelRecords[index];
                record.tutorialViewed = true;
                _currentData.levelRecords[index] = record;
                Save();
            }
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            _currentData = new PlayerProfileData
            {
                version = 1,
                highestUnlockedLevel = 1,
                levelRecords = new List<LevelRecordData>()
            };
            EnsureRecords();
            Save();
        }

        private static void EnsureLoaded()
        {
            if (_currentData == null)
            {
                Load();
            }
            else
            {
                EnsureRecords();
            }
        }
    }
}
