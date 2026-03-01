# Game Design Document (GDD)
## Bird Hunter – Event-Driven Architecture & Ball Blast Alignment

**Version:** 1.0  
**Last Updated:** February 2025  
**Reference:** Ball Blast (hypercasual cannon shooter)

---

## 1. Executive Summary

This GDD documents the event-driven score and damage architecture implemented for Bird Hunter. Each feature/module is compared against **Ball Blast** core mechanics, with alignment status and ideation flags indicating where design decisions or further iteration are needed.

---

## 2. Ball Blast Reference Mechanics

| Mechanic | Description |
|----------|-------------|
| **Numbered shapes** | Falling shapes show a number (HP). Hit reduces by 1. At 0, shape explodes. |
| **Split cascade** | Destroyed shape splits into 2 smaller shapes with half the number. Cascades until "1," then disappears. |
| **Cannon control** | Cannon at bottom; move left/right; aim and fire upward at shapes. |
| **Shapes descend** | Multiple shapes fall from top; random spawn positions; constant repositioning. |
| **Dodging** | Cannon must dodge falling shapes. Hit = game over. |
| **Coins** | Destroyed shapes drop coins. Cannon must move to collect before they disappear. |
| **Upgrades** | Coins buy cannon upgrades and new weapons. |

---

## 3. Feature & Module Comparison

### 3.1 IDamageable Interface

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Purpose** | Shared damage contract for eggs & birds | Shapes have numeric HP shown on surface | Aligned | None |
| **Contract** | `TakeDamage(int, Vector3)` with `CurrentHp`, `MaxHp`, `IsAlive` | HP decremented by 1 per hit | Partial | Consider per-hit vs per-damage: Ball Blast = 1 per hit; BH supports variable damage |
| **Hit point** | `Vector3 hitPoint` for FX at impact | N/A | Extra | None |

**Status:** Aligned. Bird Hunter extends Ball Blast by supporting variable damage and FX hooks.

---

### 3.2 Egg / EggHealth (Targets)

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Target type** | Eggs with HP; physics (bounce, walls) | Numbered shapes; fall straight | Partial | Ball Blast: no physics; BH: eggs bounce. Ideate: keep physics as differentiator or simplify? |
| **HP display** | HP stored internally; not shown | Number shown on shape | Misaligned | **Ideation:** Add HP/score text on eggs (e.g. E4→4, E3→3) for Ball Blast feel |
| **Per-hit damage** | Variable (e.g. cannon damage) | 1 per hit | Partial | Ideate: optional "1 HP per hit" mode for strict Ball Blast parity |
| **Split logic** | E4→2×E3, E3→2×E2, E2→2×E1, E1→gone | N→2×(N/2) until 1 | Aligned | Split cascade matches; tier-based (E1–E4) vs generic N |
| **Config** | `EggTierConfig`: baseHp, splitInto, splitCount, scoreOnDestroy | Implicit | Aligned | None |

**Status:** Core split cascade aligned. HP display and damage-per-hit need ideation for stronger Ball Blast alignment.

---

### 3.3 Bird / BirdHealth (Secondary Targets)

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Role** | Spawn eggs; can be shot | No birds; only shapes | Differentiator | BH uses birds as egg source. Ideate: optional "shape-only" mode without birds |
| **Movement** | Fly; lay eggs over play area | N/A | N/A | None |
| **Damage** | `BirdHealth` implements `IDamageable` | N/A | N/A | None |
| **Score** | `scoreOnDeath` on destroy | N/A | N/A | None |

**Status:** Unique to Bird Hunter. Birds add variety; Ball Blast has no equivalent.

---

### 3.4 GameEvents (Event Bus)

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **OnEggHit** | Fired on any egg hit | N/A | Supporting | None |
| **OnEggDestroyed** | Fired with score, position | Shape explode feedback | Aligned | None |
| **OnBirdHit / OnBirdDestroyed** | Same pattern for birds | N/A | Supporting | None |
| **OnCannonHit** | Optional combo/feedback | N/A | Supporting | **Ideation:** Use for combo multiplier (Ball Blast rewards rapid hits) |
| **OnLevelScoreUpdated** | Fired on score change | N/A | Supporting | None |

