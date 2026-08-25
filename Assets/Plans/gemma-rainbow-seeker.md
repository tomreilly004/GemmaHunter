# Gemma Beaker: Rainbow Seeker — Implementation Plan

> This plan is the agreed blueprint. It will be executed incrementally across subsequent prompts.
> No gameplay implementation begins until you approve this plan **and** send the next task prompt.

---

# Project Overview
- **Game Title:** Gemma Beaker: Rainbow Seeker
- **High-Level Concept:** A 2.5D free-swimming collectathon. Gemma glides through the air with momentum, gathering the seven rainbow colours **in strict order** (Red → Violet), banking progress at Rainbow Rests, dodging hazards, and reaching the Rainbow Gate once the rainbow is complete.
- **Players:** Single player.
- **Inspiration / Reference Games:** Ecco the Dolphin (momentum swimming), Ori (parallax + feel), Sonic (combo/score/star rating).
- **Tone / Art Direction:** Bright, colourful, whimsical. **Placeholder shapes/colours/text only** for the prototype — no external art/audio required.
- **Target Platform:** StandaloneWindows64 (PC).
- **Screen Orientation / Resolution:** Landscape 1920x1080.
- **Render Pipeline:** URP 17.3.0 (already installed).

## Confirmed Environment (verified during inspection)
- Unity **6000.3.10f1**
- Input System **1.18.0** (New Input System backend active)
- URP **17.3.0**, 2D toolset present, Physics2D present, Test Framework **1.6.0**
- **Cinemachine NOT installed** → will install **`com.unity.cinemachine` 3.x** (Unity 6 line; Package Manager resolves the exact compatible build). Uses `CinemachineCamera` + `CinemachineConfiner2D`.
- `Assets/GemmaRainbowSeeker/` does not exist yet → clean slate. Existing `[Scenes]`, `[Settings]`, `[Sprites]` folders will **not** be touched.

---

# Game Mechanics

## Core Gameplay Loop
1. Free-swim (4-directional, momentum-based) left-to-right through the level.
2. Identify and collect the **next required colour** gem (highlighted).
3. Correct gem → score × combo, note plays, trail brightens, rainbow meter fills, combo rises.
4. Wrong gem → not collected, combo −0.5 (min x1), warning FX/sound, short rejection cooldown.
5. Bank colours at Rainbow Rests (checkpoints, heal-once bonus).
6. Avoid/destroy hazards; dash for burst + hazard immunity + break cracked hazards.
7. Complete the rainbow in order → enter unlocked Rainbow Gate → results + star rating.

## Movement Model (critical feel requirement)
Gemma **swims**, not walks:
- Acceleration toward input direction, momentum carry, smooth exponential deceleration when input released.
- Full 4-directional control (no gravity-based platforming).
- Gentle drag; optional slight banking/rotation of sprite toward velocity for feel.
- Dash = short high-speed impulse in current facing/input direction with cooldown.
- Hard clamp so Gemma cannot leave the playable area or the camera frame (Cinemachine confiner + a `PlayableAreaBounds` clamp as backstop).

## Controls and Input Methods (New Input System — `GemmaControls.inputactions`)
| Action | Type | Bindings |
|---|---|---|
| Move | Value / Vector2 | WASD, Arrow keys, Gamepad left stick, Gamepad D-pad |
| Dash | Button | Space, Gamepad South (A/Cross) |
| Pause | Button | Escape, Gamepad Start |

Generated C# wrapper class consumed by a `PlayerInputReader` component (event-driven; no per-frame polling of the Input Manager).

---

# UI (placeholder uGUI + TextMeshPro)
All screens use `Canvas` (Screen Space – Overlay) with plain shapes/labels.

- **HUD (in-game):**
  - Rainbow Meter: 7 colour slots (top). Empty = grey outline, filled = solid colour. Next-required slot pulses/highlights.
  - Score (top-right), Combo multiplier (e.g. `x1.75`), Health (3 pips), Timer (counts up; par 180s).
