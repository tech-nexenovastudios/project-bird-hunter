# Endless Mode — Design Deep Dive

> Status: Design proposal (not yet implemented)
> Author: Generated from project analysis on 2026-05-07
> Scope: A new game mode for Bird Hunter, sitting alongside the campaign

---

## 1. Why Endless Fits This Game

The campaign is **completion-driven** (clear chapter 30, beat each boss once). Endless is **mastery-driven** (how deep can you go?). They serve different player psychologies, so endless adds reach without cannibalizing — completionists keep grinding campaign, score-chasers get a reason to log in daily.

The codebase already has ~80% of what's needed: bird configs, movement strategies, powerup system, cannon w/ HP/mana/shield, scoring, combos, boss prefabs, Google Play Games auth, NativeShare, AdsManager. Endless is mostly a **new run controller + new save profile + UI** — not new gameplay tech.

---

## 2. Run Structure — The Core Loop

Three viable models:

| Model | Feel | Trade-off |
|---|---|---|
| **A. Continuous waves** | One unbroken stream of birds, HP never resets | Pure but no breathing room → mentally exhausting on mobile |
| **B. Chunked floors** | Discrete 30–60s "levels" stacked forever | Familiar but feels too campaign-like |
| **C. Hybrid (recommended)** | Continuous combat with **checkpoints every 5 waves** for a breather + spin + heal choice | Best of both — preserves the slot-machine cadence players already know |

### Recommended pacing for Hybrid

