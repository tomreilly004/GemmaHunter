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
    /// <summary>
    /// Idempotent Level 1 builder tool.
    /// Constructs the entire Level 1 playable world beneath a scene root named "GENERATED_Level01".
    /// Re-running replaces only "GENERATED_Level01" without modifying hand-authored systems or UI.
    /// Uses background and platform assets from the Gemma Beaker sprite pack.
    /// </summary>
    public static class Level01Builder
    {
        private const string RootName = "GENERATED_Level01";

        // Sprite pack paths
        private const string SpritePackBackgrounds = "Assets/Sprites/Gemma_Beaker_Sprite_Pack/gemma_beaker_sprite_pack/backgrounds/";
        private const string SpritePackPlatforms   = "Assets/Sprites/Gemma_Beaker_Sprite_Pack/gemma_beaker_sprite_pack/platforms/";

        // Prefab paths
        private const string PrefabGemma             = "Assets/GemmaRainbowSeeker/Prefabs/Gemma.prefab";
        private const string PrefabPlatformGrassWide1 = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Grass_Wide_01.prefab";
        private const string PrefabPlatformGrassWide2 = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Grass_Wide_02.prefab";
        private const string PrefabPlatformGrassIsland = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Grass_Island_01.prefab";
        private const string PrefabPlatformGrassTileLong = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Grass_Tile_Long.prefab";
        private const string PrefabPlatformStoneIsland = "Assets/GemmaRainbowSeeker/Prefabs/Platform_Stone_Island_01.prefab";

        private const string PrefabHazardContact     = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_ContactCloud.prefab";
        private const string PrefabHazardBreakable   = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_BreakableCloud.prefab";
        private const string PrefabHazardMoving      = "Assets/GemmaRainbowSeeker/Prefabs/Hazard_MovingCloud.prefab";
        private const string PrefabRainbowRest       = "Assets/GemmaRainbowSeeker/Prefabs/RainbowRest.prefab";
        private const string PrefabRainbowGate       = "Assets/GemmaRainbowSeeker/Prefabs/RainbowGate.prefab";
        private const string PrefabCurrentRight      = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_Right.prefab";
        private const string PrefabCurrentUpRight    = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_UpRight.prefab";
        private const string PrefabCurrentDownRight  = "Assets/GemmaRainbowSeeker/Prefabs/CurrentZone_DownRight.prefab";

        private const string PrefabGemRed    = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Red.prefab";
        private const string PrefabGemOrange = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Orange.prefab";
        private const string PrefabGemYellow = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Yellow.prefab";
        private const string PrefabGemGreen  = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Green.prefab";
        private const string PrefabGemBlue   = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Blue.prefab";
        private const string PrefabGemIndigo = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Indigo.prefab";
        private const string PrefabGemViolet = "Assets/GemmaRainbowSeeker/Prefabs/GemPickup_Violet.prefab";

        private const string SpriteSolidSquare = "Assets/GemmaRainbowSeeker/Art/Placeholders/Solid_Square.png";

        [MenuItem("GemmaRainbowSeeker/Build Complete Level 01", false, 10)]
        public static void BuildLevel01()
        {
            var scene = SceneManager.GetActiveScene();
            Undo.SetCurrentGroupName("Build Complete Level 01");
            int group = Undo.GetCurrentGroup();

            // 1. Remove temporary test artifacts if present
            var rootObjects = scene.GetRootGameObjects();
            foreach (var r in rootObjects)
            {
                if (r.name.StartsWith("Test"))
                {
                    Undo.DestroyObjectImmediate(r);
                }
            }

            // 2. Clean up or disable old temporary placeholders under Environment and Gameplay
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

            // 3. Find or recreate GENERATED_Level01 root
            var existingGen = GameObject.Find(RootName);
            if (existingGen != null)
            {
                Undo.DestroyObjectImmediate(existingGen);
            }

            var genRoot = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(genRoot, "Create " + RootName);

            // Create sub-containers
            var backgroundsContainer = CreateChild(genRoot, "Backgrounds");
            var boundariesContainer  = CreateChild(genRoot, "Boundaries");
            var platformsContainer   = CreateChild(genRoot, "Platforms");
            var currentsContainer    = CreateChild(genRoot, "Currents");
            var hazardsContainer     = CreateChild(genRoot, "Hazards");
            var checkpointsContainer = CreateChild(genRoot, "Checkpoints");
            var collectiblesContainer = CreateChild(genRoot, "Collectibles");
            var gateContainer        = CreateChild(genRoot, "RainbowGate");
            var tutorialsContainer   = CreateChild(genRoot, "TutorialTriggers");

            // 4. Setup Gemma Player Spawn
            SetupPlayerSpawn();

            // 5. Setup Camera Confiner for entire course (x = -10 to 370, y = -10 to 10)
            SetupCameraConfiner();

            // Ensure CameraShake2D on Main Camera
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<CameraShake2D>() == null)
            {
                mainCam.gameObject.AddComponent<CameraShake2D>();
            }

            // 6. Build Parallax Backgrounds using sprite pack assets
            BuildParallaxBackgrounds(backgroundsContainer);

            // 7. Build Invisible Boundaries (Enclosing x = -5 to 365, y = -7 to 7)
            BuildBoundaries(boundariesContainer);

            // 8. Build Section One: Learn The Loop (x = 0 to 130)
            BuildSectionOne(platformsContainer, currentsContainer, collectiblesContainer, checkpointsContainer, tutorialsContainer);

            // 9. Build Section Two: Danger and Dash (x = 135 to 235)
            BuildSectionTwo(platformsContainer, currentsContainer, hazardsContainer, collectiblesContainer, checkpointsContainer, tutorialsContainer);

            // 10. Build Section Three: Final Challenge & Gate (x = 240 to 360)
            BuildSectionThree(platformsContainer, currentsContainer, hazardsContainer, collectiblesContainer, gateContainer, tutorialsContainer);

            // 11. Ensure TutorialCoordinator on Systems
            EnsureTutorialCoordinator();

            // 12. Ensure GameSession has valid LevelRules asset
            EnsureGameSession();

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[Level01Builder] Complete Level 01 generated successfully beneath 'GENERATED_Level01' with sprite pack assets!");
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        private static GameObject SpawnPrefab(string assetPath, Vector3 position, Transform parent, string customName = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[Level01Builder] Prefab not found at path: {assetPath}");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = position;
            if (!string.IsNullOrEmpty(customName))
            {
                instance.name = customName;
            }
            return instance;
        }

        private static GameObject SpawnPrefab(string assetPath, Vector3 position, GameObject parent, string customName = null)
        {
            return SpawnPrefab(assetPath, position, parent != null ? parent.transform : null, customName);
        }

        private static void SetupPlayerSpawn()
        {
            var gemma = GameObject.Find("Gemma") ?? GameObject.FindWithTag("Player");
            if (gemma == null)
            {
                var gameplay = GameObject.Find("Gameplay");
                gemma = SpawnPrefab(PrefabGemma, new Vector3(0f, 0f, 0f), gameplay != null ? gameplay.transform : null, "Gemma");
            }
            else
            {
                gemma.transform.position = new Vector3(0f, 0f, 0f);
            }
        }

        private static void SetupCameraConfiner()
        {
            var confinerObj = GameObject.Find("LevelConfinerBounds");
            if (confinerObj == null)
            {
                var env = GameObject.Find("Environment");
                confinerObj = new GameObject("LevelConfinerBounds");
                if (env != null) confinerObj.transform.SetParent(env.transform);
            }

            var poly = confinerObj.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = confinerObj.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;

            // Course extends from x = -10 to x = 370, y = -10 to y = 10
            Vector2[] points = new Vector2[]
            {
                new Vector2(-10f, -9f),
                new Vector2(370f, -9f),
                new Vector2(370f, 9f),
                new Vector2(-10f, 9f)
            };
            poly.points = points;

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

        private static void BuildParallaxBackgrounds(GameObject container)
        {
            var starfieldSprite = LoadSubSprite(SpritePackBackgrounds + "starfield_background.png");
            var rainbowBanner   = LoadSubSprite(SpritePackBackgrounds + "rainbow_banner_background.png");
            var mountainsWide1  = LoadSubSprite(SpritePackBackgrounds + "mountains_wide_01.png");
            var mountainsWide2  = LoadSubSprite(SpritePackBackgrounds + "mountains_wide_02.png");
            var mountainsPeaks  = LoadSubSprite(SpritePackBackgrounds + "mountains_peaks_01.png");

            // ── Far Layer: Deep Starfield & Rainbow Ribbons (Parallax factor 0.85 = slow movement) ──
            var farLayer = CreateChild(container, "Layer_BackgroundFar");
            var farParallax = farLayer.AddComponent<ParallaxLayer>();
            SetField(farParallax, "parallaxFactor", new Vector2(0.85f, 0.4f));

            // Starfield backdrops tiling every 32 units
            for (int x = -20; x <= 380; x += 32)
            {
                var skyPiece = new GameObject($"Starfield_{x}");
                skyPiece.transform.SetParent(farLayer.transform);
                skyPiece.transform.position = new Vector3(x, 0f, 15f);
                skyPiece.transform.localScale = new Vector3(8f, 7f, 1f);

                var sr = skyPiece.AddComponent<SpriteRenderer>();
                sr.sprite = starfieldSprite;
                sr.sortingLayerName = "BackgroundFar";
                sr.sortingOrder = -20;
                sr.color = Color.white;

                // Rainbow banners spaced along the horizon
                if (x % 64 == 0)
                {
                    var rainbowObj = new GameObject($"RainbowBanner_{x}");
                    rainbowObj.transform.SetParent(farLayer.transform);
                    rainbowObj.transform.position = new Vector3(x + 16f, 3.5f, 12f);
                    rainbowObj.transform.localScale = new Vector3(5f, 3.5f, 1f);

                    var rSr = rainbowObj.AddComponent<SpriteRenderer>();
                    rSr.sprite = rainbowBanner;
                    rSr.sortingLayerName = "BackgroundFar";
                    rSr.sortingOrder = -15;
                    rSr.color = new Color(1f, 1f, 1f, 0.85f);
                }
            }

            // ── Near Layer: Distant Mountains & Peaks (Parallax factor 0.55 = medium movement) ──
            var nearLayer = CreateChild(container, "Layer_BackgroundNear");
            var nearParallax = nearLayer.AddComponent<ParallaxLayer>();
            SetField(nearParallax, "parallaxFactor", new Vector2(0.55f, 0.25f));

            for (int x = -10; x <= 370; x += 18)
            {
                var mountainObj = new GameObject($"Mountain_{x}");
                mountainObj.transform.SetParent(nearLayer.transform);
                float yOffset = -3.5f + Mathf.Sin(x * 0.15f) * 0.8f;
                mountainObj.transform.position = new Vector3(x, yOffset, 8f);
                mountainObj.transform.localScale = new Vector3(4.5f, 3.5f, 1f);

                var sr = mountainObj.AddComponent<SpriteRenderer>();
                // Alternate between wide mountain ridges and mountain peaks
                int pattern = (x / 18) % 3;
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

        private static void BuildBoundaries(GameObject container)
        {
            var squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteSolidSquare);
            int solidLayer = LayerMask.NameToLayer("Solid");

            // Invisible solid boundary colliders: Top (y = 7.5), Bottom (y = -7.5), Left (x = -6), Right (x = 365)
            CreateBoundaryWall(container, "Boundary_Top", new Vector3(180f, 7.5f, 0f), new Vector2(390f, 2f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Bottom", new Vector3(180f, -7.5f, 0f), new Vector2(390f, 2f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Left", new Vector3(-6f, 0f, 0f), new Vector2(2f, 18f), squareSprite, solidLayer);
            CreateBoundaryWall(container, "Boundary_Right", new Vector3(365f, 0f, 0f), new Vector2(2f, 18f), squareSprite, solidLayer);
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
            sr.color = new Color(0.2f, 0.25f, 0.38f, 0.3f);
        }

        private static void BuildSectionOne(GameObject platforms, GameObject currents, GameObject gems, GameObject rests, GameObject tutorials)
        {
            // ── REQUIRED GEMS ──
            SpawnPrefab(PrefabGemRed,    new Vector3(25f, 0f, 0f),   gems, "Gem_01_Red_Required");
            SpawnPrefab(PrefabGemOrange, new Vector3(65f, 4f, 0f),   gems, "Gem_02_Orange_Required");
            SpawnPrefab(PrefabGemYellow, new Vector3(105f, -4f, 0f), gems, "Gem_03_Yellow_Required");

            // ── DECOY GEMS (Safely off primary path) ──
            SpawnPrefab(PrefabGemGreen,  new Vector3(45f, -4.2f, 0f), gems, "Gem_Decoy_Green_Early");
            SpawnPrefab(PrefabGemBlue,   new Vector3(85f, 4.2f, 0f),  gems, "Gem_Decoy_Blue_Early");

            // ── FIRST RAINBOW REST ──
            SpawnPrefab(PrefabRainbowRest, new Vector3(125f, 0f, 0f), rests, "RainbowRest_01");

            // ── PLATFORMS FROM SPRITE PACK (Wide S-curve swim route) ──
            SpawnPrefab(PrefabPlatformGrassWide1, new Vector3(15f, -4.5f, 0f), platforms, "Platform_Grass_01");
            SpawnPrefab(PrefabPlatformGrassWide2, new Vector3(25f, 4.5f, 0f),  platforms, "Platform_Grass_02");

            // Curve up toward Orange (x=65, y=4)
            SpawnPrefab(PrefabPlatformGrassIsland, new Vector3(45f, 0f, 0f),   platforms, "Platform_Island_01");
            SpawnPrefab(PrefabPlatformGrassWide1,  new Vector3(65f, 1.8f, 0f), platforms, "Platform_Grass_03");

            // Curve down toward Yellow (x=105, y=-4)
            SpawnPrefab(PrefabPlatformGrassIsland, new Vector3(85f, 0.5f, 0f),  platforms, "Platform_Island_02");
            SpawnPrefab(PrefabPlatformGrassWide2,  new Vector3(105f, -1.8f, 0f), platforms, "Platform_Grass_04");

            // Lead-in to Rest 1 (x=125, y=0)
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(125f, -4.5f, 0f), platforms, "Platform_Long_01");
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(125f, 4.5f, 0f),  platforms, "Platform_Long_02");

            // ── CURRENTS ──
            SpawnPrefab(PrefabCurrentRight, new Vector3(35f, 1.5f, 0f), currents, "Current_Right_01");
            SpawnPrefab(PrefabCurrentRight, new Vector3(92f, -2.5f, 0f), currents, "Current_Right_02");

            // ── TUTORIAL TRIGGERS ──
            CreateTutorialTrigger(tutorials, "Trigger_Movement", new Vector3(2f, 0f, 0f), new Vector2(6f, 12f),
                "WELCOME TO RAINBOW SEEKER",
                "Swim through the air to collect the 7 rainbow gems in order.\nMovement has buoyant momentum and smooth control.",
                "MOVE: [WASD / Arrows / Left Stick]");

            CreateTutorialTrigger(tutorials, "Trigger_FirstColour", new Vector3(18f, 0f, 0f), new Vector2(6f, 12f),
                "RAINBOW COLOUR SEQUENCE",
                "Your first target is RED. Collect gems in order: Red, Orange, Yellow, Green, Blue, Indigo, Violet.",
                "Check the Rainbow Meter at the bottom for your NEXT required colour!");

            CreateTutorialTrigger(tutorials, "Trigger_Banking", new Vector3(115f, 0f, 0f), new Vector2(6f, 12f),
                "RAINBOW REST AHEAD",
                "Swim through the Rainbow Rest shrine to permanently bank your collected colours!\nFirst activation heals 1 HP and awards 100 bonus points.",
                "Banked colours survive a knockout!");
        }

        private static void BuildSectionTwo(GameObject platforms, GameObject currents, GameObject hazards, GameObject gems, GameObject rests, GameObject tutorials)
        {
            // ── REQUIRED GEMS ──
            SpawnPrefab(PrefabGemGreen, new Vector3(165f, 5f, 0f),  gems, "Gem_04_Green_Required");
            SpawnPrefab(PrefabGemBlue,  new Vector3(205f, -5f, 0f), gems, "Gem_05_Blue_Required");

            // ── DECOY GEMS ──
            SpawnPrefab(PrefabGemIndigo, new Vector3(145f, -4.5f, 0f), gems, "Gem_Decoy_Indigo_Mid");
            SpawnPrefab(PrefabGemViolet, new Vector3(185f, 4.5f, 0f),  gems, "Gem_Decoy_Violet_Mid");

            // ── SECOND RAINBOW REST ──
            SpawnPrefab(PrefabRainbowRest, new Vector3(225f, 0f, 0f), rests, "RainbowRest_02");

            // ── CURRENTS (Up-Right toward Green, Down-Right toward Blue) ──
            SpawnPrefab(PrefabCurrentUpRight,   new Vector3(148f, 1.5f, 0f), currents, "Current_UpRight_ToGreen");
            SpawnPrefab(PrefabCurrentDownRight, new Vector3(188f, -1.5f, 0f), currents, "Current_DownRight_ToBlue");

            // ── HAZARDS (Stationary Storm Clouds with comfortable clearance >= 3 units) ──
            SpawnPrefab(PrefabHazardContact, new Vector3(155f, -1.5f, 0f), hazards, "Hazard_Storm_01");
            SpawnPrefab(PrefabHazardContact, new Vector3(175f, 2f, 0f),    hazards, "Hazard_Storm_02");
            SpawnPrefab(PrefabHazardContact, new Vector3(195f, 0f, 0f),     hazards, "Hazard_Storm_03");

            // ── CRACKED DASH-BREAKABLE CLOUD (Shortcut at central lane) ──
            SpawnPrefab(PrefabHazardBreakable, new Vector3(185f, 0f, 0f), hazards, "Hazard_Breakable_Shortcut");

            // ── PLATFORMS FROM SPRITE PACK ──
            SpawnPrefab(PrefabPlatformGrassWide1,   new Vector3(165f, 3.0f, 0f), platforms, "Platform_Grass_05");
            SpawnPrefab(PrefabPlatformGrassWide2,   new Vector3(205f, -3.0f, 0f), platforms, "Platform_Grass_06");
            SpawnPrefab(PrefabPlatformStoneIsland,  new Vector3(145f, 2.5f, 0f), platforms, "Platform_Stone_01");
            SpawnPrefab(PrefabPlatformGrassIsland,  new Vector3(215f, 2.5f, 0f), platforms, "Platform_Island_03");

            // Rest 2 bounds
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(225f, -4.5f, 0f), platforms, "Platform_Long_03");
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(225f, 4.5f, 0f),  platforms, "Platform_Long_04");

            // ── TUTORIAL TRIGGER ──
            CreateTutorialTrigger(tutorials, "Trigger_DangerAndDash", new Vector3(138f, 0f, 0f), new Vector2(6f, 12f),
                "DANGER & DASH",
                "Avoid red storm clouds — they deal damage and knock you back!\nDash through cracked purple clouds to smash them for +50 bonus points.",
                "DASH: [Space] or [Gamepad South Button]");
        }

        private static void BuildSectionThree(GameObject platforms, GameObject currents, GameObject hazards, GameObject gems, GameObject gate, GameObject tutorials)
        {
            // ── REQUIRED GEMS ──
            SpawnPrefab(PrefabGemIndigo, new Vector3(275f, 5f, 0f),   gems, "Gem_06_Indigo_Required");
            SpawnPrefab(PrefabGemViolet, new Vector3(320f, -4.5f, 0f), gems, "Gem_07_Violet_Required");

            // ── RAINBOW GATE (at x = 350, y = 0) ──
            SpawnPrefab(PrefabRainbowGate, new Vector3(350f, 0f, 0f), gate, "RainbowGate");

            // ── DECOY GEM (Optional nook) ──
            SpawnPrefab(PrefabGemRed, new Vector3(260f, -4.5f, 0f), gems, "Gem_Decoy_Red_Late");

            // ── MOVING HAZARD (Telegraphed vertical patrol near x = 295) ──
            var movingHaz = SpawnPrefab(PrefabHazardMoving, new Vector3(295f, 1f, 0f), hazards, "Hazard_MovingStorm");
            if (movingHaz != null)
            {
                var mover = movingHaz.GetComponent<MovingHazard>();
                if (mover != null)
                {
                    SetField(mover, "pointA", new Vector2(295f, -2.5f));
                    SetField(mover, "pointB", new Vector2(295f, 3.5f));
                    SetField(mover, "moveDuration", 3.2f);
                }
            }

            // ── BONUS BREAKABLE HAZARD ──
            SpawnPrefab(PrefabHazardBreakable, new Vector3(305f, 3.5f, 0f), hazards, "Hazard_Breakable_BonusScore");

            // ── STATIONARY HAZARDS ──
            SpawnPrefab(PrefabHazardContact, new Vector3(255f, 1f, 0f), hazards, "Hazard_Storm_Late_01");
            SpawnPrefab(PrefabHazardContact, new Vector3(280f, -1.5f, 0f), hazards, "Hazard_Storm_Late_02");

            // ── CURRENTS ──
            SpawnPrefab(PrefabCurrentRight, new Vector3(260f, 3.5f, 0f), currents, "Current_ToIndigo");
            SpawnPrefab(PrefabCurrentDownRight, new Vector3(300f, 0f, 0f), currents, "Current_ToViolet");
            SpawnPrefab(PrefabCurrentRight, new Vector3(335f, 0f, 0f), currents, "Current_RunwayToGate");

            // ── PLATFORMS FROM SPRITE PACK ──
            SpawnPrefab(PrefabPlatformGrassWide1,  new Vector3(275f, 3.0f, 0f), platforms, "Platform_Grass_07");
            SpawnPrefab(PrefabPlatformGrassWide2,  new Vector3(320f, -2.0f, 0f), platforms, "Platform_Grass_08");
            SpawnPrefab(PrefabPlatformStoneIsland, new Vector3(260f, -2.0f, 0f), platforms, "Platform_Stone_02");

            // Final safe runway platforms leading smoothly into Rainbow Gate (x=335 to 350)
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(340f, -4.5f, 0f), platforms, "Platform_Runway_01");
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(352f, -4.5f, 0f), platforms, "Platform_Runway_02");
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(340f, 4.5f, 0f),  platforms, "Platform_Runway_03");
            SpawnPrefab(PrefabPlatformGrassTileLong, new Vector3(352f, 4.5f, 0f),  platforms, "Platform_Runway_04");

            // ── TUTORIAL TRIGGER ──
            CreateTutorialTrigger(tutorials, "Trigger_FinalApproach", new Vector3(245f, 0f, 0f), new Vector2(6f, 12f),
                "FINAL CHALLENGE",
                "Collect Indigo [I] and Violet [V] to complete the Rainbow sequence and unlock the Gate!\nWatch out for moving storms and find optional broken clouds for bonus points.",
                "Aim for par time under 3:00 to maximize your star rating!");
        }

        private static void CreateTutorialTrigger(GameObject parent, string name, Vector3 pos, Vector2 size, string title, string body, string hint)
        {
            var trigger = new GameObject(name);
            trigger.transform.SetParent(parent.transform);
            trigger.transform.position = pos;

            int triggerLayer = LayerMask.NameToLayer("Trigger");
            if (triggerLayer >= 0) trigger.layer = triggerLayer;

            var col = trigger.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var zone = trigger.AddComponent<TutorialTriggerZone2D>();
            SetField(zone, "title", title);
            SetField(zone, "body", body);
            SetField(zone, "controlsHint", hint);
            SetField(zone, "autoDismissDuration", 6.0f);
            SetField(zone, "triggerOnce", true);
        }

        private static void EnsureTutorialCoordinator()
        {
            var systems = GameObject.Find("Systems");
            if (systems != null)
            {
                var coord = systems.GetComponentInChildren<TutorialCoordinator>();
                if (coord == null)
                {
                    systems.AddComponent<TutorialCoordinator>();
                }
            }
        }

        private static void EnsureGameSession()
        {
            var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
            if (session != null && session.LevelDefinition == null)
            {
                var def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelDefinition_Level01.asset");
                if (def == null)
                {
                    def = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/GemmaRainbowSeeker/Data/LevelRules_Level01.asset");
                }
                if (def != null)
                {
                    SetField(session, "_levelDefinition", def);
                    EditorUtility.SetDirty(session);
                }
            }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
