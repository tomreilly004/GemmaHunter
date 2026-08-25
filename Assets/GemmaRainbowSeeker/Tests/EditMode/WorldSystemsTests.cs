using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class WorldSystemsTests
    {
        private GameObject _sessionObj;
        private GameSession _session;
        private LevelRules _rules;
        private GameObject _playerObj;
        private Rigidbody2D _rb;
        private PlayerHealth _health;
        private GemmaDash _dash;
        private GemmaMotor2D _motor;

        [SetUp]
        public void Setup()
        {
            _rules = ScriptableObject.CreateInstance<LevelRules>();

            _sessionObj = new GameObject("TestGameSession");
            _session = _sessionObj.AddComponent<GameSession>();
            _session.InitializeSystems(_rules);

            _playerObj = new GameObject("TestPlayer");
            _playerObj.tag = "Player";
            _rb = _playerObj.AddComponent<Rigidbody2D>();
            _motor = _playerObj.AddComponent<GemmaMotor2D>();
            _dash = _playerObj.AddComponent<GemmaDash>();
            _health = _playerObj.AddComponent<PlayerHealth>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_playerObj != null) Object.DestroyImmediate(_playerObj);
            if (_sessionObj != null) Object.DestroyImmediate(_sessionObj);
            if (_rules != null) Object.DestroyImmediate(_rules);
        }

        [Test]
        public void PlayerHealth_TakesDamageAndEnforcesInvulnerability()
        {
            Assert.AreEqual(3, _health.CurrentHealth);

            bool damaged = _health.TakeDamage(1, Vector2.left);
            Assert.IsTrue(damaged, "First damage attempt should succeed");
            Assert.AreEqual(2, _health.CurrentHealth, "Health should drop to 2");
            Assert.IsTrue(_health.IsInvulnerable, "Player should be invulnerable after hit");

            // Immediate second hit during invulnerability
            bool secondHit = _health.TakeDamage(1, Vector2.left);
            Assert.IsFalse(secondHit, "Damage during invulnerability should be ignored");
            Assert.AreEqual(2, _health.CurrentHealth, "Health should remain 2");
        }

        [Test]
        public void Dashing_GrantsHazardImmunity()
        {
            _dash.TryDash();
            Assert.IsTrue(_dash.IsDashing, "Player should be in dashing state");

            bool damaged = _health.TakeDamage(1, Vector2.left);
            Assert.IsFalse(damaged, "Damage should be completely ignored while dashing");
            Assert.AreEqual(3, _health.CurrentHealth, "Health should remain at max 3");
        }

        [Test]
        public void PlayerHealth_ZeroHealthTriggersKnockout()
        {
            bool knockoutFired = false;
            _health.OnKnockedOut += () => knockoutFired = true;

            // Damage 3 times
            _health.TakeDamage(1, Vector2.left);
            _health.StartInvulnerability(0f); // Clear invulnerability for testing
            _health.TakeDamage(1, Vector2.left);
            _health.StartInvulnerability(0f);
            _health.TakeDamage(1, Vector2.left);

            Assert.AreEqual(0, _health.CurrentHealth);
            Assert.IsTrue(_health.IsKnockedOut, "Player should be in knocked out state");
            Assert.IsTrue(knockoutFired, "OnKnockedOut event should be fired");
        }

        [Test]
        public void BreakableHazard_NormalContactDealsDamage_DashContactBreaksAndAwardsScore()
        {
            var hazardObj = new GameObject("TestBreakableHazard");
            hazardObj.AddComponent<CircleCollider2D>();
            var hazard = hazardObj.AddComponent<BreakableHazard>();
            hazard.ConnectToSession(_session);

            // 1. Dash into hazard
            int scoreBefore = _session.ScoreManager.Score;
            _dash.TryDash();
            Assert.IsTrue(_dash.IsDashing);

            hazard.BreakHazard(); // Simulated dash contact

            Assert.IsTrue(hazard.IsBroken, "Hazard should be broken");
            Assert.AreEqual(scoreBefore + 50, _session.ScoreManager.Score, "Score should increase by 50 points");
            Assert.AreEqual(1, _session.SessionStats.HazardsBroken, "HazardsBroken stat should be 1");

            Object.DestroyImmediate(hazardObj);
        }

        [Test]
        public void BreakableHazard_ParticipatesInCheckpointRestoration()
        {
            var hazardObj = new GameObject("TestBreakableHazard");
            hazardObj.AddComponent<CircleCollider2D>();
            var hazard = hazardObj.AddComponent<BreakableHazard>();
            hazard.ConnectToSession(_session);

            // Break hazard while unbanked
            hazard.BreakHazard();
            Assert.IsTrue(hazard.IsBroken);
            Assert.IsFalse(hazard.WasBanked);

            // Restore banked progress
            _session.RestoreBankedProgress();
            Assert.IsFalse(hazard.IsBroken, "Unbanked broken hazard should be restored");

            // Break and bank
            hazard.BreakHazard();
            _session.BankProgress();
            Assert.IsTrue(hazard.WasBanked, "Hazard should be marked as banked broken");

            // Restore again
            _session.RestoreBankedProgress();
            Assert.IsTrue(hazard.IsBroken, "Banked broken hazard should remain broken");

            Object.DestroyImmediate(hazardObj);
        }

        [Test]
        public void RainbowRest_FirstActivationAwards100Points_Heals1HP_BanksProgress()
        {
            var restObj = new GameObject("TestRainbowRest");
            restObj.AddComponent<BoxCollider2D>();
            var rest = restObj.AddComponent<RainbowRest>();

            // Setup player with 2 HP and 1 collected gem
            _health.TakeDamage(1, Vector2.left);
            _health.StartInvulnerability(0f);
            Assert.AreEqual(2, _health.CurrentHealth);

            _session.TryCollectGem(RainbowColour.Red);
            Assert.AreEqual(1, _session.RainbowProgress.CollectedCount);
            Assert.AreEqual(0, _session.RainbowProgress.BankedCount);

            int scoreBefore = _session.ScoreManager.Score;

            // Activate RainbowRest
            rest.ActivateRest(_health);

            Assert.IsTrue(rest.IsActivated);
            Assert.IsTrue(rest.HasAwardedFirstBonus);
            Assert.AreEqual(1, _session.RainbowProgress.BankedCount, "Collected progress should now be banked");
            Assert.AreEqual(3, _health.CurrentHealth, "Health should heal by 1 up to 3");
            Assert.AreEqual(scoreBefore + 100, _session.ScoreManager.Score, "Score should gain 100-point first activation bonus");
            Assert.AreEqual(1, _session.SessionStats.RainbowRestsActivated);

            // Subsequent activation: no duplicate bonus
            rest.ActivateRest(_health);
            Assert.AreEqual(scoreBefore + 100, _session.ScoreManager.Score, "Score should not increase again");
            Assert.AreEqual(1, _session.SessionStats.RainbowRestsActivated);

            Object.DestroyImmediate(restObj);
        }
    }
}
