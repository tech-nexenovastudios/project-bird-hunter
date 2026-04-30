# Bird Hunter — Progression, Reward & Economy

**Version:** 1.0  
**Last Updated:** 2026-04-28  
**Scope:** How a player's session-time hours convert into upgrades, unlocks, and the next chapter — and where the seams in that conversion are weakest.

This doc maps the *runtime* hooks (events, managers, configs) so a designer or new engineer can see why a coin shows up on screen and how that same coin ends up funding a cannon upgrade weeks later. It calls out where the system holds together and where it doesn't.

---

## 1. The five surfaces

| Surface | Purpose | Source of truth |
|---|---|---|
| **Progression** | Per-chapter level loop, slot-machine spins, replay difficulty. | `GameProgressManager` + `ChapterProgressionConfig` |
| **Per-level rewards** | Coins/gems dropped during gameplay, and completion bonus + power refund + first-clear gem. | `RewardManager` + `GameplayRewardConfig` (or formula version: `EconomyFormulaConfig`) |
| **Cannon economy** | Cannon unlock + 9-tier upgrade curve. Formulas are global; per-cannon multipliers tier the cost. | `EconomyFormulaConfig` + `CannonStatsRepository` (cloud) + `CannonDatabase` (local visuals) |
| **Powerup economy** | Slot-machine driven; not bought with currency. Unlocked by chapter progression. | `PowerupConfig.unlockFromChapter` / `spinUnlockLevel` + per-spin pool builder |
| **Persistence** | `GameProgress` save file + cloud (`UserDataRepository` → `CloudDatabase` + `CloudSaveManager`) | Local: `Application.persistentDataPath/birdhunter_progress.dat`; Cloud: Unity Cloud Save under custom IDs |

---

## 2. End-to-end hook diagram

```
                      ┌────────────────┐
                      │  Egg/Bird HP   │
                      │ (EggHealth /   │
                      │  BirdHealth)   │
                      └─────┬──────────┘
                            │ TakeDamage
                            ▼
              GameEvents.FireEggHit / FireEggDestroyed
                            │
       ┌────────────────────┼─────────────────────┬──────────────┐
       ▼                    ▼                     ▼              ▼
ScoreManager        RewardManager          ComboController   FX/SFX hooks
   (LevelScore)     (coins/gems            (multiplier)
       │             per drop)
       │ FireLevelScoreUpdated
       ▼
LevelCompletionController.OnScoreUpdated
       │ when LevelScore >= TargetScore
       ▼
SpawnController.StartDrain
       │ wait min duration
       ▼
[grace 10s — bird/egg spawn paused]
       │
   ┌───┴───┐
player    grace
clears    expires
   │       │ KillRemainingEggsViaDamage  ← sets _isForceDestroying
   │       │ (eggs take fatal dmg →
   │       │  RewardManager scales by
   │       │  forceDestroyRewardMultiplier)
   ▼       ▼
GameEvents.FireAllEggsCleared
       │
       ▼
LevelCompletionController → GameManager.CompleteCurrentLevel(score)
       │
       ▼
GameProgressManager.CompleteLevel
       │  level++ ; if 5/10/15/20 trigger spin ; persist
       ▼
GameEvents.FireLevelCompleted(score)
       │
       ▼
RewardManager.HandleLevelCompleted
       │  completion coins (perf-scaled, force-destroy multiplier applied)
       │  first-clear gems (one-time; suppressed on force-destroy)
       │  power refund (random; suppressed on force-destroy)
       ▼
CurrencyManager.AddGold/AddGems/AddPower (UGS Economy)
       │
       ▼
HUD currency UI updates via OnPlayerCoinsUpdated (per RewardManager wiring)
```

The **two event buses** are real:
- `EventBus<T>` — boot/service-layer struct events (`AuthProgressEvent`, `DataLoadProgressEvent`).
- `GameEvents` — gameplay `Action<…>` delegates. Everything in the diagram above is `GameEvents`.

Don't conflate them.

---

## 3. Per-chapter economy slice (numbers)

`GameplayRewardConfig` is what the build runs against today (the asset is at `Assets/Resources/Data/New Gameplay Reward Config.asset`). It bins chapters into Early / Mid / Late.

