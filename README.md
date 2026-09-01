# Gemma Beaker: Rainbow Seeker — Level Creation Guide

This guide provides the complete, step-by-step instructions for authoring, configuring, building, and validating levels in **Gemma Beaker: Rainbow Seeker**.

---

## 1. Architecture Overview

Levels are data-driven, modular, and testable independently:
* **`LevelDefinition` (ScriptableObject):** The single source of truth for level rules (color sequence, target completion time, Rainbow Rush time window, star score thresholds, and mechanic flags).
* **`TutorialSequence` (ScriptableObject):** Manages trigger-based, non-intrusive tutorial cards per level.
* **Scene Hierarchy:** Every level scene (`Level01.unity` .. `Level10.unity`) contains:
  * `Systems` (Composition root holding authoritative `GameSession`, `CheckpointManager`, and `TutorialCoordinator`).
  * `UI` (Root container holding `UIManager` and `MainCanvas` with HUD and modals).
  * `Gameplay` (Holds `Gemma` player object).
  * `Environment` (Holds `LevelConfinerBounds`).
  * `GENERATED_LevelXX` (Idempotent generated root for course content).

---

## 2. Step-by-Step Level Creation Workflow

### Step 1: Create the `TutorialSequence` Asset
1. In the Project window, navigate to `Assets/GemmaRainbowSeeker/Data/`.
2. Right-click $\rightarrow$ **Create $\rightarrow$ Gemma Rainbow Seeker $\rightarrow$ Tutorial Sequence**.
3. Name the asset `TutorialSequence_LevelXX.asset` (e.g., `TutorialSequence_Level11.asset`).
4. Configure one or more `TutorialStep` entries:
   * **Step ID:** Unique string (e.g., `L11_Start`).
   * **Trigger Event:** `OnLevelStart`, `OnFirstCorrectGem`, `OnFirstWrongGem`, `OnFirstHazardContact`, `OnDashUsed`, `OnRainbowRestActivated`, `OnRushBroken`, or `OnRainbowComplete`.
   * **Title & Body:** Short instruction (e.g., *"Keep moving to sustain high multipliers!"*).
   * **Controls Hint:** Input reminder (e.g., *"WASD / Joystick to swim"*).
   * **Display Duration:** Typically $4.0\text{s} - 5.0\text{s}$.
   * **Show Once:** Set to `true` so respawns do not re-trigger the card.

---

### Step 2: Create the `LevelDefinition` Asset
1. In `Assets/GemmaRainbowSeeker/Data/`, right-click $\rightarrow$ **Create $\rightarrow$ Gemma Rainbow Seeker $\rightarrow$ Level Definition**.
2. Name the asset `LevelDefinition_LevelXX.asset`.
3. Configure the serialized fields:
   * **Level Number:** Integer index (e.g., `11`).
   * **Display Name:** Friendly level title (e.g., `Level 11 — Prism Peak`).
   * **Scene Name:** Target Unity scene name (e.g., `Level11`).
   * **Colour Sequence:** Ordered list of required `RainbowColour` gems (supports repeated colors, e.g., `Red -> Red -> Orange -> Yellow -> Green -> Blue -> Indigo -> Violet`).
   * **Objective Description:** Formatted objective text (e.g., `"Collect all 7 rainbow colours in order"`).
   * **Target Completion Time:** Par time in seconds (e.g., `75`).
   * **Rainbow Rush Time Window:** Seconds before Rush resets if no gem is collected (e.g., `4.0`).
   * **Star Thresholds:**
     * *1 Star:* Always awarded upon level completion.
     * *2 Stars:* Score target for a clean run (e.g., `2500`).
     * *3 Stars:* Score target for a full-Rush, zero-damage run (e.g., `3800`).
   * **Mechanics Toggles:**
     * `DashEnabled` (true/false)
     * `HealthEnabled` (true/false)
     * `CurrentsEnabled` (true/false)
     * `SolidObstaclesEnabled` (true/false)
     * `DangerousHazardsEnabled` (true/false)
     * `EnemiesEnabled` (true/false)
     * `RainbowRestsEnabled` (true/false)
   * **Tutorial Sequence:** Link the asset created in Step 1.

---

### Step 3: Create or Clone the Scene
1. Duplicate an existing scene template (e.g., `Level01.unity`) and save as `Assets/GemmaRainbowSeeker/Scenes/LevelXX.unity`.
2. Ensure the scene hierarchy contains:
   * `Systems` $\rightarrow$ with `GameSession`, `CheckpointManager`, and `TutorialCoordinator` attached. Assign `LevelDefinition_LevelXX` to `GameSession._levelDefinition`.
   * `UI` $\rightarrow$ with `UIManager` referencing all 7 views (`HUD`, `TutorialBanner`, `MobileControls`, `PauseModal`, `LevelSelectModal`, `KnockoutModal`, `ResultsModal`).
   * `Gameplay/Gemma` $\rightarrow$ Player object positioned at `(0, 0, 0)`.

---

### Step 4: Populate Level Course Elements

All gameplay elements are placed beneath an idempotent container named `GENERATED_LevelXX` using standard prefabs from `Assets/GemmaRainbowSeeker/Prefabs/`:

#### A. Collectible Gems (`Prefabs/GemPickup_*.prefab`)
* **Required Gems:** Place each required gem along the main flow in strict X-position sequence. Name them `Gem_01_Red_Required`, `Gem_02_Orange_Required`, etc.
* **Decoy Gems:** Place wrong-color gems visibly off the main path on branching routes. Name them `Decoy_01_Blue`, etc.
* *Rule:* Never place a required gem inside a solid obstacle or overlapping an enemy.

