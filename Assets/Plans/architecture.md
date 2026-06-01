# Project Overview
- Game Title: Bird Hunter
- High-Level Concept: A Ball Blast-inspired hypercasual game where a cannon shoots birds that lay eggs. Eggs split into smaller tiers and must be destroyed.
- Players: Single player
- Inspiration / Reference Games: Ball Blast
- Tone / Art Direction: Hypercasual, bright, engaging.
- Target Platform: Android + iOS
- Screen Orientation / Resolution: Portrait
- Render Pipeline: Universal Render Pipeline (URP)

# Game Mechanics
## Core Gameplay Loop
1. Shoot birds to prevent them from laying eggs.
2. Destroy eggs before they hit the cannon.
3. Collect coins and gems from destroyed eggs/birds.
4. Progress through levels and chapters.
5. Spin the slot machine for power-ups at specific level milestones.

## Controls and Input Methods
- Touch/Slide to move the cannon horizontally.
- Auto-shooting (or tap to shoot, depending on specific cannon type).

# UI
- **MainMenu**: Chapter selection, Shop, Upgrades.
- **HUD**: Score, Level progress, Health bar, Equipped power-ups.
- **SlotMachine**: Power-up selection UI.
- **GameOver/LevelComplete**: Results and rewards.

# Key Asset & Context
- `GameManager.cs`: Central state management.
- `GameProgressManager.cs`: Handles progression and saving.
- `GameEvents.cs`: Decouples gameplay logic from UI/FX.
- `EventBus.cs`: Global service-layer communication.
- `BaseCannon.cs`: Base class for all player cannons.
- `BaseBird.cs`: Base class for birds.
- `Egg.cs`: Egg logic including splitting.

# Architecture Plan
## 1. Decoupled Communication
- **Event-Driven**: Use `GameEvents` for local gameplay (e.g., `OnEggHit`) and `EventBus` for global signals (e.g., `CurrencyChanged`).
- **Service Locator**: Use `ServiceLocator` to access cross-scene services without singletons where possible.

## 2. State Management
- Refine `GameState` in `GameManager` to handle transitions between Loading, Gameplay, Slot, Paused, and GameOver.
- Use a dedicated `GameProgress` object to track the current run's state.

## 3. Entity Component System (Lightweight)
- Use ScriptableObjects for data-driven configuration (Birds, Eggs, Levels, Power-ups).
- Implement a `Damageable` component or interface to handle health and destruction consistently.

## 4. Power-up System
- Use a "Modifier" pattern for power-ups to avoid bloating the Cannon class.
- `CannonPowerUpCaster` applies modifiers based on `PowerupConfig`.

# Implementation Steps
1. **Refine Core Management**: Ensure `GameManager` and `GameProgressManager` are fully synchronized.
2. **Standardize Damage System**: Ensure all damageable entities use the same interface.
3. **UI Hookup**: Use `GameEvents` to update UI elements (Score, Health) to keep UI scripts thin.
4. **Verification**: Verify level transitions and power-up applications.

# Verification & Testing
- **Unit Tests**: Test `GameProgress` serialization/deserialization.
- **Play Mode Tests**: Verify cannon movement and shooting.
- **Manual Checks**: Verify slot machine appears at correct levels (1, 6, 11, 16).
