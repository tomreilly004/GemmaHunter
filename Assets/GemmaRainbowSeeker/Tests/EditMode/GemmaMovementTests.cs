using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    [TestFixture]
    public class GemmaMovementTests
    {
        private GameObject _gemmaObj;
        private Rigidbody2D _rb;
        private GemmaMotor2D _motor;
        private GemmaDash _dash;

        [SetUp]
        public void Setup()
        {
            _gemmaObj = new GameObject("TestGemma");
            _rb = _gemmaObj.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            _gemmaObj.AddComponent<CircleCollider2D>();
            _motor = _gemmaObj.AddComponent<GemmaMotor2D>();
            _dash = _gemmaObj.AddComponent<GemmaDash>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gemmaObj != null)
            {
                Object.DestroyImmediate(_gemmaObj);
            }
        }

        [Test]
        public void SwimMovement_AcceleratesTowardMaxSpeed()
        {
            _motor.SetMoveInput(Vector2.right);

            // Simulate fixed updates
            var fixedUpdateMethod = typeof(GemmaMotor2D).GetMethod("FixedUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            for (int i = 0; i < 30; i++)
            {
                fixedUpdateMethod.Invoke(_motor, null);
            }

            Assert.Greater(_rb.linearVelocity.x, 3.0f, "Gemma should accelerate rightward");
            Assert.LessOrEqual(_rb.linearVelocity.x, _motor.MaxSpeed + 0.01f, "Speed should not exceed maxSpeed");
        }

        [Test]
        public void SwimMovement_DeceleratesToZero_WhenInputReleased()
        {
            var fixedUpdateMethod = typeof(GemmaMotor2D).GetMethod("FixedUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Accelerate first
            _motor.SetMoveInput(Vector2.right);
            for (int i = 0; i < 30; i++) fixedUpdateMethod.Invoke(_motor, null);
            Assert.Greater(_rb.linearVelocity.magnitude, 2.0f);

            // Release input
            _motor.SetMoveInput(Vector2.zero);
            for (int i = 0; i < 60; i++) fixedUpdateMethod.Invoke(_motor, null);

            Assert.Less(_rb.linearVelocity.magnitude, 0.1f, "Gemma should decelerate to near zero");
        }

        [Test]
        public void DiagonalInput_IsNormalised()
        {
            _motor.SetMoveInput(new Vector2(1f, 1f));
            Assert.LessOrEqual(_motor.MoveInput.magnitude, 1.001f, "Diagonal input magnitude must be normalised to <= 1");
        }

        [Test]
        public void Dash_TriggersBurstAndEnforcesCooldown()
        {
            bool dashStartedFired = false;
            _dash.OnDashStarted += (dir) => dashStartedFired = true;

            _motor.SetMoveInput(Vector2.right);
            bool dashSuccess = _dash.TryDash();

            Assert.IsTrue(dashSuccess, "First dash attempt should succeed");
            Assert.IsTrue(_dash.IsDashing, "IsDashing should be true during dash");
            Assert.IsTrue(dashStartedFired, "OnDashStarted event should be invoked");

            // Attempt repeat dash immediately during cooldown
            bool repeatDash = _dash.TryDash();
            Assert.IsFalse(repeatDash, "Repeat dash should be blocked during cooldown/active dash");

            Assert.IsTrue(_dash.IsOnCooldown, "Dash should be on cooldown");
            Assert.Greater(_dash.CooldownRemaining, 0f, "Cooldown timer should be positive");
        }

        [Test]
        public void Dash_UsesLastFacing_WhenNoInputHeld()
        {
            // Set input to left then release
            _motor.SetMoveInput(Vector2.left);
            Assert.AreEqual(Vector2.left, _motor.LastNonZeroDirection);

            _motor.SetMoveInput(Vector2.zero);
            Assert.AreEqual(Vector2.zero, _motor.MoveInput);
            Assert.AreEqual(Vector2.left, _motor.LastNonZeroDirection);

            Vector2 dashedDir = Vector2.zero;
            _dash.OnDashStarted += (dir) => dashedDir = dir;

            _dash.TryDash();
            Assert.AreEqual(Vector2.left, dashedDir, "Dash should propel in last facing direction when stationary");
        }

        [Test]
        public void SolidCollision_StopsGemmaFromPenetratingWall()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int solidLayer = LayerMask.NameToLayer("Solid");
            if (playerLayer >= 0) _gemmaObj.layer = playerLayer;

            _gemmaObj.transform.position = new Vector2(0f, 0f);

            // Create solid wall at x = 2
            var wall = new GameObject("SolidWall");
            if (solidLayer >= 0) wall.layer = solidLayer;
            wall.transform.position = new Vector2(2f, 0f);
            var wallCol = wall.AddComponent<BoxCollider2D>();
            wallCol.size = new Vector2(1f, 10f);

            // Move Gemma right towards wall with physics simulation
            _rb.linearVelocity = new Vector2(5.8f, 0f);

            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < 40; i++)
            {
                Physics2D.Simulate(0.02f);
            }
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;

            // Gemma's collider (radius 0.5) against wall edge at x = 1.5 should stop before x = 1.6
            Assert.Less(_rb.position.x, 1.6f, "Solid wall should stop Gemma from penetrating");

            Object.DestroyImmediate(wall);
        }
    }
}
