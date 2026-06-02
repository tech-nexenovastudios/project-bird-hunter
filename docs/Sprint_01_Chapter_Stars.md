# Sprint 01 — Chapter Stars, Milestone Chests & Stats Panel

**Goal:** ship per-level **star ratings**, **milestone-chest grants**, and a **chapter stats panel** (celebration + carousel card) that shows stars, completion %, high score, and the next unlock.

**Build against:** `Chapter_Progression_Architecture.md` (the "§" references below point there). Design intent: `Chapter_Unlock_Panel_Design.md`.

**Definition of done (the whole sprint):**
- Clearing a level awards 0–3 stars, persisted and surviving an app restart.
- Stars never go down on replay.
- Hitting 20 / 40 / 60 chapter stars grants a Wooden / Silver / Gold chest (stub) exactly once.
- Both panel surfaces show `★ X / 60`, completion %, high score, and the next-chapter unlock teaser.
- No new compile warnings/errors; no leaked event subscriptions.

---

## How to read this doc

Each task is independently testable. Do them **in order** — each builds on the last. Every task has: **Why · Files · Steps · ✅ Done when**. Sizes: **S** ≈ <2h, **M** ≈ half-day.

> Before you start: read `Chapter_Progression_Architecture.md` §1 (the picture) and §6 (hazards — especially the 1-based vs 0-based chapter indexing). The combat log already records `endHp` and `time` per level — you do **not** need to add that.

---

## Task board

| # | Task | Size | Depends on |
|---|---|---|---|
| T1 | Extend the save data | S | — |
| T2 | Broadcast level stats | S | T1 |
| T3 | Star config + scoring service | M | T2 |
| T4 | Milestone chest grant (stub) | S | T3 |
| T5 | Chapter stats read-model | S | T1 |
| T6 | Panel A — celebration | M | T5 |
| T7 | Panel B — carousel card | M | T5 |
| T8 | Playtest & verify | S | all |

---

## T1 — Extend the save data  ·  S

**Why:** stars and claimed-chest flags need a home in the persisted progress (§3.1).

**Files:** `Assets/Scripts/Gameplay/Managers/GameProgress.cs`

**Steps:**
1. In `class ChapterProgress` (lines 10–18) add:
   ```csharp
   public int[]  levelStars = new int[20];            // index = level-1, value 0..3
   public bool[] milestoneChestsClaimed = new bool[3]; // [Wooden, Silver, Gold]
   ```
