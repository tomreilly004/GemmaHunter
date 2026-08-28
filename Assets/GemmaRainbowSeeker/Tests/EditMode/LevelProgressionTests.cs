using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GemmaRainbowSeeker;
using GemmaRainbowSeeker.Editor;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class LevelProgressionTests
    {
        [SetUp]
        public void Setup()
        {
            SaveManager.ResetProgress();
        }

        [TearDown]
        public void TearDown()
        {
            SaveManager.ResetProgress();
        }

        [Test]
        public void Level01_DataDefinition_MatchesDesignRules()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level01.asset");
            Assert.IsNotNull(def, "Level 01 definition must exist.");
            Assert.AreEqual(1, def.LevelNumber);
            Assert.AreEqual("Level01", def.SceneName);
            Assert.AreEqual(1, def.RequiredGemCount);
            Assert.AreEqual(RainbowColour.Red, def.ColourSequence[0]);
            Assert.AreEqual("Collect 1 red gem", def.ObjectiveDescription);
            Assert.AreEqual(15f, def.TargetCompletionTime);
            Assert.IsTrue(def.DashEnabled, "Dash should be enabled in Level 1.");
            Assert.IsTrue(def.HealthEnabled, "Health should be enabled in Level 1.");
            Assert.IsFalse(def.DangerousHazardsEnabled, "Hazards should be disabled in Level 1.");
            Assert.IsFalse(def.RainbowRestsEnabled, "Rainbow Rests should be disabled in Level 1.");
        }

        [Test]
        public void Level02_DataDefinition_MatchesDesignRules()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level02.asset");
            Assert.IsNotNull(def, "Level 02 definition must exist.");
            Assert.AreEqual(2, def.LevelNumber);
            Assert.AreEqual(3, def.RequiredGemCount);
            Assert.AreEqual("Collect Red, Red, Orange", def.ObjectiveDescription);
            CollectionAssert.AreEqual(new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange }, def.ColourSequence);
            Assert.AreEqual(25f, def.TargetCompletionTime);
            Assert.AreEqual(6.0f, def.RainbowRushTimeWindow);
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsFalse(def.DangerousHazardsEnabled);
        }

        [Test]
        public void Level03_DataDefinition_MatchesDesignRules()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level03.asset");
            Assert.IsNotNull(def, "Level 03 definition must exist.");
            Assert.AreEqual(3, def.LevelNumber);
            Assert.AreEqual(4, def.RequiredGemCount);
            Assert.AreEqual("Collect Orange, Orange, Yellow, Yellow", def.ObjectiveDescription);
            CollectionAssert.AreEqual(new[] { RainbowColour.Orange, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Yellow }, def.ColourSequence);
            Assert.AreEqual(30f, def.TargetCompletionTime);
            Assert.AreEqual(5.5f, def.RainbowRushTimeWindow);
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsFalse(def.DangerousHazardsEnabled);
        }

        [Test]
        public void Level04_DataDefinition_MatchesDesignRules()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level04.asset");
            Assert.IsNotNull(def, "Level 04 definition must exist.");
            Assert.AreEqual(4, def.LevelNumber);
            Assert.AreEqual(5, def.RequiredGemCount);
            Assert.AreEqual("Collect Yellow, Yellow, Green, Green, Blue", def.ObjectiveDescription);
            CollectionAssert.AreEqual(new[] { RainbowColour.Yellow, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Green, RainbowColour.Blue }, def.ColourSequence);
            Assert.AreEqual(35f, def.TargetCompletionTime);
            Assert.AreEqual(5.0f, def.RainbowRushTimeWindow);
            Assert.IsTrue(def.CurrentsEnabled, "Magical currents should be enabled in Level 4.");
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsFalse(def.DangerousHazardsEnabled);
        }

        [Test]
        public void Level05_DataDefinition_MatchesDesignRules()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level05.asset");
            Assert.IsNotNull(def, "Level 05 definition must exist.");
            Assert.AreEqual(5, def.LevelNumber);
            Assert.AreEqual(7, def.RequiredGemCount);
            Assert.AreEqual("Collect all 7 rainbow colours in order", def.ObjectiveDescription);
            CollectionAssert.AreEqual(new[] { RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo, RainbowColour.Violet }, def.ColourSequence);
            Assert.AreEqual(50f, def.TargetCompletionTime);
            Assert.AreEqual(4.75f, def.RainbowRushTimeWindow);
            Assert.IsTrue(def.RainbowRestsEnabled, "Rainbow Rests should be enabled in Level 5.");
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsFalse(def.DangerousHazardsEnabled);
        }

        [Test]
        public void StarThresholds_ComputeCorrectStars_ForAllLevels()
        {
            for (int lvl = 1; lvl <= 5; lvl++)
            {
                var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level{lvl:D2}.asset");
                Assert.IsNotNull(def);

                Assert.AreEqual(1, def.ComputeStarRating(0), "Completion should always award at least 1 star.");
                Assert.AreEqual(1, def.ComputeStarRating(def.TwoStarThreshold - 1));
                Assert.AreEqual(2, def.ComputeStarRating(def.TwoStarThreshold));
                Assert.AreEqual(2, def.ComputeStarRating(def.ThreeStarThreshold - 1));
                Assert.AreEqual(3, def.ComputeStarRating(def.ThreeStarThreshold));
                Assert.AreEqual(3, def.ComputeStarRating(def.ThreeStarThreshold + 500));
            }
        }

        [Test]
        public void ProgressionFlow_CompletingLevels1To5_UnlocksSuccessively()
        {
            Assert.AreEqual(1, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(1));
            Assert.IsFalse(SaveManager.IsLevelUnlocked(2));

            // Complete Level 1
            SaveManager.RecordLevelResult(1, 140, 3);
            Assert.AreEqual(2, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(2));

            // Complete Level 2
            SaveManager.RecordLevelResult(2, 600, 3);
            Assert.AreEqual(3, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(3));

            // Complete Level 3
            SaveManager.RecordLevelResult(3, 1000, 3);
            Assert.AreEqual(4, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(4));

            // Complete Level 4
            SaveManager.RecordLevelResult(4, 1500, 3);
            Assert.AreEqual(5, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(5));

            // Complete Level 5
            SaveManager.RecordLevelResult(5, 2600, 3);
            Assert.AreEqual(6, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(6));
        }

        [Test]
        public void SceneFiles_AllFiveLevels_ExistAndValidateSuccessfully()
        {
            for (int i = 1; i <= 5; i++)
            {
                string path = $"Assets/GemmaRainbowSeeker/Scenes/Level{i:D2}.unity";
                Assert.IsTrue(File.Exists(path), $"Scene file for Level {i} must exist at {path}.");

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var report = LevelValidator.ValidateActiveLevel(i);
                Assert.IsTrue(report.IsValid, $"Level {i} validation failed: {string.Join(", ", report.Errors)}");
            }
        }
    }
}