- **Pause Menu:** Resume / Restart Level / Quit. (Esc / Start toggles.)
- **Knockout Panel:** "Restart from Rainbow Rest" and "Restart Level".
- **Results Screen:** Score, Time, Mistakes (wrong gems), Damage taken, Restarts, Star rating (1–3 stars). Buttons: Restart Level / Quit.

Wireframe (HUD):
```
[R][O][Y][G][B][I][V]                 Score: 1250
 (next=O pulsing)                      Combo: x1.75
Health: ♥ ♥ ♡                          Time: 000:42
```

---

# Key Asset & Context

## Folder layout (all under `Assets/GemmaRainbowSeeker/`)
```
GemmaRainbowSeeker/
  Scenes/            LevelOne.unity
  Scripts/
    Core/            GameSession.cs, GameEvents.cs, ServiceRefs.cs, RainbowColor.cs
    Player/          PlayerSwimController.cs, PlayerInputReader.cs, PlayerHealth.cs,
                     PlayerDash.cs, PlayerTrail.cs, PlayableAreaBounds.cs
    Collectibles/    Gem.cs, GemColorDef.cs, RainbowProgress.cs
    Hazards/         Hazard.cs, BreakableHazard.cs, SolidBlocker.cs
    Checkpoints/     RainbowRest.cs, RespawnManager.cs
    Level/           RainbowGate.cs, ParallaxLayer.cs, LevelTimer.cs
    Scoring/         ScoreSystem.cs, ComboSystem.cs, StarRating.cs
    Audio/           NotePlayer.cs, SfxPlayer.cs
    UI/              HudController.cs, PauseMenu.cs, KnockoutPanel.cs, ResultsScreen.cs
    Input/           GemmaControls.inputactions (+ generated GemmaControls.cs)
  Prefabs/           Gem.prefab, Hazard.prefab, BreakableHazard.prefab,
                     RainbowRest.prefab, RainbowGate.prefab, Player_Gemma.prefab,
                     ParallaxLayer.prefab, GameSession.prefab, HUD.prefab
  ScriptableObjects/ GemColorDef assets (Red..Violet), LevelConfig.asset
  Art/               placeholder sprites (circle, square, capsule) + solid-colour materials
  Audio/             (empty; notes generated or simple AudioClips)
  Tests/
    EditMode/        ComboSystemTests.cs, ScoreSystemTests.cs, RainbowProgressTests.cs,
                     StarRatingTests.cs
```

## Architecture principles
- **`GameSession`** = scene composition root. Holds/creates the systems (Score, Combo, RainbowProgress, Timer, Respawn) and injects references into components via Inspector wiring or `Awake` registration. No global singletons.
- **`GameEvents`** = a plain C# event hub instance owned by `GameSession` (not static). Components subscribe/raise: `OnCorrectGem`, `OnWrongGem`, `OnColorBanked`, `OnHealthChanged`, `OnCheckpointReached`, `OnComboChanged`, `OnScoreChanged`, `OnLevelComplete`, `OnPlayerKnockout`. This satisfies "event-driven" + "avoid repeated scene searches".
- **`RainbowColor` enum:** Red, Orange, Yellow, Green, Blue, Indigo, Violet.
- Tuning values exposed with `[Tooltip]` + `[Range]`/`[Min]` on all controllers.

## Key signatures (illustrative)
```csharp
namespace GemmaRainbowSeeker
{
    public enum RainbowColor { Red, Orange, Yellow, Green, Blue, Indigo, Violet }

    // Central, non-singleton event hub owned by GameSession
    public sealed class GameEvents {
        public event System.Action<RainbowColor> CorrectGemCollected;
        public event System.Action<RainbowColor> WrongGemTouched;
        public event System.Action<float> ComboChanged;
        public event System.Action<int> ScoreChanged;
        public event System.Action<int> HealthChanged;
        public event System.Action<RainbowRest> CheckpointActivated;
        public event System.Action LevelCompleted;
        public event System.Action PlayerKnockedOut;
        // Raise* methods invoked by systems
    }

    public sealed class ComboSystem {
        public float Multiplier { get; }         // starts 1.0, +0.25 to 2.5
        public void RegisterCorrect();           // +0.25 clamp 2.5
        public void RegisterWrong();             // -0.5 clamp min 1.0
        public void Reset();                     // back to 1.0
    }
}
```

