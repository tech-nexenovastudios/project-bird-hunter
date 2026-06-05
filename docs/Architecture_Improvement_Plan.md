# Architecture Improvement Plan

_Assessment of `Assets/Scripts/` (374 files, ~46k lines). Authored 2026-06-02._

This plan targets **system-level seams**, not entity code. The individual gameplay
entities (`Egg`, `BaseCannon`, the `IDamageable` chain, `AttackingBirds` polymorphism,
boss attack template-method pattern) are well-built and are explicitly **out of scope**.
The problems are coupling, fragmentation, inconsistent pooling, orphaned infrastructure,
and folder disorganization.

## Snapshot

| Signal | Count | Reading |
|---|---|---|
| `.Instance` references | 518 across 34 singletons | Global-state coupling is the dominant pattern |
| `FindObjectOfType` / `GameObject.Find` | 19 | Moderate runtime-lookup fragility |
| Raw `Debug.Log` | 509 | New `Core/Logging` framework adopted in ~0 of them |
| Raw `Instantiate`/`Destroy` in `Gameplay/` | ~50 / ~180 | Pooling exists but is bypassed → GC churn on mobile |
| Interfaces defined vs. injected via ServiceLocator | 48 / 1 | DI is aspirational, not real |

## Top problems

### 1. Global-state coupling
- 518 `.Instance` refs vs. ~22 going through `ServiceLocator`. 48 interfaces exist; only
  `ICloudSaveManager` is injected.
- Init-order fragility: `BootController.InitializeServices` registers via
  `ServiceLocator.Register(CurrencyManager.Instance)` — assumes `.Instance` already booted;
  silent skip otherwise.
- `ServiceLocator.Clear()` exists but is **never called** → services accumulate across
  menu→game→menu transitions.
- Most-depended-on hubs: `CurrencyManager`, `GameProgressManager`, `RemoteConfigManager`,
  `CloudSaveManager`, `IAPManager`, `AdManager`.

### 2. Fragmented currency — correctness risk
- Three parallel stacks, no single source of truth:
  - `Manager/CoinManager` + `Manager/DiamondManager` — legacy, in-memory, never persisted;
    still read by `Shop/ShopPanelCurrenciesUI`.
  - `Shop/CurrencyManager` — real, Unity Economy Service, server-backed.
  - `CoinFlow/` — animation-only events.
- A player can see different balances on different screens.
- **Confirmed bug:** `Manager/DiamondManager.Spend()` does `totalDiamonds -= Diamonds`
  (the backing field) instead of `-= spendDiamonds` → any spend zeroes the entire balance.
- IAP ownership / ads-removed flags live in **PlayerPrefs**; currency in **CloudSave** —
  no sync, no conflict resolution. Saves are fire-and-forget and can be lost on app kill.

### 3. Event-layer leak surface — narrower than first thought, but real
_Per-file subscribe/unsubscribe audit (2026-06-02). Corrects two earlier claims in this doc:
`GameOverPanel` does **not** leak (its `GameEvents.OnPlayerDeath` line is commented out;
its live `ads.OnRewarded*` handlers detach cleanly), and the `EventBus` subscribers are **not**
unbalanced._

- Two event systems (`Core/EventBus` structs; `Gameplay/Events/GameEvents` static Actions);
  the boundary is respected.
- **The two formal buses are disciplined.** All 4 `EventBus.Subscribe` files
  (`BootController`, `LoadingUI`, `ChapterUnlockView`, `CurrencyUI`) are balanced; all ~30
  `GameEvents` subscriber files are balanced (`SFXController` even over-unsubscribes, 21/22).
- **The actual risk is elsewhere:** 73 subscriptions to instance/static C# events on
  long-lived singletons (`SomeManager.Instance.OnX += handler`), against 77 static events
  declared on those singletons. When a scene-scoped MonoBehaviour subscribes to a singleton
  that outlives scene unload and forgets to detach, the singleton pins the dead object →
  never GC'd, handler keeps firing → silent duplicate-event bugs. Highest-density subscribers:
  `SpawnController`, `ScoreManager`, `CameraShakeController`, `BossBirdController`,
  `GameplayUIHandler`.
- **Remediation — auto-unsubscribe base class** so a forgotten detach is impossible:
  ```csharp
  public abstract class EventSubscriberBehaviour : MonoBehaviour
  {
      readonly List<Action> _teardown = new();
      protected void Track(Action subscribe, Action unsubscribe)
      {
          subscribe();
          _teardown.Add(unsubscribe);
      }
      protected virtual void OnDestroy()
      {
          for (int i = _teardown.Count - 1; i >= 0; i--) _teardown[i]();
          _teardown.Clear();
      }
  }
  ```
  Migrate the 73 singleton-event subscribers (start with `SpawnController`, `ScoreManager`,
  `CameraShakeController`). Leave the formal-bus subscribers as-is — already balanced.

