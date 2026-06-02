# Bird Hunter — Chapter Progression & Rewards Architecture

**Version:** 1.0
**Last Updated:** 2026-06-02
**Status:** 🟢 Ratified target architecture for the stars + milestone-chest + chapter-stats-panel slice. Build against this; the sprint plan (`Sprint_01_Chapter_Stars.md`) implements it task-by-task.

**Scope:** the *architecture* for three linked features — per-level **star ratings**, **milestone chests**, and the **chapter stats panel** (`Chapter_Unlock_Panel_Design.md`). It defines the components, their contracts, and the data flow. It does **not** restate the broad codebase refactor — see `Architecture_Improvement_Plan.md` for that; we only borrow its hygiene rules (§6 here).

---

## 1. The one-paragraph picture

A level ends → **`ScoreManager`** (which already tracks score, end-HP%, hits, time for the combat log) broadcasts those numbers as a single **`LevelStats`** event → a small **`StarRatingService`** turns them into **0–3 stars** using a tunable **`StarRatingConfig`**, writes the stars into **`ChapterProgress`** (high-water-mark) via **`GameProgressManager`**, then checks whether the chapter crossed a **milestone** and grants a chest through a swappable **`IChestGranter`**. The **chapter stats panel** never computes anything itself — it asks a read-only **`ChapterStatsViewModel`** for "stars X/Y, completion %, high score, next unlock" and renders it, in both the post-chapter celebration and the carousel browse card.

```
 level ends
     │
 ScoreManager ──fires──▶ GameEvents.OnLevelStatsFinalized(LevelStats)
                                   │
                          StarRatingService
                            │            │
              (compute stars via         (on milestone:
               StarRatingConfig)          IChestGranter.Grant)
                            │
        GameProgressManager.RecordLevelStars()  ──▶ ChapterProgress.levelStars (high-water-mark) + SaveProgress
                            │
                  GameEvents.OnChapterStarsChanged
                            │
          ChapterStatsViewModel.Build(chapter)  ◀── panel surfaces read this
              (celebration panel + carousel card)
```

**Design rule:** each box does exactly one job — *source* (ScoreManager) → *logic* (StarRatingService) → *persistence* (GameProgressManager) → *read model* (ViewModel) → *view* (panels). A junior dev should be able to change any one box without touching the others.

---

## 2. Why a new service instead of cramming it into ScoreManager

`ScoreManager` and `SpawnController` are already flagged as growing classes (`Architecture_Improvement_Plan.md §5`). Star logic + reward logic do **not** belong in the scoring class. So:

- `ScoreManager` only gains **one line of new responsibility**: broadcast the stats it already computes.
- All new *logic* lives in `StarRatingService` (one small, single-purpose MonoBehaviour in the gameplay scene, same pattern as `ScoreManager`).

This keeps the change small, testable, and easy to hand off.

---

## 3. Components & contracts

### 3.1 Data model — extend `ChapterProgress`
**File:** `Assets/Scripts/Gameplay/Managers/GameProgress.cs` (the `[Serializable] class ChapterProgress`, lines 10–18).

Add two fields:

```csharp
public int[] levelStars = new int[20];        // best stars per level, index = level-1 (0..19), values 0..3
public bool[] milestoneChestsClaimed = new bool[3]; // [Wooden, Silver, Gold] — prevents double-grant
```

- 20 = levels per chapter (`ChapterProgressionConfig.totalLevels`). Fixed game-wide.
- `JsonUtility` serializes `int[]` / `bool[]` fine. **Migration:** old saves lack these fields → after load, call a `Normalize()` that ensures both arrays are non-null and sized 20 / 3 (resize/pad if needed). One method, called once on load.
- **High-water-mark rule:** `levelStars[i] = Max(existing, earned)`. Re-clearing a level can only raise stars, never lower. (Mirrors `firstClearedLevels` semantics.)

### 3.2 `LevelStats` + the broadcast event
**File:** `Assets/Scripts/Gameplay/Events/GameEvents.cs` (new struct + event, same file as other gameplay events).

```csharp
public struct LevelStats
{
    public int   level;        // 1-based level within the chapter
    public int   score;
    public int   endHpPercent; // cannon health % at clear (-1 if unknown)
    public int   hitsTaken;
    public float timeSeconds;  // active-combat clear time
}
public static event Action<LevelStats> OnLevelStatsFinalized;
public static void FireLevelStatsFinalized(LevelStats s) => OnLevelStatsFinalized?.Invoke(s);
```