- Wave = ~30 seconds of combat
- Every 5 waves → 3-second checkpoint with spin (matching the campaign's L1/L5/L10/L15 cadence)
- Every 10 waves → mid-boss; every 25 waves → major boss
- Average run: 5–12 minutes for most players, 20+ for legends

---

## 3. Wave Variety — Preventing Monotony

Don't just spawn "more birds, faster." Mix **wave archetypes** that reward different builds:

- **Standard** — baseline bird mix
- **Swarm** — many low-HP birds (rewards bounce/spread/AOE)
- **Tank** — few high-HP birds (rewards pierce/laser)
- **Sniper** — egg-heavy waves (rewards shield/invincibility)
- **Speed** — fast small birds (rewards fire rate)
- **Elite** — single shielded bird w/ dodge
- **Boss** — every Nth wave
- **Bonus** — pinata birds dropping extra loot (rest wave)

Use **weighted-random with pity timers** so a boss is guaranteed by wave X, but the order varies. The existing `WeightedShuffle` in `GameProgress.cs` is a natural fit for this.

---

## 4. Difficulty Scaling — The Hard Part

This is where most endless modes fail. Tune **multiple dials independently** rather than one global multiplier:

| Dial | Curve | Why |
|---|---|---|
| Bird HP | Slow exponential (×1.04/wave) | Keeps fights from instakilling |
| Bird speed | Capped at ~1.5× by wave 50 | Beyond that, dodging becomes unfair |
| Spawn rate | Linear, capped | Screen-clutter has a hard ceiling |
| Bird tier mix | Step function (B1 → B2/B3 at wave 10 → B3/B4 at wave 20 → Specials at wave 30) | Visual + mechanical variety |
| Movement complexity | Unlocks ZigZag/Curve/Target progressively | Skill ramp |
| Egg drop density | Logarithmic | Defensive load |

**Critical separation:** keep "cosmetic" scaling (variety) decoupled from "lethality" scaling (HP/damage). Lethality should grow **logarithmically**, not exponentially, or you build a hard wall at wave ~40 that nobody passes.

The existing `Adaptive Difficulty Config.asset` is the natural home for these curves.

---

## 5. Powerup Integration

Two paths, both viable:

### Path 1 — Faster cadence, same pool (ship first)

- Spin every 5 waves instead of every 5 levels
- All powerups unlocked (no `unlockFromChapter` gating)
- All 4 slots typically filled by wave 20
- Reuses 100% of existing infrastructure

### Path 2 — Roguelite upgrades (ship second)

- Each spin can offer "upgrade existing powerup to Tier II/III" instead of a new one
- E.g., Lightning Shot II = chains to 2 extra targets; Tier III = stuns for 1s
- Adds enormous build depth (the "should I diversify or specialize?" tension)
- Real dev cost: needs tier data per powerup + UI for upgrade choice

> ⚠️ **Critical fix prerequisite:** `CannonPowerUpCaster.Equip()` currently calls `UnequipAll()` before equipping (`CannonPowerUpCaster.cs:105-109`). This makes the 4-slot UI a lie — only the last-equipped powerup is actually active. Endless mode lives or dies on satisfying loadout-building, so this needs to be fixed before launch.

---

## 6. Run Economy — What Does a Run Pay?

A single run should produce **multiple parallel rewards** so different player types feel served:

- **Coins** — drop from birds during run, scaled by depth. Feeds campaign shop.
- **Gems** — milestone-only (first time reaching wave 10, 25, 50, 100 = one-time payout). Daily small floor.
- **Power** — for cannon upgrades. Scales with depth.
- **Endless XP** — separate track from player level. Unlocks cosmetic borders, leaderboard frames, badges.
- **Run Tokens (new currency)** — only earned in endless, spent at a separate **Endless Shop** for:
  - Permanent run starts ("begin every run with +20 HP")
  - Run modifiers ("first spin guaranteed legendary")
  - Cosmetic skins exclusive to endless

Run Tokens are the **meta-hook** that makes endless retain players past their first leaderboard placement. Without it, endless is a 1-week novelty.

---

## 7. Death, Continue, and Monetization

When cannon dies:

1. Freeze frame + dramatic SFX
2. Show "Continue?" prompt
   - Watch rewarded ad → revive at 50% HP (1× per run)
   - Or spend gems → revive at 100% HP (cost scales: 50 / 100 / 200)
3. If declined or already used: roll into run summary

> 💡 **Leaderboard fairness:** continued runs should bump to a separate "Continued" leaderboard tier, or attach a 🩹 marker. Otherwise the top-10 will be the players with the deepest wallet, which kills competitive trust.

### Run summary screen

- Big depth number (the "trophy")
- Currency earned, milestones hit
- Personal best delta + confetti
- Leaderboard rank delta
- **Share button** → screenshot with depth + cannon skin → NativeShare. Massive viral hook.

---

## 8. Leaderboards & Social

Tier the leaderboards so multiple cohorts win:

- **Daily** (resets 24h) — best single run today
- **Weekly** — best run this week
- **All-time** — personal lifetime best
- **Friends-only** — Google Play Games friends list
- **Daily Seed** — same wave order for everyone that day → rewards skill not RNG → highest-engagement leaderboard

The **Daily Seed** is the one to invest in. Same seed = fair comparison = "let me try once more before bed." Streamers/content creators love it because they can compare runs directly. Cheap to implement: just a deterministic RNG seeded by `yyyy-MM-dd`.

---

## 9. Modifiers & Live-Ops Events

Weekly modifier rotations on a separate leaderboard:

- **Glass Cannon** — 1 HP, 5× damage
- **Slow Bullets** — bullets fly at 50% speed but pierce all
- **Bird Frenzy** — 2× spawn, 2× rewards
- **Powerup Drought** — only 2 slots, no spin re-rolls
- **Boss Rush** — every 3rd wave is a mini-boss

Themed seasonal events (Halloween bat birds, holiday-themed eggs) reuse the existing `BirdConfig` system + skin swaps.

These should **never replace** base endless — they live alongside. Players who want "the real game" stay in standard; players hungry for novelty bounce in/out of weekly modifiers.

---

## 10. Pacing & Anti-Grind

Endless is infinitely playable, but rewards must be capped or the campaign economy collapses.

**Recommended caps:**

- ❌ Don't gate runs with energy/lives — kills mobile retention loops
- ✅ Daily currency cap (e.g., 500 coins/day from endless regardless of run count)
- ✅ Logarithmic reward scaling past wave 30 (so wave 100 isn't 5× wave 50's payout)
- ✅ Run Tokens uncapped (those are the endless-specific carrot)

This way, players can run endlessly for skill/leaderboard pride, but can't farm campaign currency through endless.

---

## 11. UI / UX Surface Area

### Entry

- Main-menu tile next to "Campaign", locked until chapter 5 cleared (gives campaign players a goal)

### In-run HUD additions

- Wave counter (top-center, big)
- Depth bar (subtle, bottom)
- Next-wave preview ("Wave 8: Swarm incoming")
- Boss warning banner 5 seconds before boss waves
- Combo meter (already have `ComboController` — make it pop more here)

### Run summary screen (new)

- Depth milestones with checkmarks
- Currency breakdown
- Personal best delta
- Leaderboard rank delta
- Share button

### New scenes / panels

- `EndlessGameplay.unity` (or flag-toggle on `GamePlayScene`)
- Endless main panel (entry, leaderboard preview, daily seed CTA)
- Endless shop (Run Token spend)
- Leaderboard panel (tabs: Daily / Weekly / All-Time / Friends / Seed)

---

## 12. How It Slots Into Existing Code

Touch points (high level):

| Existing system | Touch needed |
|---|---|
| `SpawnController.Configure(chapterCfg, levelIdx, priorAttempts)` | Add overload: `Configure(WaveDescriptor)` so endless can feed waves on-demand |
| `GameProgressManager` | Split into `CampaignProgressManager` + new `EndlessProgressManager` w/ separate save file |
| `CannonPowerUpCaster.Equip` | **Fix single-active enforcement** — let multiple powerups stack |
| `LevelCompletionController` | Detect mode → route to next-wave vs. next-level |
| `AuthManager` | Wire Google Play leaderboards (likely partially wired already) |
| `SlotMachineController` | Add re-roll-with-gems option, daily-seed deterministic mode |
| Boss prefabs (Gordon Griffin etc.) | Make them invocable on-demand by wave generator, not just chapter-end |
| `AnalyticsTracker` | New events: `endless_run_started`, `endless_wave_reached`, `endless_continue_used`, `endless_run_ended` |

### New systems

- `WaveGenerator` ScriptableObject — produces waves on-demand, weighted by depth
- `EndlessRunController` MonoBehaviour — orchestrates the run lifecycle
- `EndlessProgress` serializable — tracks all-time best, daily best, run tokens, milestone unlocks
- `WaveDescriptor` data class — bird mix, count, modifiers, duration

---

## 13. Top Risks to Plan For

1. **Difficulty wall around wave ~40** — most endless modes fail here. Mitigate with playtester data + remote config tuning.
2. **Powerup balance breaks at depth** — Lightning Shot is fine for 20 campaign levels but might break wave 80. Plan for an endless-specific balance pass via remote config overrides.
3. **Single-active powerup bug** — if not fixed, endless feels *worse* than campaign, not better. Hard blocker.
4. **Run length on mobile** — anything over 15 minutes has completion-rate problems. Tune so median run is 5–10 min.
5. **Reward inflation collapsing campaign economy** — cap daily endless currency.
6. **App-close mid-run** — decide upfront: pause+resume, or run dies. Recommendation: **run dies** (cleaner, prevents cheese), but autosave the depth so leaderboard credit isn't lost.

---

## 14. MVP vs. v2 Split

### Ship MVP first

- Hybrid run loop with 5-wave checkpoints
- Faster powerup spin cadence, all powerups unlocked
- Daily/Weekly/All-Time leaderboards
- Coins + gems + Run Tokens
- Run summary + share button
- Continue via ad/gems

### v2 (after data tells you it's working)

- Daily Seed mode
- Weekly modifiers
- Powerup tier upgrades (Path 2)
- Endless Shop with Run Tokens
- Friends leaderboard
- Cosmetic borders/skins

Splitting like this lets you validate the core loop in ~3-4 weeks before sinking 2 more months into the meta layer.

---

## Summary

Endless mode for this game is a **systems-integration project**, not a from-scratch build. The hardest design choices are:

1. Difficulty curve tuning
2. The single-active-powerup fix
3. The daily reward cap

Get those three right and the rest is execution.

---

## Related Existing Systems Referenced in This Doc

- `Assets/Scripts/Gameplay/Managers/GameProgressManager.cs`
- `Assets/Scripts/Gameplay/Managers/GameProgress.cs`
- `Assets/Scripts/Gameplay/PowerUps/CannonPowerUpCaster.cs`
- `Assets/Scripts/Gameplay/Slot/SlotMachineController.cs`
- `Assets/Resources/Data/Adaptive Difficulty Config.asset`
- `Assets/Resources/Data/BirdConfigs/`
- `Assets/Resources/Data/EggTierConfigs/`
- `Assets/Scenes/BirdHunter/GamePlayScene.unity`
