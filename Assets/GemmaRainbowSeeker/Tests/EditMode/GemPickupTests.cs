using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class GemPickupTests
    {
        private GameObject _sessionObj;
        private GameSession _session;
        private LevelRules _rules;
        private GameObject _playerObj;
        private GemmaTrail _trail;
        private GameObject _gemObj;
        private GemPickup _gem;

        [SetUp]
        public void Setup()
        {
            _rules = ScriptableObject.CreateInstance<LevelRules>();

            _sessionObj = new GameObject("TestGameSession");
            _session = _sessionObj.AddComponent<GameSession>();
            _session.InitializeSystems(_rules);

            _playerObj = new GameObject("TestPlayer");
            _playerObj.tag = "Player";
            _trail = _playerObj.AddComponent<GemmaTrail>();

            _gemObj = new GameObject("TestGem");
            _gemObj.AddComponent<CircleCollider2D>();
            _gem = _gemObj.AddComponent<GemPickup>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gemObj != null) Object.DestroyImmediate(_gemObj);
            if (_playerObj != null) Object.DestroyImmediate(_playerObj);
            if (_sessionObj != null) Object.DestroyImmediate(_sessionObj);
            if (_rules != null) Object.DestroyImmediate(_rules);
        }

        [Test]
        public void CorrectCollection_DisablesGemAndUpdatesProgressAndScore()
        {
            _gem.Initialize(RainbowColour.Red, _session);

            int initialScore = _session.ScoreManager.Score;
            float initialCombo = _session.ScoreManager.Combo;

            bool collected = _gem.AttemptPickup(_playerObj, Vector2.up);

            Assert.IsTrue(collected, "Red gem should be successfully collected as first target");
            Assert.IsTrue(_gem.IsCollected, "Gem should be marked as collected");
            Assert.IsFalse(_gem.GetComponent<Collider2D>().enabled, "Collider should be disabled on collection");
            Assert.AreEqual(1, _session.RainbowProgress.CollectedCount, "RainbowProgress count should advance to 1");
            Assert.AreEqual(RainbowColour.Orange, _session.RainbowProgress.CurrentTarget, "Next target should be Orange");
            Assert.AreEqual(initialScore + 100, _session.ScoreManager.Score, "Score should increase by 100 * combo");
            Assert.AreEqual(initialCombo + 0.25f, _session.ScoreManager.Combo, "Combo should increase by 0.25");
            Assert.AreEqual(1, _session.SessionStats.CorrectCollections, "Session stats correct count should be 1");
        }

        [Test]
        public void WrongRejection_DoesNotCollect_ReducesCombo_AppliesCooldown()
        {
            // Boost combo first
            _session.ScoreManager.RegisterCorrectCollection();
            float elevatedCombo = _session.ScoreManager.Combo; // 1.25

            _gem.Initialize(RainbowColour.Violet, _session); // Violet is wrong when target is Orange (or Red)

            bool collected = _gem.AttemptPickup(_playerObj, Vector2.up);

            Assert.IsFalse(collected, "Violet gem should be rejected");
            Assert.IsFalse(_gem.IsCollected, "Gem should NOT be marked as collected");
            Assert.IsTrue(_gem.GetComponent<Collider2D>().enabled, "Collider should remain enabled");
            Assert.IsTrue(_gem.IsOnRejectionCooldown, "Rejection cooldown should be active");
            Assert.Less(_session.ScoreManager.Combo, elevatedCombo, "Combo should decrease on wrong attempt");
            Assert.AreEqual(1, _session.SessionStats.WrongAttempts, "Session stats wrong attempts should be 1");

            // Attempt repeat pickup immediately during cooldown
            bool repeatAttempt = _gem.AttemptPickup(_playerObj, Vector2.up);
            Assert.IsFalse(repeatAttempt, "Repeat pickup during cooldown should be blocked");
            Assert.AreEqual(1, _session.SessionStats.WrongAttempts, "Wrong attempts count should not increment during cooldown");
        }

        [Test]
        public void Restoration_ReactivatesUnbankedGem_KeepsBankedGemDisabled()
        {
            // 1. Create Red and Orange gems
            var redObj = new GameObject("RedGem");
            redObj.AddComponent<CircleCollider2D>();
            var redGem = redObj.AddComponent<GemPickup>();
            redGem.Initialize(RainbowColour.Red, _session);

            var orangeObj = new GameObject("OrangeGem");
            orangeObj.AddComponent<CircleCollider2D>();
            var orangeGem = orangeObj.AddComponent<GemPickup>();
            orangeGem.Initialize(RainbowColour.Orange, _session);

            // 2. Collect Red
            redGem.AttemptPickup(_playerObj, Vector2.up);
            Assert.IsTrue(redGem.IsCollected);
            Assert.AreEqual(1, _session.RainbowProgress.CollectedCount);

            // 3. Bank progress at Checkpoint (BankedCount = 1)
            _session.BankProgress();
            Assert.AreEqual(1, _session.RainbowProgress.BankedCount);
            Assert.IsTrue(redGem.WasBanked, "Red gem should be marked as banked");

            // 4. Collect Orange (unbanked)
            orangeGem.AttemptPickup(_playerObj, Vector2.up);
            Assert.IsTrue(orangeGem.IsCollected);
            Assert.IsFalse(orangeGem.WasBanked, "Orange gem should not be banked yet");
            Assert.AreEqual(2, _session.RainbowProgress.CollectedCount);

            // 5. Restore banked progress (e.g. after knockout restart from Rest)
            _session.RestoreBankedProgress();
            Assert.AreEqual(1, _session.RainbowProgress.CollectedCount, "Collected count should revert to banked count (1)");
            Assert.AreEqual(RainbowColour.Orange, _session.RainbowProgress.CurrentTarget, "Target should be Orange again");

            // 6. Verify: Red stays collected/disabled, Orange reactivates
            Assert.IsTrue(redGem.IsCollected, "Banked Red gem should remain collected");
            Assert.IsFalse(redGem.GetComponent<Collider2D>().enabled, "Banked Red collider should remain disabled");

            Assert.IsFalse(orangeGem.IsCollected, "Unbanked Orange gem should be restored/uncollected");
            Assert.IsTrue(orangeGem.GetComponent<Collider2D>().enabled, "Unbanked Orange collider should be reactivated");

            Object.DestroyImmediate(redObj);
            Object.DestroyImmediate(orangeObj);
        }
    }
}
