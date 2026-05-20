# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Identity

**Bird Hunter** — a Unity 6 (6000.3.6f1, URP) mobile hypercasual game inspired by Ball Blast. Cannon at the bottom of the screen shoots upward; birds fly in and lay eggs that bounce around the play area; the player destroys eggs (which split into smaller tiers, E4→E3→E2→E1) and birds, collects coins, and progresses through chapters/levels with a slot-machine powerup system.

Ball Blast alignment, ideation backlog, and per-module status are documented in `docs/GDD_Event-Driven_Architecture.md`. Other design docs live alongside it in `docs/`:

- `EndlessMode_Design.md`, `Progression_Reward_Economy.md`, `Reward_Tuning.md`, `Shop_Pricing.md`, `Cannon_Powerup_Unlock_Plan.md`, `Soft_Launch_Verification.md`, `Timeline_Production_Ready.md`, `GDD_Artist_QA_Pitch.md`.

Consult the relevant doc *before* changing economy, reward, progression, or powerup tuning.

## Working in Unity Projects

Unity projects are sprawling (Assets/, Library/, Packages/, hundreds of prefabs and meta files). Exhaustive enumeration will fill your context with noise. Get oriented, then target.

- **Always search `Assets/` first.** `Assets/Scripts/` is the main code root. Editor extensions live in `Editor/` subfolders.
- **`Library/` is generated** — ignore it except for `Library/PackageCache/` when you need to read registry-package source. Never edit a cached package; embed it under `Packages/` first.
- **`Packages/`** holds local + embedded packages. Check `Packages/manifest.json` for git/file references.
- **`ProjectSettings/`** rarely needs edits — only for project-wide config.

**Rule of thumb:** if you're doing more than 3-4 file operations before starting actual work, stop and ask the user. They know the project better than exploration will reveal.

## Code Architecture

### Boot flow

The game boots through a fixed scene sequence handled by `Assets/Scripts/Boot/BootController.cs`:

1. **BootStrapper** scene runs `BootController.Awake` → registers services, subscribes to progress events.
2. `RunBootSequenceAsync`: AuthService sign-in (GPGS on Android, Game Center on iOS, anonymous fallback) → RemoteConfig fetch with maintenance/force-update gates → load **LoadingScene** additively → ads init (LevelPlay) in parallel → CloudSave/Currency/ChapterUnlock data load → load **MainMenu** additively → unload Loading + BootStrapper.
3. Gameplay scene is **GamePlayScene** (see `SceneNames` constants in `Assets/Scripts/Core/SceneNames.cs`). There are several legacy/test gameplay scenes (`Gameplay.unity`, `GameScene.unity`, `Testing.unity`) — prefer `GamePlayScene` unless told otherwise.
4. A 90-second `CancellationTokenSource` arms boot; on timeout/failure, the login panel is re-shown for retry. Use `ResetTimeoutCts()` if you add new retry entry points.

### Two distinct event systems (do not confuse them)

1. **`EventBus`** (`Assets/Scripts/Core/EventBus.cs`) — generic struct-typed pub/sub: `EventBus.Subscribe<T>(handler)`, `Publish<T>(eventData)`. Used for **boot, auth, scene, currency, chapter, user-data** events. Event struct definitions live in `Assets/Scripts/Events/` (e.g. `AuthEvents.cs`, `CurrencyEvents.cs`, `DataLoadEvents.cs`, `ChapterEvents.cs`, `SceneEvents.cs`).
2. **`GameEvents`** (`Assets/Scripts/Gameplay/Events/GameEvents.cs`, namespace `Gameplay.Events`) — static C# `event Action<...>` bus for **in-game gameplay**: egg/bird hits and kills, score, cannon shoot/health/destroy, level countdown, spin/powerup, boss phases, reward notifications, self-clear endgame, chapter transition. Subscribe directly to the events; fire via the `FireXxx` helpers. The `RewardNotification` struct + `RewardKind` enum are HUD-toast filters — per-egg coin drops are intentionally *not* routed here (would spam the HUD).

When adding a new gameplay signal, decide which system it belongs to: cross-scene/service-layer events go through `EventBus`; in-level gameplay reactions go through `GameEvents`.

### Service registration

`ServiceLocator` (`Assets/Scripts/Core/ServiceLocator.cs`) is a static `Dictionary<Type, object>` registry. Registered during boot (`BootController.InitializeServices`): `AuthService`, `ICloudSaveManager`, `CloudDatabase`, `SceneLoader`, `CurrencyManager`, `ChapterUnlockService`, `UserDataRepository`. Singleton-style managers (`AudioManager`, `RemoteConfigManager`, `PushNotificationService`, `CloudSaveManager`, `CurrencyManager`) also exist as `.Instance` accessors — both patterns coexist; prefer the existing pattern of whichever you're touching.

### Code organization (`Assets/Scripts/`)