**Fired by `ScoreManager`** in its existing `OnLevelCompleted` handler — it already computes every one of these for `CombatLog.Summary(...)` (§3.6 of the panel doc). Fire the event with the *same values* right after logging. **No new tracking is needed; we just publish what's already measured.**

### 3.3 `StarRatingConfig` — the tunable thresholds
**File:** `Assets/Resources/StarRatingConfig.asset` (new ScriptableObject; `Assets/Scripts/.../StarRatingConfig.cs`).

```csharp
[CreateAssetMenu(menuName = "BirdHunter/Star Rating Config")]
public class StarRatingConfig : ScriptableObject
{
    [Range(0f,1f)] public float surviveHealthPct = 0.5f;  // ★2: end-HP ≥ this
    public float defaultParSeconds = 30f;                  // ★3: clear time ≤ par
    public List<LevelPar> parOverrides = new();            // optional per (chapter,level) override
}
[Serializable] public struct LevelPar { public int chapter; public int level; public float parSeconds; }
```

⚙️ These are **strawman values** — the panel doc §3.6 explains the next telemetry capture (now logging `endHp` + `time`) will set real numbers. Ship with strawman; designer tunes the asset, no code change.

### 3.4 `StarRatingService` — the logic
**File:** `Assets/Scripts/Gameplay/Managers/StarRatingService.cs` (new; MonoBehaviour in GamePlayScene).

Responsibilities (and *only* these):
1. Subscribe to `GameEvents.OnLevelStatsFinalized`.
2. Compute stars: `★1 = always (clear)`, `★2 = endHpPercent ≥ surviveHealthPct×100`, `★3 = timeSeconds ≤ par(chapter,level)`. (Score is **not** used — see panel doc §3.5.)
3. Call `GameProgressManager.Instance.RecordLevelStars(chapter, level, stars)`.
4. Run the milestone check (§3.6) and grant via `IChestGranter`.

Keep it under ~120 lines. Use `EventSubscriberBehaviour` (§6) so the subscription can't leak.

### 3.5 Persistence — `GameProgressManager.RecordLevelStars`
**File:** `Assets/Scripts/Gameplay/Managers/GameProgressManager.cs` (new public method).

```csharp
public int RecordLevelStars(int chapter, int level, int stars) // returns the stored (high-water) value
{
    var chap = _progress.GetOrCreateChapter(chapter);
    int idx = level - 1;
    if (idx < 0 || idx >= chap.levelStars.Length) return 0;
    chap.levelStars[idx] = Mathf.Max(chap.levelStars[idx], Mathf.Clamp(stars, 0, 3));
    SaveProgress();
    GameEvents.FireChapterStarsChanged(chapter); // panel refresh signal
    return chap.levelStars[idx];
}
```

`GameProgressManager` stays the single owner of writes + save (it already owns `CompleteLevel`). Do **not** write `levelStars` from anywhere else.

### 3.6 Reward boundary — `IChestGranter` (swappable)
**The chest system does not exist yet** (`Powerup_Chest_Collection_Design.md` is design-only). So milestone rewards go through a thin interface, and we ship a stub:

```csharp
public enum ChestTier { Wooden, Silver, Gold }
public interface IChestGranter { void Grant(ChestTier tier, string reason); }
```

- **Now (stub):** `LoggingChestGranter` — logs the grant via `GameLogger` and (optionally) awards placeholder gems via `CurrencyManager.Instance.AddGems`. No reward is silently dropped.
- **Later:** when the chest collection lands, replace the binding with a real granter that adds to the chest inventory. **`StarRatingService` does not change** — only which `IChestGranter` it holds.

**Milestone thresholds** (auto-scale to any level count; here max = 3×20 = 60):

| Milestone | Stars in chapter ≥ | Tier | Claimed flag |
|---|---|---|---|
| ⅓ | 20 | Wooden | `milestoneChestsClaimed[0]` |
| ⅔ | 40 | Silver | `milestoneChestsClaimed[1]` |
| Full | 60 | Gold | `milestoneChestsClaimed[2]` |

After recording stars, `StarRatingService` sums the chapter's stars, and for each unclaimed milestone now met: set the flag, save, `IChestGranter.Grant(tier, ...)`.