**Status:** Aligned. Event-driven architecture supports FX, SFX, UI, and future combo systems without changing core logic.

---

### 3.5 ScoreManager

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Score source** | Egg destroy (`scoreOnDestroy`), Bird destroy | Shape destroy (implicit points) | Aligned | None |
| **Per-hit score** | `scorePerHit` in config; not wired | Ball Blast often gives per-hit points | Misaligned | **Ideation:** Add per-hit scoring (Ball Blast-style) vs destroy-only |
| **Combo** | Not implemented | Rapid hits = combo | Misaligned | **Ideation:** Combo multiplier; decay timer; visual feedback |
| **Display** | `CurrentScore`, `LevelScore`; `OnLevelScoreUpdated` | Simple score display | Aligned | None |

**Status:** Destroy scoring aligned. Per-hit score and combo system need ideation.

---

### 3.6 Projectile / Bullet

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Travel** | Shoots upward; hits eggs/birds | Shoots at falling shapes | Aligned | None |
| **Bounce** | Bullet bounces to nearby targets | Balls typically don’t bounce | Differentiator | BH bounce is unique. Ideate: toggle for Ball Blast-style single-hit projectiles |
| **Damage** | Variable (cannon damage) | 1 per hit | Partial | See Egg/EggHealth |
| **Pierce** | Optional pierce chance | N/A | Extra | None |

**Status:** Core behavior aligned. Bounce and damage model are design choices vs Ball Blast.

---

### 3.7 SpawnController (Target Spawning)

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Source** | Birds spawn; lay eggs | Shapes spawn from top | Different | Ball Blast: shapes from top; BH: eggs from birds |
| **Spawn position** | Bird position when laying | Random top positions | Different | **Ideation:** Optional "shape rain" mode: spawn eggs from top without birds |
| **Spawn rate** | Adaptive (performance ratio) | Escalating difficulty | Partial | Both adapt; BH uses score vs expected. Aligned conceptually |
| **Pressure** | `PressureTracker` caps active eggs | Increasing density | Aligned | Both manage difficulty via target density |

**Status:** Spawn logic aligned in spirit; source (birds vs top) differs. Ideate optional Ball Blast-style top spawn.

---

### 3.8 Cannon / CannonFire

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Position** | Bottom of screen | Bottom | Aligned | None |
| **Movement** | Left/right | Left/right | Aligned | None |
| **Aim** | Fire direction | Fire at shapes | Aligned | None |
| **Dodging** | Not implemented | Core mechanic; hit = game over | Misaligned | **Ideation:** Add cannon health; eggs/birds hitting cannon = damage/lose; Ball Blast critical mechanic |

**Status:** Aim and position aligned. Dodging/losing condition needs ideation.

---

### 3.9 FX / SFX Controllers

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **HitFXController** | Subscribes to hit/destroy events | Hit/explode feedback | Aligned | None |
| **SFXController** | Subscribes to same events | Hit/explode sounds | Aligned | None |
| **ScoreUIController** | Subscribes to `OnLevelScoreUpdated` | Score pop/delta | Aligned | **Ideation:** Floating "+X" at destroy position (Ball Blast style) |

**Status:** Aligned. Event-driven setup allows easy polish.

---

### 3.10 Level / Progression

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Level structure** | Chapters, levels, `LevelProfile`, target score | Infinite / run-based | Different | BH: structured levels; Ball Blast: endless |
| **Win condition** | Reach target score | N/A (endless) | Different | None |
| **Progression** | `GameProgressManager`, slot machine, powerups | Coins → upgrades | Partial | Both have meta-progression; BH uses slot/powerups |

**Status:** BH uses structured levels; Ball Blast is endless. Both are valid; no change required unless an endless mode is added.

---

### 3.11 Economy / Collectibles

