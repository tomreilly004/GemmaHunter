using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GemmaRainbowSeeker;
using System.Collections.Generic;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class Levels06To10Tests
    {
        private GameObject _sessionObj;
        private GameSession _session;
        private GameObject _playerObj;
        private Rigidbody2D _rb;
        private PlayerHealth _health;
        private GemmaDash _dash;
        private GemmaMotor2D _motor;

        [SetUp]
        public void Setup()
        {
            _sessionObj = new GameObject("TestGameSession");
            _session = _sessionObj.AddComponent<GameSession>();

            _playerObj = new GameObject("Gemma");
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
        }

        [Test]
        public void Level06_Definition_ConfiguredCorrectly()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level06.asset");
            Assert.IsNotNull(def);
            Assert.AreEqual(6, def.LevelNumber);
            Assert.AreEqual("Collect Red, Red, Orange, Orange, Yellow", def.ObjectiveDescription);
            Assert.AreEqual(5, def.ColourSequence.Length);
            Assert.AreEqual(RainbowColour.Red, def.ColourSequence[0]);
            Assert.AreEqual(RainbowColour.Red, def.ColourSequence[1]);
            Assert.AreEqual(RainbowColour.Orange, def.ColourSequence[2]);
            Assert.AreEqual(RainbowColour.Orange, def.ColourSequence[3]);
            Assert.AreEqual(RainbowColour.Yellow, def.ColourSequence[4]);
            Assert.IsTrue(def.SolidObstaclesEnabled);
            Assert.IsFalse(def.DashEnabled);
            Assert.IsFalse(def.DangerousHazardsEnabled);
        }

        [Test]
        public void Level07_Definition_ConfiguredCorrectly()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level07.asset");
            Assert.IsNotNull(def);
            Assert.AreEqual(7, def.LevelNumber);
            Assert.AreEqual(5, def.ColourSequence.Length);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsTrue(def.DangerousHazardsEnabled);
            Assert.IsTrue(def.RainbowRestsEnabled);
            Assert.IsFalse(def.DashEnabled);
        }

        [Test]
        public void Level08_Definition_ConfiguredCorrectly()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level08.asset");
            Assert.IsNotNull(def);
            Assert.AreEqual(8, def.LevelNumber);
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.HealthEnabled);
            Assert.IsTrue(def.SolidObstaclesEnabled);
            Assert.IsTrue(def.DangerousHazardsEnabled);
        }

        [Test]
        public void Level09_Definition_ConfiguredCorrectly()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level09.asset");
            Assert.IsNotNull(def);
            Assert.AreEqual(9, def.LevelNumber);
            Assert.IsTrue(def.EnemiesEnabled);
            Assert.IsTrue(def.DashEnabled);
            Assert.AreEqual(6, def.ColourSequence.Length);
        }

        [Test]
        public void Level10_Definition_ConfiguredCorrectly()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level10.asset");
            Assert.IsNotNull(def);
            Assert.AreEqual(10, def.LevelNumber);
            Assert.AreEqual(8, def.ColourSequence.Length);
            Assert.IsTrue(def.EnemiesEnabled);
            Assert.IsTrue(def.CurrentsEnabled);
            Assert.IsTrue(def.DashEnabled);
            Assert.IsTrue(def.RainbowRestsEnabled);
        }

        [Test]
        public void Damage_ResetsRainbowRush_ThroughExistingDamageEvent()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level10.asset");
            _session.InitializeSystems(def);

            // Collect 2 gems to build Rush to x3
            _session.TryCollectGem(RainbowColour.Red);
            _session.TryCollectGem(RainbowColour.Red);
            Assert.AreEqual(3, _session.RushController.Multiplier, "Rush multiplier should be x3");

            // Deal damage
            _health.TakeDamage(1, Vector2.left);

            Assert.AreEqual(1, _session.RushController.Multiplier, "Damage must reset Rush to x1");
            Assert.AreEqual(1, _session.SessionStats.DamageTaken, "DamageTaken stat must be recorded");
        }

        [Test]
        public void Gloomling_ConfiguresPatrol_AndDealsContactDamage()
        {
            var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level09.asset");
            _session.InitializeSystems(def);

            var gloomGO = new GameObject("TestGloomling");
            var col = gloomGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            var gloom = gloomGO.AddComponent<Gloomling>();
            gloom.ConfigurePatrol(Vector2.zero, new Vector2(0f, 4f), 2f, 0.5f);

            Assert.AreEqual(gloomGO.transform.position, gloom.WorldPointA);
            Assert.AreEqual(gloomGO.transform.position + new Vector3(0f, 4f, 0f), gloom.WorldPointB);

            // Test damage to health directly
            _health.TakeDamage(1, Vector2.left);
            Assert.AreEqual(2, _health.CurrentHealth, "Damage must remove 1 heart");

            Object.DestroyImmediate(gloomGO);
        }

        [Test]
        public void StormChaser_ChaseSpeedIsSlowerThanGemmaBaseSpeed()
        {
            var chaserGO = new GameObject("TestStormChaser");
            var col = chaserGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            var chaser = chaserGO.AddComponent<StormChaser>();

            Assert.IsTrue(chaser.ChaseSpeed < _motor.BaseMaxSpeed, 
                $"StormChaser chase speed ({chaser.ChaseSpeed}) must be slower than Gemma base speed ({_motor.BaseMaxSpeed})");

            Object.DestroyImmediate(chaserGO);
        }

        [Test]
        public void Progression_FromFreshSave_UnlocksLevels1Through10InOrder()
        {
            SaveManager.ResetProgress();
            Assert.AreEqual(1, SaveManager.HighestUnlockedLevel, "Fresh save must have highest unlocked level = 1");
            Assert.IsTrue(SaveManager.IsLevelUnlocked(1));
            Assert.IsFalse(SaveManager.IsLevelUnlocked(2));

            // Complete levels 1 through 9
            for (int i = 1; i <= 9; i++)
            {
                SaveManager.RecordLevelResult(i, 1500, 3);
                Assert.AreEqual(i + 1, SaveManager.HighestUnlockedLevel, $"Completing level {i} must unlock level {i + 1}");
                Assert.IsTrue(SaveManager.IsLevelUnlocked(i + 1));
            }

            Assert.IsTrue(SaveManager.IsLevelUnlocked(10), "Level 10 must be unlocked after completing Level 9");

            // Replay level 5
            SaveManager.RecordLevelResult(5, 2000, 3);
            Assert.AreEqual(10, SaveManager.HighestUnlockedLevel, "Replaying an earlier level must preserve Level 10 unlock");
        }
    }
}
