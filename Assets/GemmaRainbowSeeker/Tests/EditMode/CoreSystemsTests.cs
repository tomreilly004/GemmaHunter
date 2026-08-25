using NUnit.Framework;
using UnityEngine;
using GemmaRainbowSeeker;

namespace GemmaRainbowSeeker.Tests
{
    /// <summary>
    /// EditMode tests for the core progression and scoring systems.
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

        // ── Helper: build a minimal LevelRules at runtime via ScriptableObject ─
        private static LevelRules MakeRules(
            int   basePoints      = 100,
            float comboStart      = 1.0f,
            float comboIncrement  = 0.25f,
            float comboMax        = 2.5f,
            float comboWrong      = 0.5f,
            float comboMin        = 1.0f,
            int   twoStar         = 1600,
            int   threeStar       = 2200,
            int   restartPenalty  = 200)
        {
            var rules = ScriptableObject.CreateInstance<LevelRules>();
            var so    = new UnityEditor.SerializedObject(rules);

            var seq = so.FindProperty("_colourSequence");
            seq.arraySize = FullSequence.Length;
            for (int i = 0; i < FullSequence.Length; i++)
                seq.GetArrayElementAtIndex(i).enumValueIndex = (int)FullSequence[i];

            so.FindProperty("_correctGemBasePoints").intValue = basePoints;
            so.FindProperty("_comboStart").floatValue         = comboStart;
            so.FindProperty("_comboIncrement").floatValue     = comboIncrement;
            so.FindProperty("_comboMax").floatValue           = comboMax;
            so.FindProperty("_comboWrongPenalty").floatValue  = comboWrong;
            so.FindProperty("_comboMin").floatValue           = comboMin;
            so.FindProperty("_twoStarThreshold").intValue     = twoStar;
            so.FindProperty("_threeStarThreshold").intValue   = threeStar;
            so.FindProperty("_restartFromRestPenalty").intValue = restartPenalty;
            so.FindProperty("_parTimeSeconds").floatValue     = 180f;
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
            // Target starts at Red. Try every OTHER colour — none should succeed.
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

            // Now collect Red correctly, then confirm Violet still cannot be jumped to
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

            // First target is Red; try Orange (wrong)
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

            // Collect one more, then restore
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

        [Test]
        public void ResetLevelProgress_RaisesProgressChangedAndTargetChanged()
        {
            var progress = new RainbowProgress(FullSequence);
            progress.TryCollect(RainbowColour.Red);

            int progressChangedCount = 0;
            int targetChangedCount   = 0;
            progress.ProgressChanged += () => progressChangedCount++;
            progress.TargetChanged   += () => targetChangedCount++;

            progress.ResetLevelProgress();

            Assert.Greater(progressChangedCount, 0, "ProgressChanged should be raised on reset.");
            Assert.Greater(targetChangedCount,   0, "TargetChanged should be raised on reset.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ScoreManager — combo behaviour
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Combo_StartsAtConfiguredStart()
        {
            var rules   = MakeRules(comboStart: 1.0f);
            var manager = new ScoreManager(rules);

            Assert.AreEqual(1.0f, manager.Combo, 0.001f, "Combo should start at 1.0.");
        }

        [Test]
        public void Combo_IncreasesPerCorrectCollection()
        {
            var rules   = MakeRules(comboStart: 1.0f, comboIncrement: 0.25f, comboMax: 2.5f);
            var manager = new ScoreManager(rules);

            manager.RegisterCorrectCollection();
            Assert.AreEqual(1.25f, manager.Combo, 0.001f, "After 1 correct: combo should be 1.25.");

            manager.RegisterCorrectCollection();
            Assert.AreEqual(1.5f, manager.Combo, 0.001f, "After 2 corrects: combo should be 1.50.");
        }

        [Test]
        public void Combo_CapsAtMax()
        {
            var rules   = MakeRules(comboStart: 1.0f, comboIncrement: 0.25f, comboMax: 2.5f);
            var manager = new ScoreManager(rules);

            // 6 collects: 1.0 + 6×0.25 = 2.5 (exactly at cap)
            for (int i = 0; i < 10; i++)
                manager.RegisterCorrectCollection();

            Assert.AreEqual(2.5f, manager.Combo, 0.001f,
                "Combo must not exceed max (2.5) regardless of number of correct collections.");
        }

        [Test]
        public void Combo_DecreasesAfterWrongAttempt()
        {
            var rules   = MakeRules(comboStart: 1.0f, comboIncrement: 0.25f,
                                    comboMax: 2.5f, comboWrong: 0.5f, comboMin: 1.0f);
            var manager = new ScoreManager(rules);

            // Raise combo to 1.5
            manager.RegisterCorrectCollection();
            manager.RegisterCorrectCollection();
            Assert.AreEqual(1.5f, manager.Combo, 0.001f, "Sanity: combo should be 1.5.");

            manager.RegisterWrongAttempt();
            Assert.AreEqual(1.0f, manager.Combo, 0.001f,
                "After wrong attempt, combo should drop by 0.5 to 1.0.");
        }

        [Test]
        public void Combo_DoesNotDropBelowMin()
        {
            var rules   = MakeRules(comboStart: 1.0f, comboWrong: 0.5f, comboMin: 1.0f);
            var manager = new ScoreManager(rules);

            // Combo is already at min (1.0); multiple wrong attempts should not go below
            manager.RegisterWrongAttempt();
            manager.RegisterWrongAttempt();
            manager.RegisterWrongAttempt();

            Assert.AreEqual(1.0f, manager.Combo, 0.001f,
                "Combo must not drop below min (1.0) regardless of wrong attempts.");
        }

        [Test]
        public void WrongAttempt_DoesNotChangeScore()
        {
            var rules   = MakeRules(basePoints: 100, comboStart: 1.0f);
            var manager = new ScoreManager(rules);
            manager.RegisterCorrectCollection(); // score = 100

            int scoreBefore = manager.Score;
            manager.RegisterWrongAttempt();

            Assert.AreEqual(scoreBefore, manager.Score,
                "Score must not change after a wrong attempt.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ScoreManager — score never negative
        // ══════════════════════════════════════════════════════════════════════

        [Test]
        public void Score_NeverGoesNegativeAfterSubtraction()
        {
            var rules   = MakeRules(basePoints: 100);
            var manager = new ScoreManager(rules);

            // Score is 0; subtract a large amount
            manager.SubtractPoints(500);

            Assert.AreEqual(0, manager.Score,
                "Score must never go negative; clamped to 0.");
        }

        [Test]
        public void Score_NeverGoesNegativeAfterRestartPenalty()
        {
            var rules   = MakeRules(basePoints: 100, restartPenalty: 200);
            var manager = new ScoreManager(rules);

            // Earn 100 points (at combo 1.0 = 100)
            manager.RegisterCorrectCollection();
            Assert.AreEqual(100, manager.Score, "Sanity: score should be 100.");

            // Apply restart penalty of 200 (more than score)
            manager.SubtractPoints(rules.RestartFromRestPenalty);

            Assert.AreEqual(0, manager.Score,
                "Score must clamp to 0 when penalty exceeds current score.");
        }

        [Test]
        public void Score_CorrectCollectionUsesComboMultiplier()
        {
            var rules   = MakeRules(basePoints: 100, comboStart: 1.0f, comboIncrement: 0.25f);
            var manager = new ScoreManager(rules);

            // First collection: score += floor(100 * 1.0) = 100; combo becomes 1.25
            manager.RegisterCorrectCollection();
            Assert.AreEqual(100, manager.Score, "First collection should award 100 points.");

            // Second collection: score += floor(100 * 1.25) = 125; combo becomes 1.50
            manager.RegisterCorrectCollection();
            Assert.AreEqual(225, manager.Score, "Second collection should award 125 points (total 225).");
        }

        [Test]
        public void AddPoints_IncreasesScore()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);

            manager.AddPoints(50);
            Assert.AreEqual(50, manager.Score);

            manager.AddPoints(75);
            Assert.AreEqual(125, manager.Score);
        }

        [Test]
        public void SubtractPoints_DecreasesScore_Clamped()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);
            manager.AddPoints(100);

            manager.SubtractPoints(40);
            Assert.AreEqual(60, manager.Score, "Subtracting 40 from 100 should give 60.");

            manager.SubtractPoints(200);
            Assert.AreEqual(0, manager.Score, "Score should clamp to 0, not go negative.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // LevelRules — star rating helper
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

            stats.Reset();

            Assert.AreEqual(0f, stats.ElapsedSeconds,       0.001f);
            Assert.AreEqual(0,  stats.CorrectCollections);
            Assert.AreEqual(0,  stats.WrongAttempts);
            Assert.AreEqual(0,  stats.DamageTaken);
            Assert.AreEqual(0,  stats.HazardsBroken);
            Assert.AreEqual(0,  stats.RainbowRestsActivated);
            Assert.AreEqual(0,  stats.CheckpointRestarts);
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
            // 150 * 3 = 450
            Assert.AreEqual(450, manager.Score,
                "Health bonus should be 150 × 3 = 450 points.");
        }

        [Test]
        public void TimeBonusForElapsedTime_AwardsCorrectPoints()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);

            // par = 180s; elapsed = 170s; under by 10s; bonus = 10 * 5 = 50
            manager.AddTimeBonusForElapsedTime(170f);
            Assert.AreEqual(50, manager.Score,
                "Time bonus should be 10 whole seconds × 5 = 50 points.");
        }

        [Test]
        public void TimeBonusForElapsedTime_NoBonus_WhenOverPar()
        {
            var rules   = MakeRules();
            var manager = new ScoreManager(rules);

            manager.AddTimeBonusForElapsedTime(200f); // over par
            Assert.AreEqual(0, manager.Score,
                "No time bonus should be awarded when elapsed time exceeds par.");
        }
    }
}
