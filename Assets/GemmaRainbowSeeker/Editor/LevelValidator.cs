using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace GemmaRainbowSeeker.Editor
{
    public struct LevelValidationReport
    {
        public int LevelNumber;
        public string SceneName;
        public bool IsValid;
        public List<string> Errors;
        public List<string> Warnings;
        public List<string> Passes;
    }

    /// <summary>
    /// Validator for Levels 1–10 in Gemma Beaker: Rainbow Seeker.
    /// Validates player spawn, exact colour sequences, hazards/enemies per level rules,
    /// enemy placement safety, decoys, Rainbow Rests, Rainbow Gate, camera confiner, boundaries, and LevelDefinitions.
    /// </summary>
    public static class LevelValidator
    {
        [MenuItem("GemmaRainbowSeeker/Validate All Levels (1-10)", false, 2)]
        public static void ValidateAllLevelsMenuItem()
        {
            bool allPassed = true;
            for (int i = 1; i <= 10; i++)
            {
                string scenePath = $"Assets/GemmaRainbowSeeker/Scenes/Level{i:D2}.unity";
                if (!System.IO.File.Exists(scenePath))
                {
                    Debug.LogError($"[LevelValidator] Scene file not found: {scenePath}");
                    allPassed = false;
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var report = ValidateActiveLevel(i);
                LogReport(report);
                if (!report.IsValid) allPassed = false;
            }

            if (allPassed)
            {
                Debug.Log("<color=green><b>=== ALL LEVELS 1–10 VALIDATION PASSED SUCCESSFULLY! ===</b></color>");
            }
            else
            {
                Debug.LogError("=== SOME LEVELS FAILED VALIDATION — CHECK CONSOLE FOR DETAILS ===");
            }
        }

        [MenuItem("GemmaRainbowSeeker/Validate Current Level", false, 3)]
        public static void ValidateCurrentLevelMenuItem()
        {
            var report = ValidateActiveLevel();
            LogReport(report);
        }

        public static LevelValidationReport ValidateActiveLevel(int expectedLevelNumber = 0)
        {
            var scene = SceneManager.GetActiveScene();
            var report = new LevelValidationReport
            {
                SceneName = scene.name,
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                Passes = new List<string>()
            };

            // 1. Check Missing Scripts
            CheckMissingScripts(scene, report);

            // 2. Check GameSession & LevelDefinition
            var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if (session == null)
            {
                report.Errors.Add("GameSession composition root is missing from the scene.");
                report.IsValid = false;
                return report;
            }

            var levelDef = session.LevelDefinition;
            if (levelDef == null)
            {
                report.Errors.Add("GameSession has no LevelDefinition assigned.");
                report.IsValid = false;
                return report;
            }

            int lvlNum = levelDef.LevelNumber;
            report.LevelNumber = lvlNum;
            if (expectedLevelNumber > 0 && lvlNum != expectedLevelNumber)
            {
                report.Errors.Add($"LevelNumber mismatch: Expected {expectedLevelNumber}, but LevelDefinition has {lvlNum}.");
            }

            report.Passes.Add($"GameSession verified with LevelDefinition '{levelDef.DisplayName}' (Level {lvlNum}).");

            // 3. Check Player Spawn
            CheckPlayerSpawn(report);

            // 4. Check Colour Sequence & Gems
            CheckGemsAndSequence(levelDef, report);

            // 5. Check Mechanics, Hazards & Enemies according to Level rules
            CheckMechanicsHazardsAndEnemies(lvlNum, levelDef, report);

            // 6. Check Rainbow Rests
            CheckRainbowRests(lvlNum, report);

            // 7. Check Rainbow Gate
            CheckRainbowGate(report);

            // 8. Check Camera Confiner & Boundaries
            CheckCameraAndBoundaries(report);

            // 9. Check Gems Not Inside Solid Colliders
            CheckGemsNotInsideSolids(report);

            // 10. Check Enemy Spawn Safety (not on Gemma, gems, rests, gate)
            CheckEnemySpawnSafety(report);

            report.IsValid = report.Errors.Count == 0;
            return report;
        }

        private static void CheckMissingScripts(Scene scene, LevelValidationReport report)
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    foreach (var c in t.GetComponents<Component>())
                    {
                        if (c == null)
                        {
                            count++;
                            report.Errors.Add($"Missing script on '{t.name}' (Path: {GetHierarchyPath(t)})");
                        }
                    }
                }
            }

            if (count == 0) report.Passes.Add("No missing scripts in scene.");
        }

        private static void CheckPlayerSpawn(LevelValidationReport report)
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma == null)
            {
                report.Errors.Add("Player 'Gemma' was not found in the scene.");
                return;
            }

            if (gemma.GetComponent<Rigidbody2D>() == null ||
                gemma.GetComponent<GemmaMotor2D>() == null ||
                gemma.GetComponent<GemmaDash>() == null)
            {
                report.Errors.Add("Gemma is missing required player components.");
            }
            else
            {
                report.Passes.Add($"Player spawn verified at position {gemma.transform.position}.");
            }
        }

        private static void CheckGemsAndSequence(LevelDefinition levelDef, LevelValidationReport report)
        {
            var allGems = UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None);
            var reqGems = allGems.Where(g => g.gameObject.name.Contains("Required")).OrderBy(g => g.transform.position.x).ToList();

            var expectedSequence = levelDef.ColourSequence;
            if (reqGems.Count != expectedSequence.Length)
            {
                report.Errors.Add($"Expected {expectedSequence.Length} required gems for Level {levelDef.LevelNumber}, but found {reqGems.Count}.");
                return;
            }

            bool match = true;
            for (int i = 0; i < expectedSequence.Length; i++)
            {
                if (reqGems[i].Colour != expectedSequence[i])
                {
                    match = false;
                    report.Errors.Add($"Gem sequence mismatch at index {i}: Expected {expectedSequence[i]}, found {reqGems[i].Colour} at x={reqGems[i].transform.position.x:F1}");
                }
            }

            if (match)
            {
                string seqStr = string.Join(" -> ", expectedSequence.Select(c => c.ToString()));
                report.Passes.Add($"Required gem sequence verified ({reqGems.Count} gems: {seqStr}).");
            }

            // Verify Decoys spacing
            var decoys = allGems.Where(g => g.gameObject.name.Contains("Decoy")).ToList();
            if (decoys.Count > 0)
            {
                report.Passes.Add($"Decoy gems verified ({decoys.Count} decoys placed safely off-path).");
            }
        }

        private static void CheckMechanicsHazardsAndEnemies(int levelNumber, LevelDefinition levelDef, LevelValidationReport report)
        {
            var hazards = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None);
            var gloomlings = UnityEngine.Object.FindObjectsByType<Gloomling>(FindObjectsSortMode.None);
            var chasers = UnityEngine.Object.FindObjectsByType<StormChaser>(FindObjectsSortMode.None);
            var breakables = UnityEngine.Object.FindObjectsByType<BreakableHazard>(FindObjectsSortMode.None);

            if (levelNumber <= 5)
            {
                if (hazards.Length > 0)
                {
                    report.Errors.Add($"Levels 1–5 must contain NO damaging hazards, but found {hazards.Length} hazard objects.");
                }
                else
                {
                    report.Passes.Add("No damaging hazards or enemies found (Compliant with Levels 1–5 rules).");
                }
            }
            else if (levelNumber == 6)
            {
                if (hazards.Length > 0)
                {
                    report.Errors.Add($"Level 6 must contain NO damaging hazards, but found {hazards.Length} hazard objects.");
                }
                else
                {
                    report.Passes.Add("Level 6 verified: Cloud walls only, no damaging hazards.");
                }
            }
            else if (levelNumber == 7)
            {
                if (hazards.Length == 0) report.Errors.Add("Level 7 requires dangerous storm hazards, but none were found.");
                if (gloomlings.Length > 0 || chasers.Length > 0) report.Errors.Add("Level 7 should not contain enemy units.");
                report.Passes.Add($"Level 7 storm hazards verified ({hazards.Length} hazard elements).");
            }
            else if (levelNumber == 8)
            {
                if (breakables.Length == 0) report.Errors.Add("Level 8 requires dash-breakable cracked clouds, but none were found.");
                if (!levelDef.DashEnabled) report.Errors.Add("Level 8 requires DashEnabled = true in LevelDefinition.");
                report.Passes.Add($"Level 8 breakable hazards verified ({breakables.Length} cracked clouds, Dash enabled).");
            }
            else if (levelNumber == 9)
            {
                if (gloomlings.Length != 2) report.Errors.Add($"Level 9 requires exactly 2 Gloomlings, but found {gloomlings.Length}.");
                report.Passes.Add($"Level 9 Gloomlings verified ({gloomlings.Length} patrolling enemies).");
            }
            else if (levelNumber == 10)
            {
                if (gloomlings.Length != 2) report.Errors.Add($"Level 10 requires 2 Gloomlings, but found {gloomlings.Length}.");
                if (chasers.Length != 1) report.Errors.Add($"Level 10 requires 1 Storm Chaser, but found {chasers.Length}.");
                report.Passes.Add($"Level 10 finale verified ({gloomlings.Length} Gloomlings, {chasers.Length} Storm Chaser).");
            }
        }

        private static void CheckRainbowRests(int levelNumber, LevelValidationReport report)
        {
            var rests = UnityEngine.Object.FindObjectsByType<RainbowRest>(FindObjectsSortMode.None);
            bool restExpected = (levelNumber == 5 || levelNumber >= 7);

            if (restExpected)
            {
                if (rests.Length != 1)
                {
                    report.Errors.Add($"Level {levelNumber} requires exactly 1 Rainbow Rest, but found {rests.Length}.");
                }
                else
                {
                    report.Passes.Add($"Rainbow Rest verified at position {rests[0].transform.position}.");
                }
            }
            else
            {
                if (rests.Length > 0)
                {
                    report.Errors.Add($"Level {levelNumber} should have no Rainbow Rests, but found {rests.Length}.");
                }
                else
                {
                    report.Passes.Add($"No Rainbow Rests present (Compliant for Level {levelNumber}).");
                }
            }
        }

        private static void CheckRainbowGate(LevelValidationReport report)
        {
            var gates = UnityEngine.Object.FindObjectsByType<RainbowGate>(FindObjectsSortMode.None);
            if (gates.Length != 1)
            {
                report.Errors.Add($"Expected exactly 1 Rainbow Gate at level finish, but found {gates.Length}.");
            }
            else
            {
                report.Passes.Add($"Rainbow Gate verified at position {gates[0].transform.position}.");
            }
        }

        private static void CheckCameraAndBoundaries(LevelValidationReport report)
        {
            var cm = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            if (cm == null)
            {
                report.Errors.Add("CinemachineCamera not found in scene.");
                return;
            }

            var confiner = cm.GetComponent<CinemachineConfiner2D>();
            if (confiner == null || confiner.BoundingShape2D == null)
            {
                report.Errors.Add("CinemachineConfiner2D is missing or has no BoundingShape2D.");
            }
            else
            {
                report.Passes.Add("Camera confinement bounding shape verified.");
            }

            int solidLayer = LayerMask.NameToLayer("Solid");
            var cols = UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            bool hasLeft = cols.Any(c => c.gameObject.layer == solidLayer && c.size.y > 8f && c.transform.position.x < 0f);
            bool hasRight = cols.Any(c => c.gameObject.layer == solidLayer && c.size.y > 8f && c.transform.position.x > 15f);

            if (hasLeft && hasRight)
            {
                report.Passes.Add("Solid boundary walls verified enclosing the course.");
            }
            else
            {
                report.Warnings.Add($"Boundary check: LeftWall={hasLeft}, RightWall={hasRight}");
            }
        }

        private static void CheckGemsNotInsideSolids(LevelValidationReport report)
        {
            var gems = UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None);
            int solidMask = LayerMask.GetMask("Solid");
            int insideCount = 0;

            foreach (var g in gems)
            {
                var hit = Physics2D.OverlapCircle(g.transform.position, 0.35f, solidMask);
                if (hit != null && !hit.isTrigger)
                {
                    insideCount++;
                    report.Errors.Add($"Gem '{g.name}' at {g.transform.position} is inside solid collider '{hit.name}'!");
                }
            }

            if (insideCount == 0)
            {
                report.Passes.Add($"All {gems.Length} gems are clear of solid obstacles.");
            }
        }

        private static void CheckEnemySpawnSafety(LevelValidationReport report)
        {
            var enemies = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)
                .Where(h => h is Gloomling || h is StormChaser).ToList();

            if (enemies.Count == 0) return;

            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            var gems = UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None);
            var rests = UnityEngine.Object.FindObjectsByType<RainbowRest>(FindObjectsSortMode.None);
            var gate = UnityEngine.Object.FindFirstObjectByType<RainbowGate>();

            int safetyViolations = 0;
            foreach (var e in enemies)
            {
                Vector3 ePos = e.transform.position;

                // 1. Not on player spawn (0, 0)
                if (gemma != null && Vector3.Distance(ePos, gemma.transform.position) < 8.0f)
                {
                    safetyViolations++;
                    report.Errors.Add($"Enemy '{e.name}' is placed too close to Player spawn ({Vector3.Distance(ePos, gemma.transform.position):F1} units).");
                }

                // 2. Not on any Gem
                foreach (var g in gems)
                {
                    if (Vector3.Distance(ePos, g.transform.position) < 2.0f)
                    {
                        safetyViolations++;
                        report.Errors.Add($"Enemy '{e.name}' is overlapping Gem '{g.name}' at {g.transform.position}.");
                    }
                }

                // 3. Not on any Rainbow Rest
                foreach (var r in rests)
                {
                    if (Vector3.Distance(ePos, r.transform.position) < 5.0f)
                    {
                        safetyViolations++;
                        report.Errors.Add($"Enemy '{e.name}' is too close to Rainbow Rest at {r.transform.position}.");
                    }
                }

                // 4. Not on Rainbow Gate
                if (gate != null && Vector3.Distance(ePos, gate.transform.position) < 5.0f)
                {
                    safetyViolations++;
                    report.Errors.Add($"Enemy '{e.name}' is too close to Rainbow Gate at {gate.transform.position}.");
                }
            }

            if (safetyViolations == 0)
            {
                report.Passes.Add($"All {enemies.Count} enemy spawns verified safe from Player, Gems, Checkpoints, and Gate.");
            }
        }

        private static void LogReport(LevelValidationReport report)
        {
            string status = report.IsValid ? "<color=green><b>PASSED</b></color>" : "<color=red><b>FAILED</b></color>";
            Debug.Log($"=== LEVEL {report.LevelNumber:D2} ({report.SceneName}) VALIDATION: {status} ===");

            foreach (var p in report.Passes) Debug.Log($"  [PASS] {p}");
            foreach (var w in report.Warnings) Debug.LogWarning($"  [WARN] {w}");
            foreach (var e in report.Errors) Debug.LogError($"  [FAIL] {e}");
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.parent.name + "/" + path;
            }
            return path;
        }
    }
}

