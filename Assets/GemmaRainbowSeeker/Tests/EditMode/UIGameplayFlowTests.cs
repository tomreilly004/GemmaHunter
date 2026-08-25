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

            // Base score for 7 gems:
            // 1st = 100 * 1.0 = 100 (combo -> 1.25)
            // 2nd = 100 * 1.25 = 125 (combo -> 1.50)
            // 3rd = 100 * 1.50 = 150 (combo -> 1.75)
            // 4th = 100 * 1.75 = 175 (combo -> 2.00)
            // 5th = 100 * 2.00 = 200 (combo -> 2.25)
            // 6th = 100 * 2.25 = 225 (combo -> 2.50)
            // 7th = 100 * 2.50 = 250 (combo -> 2.50)
            // Total gem score = 1,225 pts
            // Health bonus = 3 * 150 = 450 pts
            // Time bonus = (180 - 60) * 5 = 120 * 5 = 600 pts
            // Total score = 1,225 + 450 + 600 = 2,275 pts (>= 2200 -> 3 Stars!)

            Assert.AreEqual(450, completionData.healthBonus, "Health bonus should be 3 * 150 = 450");
            Assert.AreEqual(600, completionData.timeBonus, "Time bonus should be 120 * 5 = 600");
            Assert.AreEqual(2275, completionData.finalScore, "Final score should include base + bonuses");
            Assert.AreEqual(3, completionData.starRating, "Score >= 2200 should award 3 stars");
            Assert.AreEqual(7, completionData.correctGems);
            Assert.AreEqual(3, completionData.remainingHealth);
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
    }
}
