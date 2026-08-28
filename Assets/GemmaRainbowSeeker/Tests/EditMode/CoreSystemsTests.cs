using NUnit.Framework;
using UnityEngine;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    /// <summary>
    /// EditMode tests for the core progression, Rainbow Rush multiplier, and scoring systems.
    /// All tests use plain C# instances — no MonoBehaviours, no scene loading.
    /// </summary>
    public class CoreSystemsTests
    {
        // ── Shared sequence ───────────────────────────────────────────────────
        private static readonly RainbowColour[] FullSequence = new[]
        {
            RainbowColour.Red,
            RainbowColour.Orange,
            RainbowColour.Yellow,
            RainbowColour.Green,
            RainbowColour.Blue,
            RainbowColour.Indigo,
            RainbowColour.Violet
        };

        // ── Helper: build a minimal LevelDefinition at runtime via ScriptableObject ─
        private static LevelDefinition MakeRules(
            int   basePoints     = 100,
            float rushWindow     = 30f,
            int   twoStar        = 1600,
            int   threeStar      = 2200,
            int   restartPenalty = 200)
        {
            var rules = ScriptableObject.CreateInstance<LevelDefinition>();
            var so    = new UnityEditor.SerializedObject(rules);

            var seq = so.FindProperty("_colourSequence");
            seq.arraySize = FullSequence.Length;
            for (int i = 0; i < FullSequence.Length; i++)
                seq.GetArrayElementAtIndex(i).enumValueIndex = (int)FullSequence[i];

            so.FindProperty("_correctGemBasePoints").intValue = basePoints;
            so.FindProperty("_rainbowRushTimeWindow").floatValue = rushWindow;
            so.FindProperty("_twoStarThreshold").intValue     = twoStar;
            so.FindProperty("_threeStarThreshold").intValue   = threeStar;
            so.FindProperty("_restartFromRestPenalty").intValue = restartPenalty;
            so.FindProperty("_targetCompletionTime").floatValue = 180f;
            so.FindProperty("_timeBonusPerSecondUnderPar").intValue = 5;
            so.FindProperty("_completionHealthBonusPerPip").intValue = 150;
            so.FindProperty("_hazardBreakPoints").intValue    = 50;
            so.FindProperty("_rainbowRestFirstActivationPoints").intValue = 100;

            so.ApplyModifiedPropertiesWithoutUndo();
            return rules;
        }

        // ══════════════════════════════════════════════════════════════════════
        // RainbowProgress — in-order collection
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void CollectingInOrder_CompletesRainbow()
        {
            var progress = new RainbowProgress(FullSequence);

            foreach (var colour in FullSequence)
                Assert.IsTrue(progress.TryCollect(colour),
                    $"Expected TryCollect({colour}) to return true when collecting in order.");

            Assert.IsTrue(progress.IsComplete,
                "Rainbow should be complete after collecting all colours in order.");
        }

        [Test]
        public void CollectingOutOfOrder_NeverCompletes()
        {
            var progress = new RainbowProgress(FullSequence);
            var wrongWhenRedIsTarget = new[]
            {
                RainbowColour.Orange,
                RainbowColour.Yellow,
                RainbowColour.Green,
                RainbowColour.Blue,
                RainbowColour.Indigo,
                RainbowColour.Violet
            };

            foreach (var colour in wrongWhenRedIsTarget)
                Assert.IsFalse(progress.TryCollect(colour),
                    $"Expected TryCollect({colour}) to return false when Red is the required target.");

            Assert.IsFalse(progress.IsComplete,
                "Rainbow must not be complete after only wrong-order attempts.");
            Assert.AreEqual(0, progress.CollectedCount,
                "Collected count must remain 0 after all wrong-order attempts.");

            Assert.IsTrue(progress.TryCollect(RainbowColour.Red),
                "Collecting Red (the correct target) must succeed.");
            Assert.AreEqual(1, progress.CollectedCount, "Count should be 1 after Red.");

            Assert.IsFalse(progress.TryCollect(RainbowColour.Violet),
                "Collecting Violet when Orange is required must still return false.");
            Assert.AreEqual(1, progress.CollectedCount,
                "Count must remain 1 after attempting wrong colour Violet.");
        }

        [Test]
        public void WrongColour_DoesNotAdvanceCollectedCount()
        {
            var progress = new RainbowProgress(FullSequence);

            bool result = progress.TryCollect(RainbowColour.Orange);

            Assert.IsFalse(result, "TryCollect with wrong colour should return false.");
            Assert.AreEqual(0, progress.CollectedCount,
                "CollectedCount must not advance after a wrong attempt.");
            Assert.AreEqual(RainbowColour.Red, progress.CurrentTarget,
                "CurrentTarget must remain Red after a wrong attempt.");
        }

        [Test]
        public void WrongColour_RaisesIncorrectColourAttemptedEvent()
        {
            var progress = new RainbowProgress(FullSequence);
            RainbowColour? raised = null;
            progress.IncorrectColourAttempted += c => raised = c;

            progress.TryCollect(RainbowColour.Blue); // wrong

            Assert.AreEqual(RainbowColour.Blue, raised,
                "IncorrectColourAttempted should carry the attempted colour.");
        }

        [Test]
        public void CorrectColour_RaisesCorrectColourCollectedEvent()
        {
            var progress = new RainbowProgress(FullSequence);
            RainbowColour? raised = null;
            progress.CorrectColourCollected += c => raised = c;

            progress.TryCollect(RainbowColour.Red);

            Assert.AreEqual(RainbowColour.Red, raised,
                "CorrectColourCollected should carry the collected colour.");
        }

        [Test]
        public void CompletingRainbow_RaisesRainbowCompletedEvent()
        {
            var progress = new RainbowProgress(FullSequence);
            bool completedRaised = false;
            progress.RainbowCompleted += () => completedRaised = true;

            foreach (var colour in FullSequence)
                progress.TryCollect(colour);

            Assert.IsTrue(completedRaised, "RainbowCompleted event should be raised.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // RainbowProgress — bank / restore / reset
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void BankCurrentProgress_RecordsCurrentCollectedCount()
        {
            var progress = new RainbowProgress(FullSequence);
            progress.TryCollect(RainbowColour.Red);
            progress.TryCollect(RainbowColour.Orange);

            progress.BankCurrentProgress();

            Assert.AreEqual(2, progress.BankedCount,
                "BankedCount should equal the collected count at bank time.");
        }

        [Test]
        public void RestoreBankedProgress_ReturnsToBankedCount()
        {
            var progress = new RainbowProgress(FullSequence);
            progress.TryCollect(RainbowColour.Red);
            progress.TryCollect(RainbowColour.Orange);
            progress.BankCurrentProgress(); // banked = 2

            progress.TryCollect(RainbowColour.Yellow);
            Assert.AreEqual(3, progress.CollectedCount, "Sanity: should be 3 before restore.");

            progress.RestoreBankedProgress();

            Assert.AreEqual(2, progress.CollectedCount,
                "CollectedCount should be restored to banked count (2).");
            Assert.AreEqual(RainbowColour.Yellow, progress.CurrentTarget,
                "After restoring to 2, current target should be Yellow.");
        }

        [Test]
        public void ResetLevelProgress_ClearsBothCountsToZero()
        {
            var progress = new RainbowProgress(FullSequence);
            progress.TryCollect(RainbowColour.Red);
            progress.TryCollect(RainbowColour.Orange);
            progress.BankCurrentProgress();
            progress.TryCollect(RainbowColour.Yellow);

            progress.ResetLevelProgress();

            Assert.AreEqual(0, progress.CollectedCount, "CollectedCount must be 0 after reset.");
            Assert.AreEqual(0, progress.BankedCount,    "BankedCount must be 0 after reset.");
            Assert.AreEqual(RainbowColour.Red, progress.CurrentTarget,
                "CurrentTarget must be Red after full reset.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // RainbowRushController — Multiplier, Cap, Timer Refresh, Resets
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Rush_StartsAtMultiplierX1()
        {
            var rush = new RainbowRushController(rushWindow: 30f);
            Assert.AreEqual(1, rush.Multiplier, "Rush must start at multiplier x1.");
            Assert.AreEqual(0f, rush.RemainingTime, "Rush timer should start at 0 before first collection.");
            Assert.IsFalse(rush.IsRushActive, "Rush should not be active at x1.");
        }

        [Test]
        public void Rush_FirstGem_ScoresAtX1_AndRaisesToX2()
        {
            var rush = new RainbowRushController(rushWindow: 30f);

            int scoreMul = rush.RegisterCorrectCollection();

            Assert.AreEqual(1, scoreMul, "First correct gem must score at x1.");
            Assert.AreEqual(2, rush.Multiplier, "First correct gem must raise Rush to x2.");
            Assert.AreEqual(30f, rush.RemainingTime, 0.001f, "Rush timer should begin and set to full window.");
            Assert.IsTrue(rush.IsRushActive, "Rush is active at x2.");
        }

        [Test]
        public void Rush_SubsequentGems_ScoreAtCurrentMultiplier_AndRaiseTier()
        {
            var rush = new RainbowRushController(rushWindow: 30f);

            // Gem 1: scores at x1, becomes x2
            Assert.AreEqual(1, rush.RegisterCorrectCollection());
            Assert.AreEqual(2, rush.Multiplier);

            // Gem 2: scores at x2, becomes x3
            Assert.AreEqual(2, rush.RegisterCorrectCollection());
            Assert.AreEqual(3, rush.Multiplier);

            // Gem 3: scores at x3, becomes x4
            Assert.AreEqual(3, rush.RegisterCorrectCollection());
            Assert.AreEqual(4, rush.Multiplier);

            // Gem 4: scores at x4, becomes x5
            Assert.AreEqual(4, rush.RegisterCorrectCollection());
            Assert.AreEqual(5, rush.Multiplier);
        }

        [Test]
        public void Rush_CapsAtMultiplierX5()
        {
            var rush = new RainbowRushController(rushWindow: 30f);

            for (int i = 0; i < 4; i++)
            {
                rush.RegisterCorrectCollection();
            }
            Assert.AreEqual(5, rush.Multiplier, "Sanity: tier should be x5 after 4 collections.");

            // 5th, 6th, 7th gem collections: score at x5 and remain at x5
            for (int i = 0; i < 5; i++)
            {
                int scoreMul = rush.RegisterCorrectCollection();
                Assert.AreEqual(5, scoreMul, "Subsequent collections at max must score at x5.");
                Assert.AreEqual(5, rush.Multiplier, "Multiplier must never exceed x5.");
            }
        }

        [Test]
        public void Rush_CorrectGem_RefreshesTimer()
        {
            var rush = new RainbowRushController(rushWindow: 30f);
            rush.RegisterCorrectCollection(); // Rush -> x2, time = 30f

            // Tick 10 seconds of active movement
            rush.Tick(10f, Vector2.right, new Vector2(5f, 0f), false);
            Assert.AreEqual(20f, rush.RemainingTime, 0.01f, "Remaining time should be 20s after 10s tick.");

            // Collect next gem
            rush.RegisterCorrectCollection();
            Assert.AreEqual(30f, rush.RemainingTime, 0.001f, "Next correct gem must refresh remaining Rush time back to 30s.");
        }

        [Test]
        public void Rush_TimerExpired_ResetsToX1()
        {
            var rush = new RainbowRushController(rushWindow: 10f);
            rush.RegisterCorrectCollection(); // x2, time = 10f

            RushResetReason? resetReason = null;
            rush.OnRushReset += r => resetReason = r;

            // Tick 10.1s (past expiration) with active movement
            rush.Tick(10.1f, Vector2.right, new Vector2(5f, 0f), false);

            Assert.AreEqual(1, rush.Multiplier, "Rush multiplier must reset to x1 when timer expires.");
            Assert.AreEqual(RushResetReason.TimerExpired, resetReason, "Reset reason must be TimerExpired.");
            Assert.AreEqual(RushResetReason.TimerExpired, rush.LastResetReason);
            Assert.AreEqual(1, rush.RushBreakCount, "Rush break count should increment.");
        }

        [Test]
        public void Rush_WrongGem_ResetsImmediately()
        {
            var rush = new RainbowRushController(rushWindow: 30f);
            rush.RegisterCorrectCollection(); // x2
            rush.RegisterCorrectCollection(); // x3

            RushResetReason? resetReason = null;
            rush.OnRushReset += r => resetReason = r;

            rush.ResetRush(RushResetReason.WrongColour);

            Assert.AreEqual(1, rush.Multiplier, "Multiplier must reset immediately to x1 on wrong gem.");
            Assert.AreEqual(RushResetReason.WrongColour, resetReason);
            Assert.AreEqual(0f, rush.RemainingTime);
        }

        [Test]
        public void Rush_DamageAndKnockout_ResetsImmediately()
        {
            var rush = new RainbowRushController(rushWindow: 30f);
            rush.RegisterCorrectCollection(); // x2

            rush.ResetRush(RushResetReason.Damage);
            Assert.AreEqual(1, rush.Multiplier, "Taking damage must reset Rush to x1.");
            Assert.AreEqual(RushResetReason.Damage, rush.LastResetReason);

            rush.RegisterCorrectCollection(); // x2 again
            rush.ResetRush(RushResetReason.KnockedOut);
            Assert.AreEqual(1, rush.Multiplier, "Knockout must reset Rush to x1.");
            Assert.AreEqual(RushResetReason.KnockedOut, rush.LastResetReason);
        }

        [Test]
        public void Rush_CheckpointRestart_ResetsImmediately()
        {
            var rush = new RainbowRushController(rushWindow: 30f);
            rush.RegisterCorrectCollection(); // x2

            rush.ResetRush(RushResetReason.Restart);
            Assert.AreEqual(1, rush.Multiplier, "Checkpoint restart must reset Rush to x1.");
            Assert.AreEqual(RushResetReason.Restart, rush.LastResetReason);
        }

        [Test]
        public void Rush_StoppedMovement_ResetsAfterGracePeriod()
        {
            var rush = new RainbowRushController(rushWindow: 30f, stopGrace: 0.45f);
            rush.RegisterCorrectCollection(); // x2

            RushResetReason? resetReason = null;
            rush.OnRushReset += r => resetReason = r;

            // Stop movement (input=0, speed=0) for 0.30s (< 0.45s grace period)
            rush.Tick(0.30f, Vector2.zero, Vector2.zero, false);
            Assert.AreEqual(2, rush.Multiplier, "Rush must not reset before grace period expires.");
            Assert.IsNull(resetReason);

            // Tick remaining 0.20s (total stop time = 0.50s > 0.45s)
            rush.Tick(0.20f, Vector2.zero, Vector2.zero, false);
            Assert.AreEqual(1, rush.Multiplier, "Rush must reset to x1 after stop duration exceeds 0.45s.");
            Assert.AreEqual(RushResetReason.Stopped, resetReason);
        }

        [Test]
        public void Rush_BriefStopUnderGracePeriod_DoesNotResetIfMovementResumes()
        {
            var rush = new RainbowRushController(rushWindow: 30f, stopGrace: 0.45f);
            rush.RegisterCorrectCollection(); // x2

            // Stop for 0.3s
            rush.Tick(0.30f, Vector2.zero, Vector2.zero, false);
            Assert.AreEqual(0.30f, rush.StopTimer, 0.01f);
            Assert.AreEqual(2, rush.Multiplier);

            // Move again! (input active)
            rush.Tick(0.10f, Vector2.right, new Vector2(3f, 0f), false);
            Assert.AreEqual(0f, rush.StopTimer, "Stop timer must reset to 0 upon resuming movement.");
            Assert.AreEqual(2, rush.Multiplier, "Rush multiplier remains x2.");

            // Stop again for another 0.3s (still < 0.45s)
            rush.Tick(0.30f, Vector2.zero, Vector2.zero, false);
            Assert.AreEqual(2, rush.Multiplier, "New stop begins from 0 and does not trigger reset.");
        }

        [Test]
        public void Rush_SuspendedState_PausesCountdownAndStopDetection()
        {
            var rush = new RainbowRushController(rushWindow: 30f, stopGrace: 0.45f);
            rush.RegisterCorrectCollection(); // x2, time = 30f

            // Suspended tick (e.g. paused or tutorial open) with zero movement
            rush.Tick(5.0f, Vector2.zero, Vector2.zero, true);

            Assert.AreEqual(30f, rush.RemainingTime, 0.001f, "Timer must not tick while suspended.");
            Assert.AreEqual(0f, rush.StopTimer, "Stop timer must not accumulate while suspended.");
            Assert.AreEqual(2, rush.Multiplier, "Multiplier must remain intact while suspended.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Speed Modifiers — 0%, 6%, 12%, 18%, 24%
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Rush_SpeedBonusTiers_MatchSpecification()
        {
            Assert.AreEqual(0.00f, RainbowRushController.GetSpeedBonusForTier(1), 0.001f, "x1 speed bonus should be 0%");
            Assert.AreEqual(0.06f, RainbowRushController.GetSpeedBonusForTier(2), 0.001f, "x2 speed bonus should be 6%");
            Assert.AreEqual(0.12f, RainbowRushController.GetSpeedBonusForTier(3), 0.001f, "x3 speed bonus should be 12%");
            Assert.AreEqual(0.18f, RainbowRushController.GetSpeedBonusForTier(4), 0.001f, "x4 speed bonus should be 18%");
            Assert.AreEqual(0.24f, RainbowRushController.GetSpeedBonusForTier(5), 0.001f, "x5 speed bonus should be 24%");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ScoreManager & Scoring with Rush Multiplier
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void ScoreManager_RegisterCorrectCollection_UsesRushMultiplier()
        {
            var rules   = MakeRules(basePoints: 100);
            var manager = new ScoreManager(rules);

            // Gem 1 scored at x1: +100
            manager.RegisterCorrectCollection(1);
            Assert.AreEqual(100, manager.Score);

            // Gem 2 scored at x2: +200 (total 300)
            manager.RegisterCorrectCollection(2);
            Assert.AreEqual(300, manager.Score);

            // Gem 3 scored at x3: +300 (total 600)
            manager.RegisterCorrectCollection(3);
            Assert.AreEqual(600, manager.Score);

            // Gem 4 scored at x4: +400 (total 1000)
            manager.RegisterCorrectCollection(4);
            Assert.AreEqual(1000, manager.Score);

            // Gem 5 scored at x5: +500 (total 1500)
            manager.RegisterCorrectCollection(5);
            Assert.AreEqual(1500, manager.Score);
        }

        [Test]
        public void Score_NeverGoesNegativeAfterPenalty()
        {
            var rules   = MakeRules(basePoints: 100, restartPenalty: 200);
            var manager = new ScoreManager(rules);

            manager.RegisterCorrectCollection(1); // score = 100
            Assert.AreEqual(100, manager.Score);

            manager.SubtractPoints(rules.RestartFromRestPenalty); // subtract 200
            Assert.AreEqual(0, manager.Score, "Score must clamp to 0 when penalty exceeds current score.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // LevelDefinition — Star Ratings & Helpers
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void StarRating_OneStar_AtAnyCompletionScore()
        {
            var rules = MakeRules(twoStar: 1600, threeStar: 2200);
            Assert.AreEqual(1, rules.ComputeStarRating(0),
                "Score 0 on completion should give 1 star.");
            Assert.AreEqual(1, rules.ComputeStarRating(1599),
                "Score 1599 should give 1 star (just below two-star threshold).");
        }

        [Test]
        public void StarRating_TwoStars_AtThreshold()
        {
            var rules = MakeRules(twoStar: 1600, threeStar: 2200);
            Assert.AreEqual(2, rules.ComputeStarRating(1600),
                "Score 1600 should give exactly 2 stars.");
            Assert.AreEqual(2, rules.ComputeStarRating(2199),
                "Score 2199 should give 2 stars (just below three-star threshold).");
        }

        [Test]
        public void StarRating_ThreeStars_AtThreshold()
        {
            var rules = MakeRules(twoStar: 1600, threeStar: 2200);
            Assert.AreEqual(3, rules.ComputeStarRating(2200),
                "Score 2200 should give exactly 3 stars.");
            Assert.AreEqual(3, rules.ComputeStarRating(9999),
                "Very high score should give 3 stars.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // LevelSessionStats
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void SessionStats_Tick_AccumulatesElapsedTime()
        {
            var stats = new LevelSessionStats();
            stats.Tick(1.5f);
            stats.Tick(0.5f);

            Assert.AreEqual(2.0f, stats.ElapsedSeconds, 0.001f,
                "ElapsedSeconds should accumulate across Tick calls.");
        }

        [Test]
        public void SessionStats_Reset_ClearsAllCounters()
        {
            var stats = new LevelSessionStats();
            stats.Tick(10f);
            stats.RecordCorrectCollection();
            stats.RecordWrongAttempt();
            stats.RecordDamageTaken();
            stats.RecordHazardBroken();
            stats.RecordRainbowRestActivated();
            stats.RecordCheckpointRestart();
            stats.UpdateRushStats(5, 45f, 2);

            stats.Reset();

            Assert.AreEqual(0f, stats.ElapsedSeconds,       0.001f);
            Assert.AreEqual(0,  stats.CorrectCollections);
            Assert.AreEqual(0,  stats.WrongAttempts);
            Assert.AreEqual(0,  stats.DamageTaken);
            Assert.AreEqual(0,  stats.HazardsBroken);
            Assert.AreEqual(0,  stats.RainbowRestsActivated);
            Assert.AreEqual(0,  stats.CheckpointRestarts);
            Assert.AreEqual(1,  stats.HighestMultiplier);
            Assert.AreEqual(0f, stats.LongestRushDuration);
            Assert.AreEqual(0,  stats.RushBreaks);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ScoreManager — completion bonuses
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void CompletionHealthBonus_AwardsCorrectPoints()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);

            manager.AddCompletionHealthBonus(3);
            Assert.AreEqual(450, manager.Score,
                "Health bonus should be 150 × 3 = 450 points.");
        }

        [Test]
        public void TimeBonusForElapsedTime_AwardsCorrectPoints()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);

            manager.AddTimeBonusForElapsedTime(170f);
            Assert.AreEqual(50, manager.Score,
                "Time bonus should be 10 whole seconds × 5 = 50 points.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Data-Driven Mobile Sequences
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void OneGemSequence_CompletesImmediatelyOnCorrectCollection()
        {
            var seq = new[] { RainbowColour.Red };
            var progress = new RainbowProgress(seq);

            Assert.AreEqual(1, progress.TotalCount);
            Assert.AreEqual(0, progress.CollectedCount);
            Assert.AreEqual(RainbowColour.Red, progress.CurrentTarget);
            Assert.IsFalse(progress.IsComplete);

            bool collectCorrect = progress.TryCollect(RainbowColour.Red);
            Assert.IsTrue(collectCorrect);
            Assert.AreEqual(1, progress.CollectedCount);
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void RepeatedColours_RedRedOrange_CollectsInExactOrder()
        {
            var seq = new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange };
            var progress = new RainbowProgress(seq);

            Assert.AreEqual(3, progress.TotalCount);
            Assert.IsTrue(progress.TryCollect(RainbowColour.Red));
            Assert.IsFalse(progress.TryCollect(RainbowColour.Orange));
            Assert.IsTrue(progress.TryCollect(RainbowColour.Red));
            Assert.IsTrue(progress.TryCollect(RainbowColour.Orange));
            Assert.IsTrue(progress.IsComplete);
        }

        [Test]
        public void TenGemSequence_CompletesSuccessfully()
        {
            var seq = new[]
            {
                RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow,
                RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo,
                RainbowColour.Violet, RainbowColour.Red, RainbowColour.Green, RainbowColour.Blue
            };
            var progress = new RainbowProgress(seq);

            for (int i = 0; i < seq.Length; i++)
            {
                Assert.IsTrue(progress.TryCollect(seq[i]));
            }

            Assert.IsTrue(progress.IsComplete);
        }
    }
}