## Physics / layer setup
- 2D physics on XY. New layers: `Player`, `Gem`, `Hazard`, `Solid`, `Checkpoint`, `Gate`, `Bounds`.
- Collision matrix configured so gems/checkpoints/gate/hazards are **triggers** where appropriate; `Solid` uses non-trigger colliders that always block.
- Gem/Hazard use `OnTriggerEnter2D`; wrong-gem rejection cooldown prevents repeat triggers.

---

# Implementation Steps

> Each step lists what to build, the assigned role, dependencies, and parallelizability.
> Steps are executed in later prompts; you approve the overall plan now.

### Step 1 — Install Cinemachine & scaffold folders
- **Description:** Add `com.unity.cinemachine` (3.x) via manifest; let it resolve/compile. Create the `Assets/GemmaRainbowSeeker/` folder tree.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (gates everything)

### Step 2 — Input actions + core types
- **Description:** Create `GemmaControls.inputactions` (Move/Dash/Pause bindings) with C# class generation. Add `RainbowColor` enum, `GameEvents` hub, `ServiceRefs`.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** Yes (with Step 3 art placeholders)

### Step 3 — Placeholder art & ScriptableObjects
- **Description:** Generate placeholder sprites (circle/square/capsule) + solid colour materials; author 7 `GemColorDef` assets (colour, note pitch, order index) and a `LevelConfig` (par time 180s, star thresholds 1600/2200).
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** Yes (with Step 2)

### Step 4 — Player: swim movement, dash, input reader, bounds
- **Description:** `PlayerSwimController` (accel/momentum/decel, 4-dir), `PlayerDash` (impulse + cooldown + hazard immunity), `PlayerInputReader` (event-driven), `PlayableAreaBounds` clamp, `PlayerTrail` (TrailRenderer). Build `Player_Gemma.prefab`.
- **Assigned role:** developer
- **Dependencies:** Steps 2, 3
- **Parallelizable:** No

### Step 5 — Scoring, combo, rainbow progress systems
- **Description:** `ScoreSystem`, `ComboSystem`, `RainbowProgress` (in-order tracking + next-required), `StarRating`. Wire to `GameEvents`.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** Yes (with Step 4)

### Step 6 — Gems (correct/wrong behaviour)
- **Description:** `Gem` + `GemColorDef`. Correct: collect/disappear, meter+score+combo+note+trail-flash+highlight-next. Wrong: no collect, combo −0.5, warning FX/sfx, rejection cooldown. `Gem.prefab`.
- **Assigned role:** developer
- **Dependencies:** Steps 4, 5
- **Parallelizable:** No

### Step 7 — Hazards, breakables, solids, health
- **Description:** `PlayerHealth` (3 HP, knockback, i-frames), `Hazard`, `BreakableHazard` (dash-destroy, +50), `SolidBlocker`. Prefabs.
- **Assigned role:** developer
- **Dependencies:** Step 4
- **Parallelizable:** Yes (with Step 6)

### Step 8 — Rainbow Rest checkpoints & respawn
- **Description:** `RainbowRest` (bank colours, set respawn, first-activation heal+100 once), `RespawnManager` (restart-from-rest: full HP, combo reset, −200 score min 0, respawn unbanked gems, keep banked). Prefab.
- **Assigned role:** developer
- **Dependencies:** Steps 5, 6, 7
- **Parallelizable:** No

