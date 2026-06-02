# Bird Hunter — Cannon DPS Upgrade & Egg Workload Design

**Version:** 0.4 — project-side changes implemented; first live-telemetry read added (§6.5)
**Last Updated:** 2026-06-02

> **Implementation status (updated 2026-05-25):**
> - ✅ Cannon damage/fire-rate curve (`CannonInventoryService`): geometric integer damage (g=1.06, +1 floor) + ×1.5 saturating fire rate.
> - ✅ Upgrade cost curve → geometric `600 × 1.11^(L-2)` (`EconomyFormulaConfig.cs`).
> - ✅ E1 concurrent cap (5) → new `maxE1` field + `SpawnController` enforcement.
> - ✅ Difficulty ramp → egg-HP `chapterMult(1→20, linear) × levelMult(1→1.5)` across 30 chapter assets (Ch5 L1 = 3.6×, ceiling 30×).
> - ✅ Retry rule → neutral (clear by upgrading, not by HP handicap).
> - ✅ DPS readout in cannon upgrade UI (`CannonInventoryView` + `bulletCount` in `CannonDatabase`).
> - ✅ Number rescale **×10**: cannon `baseDamage` *and* egg tier HP both ×10 (balance-neutral — TTK/score unchanged; fixes fractional bases + clean geometric curve). Cloud stats anchored to live values.
> - ✅ Level-completion fix: cleared screen no longer waits out min-duration (`SpawnController.BeginSelfClearAfter`).
> - ⏳ Push `cannon_stats_gameplay.json` to Cloud Save dashboard (manual).
> - ⏳ Wire the DPS text field in the MainMenu prefab Inspector (manual).
**Scope:** Three linked changes that share one mechanism — (1) a level-100 cannon upgrade ladder framed around **DPS / integer damage** (with a capped fire-rate ramp), (2) an egg-spawn rebalance that trades **count for HP** (fewer, tankier eggs) for less clutter, and (3) a **death-loop progression** that forces the player to upgrade frequently to push past their last wall.

This doc pins the decisions made so far. Final numbers depend on two anchors not yet captured (see §7). Nothing here is implemented yet — it's the review artifact before code/config moves, per the project convention of pinning economy/progression/spawn tuning in `docs/` first.

Related docs: `Cannon_Powerup_Unlock_Plan.md` (unlock pricing/gating — unchanged), `Progression_Reward_Economy.md`, `Reward_Tuning.md`, `EndlessMode_Design.md`.

---

## 1. Goals & thesis

1. **Cannon upgrades go to level 100** (today: 10) — a long-tail progression spine.
2. **Upgrades raise DPS primarily via integer damage**, plus a small **capped fire-rate ramp**. Damage is the main driver and stays whole (egg HP is integer).
3. **Gameplay reads less cluttered:** fewer eggs on screen, each tankier, same dwell time and challenge — room to move.
4. **Core player goal — force frequent upgrading.** The player should die roughly every 2–3 levels at current power, upgrade with earned coins, and push 2–3 levels past their last death. Every upgrade must have felt impact.

**Thesis:** the game already sizes on-screen workload to the player's DPS (the TTK gate, §2). The cannon side raises DPS; the egg side spends it on tankier eggs, not more; and the difficulty curve rises faster than an idle player's survival, so progress *requires* upgrading. One workload equation governs all three.

---

## 2. The unified workload model

- `BaseCannon.CurrentDps` (`BaseCannon.cs:61–81`) = `CurrentAttack × fireRate × bulletCount × modMultipliers`. `CurrentAttack` is integer-clamped (`BaseCannon.cs:52`).
- **TTK soft-gate** (`SpawnController.cs:891–918`) pauses spawning when `totalEggHP / DPS > ttkCeilingSeconds` (currently `8.0`, `:34`).

Governing relations:

```
totalWorkload (on-screen HP)  ≈  DPS × ttkCeiling           (held ~constant by the gate)
eggs on screen                ≈  totalWorkload / HP-per-egg
avg dwell time per egg        ≈  totalWorkload / DPS  ≈  ttkCeiling   (≈ constant)
```

- **Raising HP-per-egg reduces egg count automatically** — same workload over a bigger denominator. No new throttling system needed.
- **Dwell time / challenge stay constant** when total workload is fixed, regardless of how it's split → fewer, fatter eggs, same difficulty, more space.

