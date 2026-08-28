using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class UIGameplayFlowTests
    {
        private GameObject _sessionObj;
        private GameSession _session;
        private LevelRules _rules;
        private GameObject _playerObj;
        private PlayerHealth _playerHealth;

        [SetUp]
        public void Setup()
        {
            _rules = ScriptableObject.CreateInstance<LevelRules>();

            _sessionObj = new GameObject("TestGameSession");
            _session = _sessionObj.AddComponent<GameSession>();
            _session.InitializeSystems(_rules);

            _playerObj = new GameObject("TestPlayer");
            _playerObj.tag = "Player";
            _playerObj.AddComponent<Rigidbody2D>();
            _playerObj.AddComponent<GemmaMotor2D>();
            _playerObj.AddComponent<GemmaDash>();
            _playerHealth = _playerObj.AddComponent<PlayerHealth>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObj != null) Object.DestroyImmediate(_playerObj);
            if (_sessionObj != null) Object.DestroyImmediate(_sessionObj);
            if (_rules != null) Object.DestroyImmediate(_rules);
        }

        [Test]
        public void RainbowGate_BeginsLocked_AndUnlocksWhenRainbowIsComplete()
        {
            var gateObj = new GameObject("TestGate");
            gateObj.AddComponent<BoxCollider2D>();
            var gate = gateObj.AddComponent<RainbowGate>();

            Assert.IsFalse(gate.IsUnlocked, "RainbowGate should start in locked state");

            // Attempt early entry while incomplete
            gate.EnterGate(_playerObj);
            Assert.IsFalse(_session.IsLevelCompleted, "Early entry should NOT complete level");

            // Collect all 7 in order
            for (int i = 0; i < 7; i++)
            {
                _session.TryCollectGem((RainbowColour)i);
            }
            Assert.IsTrue(_session.RainbowProgress.IsComplete);

            // Trigger gate unlock
            gate.UnlockGate();
            Assert.IsTrue(gate.IsUnlocked, "Gate should now be in unlocked state");

            // Enter unlocked gate
            gate.EnterGate(_playerObj);
            Assert.IsTrue(_session.IsLevelCompleted, "Entering unlocked gate should trigger level completion");
            Assert.IsFalse(_session.IsTimerRunning, "Level timer should be stopped on completion");

            Object.DestroyImmediate(gateObj);
        }

        [Test]
        public void LevelCompletion_AwardsCorrectTimeAndHealthBonuses_AndComputesStars()
        {
            // Collect all 7 gems to complete rainbow
            for (int i = 0; i < 7; i++)
            {
                _session.TryCollectGem((RainbowColour)i);
            }

            // Simulate elapsed time = 60s (under par 180s by 120s)
            _session.SessionStats.Tick(60f);

            // Complete level with full 3 HP
            var completionData = _session.CompleteLevel(3);

            // Base score for 7 gems under Rainbow Rush:
            // 1st = 100 * 1 = 100 (Rush -> x2)
            // 2nd = 100 * 2 = 200 (Rush -> x3)
            // 3rd = 100 * 3 = 300 (Rush -> x4)
            // 4th = 100 * 4 = 400 (Rush -> x5)
            // 5th = 100 * 5 = 500 (Rush -> x5)
            // 6th = 100 * 5 = 500 (Rush -> x5)
            // 7th = 100 * 5 = 500 (Rush -> x5)
            // Total gem score = 2,500 pts
            // Health bonus = 3 * 150 = 450 pts
            // Time bonus = (180 - 60) * 5 = 120 * 5 = 600 pts
            // Total score = 2,500 + 450 + 600 = 3,550 pts (>= 2200 -> 3 Stars!)

            Assert.AreEqual(450, completionData.healthBonus, "Health bonus should be 3 * 150 = 450");
            Assert.AreEqual(600, completionData.timeBonus, "Time bonus should be 120 * 5 = 600");
            Assert.AreEqual(3550, completionData.finalScore, "Final score should include base + bonuses");
            Assert.AreEqual(3, completionData.starRating, "Score >= 2200 should award 3 stars");
            Assert.AreEqual(7, completionData.correctGems);
            Assert.AreEqual(3, completionData.remainingHealth);
            Assert.AreEqual(5, completionData.highestMultiplier, "Highest Rush tier reached should be x5");
        }

        [Test]
        public void RainbowMeterSlot_SetsCorrectVisualStates()
        {
            var slotObj = new GameObject("TestSlot");
            var slot = slotObj.AddComponent<RainbowMeterSlot>();
            slot.Initialize(RainbowColour.Yellow);

            slot.SetState(RainbowMeterSlot.SlotState.Empty);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Empty, slot.State);

            slot.SetState(RainbowMeterSlot.SlotState.NextRequired);
            Assert.AreEqual(RainbowMeterSlot.SlotState.NextRequired, slot.State);

            slot.SetState(RainbowMeterSlot.SlotState.Collected);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Collected, slot.State);

            slot.SetState(RainbowMeterSlot.SlotState.Banked);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Banked, slot.State);

            Object.DestroyImmediate(slotObj);
        }

        [Test]
        public void DynamicMeter_BuildsCorrectSlotCount_ForCustomSequences()
        {
            var hudObj = new GameObject("TestHUD");
            var hud = hudObj.AddComponent<HudController>();
            var containerObj = new GameObject("SlotsContainer", typeof(RectTransform));
            containerObj.transform.SetParent(hudObj.transform);
            containerObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();

            // 1. One gem sequence
            var seq1 = new[] { RainbowColour.Red };
            hud.BuildSequenceMeter(seq1);
            Assert.AreEqual(1, containerObj.transform.childCount);

            // 2. Three gem sequence with repeated colours [Red, Red, Orange]
            var seq3 = new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange };
            hud.BuildSequenceMeter(seq3);
            Assert.AreEqual(3, containerObj.transform.childCount);

            // 3. Ten gem sequence
            var seq10 = new[]
            {
                RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow,
                RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo,
                RainbowColour.Violet, RainbowColour.Red, RainbowColour.Green, RainbowColour.Blue
            };
            hud.BuildSequenceMeter(seq10);
            Assert.AreEqual(10, containerObj.transform.childCount);

            Object.DestroyImmediate(hudObj);
        }

        [Test]
        public void DynamicMeter_ReflectsCollectedAndBankedStates_Independently()
        {
            var def = LevelDefinition.CreateRuntimeInstance(
                levelNumber: 1,
                sequence: new[] { RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green });

            _session.LoadLevel(def);

            var hudObj = new GameObject("TestHUD");
            var hud = hudObj.AddComponent<HudController>();
            var containerObj = new GameObject("SlotsContainer", typeof(RectTransform));
            containerObj.transform.SetParent(hudObj.transform);
            containerObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();

            hud.BindEvents();
            hud.RefreshRainbowMeter();

            // Collect 1st (Red)
            _session.TryCollectGem(RainbowColour.Red);
            hud.RefreshRainbowMeter();

            // Bank Red
            _session.BankProgress();
            hud.RefreshRainbowMeter();

            // Collect 2nd (Orange)
            _session.TryCollectGem(RainbowColour.Orange);
            hud.RefreshRainbowMeter();

            // At this point:
            // Slot 0 (Red) is Banked
            // Slot 1 (Orange) is Collected (unbanked)
            // Slot 2 (Yellow) is NextRequired
            // Slot 3 (Green) is Empty

            var slots = containerObj.GetComponentsInChildren<RainbowMeterSlot>();
            Assert.AreEqual(4, slots.Length);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Banked, slots[0].State);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Collected, slots[1].State);
            Assert.AreEqual(RainbowMeterSlot.SlotState.NextRequired, slots[2].State);
            Assert.AreEqual(RainbowMeterSlot.SlotState.Empty, slots[3].State);

            Object.DestroyImmediate(hudObj);
        }
    }
}
