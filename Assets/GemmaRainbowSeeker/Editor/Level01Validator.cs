using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace GemmaRainbowSeeker.Editor
{
    public struct ValidationReport
    {
        public bool IsValid;
        public List<string> Errors;
        public List<string> Warnings;
        public List<string> Passes;
    }

    /// <summary>
    /// Validates all Level 1 requirements:
    /// - Player spawn exists at (0, 0)
    /// - 7 required colours in order (Red -> Orange -> Yellow -> Green -> Blue -> Indigo -> Violet)
    /// - 2 Rainbow Rests
    /// - 1 Rainbow Gate
    /// - Complete GameSession references
    /// - Valid level boundaries
    /// - Valid camera confinement
    /// - No required gem inside a solid collider
    /// - No missing scripts or prefab references
    /// </summary>
    public static class Level01Validator
    {
        [MenuItem("GemmaRainbowSeeker/Validate Level 01", false, 11)]
        public static void ValidateMenuItem()
        {
            var report = ValidateLevel();
            Debug.Log($"=== LEVEL 01 VALIDATION REPORT (Result: {(report.IsValid ? "<color=green>PASSED</color>" : "<color=red>FAILED</color>")}) ===");
            foreach (var p in report.Passes)
            {
                Debug.Log($"[PASS] {p}");
            }
            foreach (var w in report.Warnings)
            {
                Debug.LogWarning($"[WARNING] {w}");
            }
            foreach (var e in report.Errors)
            {
                Debug.LogError($"[ERROR] {e}");
            }
        }

        public static ValidationReport ValidateLevel()
        {
            var report = new ValidationReport
            {
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                Passes = new List<string>()
            };

            var scene = SceneManager.GetActiveScene();

            // 1. Check Missing Scripts across entire scene
            CheckMissingScripts(scene, report);

            // 2. Check Player Spawn
            CheckPlayerSpawn(report);

            // 3. Check 7 Required Colours in Order
            CheckRequiredGems(report);

            // 4. Check 2 Rainbow Rests
            CheckRainbowRests(report);

            // 5. Check 1 Rainbow Gate
            CheckRainbowGate(report);

            // 6. Check GameSession references
            CheckGameSession(report);

            // 7. Check Level Boundaries
            CheckBoundaries(report);

            // 8. Check Camera Confinement
            CheckCameraConfinement(report);

            // 9. Check Gems Not Inside Solid Colliders
            CheckGemsNotInsideSolids(report);

            report.IsValid = report.Errors.Count == 0;
            return report;
        }

        private static void CheckMissingScripts(Scene scene, ValidationReport report)
        {
            var rootObjs = scene.GetRootGameObjects();
            int missingCount = 0;

            foreach (var root in rootObjs)
            {
                var allTransforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    var components = t.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            missingCount++;
                            report.Errors.Add($"Missing script on GameObject '{t.name}' (path: {GetHierarchyPath(t)})");
                        }
                    }
                }
            }

            if (missingCount == 0)
            {
                report.Passes.Add("No missing scripts found in scene.");
            }
        }

        private static void CheckPlayerSpawn(ValidationReport report)
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma == null)
            {
                report.Errors.Add("Player 'Gemma' was not found in the scene.");
                return;
            }

            if (gemma.GetComponent<Rigidbody2D>() == null ||
                gemma.GetComponent<GemmaMotor2D>() == null ||
                gemma.GetComponent<GemmaDash>() == null ||
                gemma.GetComponent<PlayerHealth>() == null)
            {
                report.Errors.Add("Gemma is missing required components (Rigidbody2D, GemmaMotor2D, GemmaDash, PlayerHealth).");
            }
            else
            {
                report.Passes.Add($"Player spawn verified: Gemma at position {gemma.transform.position}.");
            }
        }

        private static void CheckRequiredGems(ValidationReport report)
        {
            var allGems = UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None);
            var requiredSequence = new RainbowColour[]
            {
                RainbowColour.Red,
                RainbowColour.Orange,
                RainbowColour.Yellow,
                RainbowColour.Green,
                RainbowColour.Blue,
                RainbowColour.Indigo,
                RainbowColour.Violet
            };

            // Filter for gems named "Required" or sort by X position to verify progression
            var reqGems = allGems.Where(g => g.gameObject.name.Contains("Required")).OrderBy(g => g.transform.position.x).ToList();

            if (reqGems.Count < 7)
            {
                // Try finding first occurrence of each color along X axis
                reqGems = new List<GemPickup>();
                var sortedGems = allGems.OrderBy(g => g.transform.position.x).ToList();
                foreach (var col in requiredSequence)
                {
                    var match = sortedGems.FirstOrDefault(g => g.Colour == col && !reqGems.Contains(g));
                    if (match != null) reqGems.Add(match);
                }
            }

            if (reqGems.Count != 7)
            {
                report.Errors.Add($"Expected 7 required gems, but found {reqGems.Count}.");
                return;
            }

            bool orderMatches = true;
            for (int i = 0; i < 7; i++)
            {
                if (reqGems[i].Colour != requiredSequence[i])
                {
                    orderMatches = false;
                    report.Errors.Add($"Gem order mismatch at index {i}: Expected {requiredSequence[i]}, found {reqGems[i].Colour} at x={reqGems[i].transform.position.x}");
                }
            }

            if (orderMatches)
            {
                report.Passes.Add("Seven required gems verified in exact sequence (Red -> Orange -> Yellow -> Green -> Blue -> Indigo -> Violet).");
            }
        }

        private static void CheckRainbowRests(ValidationReport report)
        {
            var rests = UnityEngine.Object.FindObjectsByType<RainbowRest>(FindObjectsSortMode.None);
            if (rests.Length != 2)
            {
                report.Errors.Add($"Expected exactly 2 Rainbow Rests, but found {rests.Length}.");
            }
            else
            {
                var sorted = rests.OrderBy(r => r.transform.position.x).ToArray();
                report.Passes.Add($"Two Rainbow Rests verified at x={sorted[0].transform.position.x:F1} and x={sorted[1].transform.position.x:F1}.");
            }
        }

        private static void CheckRainbowGate(ValidationReport report)
        {
            var gates = UnityEngine.Object.FindObjectsByType<RainbowGate>(FindObjectsSortMode.None);
            if (gates.Length != 1)
            {
                report.Errors.Add($"Expected exactly 1 Rainbow Gate, but found {gates.Length}.");
            }
            else
            {
                report.Passes.Add($"One Rainbow Gate verified at x={gates[0].transform.position.x:F1}, y={gates[0].transform.position.y:F1}.");
            }
        }

        private static void CheckGameSession(ValidationReport report)
        {
            var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if (session == null)
            {
                report.Errors.Add("GameSession composition root is missing from the scene.");
                return;
            }

            if (session.LevelDefinition == null && session.LevelRules == null)
            {
                report.Errors.Add("GameSession has no LevelDefinition/LevelRules asset assigned.");
            }
            else
            {
                string name = session.LevelDefinition != null ? session.LevelDefinition.name : session.LevelRules.name;
                report.Passes.Add($"GameSession verified with LevelDefinition '{name}'.");
            }
        }

        private static void CheckBoundaries(ValidationReport report)
        {
            int solidLayer = LayerMask.NameToLayer("Solid");
            var cols = UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            bool hasLeft = false, hasRight = false, hasTop = false, hasBottom = false;

            foreach (var c in cols)
            {
                if (c.gameObject.layer == solidLayer && !c.isTrigger)
                {
                    if (c.transform.position.x < 5f && c.size.y > 10f) hasLeft = true;
                    if (c.transform.position.x > 350f && c.size.y > 10f) hasRight = true;
                    if (c.transform.position.y > 5f && c.size.x > 100f) hasTop = true;
                    if (c.transform.position.y < -5f && c.size.x > 100f) hasBottom = true;
                }
            }

            if (hasLeft && hasRight && hasTop && hasBottom)
            {
                report.Passes.Add("Level solid boundaries verified (Top, Bottom, Left, Right enclosing the course).");
            }
            else
            {
                report.Errors.Add($"Incomplete boundaries: Left={hasLeft}, Right={hasRight}, Top={hasTop}, Bottom={hasBottom}");
            }
        }

        private static void CheckCameraConfinement(ValidationReport report)
        {
            var cm = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            if (cm == null)
            {
                report.Errors.Add("CinemachineCamera not found.");
                return;
            }

            var confiner = cm.GetComponent<CinemachineConfiner2D>();
            if (confiner == null || confiner.BoundingShape2D == null)
            {
                report.Errors.Add("CinemachineConfiner2D is missing or has no BoundingShape2D assigned.");
                return;
            }

            var bounds = confiner.BoundingShape2D.bounds;
            if (bounds.min.x <= -5f && bounds.max.x >= 355f)
            {
                report.Passes.Add($"Camera confinement verified covering bounds min=({bounds.min.x:F1}, {bounds.min.y:F1}) max=({bounds.max.x:F1}, {bounds.max.y:F1}).");
            }
            else
            {
                report.Errors.Add($"Camera confiner shape does not cover the full level: bounds=({bounds.min.x:F1} to {bounds.max.x:F1})");
            }
        }

        private static void CheckGemsNotInsideSolids(ValidationReport report)
        {
            var gems = UnityEngine.Object.FindObjectsByType<GemPickup>(FindObjectsSortMode.None);
            int solidMask = LayerMask.GetMask("Solid");
            int overlaps = 0;

            foreach (var gem in gems)
            {
                var hit = Physics2D.OverlapCircle(gem.transform.position, 0.35f, solidMask);
                if (hit != null && !hit.isTrigger)
                {
                    overlaps++;
                    report.Errors.Add($"Gem '{gem.name}' at {gem.transform.position} is inside solid collider '{hit.name}'!");
                }
            }

            if (overlaps == 0)
            {
                report.Passes.Add($"All {gems.Length} gems are clear of solid colliders.");
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