### 3.7 Read model — `ChapterStatsViewModel`
**File:** `Assets/Scripts/Chapters/ChapterStatsViewModel.cs` (new; plain C#, no MonoBehaviour).

```csharp
public readonly struct ChapterStatsVM
{
    public int Stars, MaxStars, ClearedLevels, TotalLevels, HighScore;
    public float CompletionPct;        // ClearedLevels / TotalLevels
    public bool IsUnlocked;
    public string NextUnlockLabel;     // teaser, e.g. "Unlocks: Rapid Fire" (from Cannon_Powerup_Unlock_Plan)
}
public static class ChapterStatsViewModel
{
    public static ChapterStatsVM Build(int chapter1Based) { /* read GameProgressManager + ChapterUnlockManager + configs */ }
}
```

Both panel surfaces (celebration + carousel card) call `Build` and bind the result. **No UI script computes stars/percentages itself** — single source of derived truth, so the two surfaces can never disagree.

---

## 4. Where the panel surfaces hook in

| Surface | Host | Trigger | Reads |
|---|---|---|---|
| **A. Celebration** | `ChapterVisualHandler.chapterUnlockPanel` (`Assets/Scripts/Chapters/ChapterVisualHandler.cs`) | post-chapter, existing countdown moment | `ChapterStatsViewModel.Build(justFinishedChapter)` |
| **B. Browse card** | new panel off the carousel (`Assets/Scripts/UI/MaInMenu/ChapterScrollItem.cs` area) | tap a chapter | `ChapterStatsViewModel.Build(tappedChapter)` |

Both subscribe to `GameEvents.OnChapterStarsChanged` to refresh if open.

---

## 5. Data flow summary (one table)

| Step | Who | Reads | Writes / Fires |
|---|---|---|---|
| 1 | `ScoreManager.OnLevelCompleted` | its own per-level counters | `FireLevelStatsFinalized(LevelStats)` |
| 2 | `StarRatingService` | `LevelStats`, `StarRatingConfig` | `GameProgressManager.RecordLevelStars(...)` |
| 3 | `GameProgressManager.RecordLevelStars` | `ChapterProgress.levelStars` | high-water-mark + `SaveProgress` + `FireChapterStarsChanged` |
| 4 | `StarRatingService` (milestone) | chapter star sum, claimed flags | `IChestGranter.Grant`, set flag, save |
| 5 | panel surfaces | `ChapterStatsViewModel.Build` | UI only |

---

## 6. Conventions & known hazards (read before coding)

1. **Chapter indexing is mixed.** `GameProgress` / `GameProgressManager` are **1-based** (`currentChapter = 1` is the first). `ChapterUnlockManager` is **0-based** (see the conversion already documented at `GameProgressManager.cs:257`). When the ViewModel calls both, convert with `unlockIndex = chapter1Based - 1`. **Pick one base per method and comment it.** This is the #1 place to introduce an off-by-one bug.
2. **High-water-mark only.** Never overwrite a higher star value with a lower one.
3. **Normalize on load.** New `int[]/bool[]` fields must be re-sized after deserialize (old saves won't have them).
4. **Event bus choice:** all signals here are in-level/cross-scene gameplay → use **`GameEvents`** (static Actions), not `EventBus`. (`CLAUDE.md` "two event systems".)
5. **No leaks:** new MonoBehaviour subscribers extend `EventSubscriberBehaviour` (`Architecture_Improvement_Plan.md §3`) or unsubscribe in `OnDisable`/`OnDestroy`. ScoreManager already balances its subscriptions — match that.
6. **No new singletons beyond `StarRatingService`** (one scene MonoBehaviour). Reuse `GameProgressManager`, `CurrencyManager`, `ChapterUnlockManager` as-is.
7. **Logging:** reuse `CombatLog`/`GameLogger` (`Assets/Scripts/Core/Logging/`). The stat fields are already on the `SUMMARY` line.

---

## 7. Deliberately NOT in this slice

- **Chest opening / inventory / pity / upgrades** — owned by `Powerup_Chest_Collection_Design.md`. We only *grant* via `IChestGranter`.
- **Per-chapter coin tracking** (panel doc Q4) — deferred; currency stays global.
- **Score-normalized stars / accuracy / weak-point stars** — future, pending the cannon-niche mechanic (`Cannon_Powerup_Unlock_Plan.md §1.7`).
- **Decomposing `ScoreManager`/`SpawnController`** — out of scope here; tracked in `Architecture_Improvement_Plan.md`.

---

*Keep this doc in sync if a component contract changes. The sprint plan references these section numbers.*