### Step 9 — Rainbow Gate & level completion
- **Description:** `RainbowGate` (locked until all 7 in order; entering banks final, stops timer, disables control, opens results). `LevelTimer`. Completion scoring: +150/HP, +5/sec under par, star thresholds.
- **Assigned role:** developer
- **Dependencies:** Steps 5, 8
- **Parallelizable:** No

### Step 10 — Camera & parallax (2.5D)
- **Description:** Orthographic camera + `CinemachineCamera` following Gemma with damping; `CinemachineConfiner2D` bounding the playable area. `ParallaxLayer` components + layered background prefabs.
- **Assigned role:** developer
- **Dependencies:** Step 4
- **Parallelizable:** Yes (with Steps 5–9)

### Step 11 — UI (HUD, Pause, Knockout, Results)
- **Description:** `HudController` (rainbow meter, score, combo, health, timer — all via `GameEvents`), `PauseMenu`, `KnockoutPanel`, `ResultsScreen`. `HUD.prefab`.
- **Assigned role:** developer
- **Dependencies:** Steps 5, 8, 9
- **Parallelizable:** Partly (after Step 5)

### Step 12 — Audio (notes + sfx)
- **Description:** `NotePlayer` (per-colour note on correct gem), `SfxPlayer` (warning on wrong gem, hazard, checkpoint). Placeholder/generated clips.
- **Assigned role:** developer
- **Dependencies:** Step 6
- **Parallelizable:** Yes

### Step 13 — Assemble LevelOne scene & GameSession wiring
- **Description:** Build `LevelOne.unity`: `GameSession` root, player, HUD, camera rig, parallax, all 7 gems (+ decoys) placed for the R→V route, hazards, ≥2 Rainbow Rests, Rainbow Gate, playable-area bounds. Add scene to Build Settings. Wire all references (no runtime scene searches).
- **Assigned role:** developer
- **Dependencies:** Steps 4–12
- **Parallelizable:** No (integration)

### Step 14 — EditMode tests & polish pass
- **Description:** Tests for `ComboSystem`, `ScoreSystem`, `RainbowProgress` (in-order), `StarRating`. Console cleanup; fix all introduced errors/warnings.
- **Assigned role:** developer
- **Dependencies:** Step 13
- **Parallelizable:** No

---

# Verification & Testing

## Automated (EditMode / Test Framework 1.6.0)
- **ComboSystemTests:** starts x1; +0.25 per correct capped at x2.5; −0.5 per wrong floored at x1; reset → x1.
- **ScoreSystemTests:** correct = 100 × combo; breakable = 50; rest first-activation = 100; completion = 150 × HP + 5 × secondsUnderPar; never negative after −200 restart penalty.
- **RainbowProgressTests:** only the next required colour advances; out-of-order colour is rejected; gate locked until all 7 in order.
- **StarRatingTests:** 1 star on completion; 2 stars ≥1600; 3 stars ≥2200.

## Manual (PlayMode checks)
- Movement feels like swimming: momentum + smooth decel, 4-directional, no platformer gravity snap.
- WASD/arrows/stick/D-pad move; Space/South dashes; Esc/Start pauses.
- Correct gem: disappears, meter fills, score+combo up, note plays, trail flashes, next slot highlights.
- Wrong gem: stays, no score, combo −0.5 (min x1), warning FX/sound, no repeat trigger on held contact.
- Hazard: −1 HP, knockback, i-frames; dash gives immunity; dash breaks cracked hazards (+50).
- Cannot leave playable area / escape camera frame.
- Rainbow Rest: banks colours, becomes respawn, first pass heals +1 HP and +100 (once only).
- Knockout: panel offers Restart from Rest / Restart Level; restart-from-rest restores HP, resets combo, −200 (min 0), respawns unbanked gems, keeps banked.
- Gate locked until rainbow complete in order; entering opens results with correct score/time/mistakes/damage/restarts/stars.

## Build/compile gate (per Working Method, every task)
Save → wait for compile → inspect Console → fix every introduced error → run relevant tests → report exact changes + any genuinely required manual step.
