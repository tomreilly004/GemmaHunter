using NUnit.Framework;
using UnityEngine;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class MobileProgressionTests
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
        public void SaveManager_InitialState_Level1Unlocked_OthersLocked()
        {
            Assert.IsTrue(SaveManager.IsLevelUnlocked(1), "Level 1 must be unlocked by default.");
            Assert.IsFalse(SaveManager.IsLevelUnlocked(2), "Level 2 should be locked initially.");
            Assert.IsFalse(SaveManager.IsLevelUnlocked(10), "Level 10 should be locked initially.");
            Assert.AreEqual(1, SaveManager.HighestUnlockedLevel);
        }

        [Test]
        public void SaveManager_CompletingLevel_UnlocksNextLevel_AndPersistsBestStats()
        {
            // Complete Level 1 with 1500 pts and 2 stars
            SaveManager.RecordLevelResult(1, 1500, 2);

            Assert.IsTrue(SaveManager.IsLevelUnlocked(2), "Completing Level 1 must unlock Level 2.");
            Assert.AreEqual(2, SaveManager.HighestUnlockedLevel);

            var record1 = SaveManager.GetLevelRecord(1);
            Assert.AreEqual(1500, record1.bestScore);
            Assert.AreEqual(2, record1.bestStars);

            // Complete Level 1 again with higher score & 3 stars
            SaveManager.RecordLevelResult(1, 2300, 3);
            record1 = SaveManager.GetLevelRecord(1);
            Assert.AreEqual(2300, record1.bestScore, "Best score should update to higher value.");
            Assert.AreEqual(3, record1.bestStars, "Best stars should update to 3.");

            // Lower score shouldn't overwrite best
            SaveManager.RecordLevelResult(1, 1000, 1);
            record1 = SaveManager.GetLevelRecord(1);
            Assert.AreEqual(2300, record1.bestScore);
            Assert.AreEqual(3, record1.bestStars);
        }

        [Test]
        public void SaveManager_TutorialViewed_TracksAndPersistsState()
        {
            Assert.IsFalse(SaveManager.HasTutorialBeenViewed(1), "Tutorial should not be viewed initially.");

            SaveManager.MarkTutorialViewed(1);
            Assert.IsTrue(SaveManager.HasTutorialBeenViewed(1), "Tutorial must be marked as viewed.");

            Assert.IsFalse(SaveManager.HasTutorialBeenViewed(2), "Level 2 tutorial should remain unviewed.");
        }

        [Test]
        public void SaveManager_ResetProgress_RestoresCleanDefaultState()
        {
            SaveManager.RecordLevelResult(1, 3000, 3);
            SaveManager.RecordLevelResult(2, 2500, 3);
            SaveManager.MarkTutorialViewed(1);

            Assert.AreEqual(3, SaveManager.HighestUnlockedLevel);

            SaveManager.ResetProgress();

            Assert.AreEqual(1, SaveManager.HighestUnlockedLevel);
            Assert.IsTrue(SaveManager.IsLevelUnlocked(1));
            Assert.IsFalse(SaveManager.IsLevelUnlocked(2));
            Assert.IsFalse(SaveManager.HasTutorialBeenViewed(1));
            Assert.AreEqual(0, SaveManager.GetLevelRecord(1).bestScore);
        }

        [Test]
        public void TutorialSequence_DataAsset_ConfiguresStepsAndShowOnce()
        {
            var seq = ScriptableObject.CreateInstance<TutorialSequence>();
            seq.AddStep(new TutorialSequence.TutorialStep
            {
                stepId = "step_test_1",
                triggerEvent = TutorialTriggerEvent.OnLevelStart,
                title = "SWIM",
                body = "Use joystick to swim",
                controlsHint = "Joystick",
                showOnce = true,
                displayDuration = 5.0f
            });

            Assert.AreEqual(1, seq.Steps.Count);
            Assert.AreEqual("step_test_1", seq.Steps[0].stepId);
            Assert.AreEqual(TutorialTriggerEvent.OnLevelStart, seq.Steps[0].triggerEvent);
            Assert.IsTrue(seq.Steps[0].showOnce);

            Object.DestroyImmediate(seq);
        }

        [Test]
        public void MobileControlsView_DashButton_HiddenUntilLevel8()
        {
            var sessionObj = new GameObject("TestSession");
            var session = sessionObj.AddComponent<GameSession>();

            var mcObj = new GameObject("TestMC");
            var mcView = mcObj.AddComponent<MobileControlsView>();
            var dashRoot = new GameObject("DashRoot");
            dashRoot.transform.SetParent(mcObj.transform);

            var so = new UnityEditor.SerializedObject(mcView);
            so.FindProperty("dashButtonRoot").objectReferenceValue = dashRoot;
            so.ApplyModifiedProperties();

            // Level 1: Dash disabled on mobile HUD
            var defLvl1 = LevelDefinition.CreateRuntimeInstance(levelNumber: 1, sequence: new[] { RainbowColour.Red }, dash: true);
            session.LoadLevel(defLvl1);
            mcView.RefreshControlsVisibility();
            Assert.IsFalse(dashRoot.activeSelf, "Dash button must be hidden in Level 1.");

            // Level 8: Dash enabled on mobile HUD
            var defLvl8 = LevelDefinition.CreateRuntimeInstance(levelNumber: 8, sequence: new[] { RainbowColour.Red }, dash: true);
            session.LoadLevel(defLvl8);
            mcView.RefreshControlsVisibility();
            Assert.IsTrue(dashRoot.activeSelf, "Dash button must be visible from Level 8 onwards.");

            Object.DestroyImmediate(mcObj);
            Object.DestroyImmediate(sessionObj);
            Object.DestroyImmediate(defLvl1);
            Object.DestroyImmediate(defLvl8);
        }
    }
}