> **Forcing-function caveat:** the gate adapts spawn *count* to DPS, so it does **not** by itself punish a weak cannon — it just spawns fewer eggs. The "force" must come from things the gate does **not** relax: eggs that *linger longer at low DPS* (more hits on the cannon → death) and rising bird HP/aggression. See §5.

---

## 3. Cannon DPS upgrade redesign

### 3.1 Fire rate — capped, front-loaded ramp

Fire rate scales but **saturates to a hard ceiling** so it can't break performance (bullet count) or the DPS budget. **Locked: ×1.5 cap.**

```
fireRate(L) = baseFR × ( 1 + cap × (1 − decay^(L−1)) )
cap = 0.5        // +50% max
decay ≈ 0.924    // ~90% of the bonus arrives by ~L30, then plateaus
```

Front-loading is deliberate: early levels (where the +1 damage floor is small, §3.2) get their juice from fire rate; late levels are pure damage, so no bullet-count blowup. Integer damage is untouched — fire rate is a separate float multiplier on DPS, so per-hit damage stays whole.

> Replaces the original idea of a fully-fixed fire rate. The old linear +5%/level (→ ×5.95 at L100) is *not* used — uncapped fire rate is what breaks the game.

### 3.2 Geometric, integer-safe damage curve

```
damage(L) = max( damage(L-1) + 1,  round( baseDamage × g^(L-1) ) )
g = 1.06        // ~320× base at L100
```

**Locked: small-number scale, `baseDamage = 5` for Single Shot.** Consequence to design around: with a base this small, the **+1 floor dominates ~L1–38** (flat +1/level), so the *percentage* impact of each early upgrade decays (5→6 = +20%, 30→31 = +3%). To keep **felt impact uniform despite this**, the difficulty curve is made **additive in the same early region** (§5.2) so each +1 damage buys back exactly one difficulty step — TTK relief per upgrade stays constant even as the % shrinks. From ~L38 on, both curves go geometric together.

### 3.3 Worked reference — Single Shot (CANNON_01)

Suggested base stats (anchored to Ch-1 egg HP E1 5–15, E2 10–30, E3 20–60):

| Stat | Value | Why |
|---|---|---|
| `baseDamage` | **5** | Min E1 (5) dies in 1 shot; avg E1 (~10) in 2; E3 (~40) in ~8. |
| `baseFireRate` | **3.0 /s** (→ 4.5 at cap) | Medium cadence baseline; other cannons tuned against it. |
| `baseHealth` | **100** | Matches code fallback (`BaseCannon.cs:30`). |
| `baseBulletSpeed` / `baseCannonSpeed` | *keep current cloud values* | Untouched by this redesign. |

Progression (g=1.06 damage, fire rate saturating to ×1.5):

| Level | Damage | Fire rate | DPS | Notes |
|---|---|---|---|---|
| 1 | 5 | 3.0 | 15 | base |
| 10 | 14 | ~3.8 | ~53 | +1 floor region |
| ~38 | ~43 | 4.5 | ~193 | curve transition; fire rate ~maxed |
| 50 | ~87 | 4.5 | ~390 | compounding |
| 75 | ~370 | 4.5 | ~1,665 | |
| **100** | **~1,600** | **4.5** | **~7,200** | ≈320× damage × 1.5 fire rate ≈ **480× DPS** |

Other six cannons derive from this baseline (Rapid Fire: lower dmg / higher FR; Big Bartha: higher dmg / lower FR; multi-shot via `bulletCount`).

### 3.4 Max level 1 → 100

`maxUpgradeLevel` lives per cannon in the cloud `cannon_stats` DTO (`CannonBaseStats.cs:19`, default 10). Bump to **100** in cloud + `docs/cloud-save/cannon_stats.json`. Data edit, not code. Levels are already clamped to `[1, maxUpgradeLevel]` (`CannonInventoryService.cs:161`).

### 3.5 Upgrade cost — out-compounds DPS

```
cost(L) = baseCost × c^(L-1) × perCannonCoinMult
c ≈ 1.11        // c > g, so each level buys proportionally less DPS
```

Replaces the current polynomial `400 × (toLevel-0.5)^1.25` (flattens too hard past L10). Per-cannon `coinMult` (0.6–3.5) carries over. **Cost must be tuned against coin income so the death-loop closes (§5.3).** Materials curve needs an analogous extension to L100 or a decision to drop materials past L10.

### 3.6 Health & move-speed — unchanged for now