### 4. Inconsistent pooling → mobile GC churn
- Boss eggs pool correctly, but attacking-bird projectiles (Laser/Bomb/Gun/Arrow birds) and
  powerup VFX/hazards (`Powerupmodifiers` does ~19 raw `Instantiate`) bypass pooling.
- `BossBirds/PoolManager` is a separate pool from `Gameplay/Pooling/` — two systems, no
  shared contract.

### 5. God classes — decompose vs. leave alone
- **Decompose:** `Gameplay/SpawnController.cs` (1498 — phase state-machine + bird spawning +
  egg tracking + boss orchestration + difficulty scaling); `Gameplay/Managers/GameProgressManager.cs`
  (604 — progress state + slots + attempts + cloud save + level loading);
  `Inventory/CannonInventoryView.cs` (1026 — UI + business logic);
  `UI/Profile/UserProfileDataManager.cs` (602 — validation + UI + persistence).
- **Leave alone (false positives):** `Gameplay/Input/PlayerInputAction.cs` (1505) is
  **auto-generated** Input System code; `Egg.cs` (855) and `BaseCannon.cs` (607) are dense
  but cohesive; `Powerupmodifiers.cs` (1172) is already polymorphic (strategy interfaces) —
  only the ~15-case element-effect switch is borderline.

### 6. Disorganization & dead code
- **32 loose scripts at `Scripts/` root** (`Fire.cs`, `GroundSpike.cs`, `BeeGenerator.cs`,
  `ManaManager.cs`, `ScoreManager.cs`, …) with no namespaces — belong in `Gameplay/` or `Manager/`.
- **Dead:** `PoerUpPanel/` (only `.DS_Store`); `Services/AdsManager.cs` (16-line mock, never
  instantiated, vs. real `AdManager`); root `ScoreManager.cs` (33-line `[Obsolete]` stub vs.
  real 217-line `Gameplay/Managers/ScoreManager.cs`).
- **Name collisions:** two `GameManager` (`Manager/` global + `Gameplay/Managers/` level) and
  two `ScoreManager`.
- **Typos:** `PowerUpPanel/PoweUpCardAnimator.cs`, `GamePlayUI/LevelDecription.cs`.
- **Orphaned infra:** `Core/Logging/` (`GameLogger`, `EconomyLog`) is well-built but has
  zero call sites; 509 raw `Debug.Log` remain.

## Roadmap (sequenced by risk/effort)

### Phase 0 — Cleanup & safety (low risk)
1. Delete dead code: `PoerUpPanel/`, `Services/AdsManager.cs`, root `ScoreManager.cs`.
2. Fix filename typos: `PoweUpCardAnimator` → `PowerUpCardAnimator`, `LevelDecription` → `LevelDescription`.
3. Move the 32 root scripts into `Gameplay/` / `Manager/`; add namespaces matching neighbors.
4. Rename level-scoped `GameManager` → `LevelGameManager`.
5. Introduce `EventSubscriberBehaviour` (see §3) and migrate the 73 singleton-event
   subscribers to it, starting with `SpawnController`, `ScoreManager`, `CameraShakeController`.
   (The formal `EventBus`/`GameEvents` buses are already balanced — no fix needed there.)

### Phase 1 — Currency correctness (high value)
6. Make `Shop/CurrencyManager` (Economy Service) the single source of truth; retire legacy
   `CoinManager`/`DiamondManager`/`GoldManager`; repoint `ShopPanelCurrenciesUI`. Fix the
   `Spend()` bug en route.
7. Transactional save path (await-and-confirm, not fire-and-forget) + conflict-resolution rule
   on `Refresh()`. One home for IAP / ads-removed ownership (cloud, not PlayerPrefs).

### Phase 2 — Pooling unification (mobile perf)
8. One pooling contract; route bird projectiles + powerup VFX through it; fold
   `BossBirds/PoolManager` in or give it a clear ownership boundary.

### Phase 3 — Decompose real god classes
9. `SpawnController` → phase manager / spawner / egg tracker / boss orchestrator / pressure
   scaler. Then `GameProgressManager`, `CannonInventoryView`, `UserProfileDataManager`.

### Phase 4 — Tame global state (largest, do last)
10. Composition root: register top hub singletons behind their interfaces via `ServiceLocator`;
    wire `ServiceLocator.Clear()` into scene teardown; migrate `.Instance` call sites
    incrementally. Adopt `GameLogger` as each file is touched (retire `Debug.Log` opportunistically).

## Explicitly out of scope
- Rewriting `PlayerInputAction` (auto-generated).
- Splitting `Egg` / `BaseCannon` (cohesive).
- Introducing a heavyweight DI framework or `.asmdef` split (CLAUDE.md warns against the latter).