#### B. Solid Platforms (`Prefabs/Platform_Cloud.prefab`)
* Used to carve out S-curves, upper/lower path splits, and weave corridors.
* Layer must be set to `Solid`.
* Non-damaging, smooth friction to prevent Gemma from snagging.

#### C. Magical Currents (`Prefabs/CurrentZone_*.prefab`)
* Available variants: `CurrentZone_Right`, `CurrentZone_UpRight`, `CurrentZone_DownRight`.
* Gently accelerates Gemma without removing player control.
* Size the trigger box to cover the intended current channel.

#### D. Hazards (`Prefabs/Hazard_*.prefab`)
* **Contact Storm Clouds (`Hazard_ContactCloud`):** Stationary red clouds dealing 1 damage and resetting Rush.
* **Moving Storm Clouds (`Hazard_MovingCloud`):** Uses `MovingHazard` component with 2 waypoints (`offsetA`, `offsetB`), travel duration, and pause time.
* **Dash-Breakable Clouds (`Hazard_BreakableCloud`):** Cracked purple clouds that shatter when dashed through ($+50\text{ pts}$), but damage Gemma on normal contact.

#### E. Enemies (`Prefabs/Enemy_*.prefab`)
* **Gloomling (`Enemy_Gloomling`):**
  * Configure patrol offsets (`patrolOffsetA`, `patrolOffsetB`), `travelDuration`, and `pauseDuration`.
  * Renders a visible patrol line.
  * Minimum clearance: at least $8.0\text{ units}$ from Player spawn, $2.0\text{ units}$ from gems, and $5.0\text{ units}$ from checkpoints/gate.
* **Storm Chaser (`Enemy_StormChaser`):**
  * Displays a visual detection aura circle ($7.0\text{ units}$ radius).
  * Chases at $3.4\text{ u/s}$ (slower than Gemma's $5.8\text{ u/s}$ base speed).
  * Gives up after $5.0\text{s}$ or $14.0\text{ units}$ leashed distance.
  * Rejects pursuit within $6.0\text{ units}$ of any `RainbowRest` or `RainbowGate`.

#### F. Checkpoints & Level Finish
* **Rainbow Rest (`Prefabs/RainbowRest.prefab`):**
  * Automatically banks collected colors, restores 1 health, and grants checkpoint respawn.
  * Typically placed near the halfway point or after a major sequence milestone (e.g., after Green).
* **Rainbow Gate (`Prefabs/RainbowGate.prefab`):**
  * Place at the end of the course (e.g., $x = \text{endX}$).
  * Automatically opens when `RainbowProgress` reports all required colors collected.

---

### Step 5: Camera Confiner & Course Boundaries

1. **Camera Confiner:**
   * Select `Environment/LevelConfinerBounds` in the scene.
   * Update the `PolygonCollider2D` points to encompass the full course:
     * Point 0: `(minX - 4, minY)`
     * Point 1: `(maxX + 4, minY)`
     * Point 2: `(maxX + 4, maxY)`
     * Point 3: `(minX - 4, maxY)`
   * Ensure `CinemachineConfiner2D` on `CM_PlayerCamera` points to this collider.
2. **Solid Boundaries:**
   * Enclose the playable area ($y = -6.5$ to $y = 6.5$, $x = \text{minX}$ to $x = \text{maxX}$) with solid sliced boundary walls on layer `Solid` so Gemma cannot exit the visible play area.

---

### Step 6: Idempotent Builder Scripting (`LevelBuilder.cs`)

To make level generation reproducible and scriptable:
1. Open `Assets/GemmaRainbowSeeker/Editor/LevelBuilder.cs`.
2. Add a `BuildLevelXXContent` helper method specifying object positions.
3. Add a menu item `[MenuItem("GemmaRainbowSeeker/Build Level XX")]`.
4. Update `BuildAllLevels()` and `RegisterScenesInBuildSettings()` to include the new level.

---

### Step 7: Automated Validation & Testing (`LevelValidator.cs`)

Always run the validator after modifying or adding levels:
1. From the Unity Editor menu, select **GemmaRainbowSeeker $\rightarrow$ Validate All Levels**.
2. The validator checks:
   * Correct `GameSession` & `LevelDefinition` wiring.
   * Player spawn at `(0,0,0)` with valid components.
   * Exact required color sequence matching the `LevelDefinition`.
   * No gems inside solid colliders.
   * Decoy gems placed safely off the main path.
   * Enemy safety clearances (no enemies on Gemma, gems, rests, or gate).
   * Exactly 1 Rainbow Gate at level end.
   * Camera confiner and solid boundary enclosure.
   * Compliance with mechanics flags (e.g. no hazards in early levels).

---

## 3. Level Design Best Practices

1. **Rhythm & Flow:** Space required gems so a swimming player can sustain Rainbow Rush without stopping ($12 - 16\text{ units}$ apart for base speed, longer when assisted by currents).
2. **Decoy Placement:** Decoys must be visible choices, never unavoidable obstacles placed on the direct route.
3. **Enemy Fairness:** Gloomlings should always present a clear visual timing window; Storm Chasers must never corner the player near checkpoints or gate.
4. **Dash Introduction:** Only introduce cracked purple clouds in levels where `DashEnabled = true` (Level 8+).
5. **Score Parity:** Base star thresholds on completion ($1\bigstar$), steady run ($2\bigstar$), and continuous Rush streak ($3\bigstar$).