Health (+10%/lvl) and move speed (+2%/lvl) keep current linear PctAdd (`CannonInventoryService.cs:344, 346`). Revisit only if L100 makes the cannon unkillable.

### 3.7 Implementation touchpoints

| Change | Location |
|---|---|
| Geometric integer damage target (replace linear Damage PctAdd) | `CannonInventoryService.ApplyUpgradeModifier` (`:343`) |
| Fire rate → saturating ×1.5 curve (replace linear +5%) | `CannonInventoryService.cs:345` + new curve fields (`:28`) |
| Replace `damagePerLevelPct` with growth factor `g` + floor | `CannonInventoryService.cs:26` |
| `maxUpgradeLevel` 10 → 100 | cloud `cannon_stats` + `docs/cloud-save/cannon_stats.json` |
| Geometric upgrade-cost curve to L100 | `EconomyFormulaConfig` |
| Show **DPS** in upgrade UI | `CannonInventoryView.cs` |

`CurrentAttack` / `CurrentDps` already integer-clamp and feed the gate — no gameplay-side rewrite.

---

## 4. Egg workload / declutter redesign

### 4.1 Clutter source

- **Split cascade** (`SpawnController.cs:1229–1265`): `splitCount = 2` every tier → one E4 can become up to **14 eggs**.
- **E1 has no concurrent cap** (other tiers do: maxE3 1–2, maxE2 2–3 in `ChapterProgressionConfig`).
- Base tier HP (`Assets/Resources/Data/EggTierConfigs/`): E4 40–120, E3 20–60, E2 10–30, E1 5–15, × `_hpMultiplier`.

### 4.2 Decisions locked (IMPLEMENTED)

Keep `splitCount = 2` (the pop-cascade is the satisfying mechanic). Declutter by capping **count**, not by flat-multiplying base HP — tankiness is delivered by the per-chapter `hpMult` ramp (§5.2) instead, so early chapters stay poppy and the spawn gate is never over-fed. Target density **Moderate (~6–8 eggs at peak)**, down from ~15–18.

### 4.3 Concrete knobs (IMPLEMENTED)

| Knob | Was | Now | Where |
|---|---|---|---|
| **E1 concurrent cap** | none (uncapped → the flood) | **5** | `ChapterProgressionConfig.maxE1Start/End` + `SpawnController` enforcement |
| Parent caps (E4/E3/E2) | per-chapter, hand-tuned | unchanged | `ChapterProgressionConfig` |
| Base tier HP | 5–15 / 10–30 / 20–60 / 40–120 | **×10** → 50–150 / 100–300 / 200–600 / 400–1200 | `EggTierConfig` assets |
| splitCount | 2 | 2 (unchanged) | `EggTierConfig` |
| Per-egg tankiness | flat | rises via `hpMult` 1→30 (§5.2) | 30 chapter assets |

> The E1 cap is the key lever — E1 was the only uncapped tier, so the split cascade flooded the screen with small eggs. Capping it at **5** (plus the existing pressure budget) bounds the peak; the `hpMult` ramp makes the *remaining* eggs progressively tankier. Base tier HP was scaled **×10** — but purely as the balance-neutral partner to the ×10 cannon-damage rescale (TTK unchanged), *not* as a difficulty lever. The per-chapter "increase egg health" effect still comes from the `hpMult` ramp.

### 4.4 Implementation touchpoints

| Change | Location |
|---|---|
| Hard E1 concurrent cap + enforcement | `SpawnController` tier-count (`:842–866`, split path `:1229–1265`) |
| Lower maxE2 / maxE3 endpoints | `ChapterProgressionConfig` |
| Raise `baseHpMin/Max` per tier | `Assets/Resources/Data/EggTierConfigs/*` |
| (Possibly) retune `ttkCeilingSeconds` | `SpawnController.cs:34` |

---

## 5. Death-loop forcing function (force frequent upgrades)

**Goal:** player dies ~every 2–3 levels at current power → upgrades → pushes 2–3 levels further → repeats. Every upgrade has felt impact.

### 5.1 Why death, not a score quota

The TTK gate keeps the game *clearable* at any DPS by throttling spawns, so it can't be the pressure. Death pressure, however, scales with DPS even through the gate: **low DPS → eggs linger their full dwell (~8s) → more bounces/collisions on the cannon → more damage → death.** Rising bird HP/aggression adds to it. So the forcing function is survival, and it tightens automatically as difficulty climbs.