| Aspect | Bird Hunter | Ball Blast | Alignment | Ideation |
|--------|-------------|------------|-----------|----------|
| **Coins** | `Coin.cs`, `collectCoin` in legacy `LevelManager` | Shapes drop coins | Partial | Legacy only; not in new event flow |
| **Collection** | Cannon moves to collect | Cannon must move to collect | Aligned | **Ideation:** Wire coins to `GameEvents`; add `OnCoinDropped`, `OnCoinCollected` |
| **Upgrades** | Powerups from slot machine | Coins buy upgrades | Partial | BH: powerups; Ball Blast: coins → upgrades |

**Status:** Concept aligned; coin system needs ideation and event integration.

---

## 4. Alignment Summary Matrix

| Module | Aligned | Partial | Misaligned | Ideation Needed |
|--------|---------|---------|------------|-----------------|
| IDamageable | X | | | No |
| Egg/EggHealth | | X | | Yes (HP display, per-hit) |
| Bird/BirdHealth | | | | Yes (optional bird-less mode) |
| GameEvents | X | | | Yes (combo) |
| ScoreManager | | X | | Yes (per-hit, combo) |
| Projectile | | X | | Yes (bounce toggle) |
| SpawnController | | X | | Yes (top spawn mode) |
| Cannon | | X | | Yes (dodging) |
| FX/SFX | X | | | Yes (floating score) |
| Level/Progression | | | | No (different design) |
| Economy/Coins | | X | | Yes (event integration) |

---

## 5. Ideation Backlog (Prioritized)

### High Priority (Ball Blast Core)
1. **Cannon dodging / loss condition** – Eggs/birds hitting cannon = damage or game over.
2. **HP/score display on eggs** – Show number on egg (e.g. E4→4) for Ball Blast feel.
3. **Combo system** – Rapid hits = multiplier; visual/audio feedback.

### Medium Priority (Polish)
4. **Per-hit scoring** – Optional points per hit, not only on destroy.
5. **Coin events** – `OnCoinDropped`, `OnCoinCollected` in `GameEvents`.
6. **Floating score pop** – "+X" at destroy position.

### Low Priority (Modes / Variety)
7. **Bounce toggle** – Option for single-hit projectiles.
8. **Top spawn mode** – Eggs from top without birds.
9. **Shape-only mode** – No birds; pure shape destruction.

---

## 6. Architecture Diagram (Current)

```
[Projectiles] ──TryGetComponent<IDamageable>──► [EggHealth / BirdHealth]
                                                      │
                                                      ▼
                                               [GameEvents]
                                                      │
                    ┌─────────────────────────────────┼─────────────────────────────────┐
                    ▼                                 ▼                                 ▼
             [ScoreManager]                   [HitFXController]                  [SFXController]
                    │                                 │                                 │
                    ▼                                 ▼                                 ▼
             [OnLevelScoreUpdated]            [Spawn particles]                  [Play sounds]
                    │
                    ▼
             [ScoreUIController]
```

---

## 7. Appendix: File Reference

| Module | Files |
|--------|-------|
| IDamageable | `Assets/Scripts/Gameplay/Interfaces/IDamageable.cs` |
| GameEvents | `Assets/Scripts/Events/GameEvents.cs` |
| EggHealth | `Assets/Scripts/Gameplay/Health/EggHealth.cs` |
| BirdHealth | `Assets/Scripts/Gameplay/Health/BirdHealth.cs` |
| ScoreManager | `Assets/Scripts/Gameplay/Managers/ScoreManager.cs` |
| SpawnController | `Assets/Scripts/Gameplay/SpawnController.cs` |
| Egg | `Assets/Scripts/Gameplay/Eggs/Egg.cs` |
| EggTierConfig | `Assets/Scripts/Gameplay/Eggs/EggTierConfig.cs` |
| BaseBird | `Assets/Scripts/Gameplay/Birds/BaseBird.cs` |
| Bullet | `Assets/Scripts/Bullet.cs` |
| HitFXController | `Assets/Scripts/FX/HitFXController.cs` |
| SFXController | `Assets/Scripts/Audio/SFXController.cs` |
| ScoreUIController | `Assets/Scripts/UI/ScoreUIController.cs` |
