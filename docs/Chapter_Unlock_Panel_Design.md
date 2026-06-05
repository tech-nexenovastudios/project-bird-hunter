# Bird Hunter — Chapter Unlock Panel & Stats Design

**Version:** 0.2 (ideation draft — telemetry-informed)
**Last Updated:** 2026-06-02
**Status:** 🟡 Ideating — not implemented. Captures design intent before code. Open design forks in §9; ⚙️ tuning numbers to be locked. v0.2 folds in the 2026-06-02 logger analysis (§3.5): score excluded from stars, health-logging gap surfaced.
**Scope:** Turn the current chapter-unlock countdown gate into a **reward + anticipation moment** that surfaces per-chapter stats (stars, completion, coins, chests, unlocks). Define a **per-chapter star system** as the new headline progression metric.

This doc pairs with:
- `Powerup_Chest_Collection_Design.md` — the chest/collection meta. The panel **displays** chest progress; it does not own chest logic.
- `Progression_Reward_Economy.md` / `Reward_Tuning.md` — star thresholds and any star-gated rewards fold into the earn-rate model.
- `Cannon_Powerup_Unlock_Plan.md` — which cannon/powerup unlocks to tease per chapter.

> All numbers are **strawman values for tuning**, flagged ⚙️.

---

## 1. What we have today

- **`ChapterVisualHandler.cs`** (`Assets/Scripts/Chapters/`) owns a `chapterUnlockPanel` GameObject. Today it is a **transition gate**: shows `chapterNameText` + a `countdownText` ticking 5→1, then runs the DoTween environment transition (platform drop → background slide → cannon drop). It fires off `GameEvents.OnChapterCompleted`.
- **`ChapterScrollItem.cs`** (carousel) already shows per-chapter `highScore` and `bestLevel`, read from `GameProgressManager.Instance.Data.chapters[index]`.
- Per-chapter persisted data (`GameProgress.chapters[]`): `highScore`, `highestLevelReached`, `cleared`, `firstClearedLevels: List<int>`, `attempts`.
- **No star system. No chest UI.** Currency (`gold`/`gems`/`power`) is **global**, not bucketed per chapter.

### 1.1 The problem
The unlock moment is a bare countdown. Unlocking Chapter 5 feels identical to Chapter 2 — no sense of "look what I accomplished" and no pull toward "look what's ahead." We have rich per-chapter data sitting unused.

### 1.2 The design in one sentence
> Make the chapter-unlock panel a **two-beat moment**: a **retrospective** ("here's how you did in the chapter you just cleared" — stars, completion, coins, chests) and a **prospective** teaser ("here's the new biome, enemy, and reward waiting"), with a **per-chapter star rating** as the headline metric you chase.

---

## 2. Two surfaces (don't conflate them)

The stats can live in **two distinct UI contexts**. They share data and widgets but trigger differently:

| | **A. Unlock celebration** (event-driven) | **B. Chapter info card** (browse) |
|---|---|---|
| Trigger | Chapter cleared → the existing `chapterUnlockPanel` moment | Player taps a chapter in the carousel (`ChapterScrollItem`) |
| Mood | Celebratory, animated, one-shot | Calm, reference, re-openable any time |
| Shows | What you *just* earned + tease of next | Lifetime stats for *that* chapter + what's locked |
| Lives in | `ChapterVisualHandler.chapterUnlockPanel` | New panel off the carousel |

**Decided (Q1): ship both** — build one shared stat-widget set, surface it in **A** (celebration) and **B** (browse card).

---

## 3. The star system (new headline metric)

Stars are the spine of the retrospective. **Stars are earned per level, summed per chapter.**

### 3.0 Model decision — independent objectives (Sky Force model)

**Decided:** use **3 independent per-level objectives**, not a tiered single-score (Angry Birds) or a composite weighted grade.

Industry comparison that drove this:

| Game | Model | Trade-off |
|---|---|---|
| Angry Birds / Candy Crush | Tiered score (one axis, 3 thresholds) | Simple but one-dimensional — only rewards score. |
| Cut the Rope | Collectibles placed in level | Puzzle-only, doesn't map to a shooter. |
| **Sky Force (Reloaded/2014)** | **Independent objectives** (complete / destroy ≥X% / no-hit / rescue) | **Chosen** — the shooter gold standard; multi-axis yet each star legible. |
| Jetpack Joyride | Rotating mission triad | Engaging but muddies a stable "best" total. |

Independent objectives satisfy the "evaluate **how** the player performed across multiple axes" goal while keeping each star **self-explanatory** (player knows exactly which one they missed → replay hook). A composite grade was rejected for being opaque ("why 2 not 3?").

### 3.1 Earning stars (per level)
Each level awards **0–3 stars**; clearing always grants ★1, so completion % and stars stay linked but distinct:

| Star | Condition | Tuning |
|---|---|---|
| ★ 1 | **Clear** the level | none — also the completion-% signal |
| ★ 2 | **Survive** — end with cannon health ≥ ⚙️50% (or ≤ N hits taken) | condition-based, no per-level tuning; **needs new logging (§3.5)** |
| ★ 3 | **Speed clear** — finish under ⚙️ par time | par time per level; timestamps already available |

**Decided (Q3, 2026-06-02):** ★2 = survive (health ≥ ⚙️50%), ★3 = speed clear (under ⚙️ par time). **Score is excluded** — telemetry shows it is upgrade-dominated, not skill (§3.5). Both gates are legible and upgrade-independent.

### 3.5 Telemetry finding (2026-06-02) — why not score

Analysis of `GameLogs/combat.log` (143 level clears) reshaped the ★2/★3 choice:

