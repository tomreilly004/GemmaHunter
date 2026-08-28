using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace GemmaRainbowSeeker.Editor
{
    /// <summary>
    /// Idempotent Level Builder for Levels 1–10 in Gemma Beaker: Rainbow Seeker.
    /// Builds separate LevelDefinition assets, TutorialSequence assets, and scenes Level01..Level10.
    /// Each level replaces only its specific generated root (e.g. GENERATED_Level01).
    /// </summary>
    public static class LevelBuilder
    {
        // Sprite pack paths
        private const string SpritePackBackgrounds = "Assets/Sprites/Gemma_Beaker_Sprite_Pack/gemma_beaker_sprite_pack/backgrounds/";
        private const string SpriteSolidSquare     = "Assets/GemmaRainbowSeeker/Art/Placeholders/Solid_Square.png";

        // Prefab paths
        private const string PrefabGemma            = "Assets/GemmaRainbowSeeker/Prefabs/Gemma.prefab";
        private const string PrefabRainbowRest      = "Assets/GemmaRainbowSeeker/Prefabs/RainbowRest.prefab";
        private const string PrefabRainbowGate      = "Assets/GemmaRainbowSeeker/Prefabs/RainbowGate.prefab";
        private const string PrefabCurrentRight     = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_Right.prefab";
        private const string PrefabCurrentUpRight   = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_UpRight.prefab";
        private const string PrefabCurrentDownRight = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_DownRight.prefab";

        private const string PrefabPlatformCloud    = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Cloud.prefab";
        private const string PrefabHazardContact    = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_ContactCloud.prefab";
        private const string PrefabHazardBreakable  = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_BreakableCloud.prefab";
        private const string PrefabHazardMoving     = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_MovingCloud.prefab";
        private const string PrefabEnemyGloomling   = "Assets/GemmaRainbowSeeker/Prefabs/Enemy_Gloomling.prefab";
        private const string PrefabEnemyStormChaser = "Assets/GemmaRainbowSeeker/Prefabs/Enemy_StormChaser.prefab";

        private const string PrefabGemRed    = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Red.prefab";
        private const string PrefabGemOrange = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Orange.prefab";
        private const string PrefabGemYellow = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Yellow.prefab";
        private const string PrefabGemGreen  = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Green.prefab";
        private const string PrefabGemBlue   = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Blue.prefab";
        private const string PrefabGemIndigo = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Indigo.prefab";
        private const string PrefabGemViolet = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Violet.prefab";

        [MenuItem("GemmaRainbowSeeker/Build All Levels (1-10)", false, 1)]
        public static void BuildAllLevels()
        {
            BuildDataAssets();
            for (int i = 1; i <= 10; i++)
            {
                BuildLevel(i);
            }
            RegisterScenesInBuildSettings();
            Debug.Log("<color=green><b>[LevelBuilder] All Levels 1–10 built, configured, and registered successfully!</b></color>");
        }

        [MenuItem("GemmaRainbowSeeker/Build Level 01 (First Spark)", false, 11)]
        public static void BuildLevel01Menu() => BuildLevel(1);

        [MenuItem("GemmaRainbowSeeker/Build Level 02 (Rainbow Rush)", false, 12)]
        public static void BuildLevel02Menu() => BuildLevel(2);

        [MenuItem("GemmaRainbowSeeker/Build Level 03 (Choose Carefully)", false, 13)]
        public static void BuildLevel03Menu() => BuildLevel(3);

        [MenuItem("GemmaRainbowSeeker/Build Level 04 (Stay In Flow)", false, 14)]
        public static void BuildLevel04Menu() => BuildLevel(4);

        [MenuItem("GemmaRainbowSeeker/Build Level 05 (Make A Rainbow)", false, 15)]
        public static void BuildLevel05Menu() => BuildLevel(5);

        [MenuItem("GemmaRainbowSeeker/Build Level 06 (Cloud Weave)", false, 16)]
        public static void BuildLevel06Menu() => BuildLevel(6);

        [MenuItem("GemmaRainbowSeeker/Build Level 07 (Storm Warning)", false, 17)]
        public static void BuildLevel07Menu() => BuildLevel(7);

        [MenuItem("GemmaRainbowSeeker/Build Level 08 (Breakthrough)", false, 18)]
        public static void BuildLevel08Menu() => BuildLevel(8);

        [MenuItem("GemmaRainbowSeeker/Build Level 09 (Gloom Patrol)", false, 19)]
        public static void BuildLevel09Menu() => BuildLevel(9);

        [MenuItem("GemmaRainbowSeeker/Build Level 10 (Rainbow Run)", false, 20)]
        public static void BuildLevel10Menu() => BuildLevel(10);

        // ── Data Assets Generation ────────────────────────────────────────────

        public static void BuildDataAssets()
        {
            EnsureFolder("Assets/GemmaRainbowSeeker/Data");

            // ── Level 1 ──
            var tut1 = CreateOrGetTutorialSequence(1, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L01_Swim",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "HOW TO PLAY",
                    body = "Drag the joystick to swim.\nCollect the RED gem ahead!",
                    controlsHint = "MOVE: Joystick / WASD / D-Pad",
                    displayDuration = 4.5f,
                    showOnce = true
                },
                new TutorialSequence.TutorialStep
                {
                    stepId = "L01_Gate",
                    triggerEvent = TutorialTriggerEvent.OnRainbowComplete,
                    title = "GATE UNLOCKED",
                    body = "Now enter the Rainbow Gate to complete the level!",
                    controlsHint = "Enter the Rainbow Gate ahead!",
                    displayDuration = 4.5f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(1,
                name: "Level 01 — First Spark",
                scene: "Level01",
                sequence: new[] { RainbowColour.Red },
                objective: "Collect 1 red gem",
                targetTime: 15f,
                rushWindow: 0f,
                twoStar: 120,
                threeStar: 140,
                dash: false,
                health: false,
                currents: false,
                solids: false,
                hazards: false,
                rests: false,
                enemies: false,
                tutSequence: tut1
            );

            // ── Level 2 ──
            var tut2 = CreateOrGetTutorialSequence(2, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L02_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "RAINBOW RUSH",
                    body = "Correct gems build RAINBOW RUSH!\nKeep moving for more score and speed.",
                    controlsHint = "Collect in sequence: Red -> Red -> Orange",
                    displayDuration = 4.5f,
                    showOnce = true
                },
                new TutorialSequence.TutorialStep
                {
                    stepId = "L02_RushBroken",
                    triggerEvent = TutorialTriggerEvent.OnRushBroken,
                    title = "RUSH LOST",
                    body = "Stopping or waiting breaks your Rush.\nKeep up momentum to sustain high multipliers!",
                    controlsHint = "",
                    displayDuration = 4.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(2,
                name: "Level 02 — Rainbow Rush",
                scene: "Level02",
                sequence: new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange },
                objective: "Collect Red, Red, Orange",
                targetTime: 25f,
                rushWindow: 6.0f,
                twoStar: 400,
                threeStar: 600,
                dash: false,
                health: false,
                currents: false,
                solids: false,
                hazards: false,
                rests: false,
                enemies: false,
                tutSequence: tut2
            );

            // ── Level 3 ──
            var tut3 = CreateOrGetTutorialSequence(3, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L03_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "CHOOSE CAREFULLY",
                    body = "Collect the colour shown in the meter.\nA wrong colour resets Rainbow Rush.",
                    controlsHint = "Sequence: Orange -> Orange -> Yellow -> Yellow",
                    displayDuration = 4.5f,
                    showOnce = true
                },
                new TutorialSequence.TutorialStep
                {
                    stepId = "L03_Wrong",
                    triggerEvent = TutorialTriggerEvent.OnFirstWrongGem,
                    title = "WRONG COLOUR",
                    body = "Touching a wrong colour resets Rainbow Rush to x1.\nCheck the meter to stay on target!",
                    controlsHint = "",
                    displayDuration = 4.5f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(3,
                name: "Level 03 — Choose Carefully",
                scene: "Level03",
                sequence: new[] { RainbowColour.Orange, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Yellow },
                objective: "Collect Orange, Orange, Yellow, Yellow",
                targetTime: 30f,
                rushWindow: 5.5f,
                twoStar: 700,
                threeStar: 1000,
                dash: false,
                health: false,
                currents: false,
                solids: false,
                hazards: false,
                rests: false,
                enemies: false,
                tutSequence: tut3
            );

            // ── Level 4 ──
            var tut4 = CreateOrGetTutorialSequence(4, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L04_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "STAY IN FLOW",
                    body = "Collect the next gem before the Rush ring empties.\nUse magical currents to keep your speed.",
                    controlsHint = "Sequence: Yellow -> Yellow -> Green -> Green -> Blue",
                    displayDuration = 4.5f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(4,
                name: "Level 04 — Stay In Flow",
                scene: "Level04",
                sequence: new[] { RainbowColour.Yellow, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Green, RainbowColour.Blue },
                objective: "Collect Yellow, Yellow, Green, Green, Blue",
                targetTime: 35f,
                rushWindow: 5.0f,
                twoStar: 1100,
                threeStar: 1500,
                dash: false,
                health: false,
                currents: true,
                solids: false,
                hazards: false,
                rests: false,
                enemies: false,
                tutSequence: tut4
            );

            // ── Level 5 ──
            var tut5 = CreateOrGetTutorialSequence(5, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L05_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "MAKE A RAINBOW",
                    body = "Pass through a Rainbow Rest to bank your colours.\nComplete all seven colours to open the Gate.",
                    controlsHint = "Sequence: R -> O -> Y -> G -> B -> I -> V",
                    displayDuration = 5.0f,
                    showOnce = true
                },
                new TutorialSequence.TutorialStep
                {
                    stepId = "L05_Gate",
                    triggerEvent = TutorialTriggerEvent.OnRainbowComplete,
                    title = "RAINBOW COMPLETE!",
                    body = "All seven colours collected! Fly through the Rainbow Gate to finish!",
                    controlsHint = "",
                    displayDuration = 4.5f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(5,
                name: "Level 05 — Make A Rainbow",
                scene: "Level05",
                sequence: new[] { RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo, RainbowColour.Violet },
                objective: "Collect all 7 rainbow colours in order",
                targetTime: 50f,
                rushWindow: 4.75f,
                twoStar: 1800,
                threeStar: 2500,
                dash: false,
                health: false,
                currents: true,
                solids: false,
                hazards: false,
                rests: true,
                enemies: false,
                tutSequence: tut5
            );

            // ── Level 6 ──
            var tut6 = CreateOrGetTutorialSequence(6, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L06_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "CLOUD WEAVE",
                    body = "Cloud walls block your path. Keep flowing around them.",
                    controlsHint = "Sequence: Red -> Red -> Orange -> Orange -> Yellow",
                    displayDuration = 5.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(6,
                name: "Level 06 — Cloud Weave",
                scene: "Level06",
                sequence: new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange, RainbowColour.Orange, RainbowColour.Yellow },
                objective: "Collect Red, Red, Orange, Orange, Yellow",
                targetTime: 50f,
                rushWindow: 4.75f,
                twoStar: 1400,
                threeStar: 2000,
                dash: false,
                health: false,
                currents: false,
                solids: true,
                hazards: false,
                rests: false,
                enemies: false,
                tutSequence: tut6
            );

            // ── Level 7 ──
            var tut7 = CreateOrGetTutorialSequence(7, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L07_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "STORM WARNING",
                    body = "Storm clouds cost a heart and reset Rainbow Rush.",
                    controlsHint = "Avoid red storm clouds! Use the Rainbow Rest to bank.",
                    displayDuration = 5.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(7,
                name: "Level 07 — Storm Warning",
                scene: "Level07",
                sequence: new[] { RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue },
                objective: "Collect Orange, Yellow, Yellow, Green, Blue",
                targetTime: 55f,
                rushWindow: 4.5f,
                twoStar: 1600,
                threeStar: 2300,
                dash: false,
                health: true,
                currents: false,
                solids: false,
                hazards: true,
                rests: true,
                enemies: false,
                tutSequence: tut7
            );

            // ── Level 8 ──
            var tut8 = CreateOrGetTutorialSequence(8, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L08_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "DASH ABILITY",
                    body = "Tap DASH for a burst of speed.\nDash through cracked purple clouds to break them!",
                    controlsHint = "DASH: Space / Gamepad South / Dash Button",
                    displayDuration = 5.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(8,
                name: "Level 08 — Breakthrough",
                scene: "Level08",
                sequence: new[] { RainbowColour.Green, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo, RainbowColour.Indigo },
                objective: "Collect Green, Green, Blue, Indigo, Indigo",
                targetTime: 60f,
                rushWindow: 4.5f,
                twoStar: 1800,
                threeStar: 2600,
                dash: true,
                health: true,
                currents: false,
                solids: true,
                hazards: true,
                rests: true,
                enemies: false,
                tutSequence: tut8
            );

            // ── Level 9 ──
            var tut9 = CreateOrGetTutorialSequence(9, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L09_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "GLOOM PATROL",
                    body = "Gloomlings patrol the sky. Watch their movement and slip past.",
                    controlsHint = "Time your swim carefully to pass patrolling Gloomlings!",
                    displayDuration = 5.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(9,
                name: "Level 09 — Gloom Patrol",
                scene: "Level09",
                sequence: new[] { RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo },
                objective: "Collect Red, Orange, Yellow, Green, Blue, Indigo",
                targetTime: 65f,
                rushWindow: 4.25f,
                twoStar: 2200,
                threeStar: 3200,
                dash: true,
                health: true,
                currents: false,
                solids: true,
                hazards: true,
                rests: true,
                enemies: true,
                tutSequence: tut9
            );

            // ── Level 10 ──
            var tut10 = CreateOrGetTutorialSequence(10, new[]
            {
                new TutorialSequence.TutorialStep
                {
                    stepId = "L10_Start",
                    triggerEvent = TutorialTriggerEvent.OnLevelStart,
                    title = "RAINBOW RUN",
                    body = "Storm Chasers follow you. Keep moving and use your route.",
                    controlsHint = "Outswim the Storm Chaser! Safe zones protect you.",
                    displayDuration = 5.0f,
                    showOnce = true
                }
            });

            CreateOrGetLevelDefinition(10,
                name: "Level 10 — Rainbow Run",
                scene: "Level10",
                sequence: new[] { RainbowColour.Red, RainbowColour.Red, RainbowColour.Orange, RainbowColour.Yellow, RainbowColour.Green, RainbowColour.Blue, RainbowColour.Indigo, RainbowColour.Violet },
                objective: "Collect Red, Red, Orange, Yellow, Green, Blue, Indigo, Violet",
                targetTime: 80f,
                rushWindow: 4.0f,
                twoStar: 3000,
                threeStar: 4200,
                dash: true,
                health: true,
                currents: true,
                solids: true,
                hazards: true,
                rests: true,
                enemies: true,
                tutSequence: tut10
            );

            AssetDatabase.SaveAssets();
        }

        private static TutorialSequence CreateOrGetTutorialSequence(int levelNumber, IEnumerable<TutorialSequence.TutorialStep> steps)
        {
            string path = $"Assets/GemmaRainbowSeeker/Data/TutorialSequence_Level{levelNumber:D2}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TutorialSequence>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TutorialSequence>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.ClearSteps();
            foreach (var s in steps) asset.AddStep(s);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static LevelDefinition CreateOrGetLevelDefinition(
            int levelNumber,
            string name,
            string scene,
            RainbowColour[] sequence,
            string objective,
            float targetTime,
            float rushWindow,
            int twoStar,
            int threeStar,
            bool dash,
            bool health,
            bool currents,
            bool solids,
            bool hazards,
            bool rests,
            bool enemies,
            TutorialSequence tutSequence)
        {
            string path = $"Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level{levelNumber:D2}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LevelDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            SetField(asset, "_levelNumber", levelNumber);
            SetField(asset, "_displayName", name);
            SetField(asset, "_sceneName", scene);
            SetField(asset, "_colourSequence", new List<RainbowColour>(sequence));
            SetField(asset, "_objectiveDescription", objective);
            SetField(asset, "_targetCompletionTime", targetTime);
            SetField(asset, "_rainbowRushTimeWindow", rushWindow);
            SetField(asset, "_twoStarThreshold", twoStar);
            SetField(asset, "_threeStarThreshold", threeStar);
            SetField(asset, "_dashEnabled", dash);
            SetField(asset, "_healthEnabled", health);
            SetField(asset, "_currentsEnabled", currents);
            SetField(asset, "_solidObstaclesEnabled", solids);
            SetField(asset, "_dangerousHazardsEnabled", hazards);
            SetField(asset, "_enemiesEnabled", enemies);
            SetField(asset, "_rainbowRestsEnabled", rests);
            SetField(asset, "_tutorialSequence", tutSequence);

            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ── Scene Building ────────────────────────────────────────────────────

        public static void BuildLevel(int levelNumber)
        {
            BuildDataAssets();

            string scenePath = $"Assets/GemmaRainbowSeeker/Scenes/Level{levelNumber:D2}.unity";
            Scene scene;

            if (System.IO.File.Exists(scenePath))
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            else
            {
                // Clone from Level01 as a template to preserve full UI/Camera setup
                string templatePath = "Assets/GemmaRainbowSeeker/Scenes/Level01.unity";
                if (System.IO.File.Exists(templatePath))
                {
                    AssetDatabase.CopyAsset(templatePath, scenePath);
                    AssetDatabase.Refresh();
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                    EditorSceneManager.SaveScene(scene, scenePath);
                }
            }

            string rootName = $"GENERATED_Level{levelNumber:D2}";
            Undo.SetCurrentGroupName($"Build {rootName}");
            int group = Undo.GetCurrentGroup();

            // 1. Clean up old test objects and previous generated roots
            var rootObjects = scene.GetRootGameObjects();
            foreach (var r in rootObjects)
            {
                if (r.name.StartsWith("Test") || r.name.StartsWith("GENERATED_"))
                {
                    Undo.DestroyObjectImmediate(r);
                }
            }

            // Clean Environment placeholders
            var envRoot = GameObject.Find("Environment");
            if (envRoot != null)
            {
                for (int i = envRoot.transform.childCount - 1; i >= 0; i--)
                {
                    var child = envRoot.transform.GetChild(i).gameObject;
                    if (child.name != "LevelConfinerBounds")
                    {
                        Undo.DestroyObjectImmediate(child);
                    }
                }
            }

            // Clean Gameplay placeholders
            var gameplayRoot = GameObject.Find("Gameplay");
            if (gameplayRoot != null)
            {
                for (int i = gameplayRoot.transform.childCount - 1; i >= 0; i--)
                {
                    var child = gameplayRoot.transform.GetChild(i).gameObject;
                    if (child.name != "Gemma")
                    {
                        Undo.DestroyObjectImmediate(child);
                    }
                }
            }

            // 2. Create the new generated root
            var genRoot = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(genRoot, "Create " + rootName);

            var backgroundsContainer = CreateChild(genRoot, "Backgrounds");
            var boundariesContainer  = CreateChild(genRoot, "Boundaries");
            var platformsContainer   = CreateChild(genRoot, "Platforms");
            var currentsContainer    = CreateChild(genRoot, "Currents");
            var hazardsContainer     = CreateChild(genRoot, "Hazards");
            var enemiesContainer     = CreateChild(genRoot, "Enemies");
            var checkpointsContainer = CreateChild(genRoot, "Checkpoints");
            var collectiblesContainer = CreateChild(genRoot, "Collectibles");
            var gateContainer        = CreateChild(genRoot, "RainbowGate");

            // 3. Setup Gemma Player Spawn at (0, 0)
            SetupPlayerSpawn();

            // 4. Setup Level-Specific Dimensions and Content
            float endX = 40f;
            switch (levelNumber)
            {
                case 1:
                    endX = BuildLevel01Content(collectiblesContainer, gateContainer);
                    break;
                case 2:
                    endX = BuildLevel02Content(collectiblesContainer, gateContainer);
                    break;
                case 3:
                    endX = BuildLevel03Content(collectiblesContainer, gateContainer);
                    break;
                case 4:
                    endX = BuildLevel04Content(collectiblesContainer, currentsContainer, gateContainer);
                    break;
                case 5:
                    endX = BuildLevel05Content(collectiblesContainer, currentsContainer, checkpointsContainer, gateContainer);
                    break;
                case 6:
                    endX = BuildLevel06Content(collectiblesContainer, platformsContainer, gateContainer);
                    break;
                case 7:
                    endX = BuildLevel07Content(collectiblesContainer, hazardsContainer, checkpointsContainer, gateContainer);
                    break;
                case 8:
                    endX = BuildLevel08Content(collectiblesContainer, platformsContainer, hazardsContainer, checkpointsContainer, gateContainer);
                    break;
                case 9:
                    endX = BuildLevel09Content(collectiblesContainer, platformsContainer, hazardsContainer, enemiesContainer, checkpointsContainer, gateContainer);
                    break;
                case 10:
                    endX = BuildLevel10Content(collectiblesContainer, platformsContainer, currentsContainer, hazardsContainer, enemiesContainer, checkpointsContainer, gateContainer);
                    break;
            }

            // 5. Setup Camera Confiner and Invisible Boundaries
            float minX = -6f;
            float maxX = endX + 12f;
            SetupCameraConfiner(minX - 4f, maxX + 4f, -7f, 7f);
            BuildBoundaries(boundariesContainer, minX, maxX, -6.5f, 6.5f);
            BuildParallaxBackgrounds(backgroundsContainer, minX, maxX);

            // 6. Ensure Systems & GameSession with LevelDefinition
            EnsureSystems(levelNumber);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log($"[LevelBuilder] Level {levelNumber:D2} built successfully in {scenePath} (Course: x={minX:F0} to {maxX:F0})");
        }

        // ── Level Specific Layouts ───────────────────────────────────────────

        private static float BuildLevel01Content(GameObject gems, GameObject gate)
        {
            // Level 1: Red (1 gem) along a gentle rightward route, Gate shortly after.
            SpawnPrefab(PrefabGemRed, new Vector3(12f, 0f, 0f), gems, "Gem_01_Red_Required");
            SpawnPrefab(PrefabRainbowGate, new Vector3(24f, 0f, 0f), gate, "RainbowGate");
            return 28f;
        }

        private static float BuildLevel02Content(GameObject gems, GameObject gate)
        {
            // Level 2: Red, Red, Orange (3 gems) along one flowing curved path.
            SpawnPrefab(PrefabGemRed,    new Vector3(14f, 1.2f, 0f),  gems, "Gem_01_Red_Required");
            SpawnPrefab(PrefabGemRed,    new Vector3(28f, -1.2f, 0f), gems, "Gem_02_Red_Required");
            SpawnPrefab(PrefabGemOrange, new Vector3(42f, 1.0f, 0f),  gems, "Gem_03_Orange_Required");
            SpawnPrefab(PrefabRainbowGate, new Vector3(56f, 0f, 0f), gate, "RainbowGate");
            return 60f;
        }

        private static float BuildLevel03Content(GameObject gems, GameObject gate)
        {
            // Level 3: Orange, Orange, Yellow, Yellow (4 gems) with decoys.
            SpawnPrefab(PrefabGemOrange, new Vector3(16f, 2.0f, 0f),  gems, "Gem_01_Orange_Required");
            SpawnPrefab(PrefabGemBlue,   new Vector3(16f, -3.0f, 0f), gems, "Decoy_01_Blue");

            SpawnPrefab(PrefabGemOrange, new Vector3(34f, -2.0f, 0f), gems, "Gem_02_Orange_Required");
            SpawnPrefab(PrefabGemGreen,  new Vector3(34f, 3.0f, 0f),  gems, "Decoy_02_Green");

            SpawnPrefab(PrefabGemYellow, new Vector3(52f, 2.2f, 0f),  gems, "Gem_03_Yellow_Required");
            SpawnPrefab(PrefabGemIndigo, new Vector3(52f, -3.0f, 0f), gems, "Decoy_03_Indigo");

            SpawnPrefab(PrefabGemYellow, new Vector3(70f, -1.5f, 0f), gems, "Gem_04_Yellow_Required");
            SpawnPrefab(PrefabGemViolet, new Vector3(70f, 3.0f, 0f),  gems, "Decoy_04_Violet");

            SpawnPrefab(PrefabRainbowGate, new Vector3(86f, 0f, 0f), gate, "RainbowGate");
            return 90f;
        }

        private static float BuildLevel04Content(GameObject gems, GameObject currents, GameObject gate)
        {
            // Level 4: Yellow, Yellow, Green, Green, Blue (5 gems) with currents.
            SpawnPrefab(PrefabGemYellow, new Vector3(16f, -2.0f, 0f), gems, "Gem_01_Yellow_Required");

            SpawnCurrent(PrefabCurrentUpRight, new Vector3(25f, 0f, 0f), new Vector2(10f, 4f), currents, "Current_01_UpRight");
            SpawnPrefab(PrefabGemYellow, new Vector3(36f, 2.8f, 0f), gems, "Gem_02_Yellow_Required");

            SpawnCurrent(PrefabCurrentDownRight, new Vector3(46f, 0.5f, 0f), new Vector2(10f, 4f), currents, "Current_02_DownRight");
            SpawnPrefab(PrefabGemGreen, new Vector3(58f, -2.8f, 0f), gems, "Gem_03_Green_Required");

            SpawnCurrent(PrefabCurrentUpRight, new Vector3(68f, 0f, 0f), new Vector2(10f, 4f), currents, "Current_03_UpRight");
            SpawnPrefab(PrefabGemGreen, new Vector3(80f, 2.8f, 0f), gems, "Gem_04_Green_Required");

            SpawnCurrent(PrefabCurrentRight, new Vector3(90f, 1.2f, 0f), new Vector2(10f, 3.5f), currents, "Current_04_Right");
            SpawnPrefab(PrefabGemBlue, new Vector3(102f, 0f, 0f), gems, "Gem_05_Blue_Required");

            SpawnPrefab(PrefabRainbowGate, new Vector3(118f, 0f, 0f), gate, "RainbowGate");
            return 124f;
        }

        private static float BuildLevel05Content(GameObject gems, GameObject currents, GameObject rests, GameObject gate)
        {
            // Level 5: Red, Orange, Yellow, Green, Blue, Indigo, Violet (all 7 gems) with 1 Rest.
            SpawnPrefab(PrefabGemRed, new Vector3(16f, 1.2f, 0f), gems, "Gem_01_Red_Required");
            SpawnPrefab(PrefabGemGreen, new Vector3(16f, -3.5f, 0f), gems, "Decoy_01_Green");

            SpawnCurrent(PrefabCurrentUpRight, new Vector3(24f, 0f, 0f), new Vector2(8f, 3.5f), currents, "Current_01_UpRight");
            SpawnPrefab(PrefabGemOrange, new Vector3(34f, -2.0f, 0f), gems, "Gem_02_Orange_Required");

            SpawnPrefab(PrefabGemYellow, new Vector3(52f, 2.5f, 0f), gems, "Gem_03_Yellow_Required");
            SpawnPrefab(PrefabGemBlue, new Vector3(52f, -3.5f, 0f), gems, "Decoy_02_Blue");

            SpawnCurrent(PrefabCurrentDownRight, new Vector3(60f, 0.5f, 0f), new Vector2(8f, 3.5f), currents, "Current_02_DownRight");
            SpawnPrefab(PrefabGemGreen, new Vector3(70f, -1.5f, 0f), gems, "Gem_04_Green_Required");

            SpawnPrefab(PrefabRainbowRest, new Vector3(86f, 0f, 0f), rests, "RainbowRest_Checkpoint");

            SpawnCurrent(PrefabCurrentRight, new Vector3(96f, 0.5f, 0f), new Vector2(10f, 3.5f), currents, "Current_03_Right");
            SpawnPrefab(PrefabGemBlue, new Vector3(108f, 2.2f, 0f), gems, "Gem_05_Blue_Required");
            SpawnPrefab(PrefabGemYellow, new Vector3(108f, -3.5f, 0f), gems, "Decoy_03_Yellow");

            SpawnCurrent(PrefabCurrentDownRight, new Vector3(118f, 0f, 0f), new Vector2(8f, 3.5f), currents, "Current_04_DownRight");
            SpawnPrefab(PrefabGemIndigo, new Vector3(128f, -2.2f, 0f), gems, "Gem_06_Indigo_Required");

            SpawnCurrent(PrefabCurrentUpRight, new Vector3(138f, 0f, 0f), new Vector2(8f, 3.5f), currents, "Current_05_UpRight");
            SpawnPrefab(PrefabGemViolet, new Vector3(148f, 1.5f, 0f), gems, "Gem_07_Violet_Required");
            SpawnPrefab(PrefabGemRed, new Vector3(148f, -3.5f, 0f), gems, "Decoy_04_Red");

            SpawnPrefab(PrefabRainbowGate, new Vector3(164f, 0f, 0f), gate, "RainbowGate");
            return 170f;
        }

        private static float BuildLevel06Content(GameObject gems, GameObject platforms, GameObject gate)
        {
            // Level 6 — Cloud Weave: Red, Red, Orange, Orange, Yellow (5 gems).
            // Introduce solid cloud walls: cause no damage, block movement, wide readable gaps, alternating routes.
            SpawnPrefab(PrefabGemRed, new Vector3(16f, 2.0f, 0f), gems, "Gem_01_Red_Required");

            // Wall 1: Upper and Lower cloud obstacles, wide center flow gap
            SpawnPrefab(PrefabPlatformCloud, new Vector3(26f, 3.5f, 0f), platforms, "CloudWall_01_Top");
            SpawnPrefab(PrefabPlatformCloud, new Vector3(26f, -3.5f, 0f), platforms, "CloudWall_01_Bottom");

            SpawnPrefab(PrefabGemRed, new Vector3(36f, -2.2f, 0f), gems, "Gem_02_Red_Required");

            // Wall 2: Center cloud obstacle, forcing upper or lower weave
            SpawnPrefab(PrefabPlatformCloud, new Vector3(46f, 0.0f, 0f), platforms, "CloudWall_02_Center");

            SpawnPrefab(PrefabGemOrange, new Vector3(56f, 2.5f, 0f), gems, "Gem_03_Orange_Required");

            // Wall 3: Upper/Lower split
            SpawnPrefab(PrefabPlatformCloud, new Vector3(66f, 3.8f, 0f), platforms, "CloudWall_03_Top");
            SpawnPrefab(PrefabPlatformCloud, new Vector3(66f, -3.8f, 0f), platforms, "CloudWall_03_Bottom");

            SpawnPrefab(PrefabGemOrange, new Vector3(76f, -2.5f, 0f), gems, "Gem_04_Orange_Required");

            // Wall 4: Staggered weave bar
            SpawnPrefab(PrefabPlatformCloud, new Vector3(88f, 1.8f, 0f), platforms, "CloudWall_04_Upper");
            SpawnPrefab(PrefabPlatformCloud, new Vector3(88f, -3.5f, 0f), platforms, "CloudWall_04_Lower");

            SpawnPrefab(PrefabGemYellow, new Vector3(100f, 0.0f, 0f), gems, "Gem_05_Yellow_Required");

            SpawnPrefab(PrefabRainbowGate, new Vector3(116f, 0f, 0f), gate, "RainbowGate");
            return 120f;
        }

        private static float BuildLevel07Content(GameObject gems, GameObject hazards, GameObject rests, GameObject gate)
        {
            // Level 7 — Storm Warning: Orange, Yellow, Yellow, Green, Blue (5 gems).
            // Introduce dangerous red storm clouds. 1 Rainbow Rest at halfway.
            SpawnPrefab(PrefabGemOrange, new Vector3(16f, 1.5f, 0f), gems, "Gem_01_Orange_Required");

            // Storm Hazard 1: Top and bottom flank, clear safe middle corridor
            SpawnPrefab(PrefabHazardContact, new Vector3(26f, 3.5f, 0f), hazards, "Storm_01_Top");
            SpawnPrefab(PrefabHazardContact, new Vector3(26f, -3.2f, 0f), hazards, "Storm_01_Bottom");

            SpawnPrefab(PrefabGemYellow, new Vector3(36f, -2.0f, 0f), gems, "Gem_02_Yellow_Required");
            SpawnPrefab(PrefabGemRed,    new Vector3(36f, 3.5f, 0f),  gems, "Decoy_01_Red");

            // Storm Hazard 2: Center-upper storm cloud
            SpawnPrefab(PrefabHazardContact, new Vector3(48f, 2.8f, 0f), hazards, "Storm_02_Upper");

            SpawnPrefab(PrefabGemYellow, new Vector3(58f, 2.0f, 0f), gems, "Gem_03_Yellow_Required");

            // Rainbow Rest at halfway (x = 72)
            SpawnPrefab(PrefabRainbowRest, new Vector3(72f, 0f, 0f), rests, "RainbowRest_Checkpoint");

            // Storm Hazard 3: Moving hazard with clear predictable vertical timing
            var movingHazard = SpawnPrefab(PrefabHazardMoving, new Vector3(84f, 0f, 0f), hazards, "Storm_03_Moving");
            if (movingHazard != null)
            {
                var mh = movingHazard.GetComponent<MovingHazard>();
                if (mh != null)
                {
                    SetField(mh, "offsetA", new Vector2(0f, -2.5f));
                    SetField(mh, "offsetB", new Vector2(0f, 2.5f));
                    SetField(mh, "travelDuration", 3.0f);
                    SetField(mh, "pauseDuration", 0.4f);
                }
            }

            SpawnPrefab(PrefabGemGreen, new Vector3(96f, -2.0f, 0f), gems, "Gem_04_Green_Required");
            SpawnPrefab(PrefabGemViolet, new Vector3(96f, 3.5f, 0f), gems, "Decoy_02_Violet");

            // Storm Hazard 4: Lower hazard before Blue
            SpawnPrefab(PrefabHazardContact, new Vector3(108f, -3.0f, 0f), hazards, "Storm_04_Lower");

            SpawnPrefab(PrefabGemBlue, new Vector3(116f, 1.5f, 0f), gems, "Gem_05_Blue_Required");

            SpawnPrefab(PrefabRainbowGate, new Vector3(130f, 0f, 0f), gate, "RainbowGate");
            return 134f;
        }

        private static float BuildLevel08Content(GameObject gems, GameObject platforms, GameObject hazards, GameObject rests, GameObject gate)
        {
            // Level 8 — Breakthrough: Green, Green, Blue, Indigo, Indigo (5 gems).
            // Introduce Dash & cracked purple clouds. Safe start area to practice dash.
            // First cracked cloud at x = 18 is isolated.
            SpawnPrefab(PrefabHazardBreakable, new Vector3(18f, 0f, 0f), hazards, "CrackedCloud_01_Intro");
            SpawnPrefab(PrefabGemGreen, new Vector3(26f, 0f, 0f), gems, "Gem_01_Green_Required");

            // Obstacle & Breakable Shortcut to Green 2
            SpawnPrefab(PrefabPlatformCloud, new Vector3(38f, -2.5f, 0f), platforms, "SolidPlatform_01");
            SpawnPrefab(PrefabHazardBreakable, new Vector3(38f, 2.2f, 0f), hazards, "CrackedCloud_02_Shortcut");

            SpawnPrefab(PrefabGemGreen, new Vector3(48f, 2.5f, 0f), gems, "Gem_02_Green_Required");

            // Dangerous hazard flank with central cracked cloud
            SpawnPrefab(PrefabHazardContact, new Vector3(60f, 3.5f, 0f), hazards, "Storm_01_Upper");
            SpawnPrefab(PrefabHazardBreakable, new Vector3(60f, 0f, 0f), hazards, "CrackedCloud_03_Center");
            SpawnPrefab(PrefabHazardContact, new Vector3(60f, -3.5f, 0f), hazards, "Storm_01_Lower");

            SpawnPrefab(PrefabGemBlue, new Vector3(72f, -2.0f, 0f), gems, "Gem_03_Blue_Required");

            // Rainbow Rest at x = 86
            SpawnPrefab(PrefabRainbowRest, new Vector3(86f, 0f, 0f), rests, "RainbowRest_Checkpoint");

            // Cracked bonus cloud & Indigo 1
            SpawnPrefab(PrefabHazardBreakable, new Vector3(98f, 2.5f, 0f), hazards, "CrackedCloud_04_Bonus");
            SpawnPrefab(PrefabGemIndigo, new Vector3(108f, 2.5f, 0f), gems, "Gem_04_Indigo_Required");

            // Solid wall and cracked bypass to Indigo 2
            SpawnPrefab(PrefabPlatformCloud, new Vector3(120f, 1.8f, 0f), platforms, "SolidPlatform_02");
            SpawnPrefab(PrefabHazardBreakable, new Vector3(120f, -1.8f, 0f), hazards, "CrackedCloud_05_Lower");

            SpawnPrefab(PrefabGemIndigo, new Vector3(130f, -1.8f, 0f), gems, "Gem_05_Indigo_Required");

            SpawnPrefab(PrefabRainbowGate, new Vector3(146f, 0f, 0f), gate, "RainbowGate");
            return 150f;
        }

        private static float BuildLevel09Content(GameObject gems, GameObject platforms, GameObject hazards, GameObject enemies, GameObject rests, GameObject gate)
        {
            // Level 9 — Gloom Patrol: Red, Orange, Yellow, Green, Blue, Indigo (6 gems).
            // Introduce Gloomling enemy: predictable patrol, pauses, visible route, contact deals damage/resets rush.
            SpawnPrefab(PrefabGemRed, new Vector3(16f, 2.0f, 0f), gems, "Gem_01_Red_Required");

            // Gloomling 1 Encounter (x = 28, patrols y: -2.5 to 2.5)
            var gloom1 = SpawnPrefab(PrefabEnemyGloomling, new Vector3(28f, 0f, 0f), enemies, "Gloomling_01");
            if (gloom1 != null)
            {
                var g = gloom1.GetComponent<Gloomling>();
                if (g != null) g.ConfigurePatrol(new Vector2(0f, -2.5f), new Vector2(0f, 2.5f), 2.8f, 0.45f);
            }

            SpawnPrefab(PrefabGemOrange, new Vector3(40f, -2.5f, 0f), gems, "Gem_02_Orange_Required");
            SpawnPrefab(PrefabGemGreen,  new Vector3(40f, 3.5f, 0f),  gems, "Decoy_01_Green");

            // Solid cloud and storm cloud framing path
            SpawnPrefab(PrefabPlatformCloud, new Vector3(52f, 0f, 0f), platforms, "SolidCloud_Center");
            SpawnPrefab(PrefabHazardContact, new Vector3(64f, -3.5f, 0f), hazards, "Storm_Lower");

            SpawnPrefab(PrefabGemYellow, new Vector3(64f, 2.2f, 0f), gems, "Gem_03_Yellow_Required");

            // Rainbow Rest at halfway (x = 78)
            SpawnPrefab(PrefabRainbowRest, new Vector3(78f, 0f, 0f), rests, "RainbowRest_Checkpoint");

            // Gloomling 2 Encounter (x = 94, patrols y: -2.8 to 2.8)
            var gloom2 = SpawnPrefab(PrefabEnemyGloomling, new Vector3(94f, 0f, 0f), enemies, "Gloomling_02");
            if (gloom2 != null)
            {
                var g = gloom2.GetComponent<Gloomling>();
                if (g != null) g.ConfigurePatrol(new Vector2(0f, 2.8f), new Vector2(0f, -2.8f), 2.6f, 0.45f);
            }

            SpawnPrefab(PrefabGemGreen, new Vector3(106f, -2.0f, 0f), gems, "Gem_04_Green_Required");

            // Cracked cloud shortcut to Blue
            SpawnPrefab(PrefabHazardBreakable, new Vector3(118f, 0f, 0f), hazards, "CrackedCloud_Shortcut");
            SpawnPrefab(PrefabGemBlue, new Vector3(126f, 2.2f, 0f), gems, "Gem_05_Blue_Required");
            SpawnPrefab(PrefabGemRed,  new Vector3(126f, -3.5f, 0f), gems, "Decoy_02_Red");

            SpawnPrefab(PrefabGemIndigo, new Vector3(142f, 0f, 0f), gems, "Gem_06_Indigo_Required");

            SpawnPrefab(PrefabRainbowGate, new Vector3(158f, 0f, 0f), gate, "RainbowGate");
            return 162f;
        }

        private static float BuildLevel10Content(
            GameObject gems,
            GameObject platforms,
            GameObject currents,
            GameObject hazards,
            GameObject enemies,
            GameObject rests,
            GameObject gate)
        {
            // Level 10 — Rainbow Run (World 1 Finale): Red, Red, Orange, Yellow, Green, Blue, Indigo, Violet (8 gems).
            // Combines: solid cloud walls, storm clouds, cracked clouds, 2 Gloomlings, currents, decoys, 1 Rest, 1 StormChaser.
            // Gem 1: Red
            SpawnPrefab(PrefabGemRed, new Vector3(16f, 1.5f, 0f), gems, "Gem_01_Red_Required");
            SpawnPrefab(PrefabGemBlue, new Vector3(16f, -3.5f, 0f), gems, "Decoy_01_Blue");

            // Gloomling 1 (x = 26)
            var gloom1 = SpawnPrefab(PrefabEnemyGloomling, new Vector3(26f, 0f, 0f), enemies, "Gloomling_01");
            if (gloom1 != null)
            {
                var g = gloom1.GetComponent<Gloomling>();
                if (g != null) g.ConfigurePatrol(new Vector2(0f, -2.5f), new Vector2(0f, 2.5f), 2.8f, 0.4f);
            }

            // Gem 2: Red
            SpawnPrefab(PrefabGemRed, new Vector3(36f, -2.0f, 0f), gems, "Gem_02_Red_Required");

            // Current 1: UpRight assisting toward Orange
            SpawnCurrent(PrefabCurrentUpRight, new Vector3(44f, 0f, 0f), new Vector2(8f, 3.5f), currents, "Current_01_UpRight");
            SpawnPrefab(PrefabGemOrange, new Vector3(54f, 2.5f, 0f), gems, "Gem_03_Orange_Required");

            // Storm Chaser Encounter (spacious arena around x = 66, chases slower than Gemma, gives up after 5s/14u)
            SpawnPrefab(PrefabEnemyStormChaser, new Vector3(66f, 0f, 0f), enemies, "StormChaser_Boss");

            // Gem 4: Yellow
            SpawnPrefab(PrefabGemYellow, new Vector3(76f, -2.2f, 0f), gems, "Gem_04_Yellow_Required");

            // Current 2: DownRight assisting toward Green
            SpawnCurrent(PrefabCurrentDownRight, new Vector3(86f, 0.5f, 0f), new Vector2(8f, 3.5f), currents, "Current_02_DownRight");
            SpawnPrefab(PrefabHazardBreakable, new Vector3(92f, 2.5f, 0f), hazards, "CrackedCloud_Bonus");
            SpawnPrefab(PrefabGemGreen, new Vector3(98f, 1.5f, 0f), gems, "Gem_05_Green_Required");

            // Rainbow Rest (Safe Zone at x = 110 after Green)
            SpawnPrefab(PrefabRainbowRest, new Vector3(110f, 0f, 0f), rests, "RainbowRest_Checkpoint");

            // Current 3: Right after Rest into Blue
            SpawnCurrent(PrefabCurrentRight, new Vector3(120f, 0.5f, 0f), new Vector2(10f, 3.5f), currents, "Current_03_Right");
            SpawnPrefab(PrefabGemBlue, new Vector3(132f, -2.5f, 0f), gems, "Gem_06_Blue_Required");
            SpawnPrefab(PrefabGemRed,  new Vector3(132f, 3.5f, 0f),  gems, "Decoy_02_Red");

            // Gloomling 2 (x = 142)
            var gloom2 = SpawnPrefab(PrefabEnemyGloomling, new Vector3(142f, 0f, 0f), enemies, "Gloomling_02");
            if (gloom2 != null)
            {
                var g = gloom2.GetComponent<Gloomling>();
                if (g != null) g.ConfigurePatrol(new Vector2(0f, 2.6f), new Vector2(0f, -2.6f), 2.5f, 0.4f);
            }

            // Gem 7: Indigo
            SpawnPrefab(PrefabPlatformCloud, new Vector3(150f, -2.5f, 0f), platforms, "SolidPlatform_Final");
            SpawnPrefab(PrefabGemIndigo, new Vector3(154f, 2.5f, 0f), gems, "Gem_07_Indigo_Required");

            // Final approach to Violet & Gate
            SpawnPrefab(PrefabHazardBreakable, new Vector3(164f, 0f, 0f), hazards, "CrackedCloud_Final");
            SpawnPrefab(PrefabGemViolet, new Vector3(172f, 0f, 0f), gems, "Gem_08_Violet_Required");
            SpawnPrefab(PrefabGemGreen,  new Vector3(172f, -3.5f, 0f), gems, "Decoy_03_Green");

            SpawnPrefab(PrefabRainbowGate, new Vector3(188f, 0f, 0f), gate, "RainbowGate");
            return 194f;
        }

        // ── Helper Spawners ───────────────────────────────────────────────────

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        private static GameObject SpawnPrefab(string assetPath, Vector3 position, GameObject parent, string customName = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[LevelBuilder] Prefab not found at path: {assetPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent != null ? parent.transform : null);
            instance.transform.position = position;
            if (!string.IsNullOrEmpty(customName))
            {
                instance.name = customName;
            }
            return instance;
        }

        private static GameObject SpawnCurrent(string assetPath, Vector3 position, Vector2 boxSize, GameObject parent, string customName)
        {
            var go = SpawnPrefab(assetPath, position, parent, customName);
            if (go != null)
            {
                var col = go.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = boxSize;
                }
            }
            return go;
        }

        private static void SetupPlayerSpawn()
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma == null)
            {
                var gameplay = GameObject.Find("Gameplay");
                gemma = SpawnPrefab(PrefabGemma, Vector3.zero, gameplay, "Gemma");
            }
            else
            {
                gemma.transform.position = Vector3.zero;
            }
        }

        private static void SetupCameraConfiner(float minX, float maxX, float minY, float maxY)
        {
            var confinerObj = GameObject.Find("LevelConfinerBounds");
            if (confinerObj == null)
            {
                var env = GameObject.Find("Environment");
                confinerObj = new GameObject("LevelConfinerBounds");
                if (env != null) confinerObj.transform.SetParent(env.transform);
            }

            var poly = confinerObj.GetComponent<PolygonCollider2D>() ?? confinerObj.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            poly.points = new Vector2[]
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            };

            var cm = UnityEngine.Object.FindFirstObjectByType<CinemachineCamera>();
            if (cm != null)
            {
                var cmConfiner = cm.GetComponent<CinemachineConfiner2D>();
                if (cmConfiner != null)
                {
                    cmConfiner.BoundingShape2D = poly;
                    cmConfiner.InvalidateBoundingShapeCache();
                }
            }
        }

        private static void BuildBoundaries(GameObject container, float minX, float maxX, float minY, float maxY)
        {
            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteSolidSquare);
            int solidLayer = LayerMask.NameToLayer("Solid");

            float width = maxX - minX + 10f;
            float midX = (minX + maxX) * 0.5f;

            CreateBoundaryWall(container, "Boundary_Top", new Vector3(midX, maxY + 1f, 0f), new Vector2(width, 2f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Bottom", new Vector3(midX, minY - 1f, 0f), new Vector2(width, 2f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Left", new Vector3(minX - 1f, 0f, 0f), new Vector2(2f, maxY - minY + 6f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Right", new Vector3(maxX + 1f, 0f, 0f), new Vector2(2f, maxY - minY + 6f), squareSprite, solidLayer);
        }

        private static void CreateBoundaryWall(GameObject parent, string name, Vector3 pos, Vector2 size, Sprite sprite, int layer)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            if (layer >= 0) wall.layer = layer;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.size = size;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingLayerName = "GameplayBack";
            sr.color = new Color(0.2f, 0.25f, 0.38f, 0.25f);
        }

        private static void BuildParallaxBackgrounds(GameObject container, float minX, float maxX)
        {
            var starfieldSprite = LoadSubSprite(SpritePackBackgrounds + "starfield_background.png");
            var rainbowBanner   = LoadSubSprite(SpritePackBackgrounds + "rainbow_banner_background.png");
            var mountainsWide1  = LoadSubSprite(SpritePackBackgrounds + "mountains_wide_01.png");
            var mountainsWide2  = LoadSubSprite(SpritePackBackgrounds + "mountains_wide_02.png");
            var mountainsPeaks  = LoadSubSprite(SpritePackBackgrounds + "mountains_peaks_01.png");

            // Far Layer
            var farLayer = CreateChild(container, "Layer_BackgroundFar");
            var farParallax = farLayer.AddComponent<ParallaxLayer>();
            SetField(farParallax, "parallaxFactor", new Vector2(0.85f, 0.4f));

            for (float x = minX - 10f; x <= maxX + 20f; x += 32f)
            {
                var skyPiece = new GameObject($"Starfield_{x:F0}");
                skyPiece.transform.SetParent(farLayer.transform);
                skyPiece.transform.position = new Vector3(x, 0f, 15f);
                skyPiece.transform.localScale = new Vector3(8f, 7f, 1f);

                var sr = skyPiece.AddComponent<SpriteRenderer>();
                sr.sprite = starfieldSprite;
                sr.sortingLayerName = "BackgroundFar";
                sr.sortingOrder = -20;
                sr.color = Color.white;

                var rainbowObj = new GameObject($"RainbowBanner_{x:F0}");
                rainbowObj.transform.SetParent(farLayer.transform);
                rainbowObj.transform.position = new Vector3(x + 16f, 3.5f, 12f);
                rainbowObj.transform.localScale = new Vector3(5f, 3.5f, 1f);

                var rSr = rainbowObj.AddComponent<SpriteRenderer>();
                rSr.sprite = rainbowBanner;
                rSr.sortingLayerName = "BackgroundFar";
                rSr.sortingOrder = -15;
                rSr.color = new Color(1f, 1f, 1f, 0.85f);
            }

            // Near Layer
            var nearLayer = CreateChild(container, "Layer_BackgroundNear");
            var nearParallax = nearLayer.AddComponent<ParallaxLayer>();
            SetField(nearParallax, "parallaxFactor", new Vector2(0.55f, 0.25f));

            for (float x = minX - 5f; x <= maxX + 10f; x += 18f)
            {
                var mountainObj = new GameObject($"Mountain_{x:F0}");
                mountainObj.transform.SetParent(nearLayer.transform);
                float yOffset = -3.5f + Mathf.Sin(x * 0.15f) * 0.8f;
                mountainObj.transform.position = new Vector3(x, yOffset, 8f);
                mountainObj.transform.localScale = new Vector3(4.5f, 3.5f, 1f);

                var sr = mountainObj.AddComponent<SpriteRenderer>();
                int pattern = ((int)x / 18) % 3;
                if (pattern == 0) sr.sprite = mountainsWide1;
                else if (pattern == 1) sr.sprite = mountainsWide2;
                else sr.sprite = mountainsPeaks;

                sr.sortingLayerName = "BackgroundNear";
                sr.sortingOrder = 0;
                sr.color = new Color(0.85f, 0.9f, 1.0f, 0.8f);
            }
        }

        private static Sprite LoadSubSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            return null;
        }

        private static void EnsureSystems(int levelNumber)
        {
            var systems = GameObject.Find("Systems");
            if (systems == null)
            {
                systems = new GameObject("Systems");
            }

            // Clean up legacy redundant child objects
            var legacyGS = systems.transform.Find("GameSession");
            if (legacyGS != null) Undo.DestroyObjectImmediate(legacyGS.gameObject);

            var legacyCM = systems.transform.Find("CheckpointManager");
            if (legacyCM != null) Undo.DestroyObjectImmediate(legacyCM.gameObject);

            // Clean up duplicate UIManager on MainCanvas if present
            var mainCanvas = GameObject.Find("UI/MainCanvas");
            if (mainCanvas != null)
            {
                var duplicateUIM = mainCanvas.GetComponent<UIManager>();
                if (duplicateUIM != null) Undo.DestroyObjectImmediate(duplicateUIM);
            }

            var session = systems.GetComponent<GameSession>() ?? systems.AddComponent<GameSession>();
            string defPath = $"Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level{levelNumber:D2}.asset";
            var levelDef = AssetDatabase.LoadAssetAtPath<LevelDefinition>(defPath);
            SetField(session, "_levelDefinition", levelDef);

            var tutorial = systems.GetComponent<TutorialCoordinator>() ?? systems.AddComponent<TutorialCoordinator>();
            var checkpoint = systems.GetComponent<CheckpointManager>() ?? systems.AddComponent<CheckpointManager>();

            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<CameraShake2D>() == null)
            {
                mainCam.gameObject.AddComponent<CameraShake2D>();
            }
        }

        public static void RegisterScenesInBuildSettings()
        {
            var scenePaths = new List<string>();
            for (int i = 1; i <= 10; i++)
            {
                string path = $"Assets/GemmaRainbowSeeker/Scenes/Level{i:D2}.unity";
                if (System.IO.File.Exists(path))
                {
                    scenePaths.Add(path);
                }
            }

            var existingScenes = EditorBuildSettings.scenes.ToList();
            foreach (var p in scenePaths)
            {
                if (!existingScenes.Any(s => s.path == p))
                {
                    existingScenes.Add(new EditorBuildSettingsScene(p, true));
                }
            }

            EditorBuildSettings.scenes = existingScenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                type = type.BaseType;
            }

            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}