| Phase | Chapters | Coin drops/egg | Gem drop chance | Gem amount | Completion base | Completion perf bonus | First-clear gems |
|---|---|---|---|---|---|---|---|
| Early | 1–10 | 30–80 | 3% | 1 | 550 | 50–100 | 10 |
| Mid | 11–20 | 100–200 | 5% | 1–2 | 1,320 | 120–240 | 10 |
| Late | 21–30 | 250–400 | 8% | 2–3 | 2,750 | 250–500 | 10 |

**Power refund** (a separate energy resource): 25% chance per completion → +5 power.

### Expected coin earn per chapter (ballpark)

Assuming ~15 destroy-drop events per level (eggs + birds) and target-perfect performance:

| Phase | Coins/level (drops + completion) | Coins/chapter (× 20 levels) |
|---|---|---|
| Early | ~825 + ~625 = **~1,450** | **~29,000** |
| Mid | ~2,250 + ~1,500 = **~3,750** | **~75,000** |
| Late | ~4,875 + ~3,125 = **~8,000** | **~160,000** |

Numbers are coarse but useful for sizing unlocks below.

---

## 4. Why the system works (when it works)

1. **Single completion event, single reward sink.**  
   Every level (regular or boss) ends through one path: `FireAllEggsCleared` → `LevelCompletionController` → `GameProgressManager.CompleteLevel` → `RewardManager.HandleLevelCompleted`. New reward types only need one subscriber — they don't need to know which path got the player here (player-cleared, force-destroyed, boss-defeated).

2. **Performance ratio is the only knob the player controls.**  
   `performanceRatio = LevelScore / TargetScore`, clamped to `[0, 1]`, is the input to the completion-bonus lerp. It isn't gamed by quitting/retrying because `RegisterLevelAttempt` increments before the level starts and `replayHpStep`/`replayPressureStep` make replays *harder*, not easier. Speedrunning is rewarded; rage-retrying isn't.

3. **Currency is wired through UGS Economy, not a local int.**  
   `CurrencyManager.AddGold/AddGems/AddPower` writes to Unity Economy and fires `OnCurrencyChanged` real-time. UI (`CoinFlowManager`) animates on that event. The save file does *not* duplicate balances — only progression — so a desync at save time can't double-spend.

4. **Spin economy is gated by progression, not paid in currency.**  
   Mid-chapter spins fire after L5/L10/L15 and chapter-rollover spin after L20. Players never *buy* powerups — they just unlock pool entries by reaching `unlockFromChapter`, then roll for them. This sidesteps the gacha-balancing hell of selling powerups directly, but means the only economy lever for powerups is *breadth* of options, not depth.

5. **Replay-difficulty bump uses session-only counters.**  
   Per-attempt difficulty bumps (`replayMaxBumps` cap) reset on app restart — players who rage-quit and come back tomorrow get a fresh roll. Important for retention; would be cruel otherwise.

6. **Grace-time penalty closes the "ghost win" loophole.**  
   Force-destroy on grace expiry reduces in-level coin/gem drops by `forceDestroyRewardMultiplier` (default 0.5) and suppresses both the first-clear gem bonus and the power refund. So grace is a real safety net; it isn't a no-cost shortcut.

---

## 5. Flaws and risks

These are the spots where this system will break under load. Listed roughly worst-first.

### 5.1 Two parallel reward configs — drift hazard

There are two independent reward formulas in the repo:

- **`GameplayRewardConfig`** (ScriptableObject, asset-driven, `Assets/Resources/Data/New Gameplay Reward Config.asset`) — bin-based: Early/Mid/Late.
- **`EconomyFormulaConfig`** (ScriptableObject, formula-based, `Assets/Resources/Data/EconomyFormulaConfig.asset`) — continuous chapter formulas.

`RewardManager` uses **only the bin-based one**. `EconomyFormulaConfig` is referenced by the cannon unlock/upgrade pipeline (`GetCannonUnlockCoins`, `GetUpgradeCostCoins`). The two configs do not cross-validate. If a designer tweaks Early-phase coin drops to 60–120 in `GameplayRewardConfig` but doesn't update the Early curve in `EconomyFormulaConfig`, the cannon-unlock cost curve stays calibrated against the old payout.

**Mitigation:** pick one. Either delete `EconomyFormulaConfig.GetCoinsDropMin/Max` etc. and have it consume `GameplayRewardConfig` for payout, or move `RewardManager` over to `EconomyFormulaConfig`. The bin-based one is shippable today; the formula version is more designer-friendly long-term.