- **Score is dominated by cannon-upgrade level, not skill.** Per-level score ran **2,306 (min) → 8,663 (median) → 31,073 (max)** — a ~13× spread tracking the single-shot cannon climbing from avgDps ~68 to ~800+. An *absolute* score threshold would hand ★3 to any upgraded player for free and deny it to a skilled new one. **Decision: drop absolute-score ★3.** If score is ever used it must be normalized (score ÷ level's expected enemy value), which needs the workload math in `Cannon_DPS_and_Egg_Workload_Design.md`.
- **Logging gap:** combat.log records damage *dealt* (totalDmg, DPS) but **not cannon health / damage taken**. So health-based ★2 (survive/flawless) is **not measurable yet** — it needs new hits-taken/end-health logging before it can be evaluated.
- **Speed clear is derivable today** from log timestamps, and is upgrade-independent-ish and on-theme (efficient killing). Hence the revised ★3 = speed clear.

**Decided (Q3):** ★1 = clear · ★2 = survive (health ≥50%) *(blocked on health logging — §3.6)* · ★3 = speed clear (par time) *(buildable now)*.

> **Star/cannon synergy:** the cannon-niche work (`Cannon_Powerup_Unlock_Plan.md §1.7`) adds a **weak-point / accuracy** mechanic. That is an *upgrade-independent skill signal* — a strong future candidate to replace or augment a star gate (accuracy %, weak-point-kill %), unlike score. Revisit star conditions once that mechanic lands.

### 3.6 Logging for ★2 — ✅ implemented (2026-06-02)

The level `SUMMARY` line now carries the defensive fields ★2/★3 need:

```
SUMMARY  level=12  score=4200  totalDmg=8600  peakDps=210.0  avgDps=118.4  time=23.4s  endHp=72%  hits=3  dmgTaken=28
```

- **`endHp=%`** — cannon health at level complete → drives ★2 (survive ≥50%). `-1` = no cannon-health event seen.
- **`hits` / `dmgTaken`** — cannon hits taken / total damage taken this level (tuning + alt ★2 = ≤N hits).
- **`time=Ns`** — active-combat clear time → drives ★3 (speed clear under par).

Implementation: `ScoreManager` subscribes to `GameEvents.OnCannonHealthChanged` (latest HP, carried across levels) + `OnCannonHit` (per-level hit/damage counters, reset each level); emits via `CombatLog.Summary`. **Next telemetry capture will reveal how often players actually take damage / how long levels run — use it to set the ⚙️50% and ⚙️par-time thresholds.**

### 3.2 Aggregating per chapter — the max is deterministic
- **Per level:** 3★ max.
- **Per chapter:** `3 × levelCount`. A 15-level chapter = **45★**.
- **Whole game:** `3 × Σ(all levels across chapters)`.
- Panel headline: `★ 38 / 45` = `chapterStars / (3 × levels)` with a fill bar.
- **Best-ever, not last-run:** stars are a high-water mark per level (re-clearing can only raise, never lower) — mirrors `firstClearedLevels` semantics.

### 3.3 Why stars (vs. just %)
Stars give: (a) a re-play hook (go back for 3★ on missed levels), (b) a clean **gate currency** for rewards ("collect 30★ in Ch3 → bonus chest"), (c) a cross-chapter prestige total ("412★ collected"). Completion % alone is binary and re-play-dead.

### 3.4 Star-gated rewards (DECIDED — ties to chests)
**Decided (Q5):** stars unlock **milestone chests**, gated at **fractions of the chapter max** so they auto-scale to any level count:

| Milestone | Threshold | Reward |
|---|---|---|
| ⅓ of chapter ★ | `≥ levels × 1` | ⚙️ Wooden chest |
| ⅔ of chapter ★ | `≥ levels × 2` | ⚙️ Silver chest |
| Full (3★ all) | `= levels × 3` | ⚙️ Gold chest |

Chests award via `Powerup_Chest_Collection_Design.md` (the panel grants, the chest system owns the open/roll). Claim state persists in `milestoneChestsClaimed` (§6). **Dependency:** the reward side needs the chest system live; until then stars still display and gates can show as "claim when unlocked."

---

## 4. Stat catalog — what the panel can show

Grouped by intent. v1 set chosen in §9 Q2.

### 4.1 Mastery (how well)
- **Star rating** `★ 38/45` + fill bar — *headline*. (needs §3 star system)
- **Completion %** — levels cleared / total. Radial ring around chapter icon. (data: `firstClearedLevels.Count`)
- **High score** — already tracked (`highScore`). (ready)
- **Perfect clears** — levels with 3★ / no-damage. (needs §3)
- **Best combo / max chain** — top egg-split chain. (needs new tracking)

### 4.2 Collection (what earned)
- **Chests earned** — bronze/silver/gold tiers with locked silhouettes. (needs chest system live)
- **Coins collected (this chapter)** — requires **new per-chapter coin tracking** (currency is global today). (needs data work — §9 Q4)
- **Powerups / cannon unlocked** — show the actual unlocked cannon/powerup icon. (data: `Cannon_Powerup_Unlock_Plan.md`)
- **Birds / bosses defeated** — mini-bestiary counter. (needs new tracking)

### 4.3 Anticipation (why enter next) — highest value, most underused
- **Theme reveal** — hero art of the new biome (`ChapterData.chapterBackground`). (ready)
- **New enemy teaser** — silhouette/loop of the chapter's new bird/boss. (needs art hook)
- **Reward roadmap** — node track of what unlocks at L5/L10/L15 of the new chapter. (needs config)
- **Difficulty meter** — flame/skull pips. (ready, static config)

### 4.4 Social / meta (optional)
- **Leaderboard snippet** — your rank vs. friends (LeaderboardManager exists).
- **Global completion %** — "12% of players cleared this" FOMO. (needs backend stat)
- **Best vs. last attempt** delta.

---

## 5. Phased rollout

| Phase | Contents | Depends on |
|---|---|---|
| **P1 — Ship now** | Star system (§3.1–3.2) + Completion % + High score + Cannon/powerup unlock teaser | star data field; rest ready |
| **P2 — With chests** | Chest tier row + star-gated milestone chests + reward roadmap | `Powerup_Chest_Collection_Design.md` live |
| **P3 — Polish/retention** | Theme + enemy teaser, difficulty meter, global completion % FOMO | art hooks, backend stat |

P1 is the MVP and the only part needing new persistent data (stars).

---

## 6. Data model

New per-chapter fields in `GameProgress.chapters[]` (extends existing `ChapterProgress`):

```
ChapterProgress (existing + new)
  ├─ highScore            (int)  existing
  ├─ highestLevelReached  (int)  existing
  ├─ cleared              (bool) existing
  ├─ firstClearedLevels   (List<int>) existing
  ├─ attempts             (int)  existing
  ├─ levelStars           (Dictionary<int,int> or int[])  NEW — best stars per level (0..3)
  ├─ coinsCollected        (int)  NEW? — per-chapter coin tally (§9 Q4)
  └─ milestoneChestsClaimed (flags) NEW? — which star-gated chests taken (§3.4)
```

- Stars are a **high-water mark**: `levelStars[lvl] = max(existing, earned)` on clear.
- Persisted via the existing `GameProgressManager` cloud-save path — no new `CloudKeys` entry needed if it nests under chapter progress.
- Chapter star total + completion % are **derived** (compute on read), not stored.

---

## 7. Where star awarding hooks in

- Level-clear is the evaluation point. Find the existing level-complete signal (level-countdown / self-clear endgame in `GameEvents`) and compute stars from the run's stats (score, damage taken, time) there.
- Persist via `GameProgressManager`, then the unlock panel reads back the updated totals when it opens.
- Fire a lightweight event (e.g. `GameEvents` star-earned or reuse `OnChapterCompleted`) so the panel/carousel refresh.

---

## 8. UX / presentation

- **Locked vs unlocked card** — locked chapter card is dim/desaturated with a glowing requirement ribbon ("Reach Level 20 to unlock"); unlock plays a seal-break / chain-snap.
- **One headline stat, big** — stars in large type; secondary stats smaller. Avoid a spreadsheet.
- **Tappable stat tiles** that expand for detail; default view stays clean.
- **Reuse the rarity palette** from `PowerupLockView` for chest tiers (Common `#98F3AF`, Rare `#F8E64B`, Epic `#EAB3FF`, Legendary `#FF9B94`) for cross-screen consistency.
- The celebration surface (§2A) animates in over the existing countdown beat; the browse surface (§2B) is static.

---

## 9. Design forks

**Resolved (2026-06-02 ideation session):**

| # | Question | Decision |
|---|---|---|
| Q1 | Which surface for v1 | **Both** — one shared stat-widget set, surfaced in the unlock celebration (2A) **and** a tappable carousel card (2B). |
| Q2 | v1 stat set | **Star rating + bar, Completion %, High score, Unlock teaser** (all four). |
| Q5 | Star-gated chests | **Yes** — milestone chests at ⅓ / ⅔ / full chapter ★ (§3.4). |
| Q3 | ★2 / ★3 conditions | **★2 = survive (health ≥50%)**, **★3 = speed clear (par time)**; **score excluded** (§3.5). |
| — | Star model | **Independent objectives** (Sky Force), 3★/level, ★1 = clear (§3.0). |

**Still open:**

| # | Question | Why it matters |
|---|---|---|
| Q4 | **Per-chapter coins** — track coins per chapter (new tally) or drop the stat? Not in the chosen P1 set, so deferrable. | Currency is global today. |
| Q6 | **★3 par-time sourcing** — derive par time from `Cannon_DPS_and_Egg_Workload_Design.md` expected workload, or hand-tune per level? Now unblocked: `time=` is logged (§3.6) so a capture can set it empirically. | Sets the per-level tuning lever. |

**Closed:** Q7 (health logging) — ✅ implemented 2026-06-02, see §3.6.

---

## 10. Build order (once locked — NOT started)

1. **Data** — add `levelStars` to `ChapterProgress`; high-water-mark write on level clear; derived totals on read.
2. **Award logic** — compute stars from run stats at level-complete; persist.
3. **Panel data binding** — feed star total / completion / high score / unlock teaser into the chosen surface (§2).
4. **Widgets** — star fill bar, completion ring, unlock-teaser card.
5. **(P2)** chest row + star-gated milestone chests once the chest system lands.
6. **(P3)** theme/enemy teaser, difficulty meter, FOMO stat.
7. **Tuning + analytics** — lock ⚙️ star thresholds; log star earns / 3★ rate / panel opens.

---

*Maintain through ideation. Promote to a versioned spec (lock ⚙️ numbers, drop 🟡) before implementation.*