### 5.2 The two curves move in lockstep

```
difficulty per level   ↑   (egg _hpMultiplier, bird HP/aggression)   — creates the wall
cannon damage           ↑   (the upgrade)                            — the relief
```

The old `hpMultGlobalMax = 4.0` (generator) scaled egg HP only **~4× across all 30 chapters** — far too flat to force anything; an idle player coasts. **Corrected target:** the 480× figure is the L100 *ceiling*, but a real player during Ch1–30 sits far below it. Deriving from the new cost curve + chapter income, realistic Single-Shot DPS is ~**12× by Ch10, ~26× by Ch20, ~30–47× by Ch30** (focused vs spread play). So egg HP should track ~**30×** by Ch30, not 480×.

**Implemented:** egg-HP multiplier = `chapterMult(chapter) × levelMult(levelInChapter)`, written into the 30 hand-tuned chapter assets (generator untouched; `hpMultMin` = chapter floor, `hpMultMax` = floor × within-chapter ramp, and SpawnController lerps between them):
- **chapterMult:** linear **1.0 (Ch1) → 20.0 (Ch30)** — each chapter is distinctly tankier. Ch5 L1 = **3.6×** (the earlier geometric-over-600 curve gave only 1.6×, badly lagging the ~3–5× DPS a Ch5 player already has).
- **levelMult:** **1.0 (L1) → 1.5 (L20)** — leveling within a chapter raises HP ~50%.
- **ceiling:** Ch30 L20 = 20 × 1.5 = **30×** (the locked Balanced target).

The linear chapter ramp is intentionally steep early to track the fast early DPS growth (cheap early upgrades), so a fixed-power player stalls within ~2–3 levels and must upgrade.

### 5.3 Economic loop closure

Coins earned in a ~2–3 level run must buy an upgrade **burst** large enough to push the wall ~2–3 levels further. Early chapters earn ~1,450 coins/level (`Progression_Reward_Economy.md`); early Single Shot upgrades are ~260 coins (440 × 0.6) — so the player can already afford frequent bursts early. The geometric cost curve (§3.5, `c≈1.11`) naturally slows cadence late. Tune `baseCost`/`c` so `coins(2–3 levels) ≈ cost(burst that clears next 2–3 levels)`.

### 5.4 Retry rule — identical difficulty; clear by upgrading (locked)

On death/retry the level plays at **identical difficulty** — no HP or pressure handicap. A failed level becomes clearable because the player **upgrades their cannon** between attempts (more DPS), never because the game softens the wall. **This disables the prior behavior** (`ChapterProgressionConfig`: +2% HP / +3% pressure per retry), which made retries *harder* and fought "push further." The `replay*` fields are retained but inert; the consumer in `SpawnController.ResolveChapterLevel` no longer applies them.

---

## 6. How the pieces interact across progression

- Cannon climbs to ~480× DPS over 100 levels → the gate sustains proportionally more workload.
- Low concurrent caps + tankier-egg HP → that surplus shows up as **tankier eggs, never more eggs** → clutter stays bounded all game.
- The difficulty ramp + death pressure means the player must keep upgrading to hold TTK/survival → **frequent upgrades**, each one felt.
- Retry-easier softens dead-ends without removing the upgrade pressure.

Critical pairing to validate: **damage curve (per cannon level) × egg-HP/difficulty curve (per chapter) × coin income** — every (level, chapter) cell should keep the death cadence at ~2–3 levels and each upgrade impactful.

---

## 6.5 Telemetry findings (2026-06-02) — first live-log read

First analysis of `GameLogs/combat.log` + `economy.log` from a real playthrough (143 level clears, 9 sessions). What it validates and what it flags against this design:

- **Single-cannon dominance — the headline gap.** The player cleared **all 5 shipped chapters, reaching ~level 35, on `SingleShotCannon` alone** — 34 upgrades (lvl2→lvl35), **zero** other cannons unlocked or equipped, despite Rapid Fire (Ch3), Lucky (Ch6) etc. being available (`Cannon_Powerup_Unlock_Plan.md §1.2`). The §5 death-loop successfully forces *upgrading*, but nothing forces **cannon variety** — single-shot's curve brute-forces every wall. This is a roster/niche problem; **resolution direction in `Cannon_Powerup_Unlock_Plan.md §1.7`** (per-cannon niches: single-shot = precision/weak-point with no AoE). **This design owes that plan new enemy archetypes** — dense E1/E2 **swarm waves** (punish single-target → Shotgun), **shielded/armored** enemies (→ Double), and **weak-point-gated bosses** (→ Single Shot/Bartha). Without those, niches are flavor and dominance returns. Fold these into the §5.2 difficulty curve.
- **DPS scaled hard, as intended.** avgDps climbed ~**68 → 800+**; peakDps bursts to **7,628** (powerup-driven). The DPS-raising half of the thesis works.
- **Death cadence UNVALIDATED.** combat.log logs **no death / retry / game-over events** — the §5 "die every 2–3 levels" forcing function cannot be confirmed from telemetry. **Action: add death/retry logging** before claiming the §5.2 lockstep holds.
- **`level_completion` reward is a flat 340 gold** across all 143 clears (does not scale by chapter/level). The §5.3 loop assumes coin income rises with progression; today only per-egg drops scale, the completion bonus does not. Reconcile with `Progression_Reward_Economy.md` (see its telemetry note).
- **Score is upgrade-dominated** (2,306 → median 8,663 → 31,073 per level) — noted here because it kills absolute-score thresholds for the star system (`Chapter_Unlock_Panel_Design.md §3.5`); if §3.3's expected-workload math is built, it can normalize both star par-times and score.

> These are from one player/device — directional, not statistically settled. The single-cannon and flat-completion-reward findings are the two that most clearly contradict design intent.

## 7. Open anchors — needed to lock final numbers

1. **Base damage per cannon** (cloud `cannon_stats[*].baseDamage`). Single Shot proposed at **5**; the other six derive from it. Read live values to anchor exactly.
2. **Chapter difficulty curve** (`_hpMultiplier` + bird HP/aggression). Currently 1.0→1.1 — needs the ~3× steeper, additive-early-then-geometric redesign of §5.2. This is the linchpin for "force frequent upgrades."
3. **Coin income per chapter-level** (from `Progression_Reward_Economy.md` / `Reward_Tuning.md`) to close the §5.3 economic loop.

---

## 8. Action items, ordered by ROI

| # | Item | Owner | Why |
|---|---|---|---|
| 1 | Redesign the per-level difficulty curve (§5.2): ~3× steeper, additive early → geometric late, tracking 480× DPS. | Design | The forcing function. Nothing else forces upgrades without it. |
| 2 | Build the damage × difficulty × income validation sheet; confirm 2–3-level death cadence and per-upgrade impact everywhere. | Design | Proves the loop holds across L1–100 × Ch1–30. |
| 3 | Implement geometric integer damage + saturating ×1.5 fire-rate curve. | Eng | Core DPS framing; small change (§3.7). |
| 4 | `maxUpgradeLevel` 10 → 100 in cloud + import file. | Eng | Unblocks the long-tail ladder. |
| 5 | Geometric upgrade-cost curve to L100; tune to income (§5.3). | Design + Eng | Prevents DPS-per-coin runaway; closes the loop. |
| 6 | E1 cap + lower E2/E3 caps + raise base HP ~2–2.5×. | Eng | Delivers the declutter. |
| 7 | Flip retry rule to slightly-easier (§5.4). | Eng | Stops the current bump from fighting "push further." |
| 8 | Surface DPS in the cannon upgrade UI. | Eng | Player-facing half of the framing. |

---

## Decisions locked (v0.2)

- Max upgrade level **100**.
- Damage: geometric **g = 1.06** (~320× at L100), **+1/level integer floor**, **small scale, Single Shot base = 5** (→ ~1,600 dmg at L100).
- Fire rate: **front-loaded ramp, ×1.5 cap**, plateaus ~L30. → total DPS ~**480×** at L100.
- Upgrade cost: **geometric, c ≈ 1.11** (out-compounds DPS).
- Health / move speed: linear, unchanged.
- Declutter: **keep splitCount = 2, cap count + raise HP**; target **~6–8 eggs peak (Moderate)**.
- Forcing function: **death loop**, **Moderate ~2–3 level cadence**; uniform per-upgrade impact via additive-early difficulty.
- Retry: **identical difficulty** — progression comes from upgrades, not handicaps (prior +2%/retry disabled).

*Maintain this doc when:* growth factors / cap / max level change, the egg HP or difficulty curve changes, the retry rule changes, or the §7 anchors are captured (promote from DRAFT and fill real numbers).