### 5.2 Cannon stats live in cloud, configs live locally

`CannonStatsRepository.LoadAsync()` pulls `cannon_stats` JSON from Cloud Save. `EconomyFormulaConfig` and `CannonDatabase` (visuals/prefabs) are local. If the cloud key is missing or the dashboard hasn't been updated with a new cannon, the cannon ships with no stats and no error path — `LoadAsync` logs and returns. The local `cannonId` (e.g. `CANNON_03`) silently mismatches the dashboard sub-key (e.g. `LuckyCannon`).

**Mitigation:** add a startup validator that diffs `CannonDatabase.entries[].cannonKey` against `CannonStatsRepository.All` and against `EconomyFormulaConfig.cannonMultipliers[].cannonId`. Fail boot in editor if any of the three lists disagree.

### 5.3 `CANNON_01` has multiplier 0 → upgrades are free

`EconomyFormulaConfig.cannonMultipliers[0]` has `coinMultiplier: 0` and `matMultiplier: 0`. Used as-is, this makes Single Shot's 9-tier upgrade curve cost **zero** coins and zero materials. The intent was probably "unlock cost is 0 because you start with it" — but the same multiplier feeds `GetUpgradeCostCoins`.

**Mitigation:** split into two arrays (`unlockMultiplier`, `upgradeMultiplier`) or hard-code starter unlock = 0 and force `upgradeMultiplier ≥ 1`.

### 5.4 Chapter configs say `totalLevels: 20` but boss flow assumes 10

The retreat path in `SpawnController.HandleBossRetreated` parks the boss with `_hasBossWaitingForLevel20 = true`, and `TrySpawnBoss` re-spawns it when `_globalLevel % 20 == 0`. Combined with the new fix in `ResolveChapterLevel` that flags both L10 and L20 as boss levels, the design is now: mid-boss at chapter L10, rematch at chapter L20. This works for chapters with 20 levels. It will silently fail for any chapter shorter than 20 — the L10 mid-boss will fire, retreat, and the rematch never comes because the chapter ends before L20.

**Mitigation:** when authoring chapters with non-20 lengths, gate the L10 mid-boss on `n >= 20`, or move both boss-level indices into `ChapterProgressionConfig` instead of hard-coding 10/20.

### 5.5 Save file is binary `.dat`, single file, no schema version

`GameProgressManager` uses `BinaryFormatter` against a private `[Serializable] SaveData` class. There is no version field, no migration path, and `BinaryFormatter` is officially obsolete in newer .NET versions and a security risk to deserialize untrusted bytes. A field rename in `SaveData` will brick every existing player save.

**Mitigation:** migrate to JSON (Newtonsoft is already in the project — see `CannonStatsRepository`). Add a `version` field. Keep a one-shot `BinaryFormatter` reader behind `version == 0` so existing saves migrate.

### 5.6 In-level rewards are tied to chapter only, not global level

Both `GameplayRewardConfig` and `EconomyFormulaConfig` ramp drops by chapter. They do *not* ramp by *level within chapter*. So Ch1-L1 and Ch1-L20 pay the same per egg, even though L20 has the boss and ~3× the bird density. Players will feel L20 of any given chapter as a "pay flat, work harder" levels.

**Mitigation:** add a `levelInChapter` factor to `GameplayRewardConfig.GetRandomCoinDrop`, mirroring what `EconomyFormulaConfig.GetCoinsDropMin` already does (`lf = 1 + 0.10 * ((levelInChapter - 1) / 19)`). Cheap fix, big perceived-fairness win.

### 5.7 First-clear gems are flat 10 across all 30 chapters

`gemsOnFirstTimeClear = 10`. Late-chapter players who break a Ch28-L17 wall feel the same drip-feed reward as a Ch1-L2 first-timer. `EconomyFormulaConfig.GetFirstClearGems` already has a ramp + milestone/boss bonus — it's just unused.

**Mitigation:** wire `RewardManager.HandleLevelCompleted` to call `EconomyFormulaConfig.GetFirstClearGems(chapter, levelInChapter, isMilestone, isBoss)` instead of the flat 10.

### 5.8 Force-destroy "performance ratio" can still hit 1.0

