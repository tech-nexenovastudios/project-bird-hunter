# Project Overview
- Game Title: Bird Hunter
- High-Level Concept: Ball Blast-inspired hypercasual. Cannon shoots birds and eggs.
- Players: Single player.
- Inspiration: Ball Blast.
- Render Pipeline: URP.
- Target Platform: Android + iOS.
- Unity Version: 6000.3.6f1

# Game Mechanics
## Core Gameplay Loop
The player controls a cannon at the bottom of the screen, shooting projectiles upward at birds (bosses) and eggs. When a boss's HP reaches 0, it triggers a death sequence.
## Controls and Input Methods
The cannon moves horizontally based on touch/mouse input and shoots automatically or on press.

# UI
Not directly modified, but the VFX will be visible in the gameplay scene upon boss defeat.

# Key Asset & Context
- Boss Prefab: `Assets/Prefab/BossBirds/1.GorgonGriffin.prefab`
- Boss Config: `Assets/Resources/Data/BirdConfigs/BossBirds/Configs/1-GorgonGriffin.asset`
- Pooling: `PoolManager.cs` using `IPoolable` interface.
- VFX Tech: Visual Effect Graph (URP).

# Implementation Steps

## 1. Create VFX Management Script
Implement a script to handle VFX Graph lifecycle within the project's pooling system.
- File: `Assets/Scripts/Gameplay/VFX/VFXGraphAutoReturn.cs`
- Logic:
    - Implement `IPoolable`.
    - In `OnPoolSpawned`, reset the `VisualEffect` component.
    - Start a `UniTask` or `Coroutine` based on a serialized duration or `VisualEffect` event to call `PoolManager.ReturnDelayed(gameObject, delay)`.

## 2. Create the VFX Graph Asset
Implement the "Petrification Shatter" effect.
- File: `Assets/VFX/BossBird/GorgonGriffin_Death_VFX.vfx`
- Systems:
    - **Stone Shards (GPU Particles):** 
        - Spawn Mode: Burst (50-100 particles).
        - Shape: Sample from a box/sphere matching the boss's bounds (~9x3 units).
        - Movement: Explode outward with high initial velocity, drag, and gravity.
        - Look: Textured quads or small meshes with a stone-gray color/texture.
    - **Dust Cloud:**
        - Spawn Mode: Burst.
        - Look: Soft gray-to-transparent quads using `explosion-smoke` texture.
        - Purpose: To mask the boss's "poofing" out of existence.
    - **Golden Essence:**
        - Spawn Mode: Constant rate over a short duration.
        - Movement: Float upward with turbulence.
        - Look: Bright golden/green sparkles representing the Griffin's soul.

## 3. Create the VFX Prefab
Combine the graph and the management script.
- File: `Assets/Prefab/VFX/PFX_GorgonGriffin_Death.prefab`
- Setup:
    - Add `VisualEffect` component referencing `GorgonGriffin_Death_VFX`.
    - Add `VFXGraphAutoReturn` script.
    - Configure the auto-return delay (e.g., 3.0 seconds).

## 4. Integrate with Boss Configuration
Hook the new VFX into the existing boss data.
- Asset: `Assets/Resources/Data/BirdConfigs/BossBirds/Configs/1-GorgonGriffin.asset`
- Action: Assign `PFX_GorgonGriffin_Death` to the `deathVFX` field.

# Verification & Testing
- **Visual Verification:** Manually verify in the `GamePlayScene` that the VFX triggers when the Gorgon Griffin's health reaches zero. Check that the scale and positioning match the boss.
- **Pooling Verification:** Verify that the `PFX_GorgonGriffin_Death` instances are returned to the pool after the duration (no infinite growth in hierarchy).
- **Style Match:** Ensure the gray/gold color palette fits the "Gorgon Griffin" mythos and the game's overall vibrant hypercasual look.