2. Add a `Normalize()` method on `ChapterProgress` that ensures `levelStars` is non-null and length 20, and `milestoneChestsClaimed` is non-null and length 3 (create/resize if not — old saves won't have them).
3. Find where `GameProgress` is loaded/deserialized in `GameProgressManager` (the load path that calls `JsonUtility`/`FromJson`). After load, loop `chapters` and call `Normalize()` on each.

**✅ Done when:** a save created before this change loads without error and every chapter ends up with a 20-long `levelStars` and 3-long `milestoneChestsClaimed`. (Quick check: log `chapters[0].levelStars.Length` on boot.)

---

## T2 — Broadcast level stats  ·  S

**Why:** the scoring class already measures score/end-HP/hits/time for the log; publish them so the star service can consume them (§3.2). No new tracking.

**Files:** `Assets/Scripts/Gameplay/Events/GameEvents.cs`, `Assets/Scripts/Gameplay/Managers/ScoreManager.cs`

**Steps:**
1. In `GameEvents.cs` add the `LevelStats` struct, `OnLevelStatsFinalized` event, and `FireLevelStatsFinalized(...)` exactly as in §3.2.
2. In `ScoreManager.OnLevelCompleted` (the method that already calls `CombatLog.Summary(...)`), right after the log call, build a `LevelStats` from the **same values** (`_currentLevelIndex`, `finalScore`, `endHpPct`, `_hitsTakenThisLevel`, `_levelElapsed`) and call `GameEvents.FireLevelStatsFinalized(stats)`.

**✅ Done when:** a temporary `Debug.Log` in a test subscriber prints the correct score/endHp/time when a level completes.

---

## T3 — Star config + scoring service  ·  M

**Why:** turn stats into stars and persist them (§3.3–3.5). This is the core feature.

**Files (new):** `StarRatingConfig.cs` + `Assets/Resources/StarRatingConfig.asset`, `StarRatingService.cs`
**Files (edit):** `GameProgressManager.cs`

**Steps:**
1. Create `StarRatingConfig` ScriptableObject (§3.3): `surviveHealthPct = 0.5`, `defaultParSeconds = 30`, optional `parOverrides`. Create the asset in `Assets/Resources/` so it can be `Resources.Load`ed.
2. Add `GameProgressManager.RecordLevelStars(chapter, level, stars)` exactly as §3.5 (high-water-mark + `SaveProgress` + fire `OnChapterStarsChanged`). Also add the `FireChapterStarsChanged(int chapter)` event in `GameEvents.cs`.
3. Create `StarRatingService` (MonoBehaviour, in GamePlayScene, extends `EventSubscriberBehaviour` — §6.5). On enable, subscribe to `OnLevelStatsFinalized`. In the handler:
   - `★1` always (the level was cleared).
   - `+★` if `endHpPercent >= surviveHealthPct * 100`.
   - `+★` if `timeSeconds <= ParFor(chapter, level)` (use override else `defaultParSeconds`).
   - read `chapter`/`level` from `GameProgressManager.Instance.Data` (`currentChapter` / `currentLevel`). **Mind 1-based indexing (§6.1).**
   - call `RecordLevelStars(chapter, level, stars)`.
4. Add the `StarRatingService` component to the gameplay scene (next to `ScoreManager`), assign the config.

**✅ Done when:** completing a level with full health and a fast clear stores 3 stars; a slow, damaged clear stores 1–2. Re-clearing worse keeps the higher value. Restart the app → stars persist.

---

## T4 — Milestone chest grant (stub)  ·  S

**Why:** reward stars with chests, through a boundary we can swap for the real chest system later (§3.6).

**Files (new):** `IChestGranter.cs` (+ `ChestTier` enum), `LoggingChestGranter.cs`
**Files (edit):** `StarRatingService.cs`

**Steps:**
1. Add `enum ChestTier { Wooden, Silver, Gold }` and `interface IChestGranter { void Grant(ChestTier tier, string reason); }`.
2. `LoggingChestGranter` (stub): log via `GameLogger`, and award placeholder gems via `CurrencyManager.Instance.AddGems(...)` so nothing is dropped. Add a `// TODO: replace with real chest inventory when chest system lands`.
3. In `StarRatingService`, after `RecordLevelStars`, run the milestone check: sum the chapter's `levelStars`; thresholds **20 / 40 / 60** → Wooden / Silver / Gold. For each unclaimed milestone now met, set `milestoneChestsClaimed[i] = true`, `SaveProgress()`, and call `_chestGranter.Grant(tier, "chapter_star_milestone")`.

**✅ Done when:** reaching 20 stars in a chapter grants exactly one Wooden chest (check the log), and re-completing levels does **not** re-grant it.

---

## T5 — Chapter stats read-model  ·  S

**Why:** one place computes everything the panels show, so the two surfaces can't disagree (§3.7).

**Files (new):** `Assets/Scripts/Chapters/ChapterStatsViewModel.cs`

**Steps:**
1. Implement `ChapterStatsVM` struct + `ChapterStatsViewModel.Build(int chapter1Based)` per §3.7.
2. Inside `Build`, read:
   - stars = sum of `levelStars`; maxStars = `3 * 20`.
   - clearedLevels = `firstClearedLevels.Count`; completionPct = cleared / total.
   - highScore from `ChapterProgress.highScore`.
   - isUnlocked from `ChapterUnlockManager.Instance.IsUnlocked(chapter1Based - 1)` — **0-based! (§6.1)**.
   - `NextUnlockLabel` from the unlock map in `Cannon_Powerup_Unlock_Plan.md §1.2` (hardcode a small lookup for now; tag with a `// TODO: data-drive`).

**✅ Done when:** a unit-style test or a temporary debug button prints correct `★ X/60`, completion %, and high score for a known chapter.

---

## T6 — Panel A: celebration  ·  M

**Why:** show the retrospective in the existing post-chapter moment (panel doc §2A).

**Files:** `Assets/Scripts/Chapters/ChapterVisualHandler.cs` (+ the `chapterUnlockPanel` prefab/UI)

**Steps:**
1. Add UI fields to the unlock panel: star bar (`★ X / 60`), completion ring/bar, high-score text, "Unlocks: ___" teaser.
2. When the panel shows (the existing countdown beat), call `ChapterStatsViewModel.Build(justFinishedChapter)` and bind the result.
3. Subscribe to `GameEvents.OnChapterStarsChanged` to refresh while visible; unsubscribe on hide/destroy (§6.5).

**✅ Done when:** finishing a chapter shows the panel populated with that chapter's real stars/completion/high-score and the next unlock.

---

## T7 — Panel B: carousel card  ·  M

**Why:** the re-openable browse view (panel doc §2B), sharing the same widgets/VM as T6.

**Files:** `Assets/Scripts/UI/MaInMenu/ChapterScrollItem.cs` (+ a new tap-to-open card panel)

**Steps:**
1. Add a card panel that opens when a chapter in the carousel is tapped.
2. Bind it from `ChapterStatsViewModel.Build(tappedChapter)` — reuse the same widget prefab as T6 where possible.
3. Locked chapters: show the dim/locked state + requirement ribbon (panel doc §8).

**✅ Done when:** tapping any chapter opens a card with correct stats; locked chapters read as locked.

---

## T8 — Playtest & verify  ·  S

**Steps:**
1. Play a chapter to completion. Open `GameLogs/combat.log` → confirm `SUMMARY` lines carry `endHp=` and `time=`, and that the stars stored match those numbers vs the config thresholds.
2. Restart the app → confirm stars and claimed-chest flags persisted.
3. Force a milestone (e.g. temporarily lower a threshold) → confirm one grant, logged, not repeated.
4. Open both panels → confirm identical numbers for the same chapter.
5. Check the Unity console: zero new errors/warnings.

**✅ Done when:** all five pass and the sprint Definition of Done holds.

---

## Out of scope (do NOT build this sprint)
- Chest **opening / inventory / upgrades** — separate system (`Powerup_Chest_Collection_Design.md`). We only grant via the stub.
- Per-chapter coin tracking (panel doc Q4).
- Accuracy / weak-point stars (waits on the cannon-niche mechanic).
- Tuning the ⚙️ star thresholds to final values — ships with strawman; designer tunes `StarRatingConfig.asset` after the next telemetry capture.

## If you get stuck
- Indexing off-by-one → re-read §6.1 and the comment at `GameProgressManager.cs:257`.
- Stars not persisting → confirm `SaveProgress()` runs (T3 step 2) and `Normalize()` ran on load (T1).
- Stat values wrong/zero → check T2 fires after `CombatLog.Summary`, using the same locals.