`RewardManager.HandleLevelCompleted` reads `finalScore / TargetScore`. When grace expires and we force-destroy, the destroyed eggs *still award score* (via `EggHealth.Die → FireEggDestroyed`). So the player can grace-out and still hit 100% performance, getting the full `performanceBonusMax`. The new force-destroy multiplier covers the *base* completion coins but the perf bonus rides through unscaled.

**Mitigation:** trivial — multiplier already applied at the call site; this is by design as written. If we want grace to never give max-perf bonus, cap `performanceRatio` at 0.6 when `forceDestroyed == true`.

### 5.9 Powerup unlock fields are doubled (chapter + spin level)

`PowerupConfig` exposes `unlockFromChapter` *and* `spinUnlockLevel`. The spin-level field's tooltip says "Unlocks at chapter level 0, 6, 11, 16" — which suggests it gates *which spin* (L0/5/10/15 + 5 = 1/6/11/16 1-based) the powerup can show in. Combined with `unlockFromChapter`, the actual rule is "unlocked once you've reached *both* the chapter and the within-chapter spin slot."  This is fine as a design but undocumented in code; the field names are easy to confuse.

**Mitigation:** rename to `firstSpinSlot` (with values 0/1/2/3 instead of 0/6/11/16) and `firstChapter`. Or document the existing fields' joint semantics in the `PowerupConfig` header.

### 5.10 Currency, save, and analytics don't share an idempotency key

If `RewardManager.AwardCoins` fires, `CurrencyManager.AddGold(amount).Forget()` runs without awaiting — and on a flaky network, the UGS Economy call may silently retry or drop. There's no client-side "I already credited this level's bonus" idempotency token. Two cases worth bracing for: (a) duplicate awards on retry storms, (b) lost awards when an already-pending request fails after an app suspend.

**Mitigation:** generate `levelKey = $"{chapter}-{levelInChapter}-{attemptId}"` per level start, and when awarding, write `Economy.AddGold(amount, idempotencyKey: levelKey + "_completion")`. UGS Economy supports request keys; we just don't use them.

---

## 6. Designer cheat sheet — dials worth touching first

| Dial | Where | Why touch it |
|---|---|---|
| `forceDestroyRewardMultiplier` | `GameplayRewardConfig` | Tune grace penalty (0.5 default; lower = harsher). |
| `gemsOnFirstTimeClear` | `GameplayRewardConfig` | Replace with `EconomyFormulaConfig.GetFirstClearGems` for a chapter-scaled ramp. |
| `replayMaxBumps`, `replayHpStep`, `replayPressureStep` | `ChapterProgressionConfig` | Aggressiveness of replay difficulty. |
| `minDurationStart/End` | `ChapterProgressionConfig` | Min seconds before grace can begin. Combined with the new 10s grace, sets the "earliest possible completion time" floor. |
| `targetScoreMin/Max` | `ChapterProgressionConfig` | Geometric ramp across the chapter. |
| `cannonMultipliers[]` | `EconomyFormulaConfig` | Per-cannon coin/material costs. Edit here, not in cannon JSON. |
| `coinsPerGem = 110f` | `EconomyFormulaConfig.GetCannonUnlockGems` | Hard→soft conversion ratio for paying with gems instead of coins. |
| Spin slot gates | `PowerupConfig.spinUnlockLevel` | Which spin slot a powerup first becomes available at. |

---

## 7. Open questions for design / production

- **Save versioning**: do we ship 1.0 with binary saves and migrate later, or eat the migration cost now?
- **Cannon stats authority**: is cloud the right place for base stats, or should they ship in a SO and only *upgrades* live cloud-side? (The current split makes a "cannon balance hotfix without app update" possible — is that a feature or a footgun?)
- **Powerup pricing**: are we sure powerups should remain spin-only? Direct purchase via gems (with a hard wall on Legendary) is a common Ball-Blast-likes pattern and would open a spend sink that we don't currently have.
- **Soft currency cap**: there's no max coin stash. A Ch28 player has effectively-infinite coins relative to upgrade costs (see §5.1). Add a meaningful sink — guild buy-in, daily reroll cost, decoration purchases.

---

*Maintain this doc when:*
- A new event lands on the level-completion path.
- `GameplayRewardConfig` or `EconomyFormulaConfig` changes shape.
- The save schema migrates.
- A flaw in §5 gets resolved (delete the entry, don't strike-through).
