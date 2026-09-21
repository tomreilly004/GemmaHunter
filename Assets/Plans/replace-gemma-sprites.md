# Project Overview
- Game Title: Gemma Beaker: Rainbow Hunter
- High-Level Concept: A buoyant momentum 2D arcade platformer where the player controls Gemma swimming/gliding through vibrant atmospheric levels, gathering rainbow gems in spectral order, banking progress at Rainbow Rest shrines, and reaching the final Rainbow Gate.
- Players: Single player
- Inspiration / Reference Games: Flappy Bird, Ecco the Dolphin, Tiny Wings, 2D momentum platformers
- Tone / Art Direction: Whimsical, vibrant aquatic/celestial fantasy with lush hand-drawn sprites and particle trails
- Target Platform: Standalone Windows / PC (extensible to mobile)
- Screen Orientation / Resolution: Landscape 1920x1080
- Render Pipeline: UniversalRP (URP 2D)

# Game Mechanics
## Core Gameplay Loop
Gemma maneuvers through 2D space collecting 7 rainbow gems in strict spectral sequence (Red, Orange, Yellow, Green, Blue, Indigo, Violet). Gemma must navigate around static, breakable, and moving clouds/hazards and enemies (Gloomlings, Storm Chasers), bank collected colors at Rainbow Rest checkpoints, and pass through the final Rainbow Gate to clear each level. Continuous movement and dash abilities provide momentum-driven navigation.

## Controls and Input Methods
- Directional movement (WASD / Arrow keys / Gamepad Stick / Mobile touch): Applies acceleration and buoyant momentum via `GemmaMotor2D`.
- Dash (Space / Gamepad South / Touch button): High-velocity burst forward via `GemmaDash`.
- Smooth visual banking, squash & stretch, and tilt interpolation are dynamically driven by `GemmaVisual` responding to velocity and dash states.

# UI
- In-game HUD: Rainbow Meter display showing collected spectral gems, player health hearts, score, and level progression timer.
- Floating score popups and contextual tutorial dialogue overlays.
- Pause Modal, Knockout Modal, and Level Results Modal.
- The sprite replacement targets in-game character presentation; UI elements remain visually consistent and unimpacted.

# Key Asset & Context
- Source Sprites: `Assets/Sprites/Gemma_Beaker_Sprite_Pack/gemma_beaker_sprite_pack/Gemma_Upscaled_Character/GBRH_anim_001.png` to `GBRH_anim_061.png` (60 individual high-resolution PNG frames, note: frame 020 is omitted in source pack, 60 frames total).
- Old Sprites to be removed:
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_01.png` (+ .meta)
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_02.png` (+ .meta)
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_03.png` (+ .meta)
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_04.png` (+ .meta)
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_05.png` (+ .meta)
  - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_06.png` (+ .meta)
- Primary Animation Clip: `Assets/GemmaRainbowSeeker/Animations/Gemma_Swim.anim` (currently 6 keyframes referencing the old frames).
- Target Prefab: `Assets/GemmaRainbowSeeker/Prefabs/Gemma.prefab` (the `Visual` child has `SpriteRenderer` referencing `GemmerBunterRainbowHunter_Frame_01_0` and `Animator` playing `Gemma_Swim`).
- Pixels Per Unit (PPU) Context:
  - The old frames are ~270x173 pixels at PPU 100, giving Gemma an in-world bounding size of ~2.5 x 1.8 units.
  - The new upscaled frames are ~1665x940 pixels (approx. 6x upscale).
  - To preserve Gemma's exact physical footprint, collider fit (`CircleCollider2D` radius = 0.65), camera framing, and boundary clamping without modifying runtime scale logic, all 60 upscaled textures should be configured with `Pixels Per Unit = 600`.

# Implementation Steps
1. **Configure Texture Importer Settings for Upscaled Sprites**
   - **Description**: Inspect and configure all 60 texture assets in `Assets/Sprites/Gemma_Beaker_Sprite_Pack/gemma_beaker_sprite_pack/Gemma_Upscaled_Character/`:
     - Texture Type: `Sprite (2D and UI)`
     - Sprite Mode: `Single`
     - Pixels Per Unit (PPU): `600` (scales the 1665x940 texture to ~2.77 x 1.57 world units, perfectly matching the original ~2.5 x 1.8 unit size)
     - Pivot: `Center (0.5, 0.5)`
     - Filter Mode: `Bilinear`
     - Reimport textures to apply changes.
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: No

2. **Update Animation Clip (`Gemma_Swim.anim`)**
   - **Description**: Rebuild `Assets/GemmaRainbowSeeker/Animations/Gemma_Swim.anim`:
     - Clear old 6-frame keyframes bound to `GemmerBunterRainbowHunter_Frame_01..06`.
     - Assign all 60 upscaled frames (`GBRH_anim_001` .. `GBRH_anim_019`, `GBRH_anim_021` .. `GBRH_anim_061`) in sequential order to the `m_Sprite` curve on path `""` (the Visual GameObject's SpriteRenderer).
     - Set sample frame rate to `60` FPS (yielding a smooth, natural 1.0-second swimming loop) with loop time enabled.
   - **Assigned role**: developer
   - **Dependencies**: Depends on Step 1
   - **Parallelizable**: No

3. **Update Gemma Prefab Reference**
   - **Description**: Update `Assets/GemmaRainbowSeeker/Prefabs/Gemma.prefab`:
     - On the `Visual` child GameObject, set `SpriteRenderer.sprite` to `GBRH_anim_001` (replacing the old reference to `GemmerBunterRainbowHunter_Frame_01_0`).
     - Save prefab asset and verify instance overrides in open scenes (such as `Level01.unity`).
   - **Assigned role**: developer
   - **Dependencies**: Depends on Step 1 and Step 2
   - **Parallelizable**: No

4. **Delete Obsolete Sprite Assets**
   - **Description**: Safely delete the old low-resolution sprite assets and their `.meta` files using `AssetDatabase.DeleteAsset`:
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_01.png`
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_02.png`
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_03.png`
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_04.png`
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_05.png`
     - `Assets/Sprites/GemmerBunterRainbowHunter_Frame_06.png`
   - **Assigned role**: developer
   - **Dependencies**: Depends on Step 3
   - **Parallelizable**: No

5. **Execute Validation and Regression Tests**
   - **Description**: Run existing EditMode tests (`GemmaRainbowSeeker.Tests.EditMode`) and run level validation (`LevelValidator`) to confirm zero broken asset references, verify player bounding size matches collider bounds, and ensure swimming animation plays correctly.
   - **Assigned role**: developer
   - **Dependencies**: Depends on Step 4
   - **Parallelizable**: No

# Verification & Testing
- **Visual & Collider Fit Check**: In `Level01.unity`, select `Gemma` and ensure the SpriteRenderer bounds surround the `CircleCollider2D` (radius 0.65) consistently, with no clipping through terrain or cloud hazards.
- **Animation Playback Check**: Enter PlayMode or scrub the Animator/Animation window on `Gemma_Swim` to verify continuous 60-frame looping without jitter, popping, or missing frame artifacts.
- **Reference Integrity Check**: Run a project-wide GUID audit to ensure no missing (`MissingReferenceException` or `null` sprite) warnings exist in scenes or prefabs.
- **Automated Tests**: Run all tests in `Assets/GemmaRainbowSeeker/Tests/EditMode/` via Unity Test Runner to confirm 100% pass rate.