- `Boot/`, `Bootstrappers/` — entry-point controllers and auth bootstrapper.
- `Core/` — `EventBus`, `ServiceLocator`, `SceneNames`, `CloudKeys`, `EconomyKeys`.
- `Events/` — `EventBus` event struct definitions (service-layer).
- `Gameplay/` — in-level systems. Subfolders: `Birds/`, `BossBirds/`, `Eggs/`, `Health/` (`EggHealth`, `BirdHealth` implement `IDamageable`), `Interfaces/`, `Levels/`, `Managers/` (e.g. `ScoreManager`), `Player/`, `Pooling/`, `PowerUps/`, `VFX/`, `Slot/`, plus `SpawnController.cs`.
- `Manager/` — global runtime managers (`GameManager`, `CoinManager`, `DiamondManager`, `GoldManager`, `EggManager`, `GamePoolManager`, `ScreenBoundManager`, `ThemeManager`, `LayerManager`, `TagManager`, `PointManager`). Note this is separate from `Gameplay/Managers/`.
- `Services/` — backend & SDK integration: `AuthService`, `CloudSaveManager`, `CloudDatabase`, `AdManager`/`AdsManager` (LevelPlay), `IAPManager`, `AnalyticsServices`, `EconomyManager`, `LeaderboardManager`, `PushManager`/`PushNotificationService`, `RewardHandler`, `SceneLoader`, `UserDataRepository`, `ChapterUnlockService`, `PowerupGate`.
- `Data/` — DTOs: `UserData`, `CurrencyData`, `InventoryData`, `ShopData`, `ChapterUnlockStatusData`.
- `RemoteConfig/`, `Audio/`, `UI/`, `Canvas/`, `MainMenu/`, `Chapters/`, `Shop/`, `Slot/`, `PowerUpPanel/`, `Achievements/`, `Task Center/`, `Inventory/`, `Cannons/`, `FX/`, `CoinFlow/`, `Scriptbles/` (ScriptableObject configs).

ScriptableObject configs live in `Assets/Resources/` (e.g. `Chapters Config.asset`, `PowerupDatabase.asset`, `BossEventBus.asset`, `New Game Progress Manager.asset`).

### Conventions

- Async work uses **UniTask** (`Cysharp.Threading.Tasks`) — `UniTask`, `UniTaskVoid`, `.Forget()`, `CancellationToken`-first APIs. Tween library is **DOTween**. Mobile haptics via **Lofelt Nice Vibrations**. Most async paths thread a `CancellationToken` linked to `AppLifetime.Token` + `GetCancellationTokenOnDestroy()`.
- No assembly-definition (`.asmdef`) split across gameplay code — everything compiles into `Assembly-CSharp` (plus Editor + first-pass). Don't introduce `.asmdef` files casually; it will reshuffle compilation.
- Namespacing is partial. Gameplay code uses `Gameplay.*` (`Gameplay.Events`, `Gameplay.Birds`, `Gameplay.Levels`, `Gameplay.PowerUps`, `Gameplay.Interfaces`); service/boot code is mostly in the global namespace. Match the surrounding file.
- `GameRoot` (`Assets/Scripts/Boot/GameBoot.cs`, despite the filename) sits in BootStrapper and forces `targetFrameRate = 60`, `vSyncCount = 0`, `antiAliasing = 1`, and runs `GC.Collect` on scene-load and focus-return.

## Unity Code Execution (RunCommand)

When asked to execute C# in the editor (create objects, modify scenes, change materials, etc.), use the **RunCommand** tool with this template:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        result.RegisterObjectCreation(cube);
        result.Log("Created {0}", cube);
    }
}
```

Non-negotiable rules:
- Class must be `internal class CommandScript` — any other name or `public` will fail.
- Use `result.RegisterObjectCreation(obj)` after creating, `result.RegisterObjectModification(obj)` **before** changing, `result.DestroyObject(obj)` instead of `DestroyImmediate`.
- Log via `result.Log("text {0}", obj)`, `result.LogWarning`, `result.LogError`. No top-level statements.

## Verifying Unity Work

Unity changes give no immediate feedback — code compiles and objects spawn but you can't see whether they look right.

- **Object creation/modification** → screenshot via `Unity.SceneView.CaptureMultiAngleSceneView` (3D) or `Unity.SceneView.Capture2DScene` (2D, this project is mostly 2D gameplay).
- **Code/logic changes** → check console via `Unity.GetConsoleLogs`.
- **Both** → console first, then screenshot.

Fix simple visual/positional/console-error issues immediately. Ask before fixing when the work expands meaningfully or when multiple valid solutions exist (e.g. render-pipeline ambiguity). Verify after each logical phase of multi-step work; one screenshot at the end of a batch, not after every object.

## Decision Making

**Build/create X:**
- If similar X exists → ask "extend or start fresh?"
- If not → proceed.

**Fix/modify X:**
- One targeted search → read → fix.
- If not found → ask for a pointer.

Don't read files "just in case." Don't enumerate folders looking for "relevant" code. The user can supply context faster than exploration.
