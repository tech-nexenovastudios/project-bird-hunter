# Bird Hunter — Cannon & Powerup Unlock Plan

**Version:** 1.0  
**Last Updated:** 2026-04-28  
**Scope:** Production-ready unlock and pricing strategy for the 7 cannons and 29 powerups, computed from the actual ScriptableObject + formula config in the repo today.

This doc is the canonical source for "when does the player see X, and what does it cost?". Everything below is derived from `EconomyFormulaConfig`, `CannonDatabase`, `CannonStatsRepository`, and the per-powerup `PowerupConfig` assets in `Assets/Resources/Data/PowerUps/`.

If the source data changes, regenerate this doc.

---

## 1. Cannons — the 7-tier ladder

### 1.1 Identity table

| Tier | `cannonId` | Display name | `cannonKey` (visuals) | Vibe |
|---|---|---|---|---|
| 1 | `CANNON_01` | Single Shot | `SingleShotCannon` | Reliable starter; one bullet, focused. |
| 2 | `CANNON_02` | Rapid Fire | `RapidFireCannon` | Higher fire rate, less per-bullet damage. |
| 3 | `CANNON_03` | Lucky | `LuckyCannon` | Crit-flavoured; coin-bonus chance. |
| 4 | `CANNON_04` | Double Cannon | `DoubleCannon` | Two parallel barrels, slower fire rate. |
| 5 | `CANNON_05` | Shotgun | `ShotgunCannon` | Spread; close-range power, falloff. |
| 6 | `CANNON_06` | Big Bartha | `BigBarthaCannon` | Heavy single-shot; siege flavour. |
| 7 | `CANNON_07` | Triple Cannon | `TripleCannon` | Three-barrel finale unit. |

Source: `Assets/Resources/Data/Inventory/CannonDatabase.asset` for keys, `EconomyFormulaConfig.cannonMultipliers[]` for IDs.

### 1.2 Recommended unlock progression

The `EconomyFormulaConfig.GetBaseUnlockCoinsForChapter(c) = 1500 × (1 + 0.10 × (c-1)^1.1)` formula assumes you pick which chapter each cannon unlocks at. Below is a paced ramp that aligns with the chapter-economy slice in `Progression_Reward_Economy.md` §3 — each unlock sits at the "you've earned about 4× this coin cost by the time you arrive" level, leaving the player a real choice between unlocking and upgrading what they already have.

| Tier | Cannon | Unlock at chapter | Why this chapter |
|---|---|---|---|
| 1 | Single Shot | **Ch1 (free, owned at start)** | Tutorial weapon; multiplier is 0 in config. |
| 2 | Rapid Fire | **Ch3** | First post-tutorial buy; player has ~58k coins by end of Ch2. |
| 3 | Lucky | **Ch6** | Mid Early-phase reward; coincides with first crit/coin meta lessons. |
| 4 | Double Cannon | **Ch10** | Coincides with chapter mid-boss debut; the "fork" weapon. |
| 5 | Shotgun | **Ch14** | First Mid-phase weapon; matches denser-spawn re-balance. |
| 6 | Big Bartha | **Ch20** | End-of-Mid-phase capstone; same chapter as first 200-coin-drop wave. |
| 7 | Triple | **Ch25** | Late-phase finale; ~5 chapters of soft-cap headroom for upgrades after. |

These should land in `CannonStatsRepository`'s cloud JSON as `unlockAtChapter` per cannon. If you change them, recompute the unlock-cost column below.

### 1.3 Unlock cost (computed from formula)

Formula: `unlockCoins = round(GetBaseUnlockCoinsForChapter(unlockChapter) × coinMultiplier)`. Gems alternative: `ceil(unlockCoins / 110)`.

| Tier | Cannon | Unlock Ch | `coinMultiplier` | Coins | Gems (alt) |
|---|---|---|---|---|---|
| 1 | Single Shot | 1 | 0.0 | **0** | 0 |
| 2 | Rapid Fire | 3 | 1.0 | **1,821** | 17 |
| 3 | Lucky | 6 | 1.2 | **2,861** | 27 |
| 4 | Double Cannon | 10 | 1.5 | **4,766** | 44 |
| 5 | Shotgun | 14 | 1.8 | **7,213** | 66 |
| 6 | Big Bartha | 20 | 2.5 | **13,325** | 122 |
| 7 | Triple | 25 | 3.5 | **22,561** | 206 |

Approximate coins available at unlock time (cumulative, perfect runs):

- Ch3 cumulative: ~58k → Rapid Fire (1.8k) is a **3% spend** of total earned. Trivial.
- Ch6 cumulative: ~145k → Lucky (2.9k) is **2%**.
- Ch10 cumulative: ~290k → Double (4.8k) is **1.6%**.
- Ch14 cumulative: ~590k (entered Mid) → Shotgun (7.2k) is **1.2%**.
- Ch20 cumulative: ~1.34M → Big Bartha (13.3k) is **1.0%**.
- Ch25 cumulative: ~1.74M → Triple (22.5k) is **1.3%**.

> **Flaw:** unlocks are too cheap relative to coin earn rate. See §1.6.

### 1.4 Upgrade cost (Levels 2 → 10)

Formula: `cost(toLevel) = round(400 × (toLevel - 0.5)^1.25 × coinMultiplier)`.  
Materials: 0 mats for L2-L5; ramp begins at L6 as `floor(0.8 × (fromLevel - 4)^1.2) × matMultiplier`.

#### Per-level base cost (mult = 1.0, before per-cannon scaling)

| toLevel | Base coins | Base materials |
|---|---|---|
| L2 | 664 | 0 |
| L3 | 1,257 | 0 |
| L4 | 1,915 | 0 |
| L5 | 2,620 | 0 |
| L6 | 3,358 | 0 |
| L7 | 4,152 | 1 |
| L8 | 4,964 | 2 |
| L9 | 5,804 | 4 |
| L10 | 6,668 | 5 |
| **Total to L10** | **31,402** | **12** |

#### Total upgrade cost per cannon, L1 → L10

| Tier | Cannon | `coinMult` | `matMult` | Coins to L10 | Materials to L10 |
|---|---|---|---|---|---|
| 1 | Single Shot | 0.0 | 0.0 | **0**¹ | **0**¹ |
| 2 | Rapid Fire | 1.0 | 1.0 | 31,402 | 12 |
| 3 | Lucky | 1.2 | 1.0 | 37,682 | 12 |
| 4 | Double Cannon | 1.5 | 1.2 | 47,103 | 14 |
| 5 | Shotgun | 1.8 | 1.5 | 56,524 | 18 |
| 6 | Big Bartha | 2.5 | 2.0 | 78,505 | 24 |
| 7 | Triple | 3.5 | 3.0 | 109,907 | 36 |

¹ **Bug**: see §1.6.

### 1.5 Concrete unlock + max-upgrade chapter mapping

Given a player who progresses linearly with no skips:

| Chapter milestone | What unlocks / what they spend on |
|---|---|
| Ch1 (start) | Own Single Shot. Begin upgrading it (~16k coins to L7 by Ch3). |
| Ch3 | Unlock Rapid Fire (1.8k). Start upgrading (~7k by Ch6). |
| Ch6 | Unlock Lucky (2.9k). |
| Ch10 | Unlock Double Cannon (4.8k). First mid-boss appears in this same chapter. |
| Ch14 | Unlock Shotgun (7.2k). Mid-phase economy kicks in. |
| Ch20 | Unlock Big Bartha (13.3k). Player should have most cannons at L7-L8. |
| Ch25 | Unlock Triple (22.5k). |
| Ch28 | Most upgrades should top out at L10 except Triple, which is the long-tail goal. |
| Ch30 | All cannons unlocked, most at L10. Triple at L8-L10 depending on grind. |

### 1.6 Cannon flaws to fix before ship

1. **`CANNON_01.coinMultiplier = 0` makes upgrades free.**  
   Single Shot's L1→L10 upgrade chain is free with current config because the same multiplier is used for unlock *and* upgrade cost. Either:
   - Set `CANNON_01.coinMultiplier = 0.6` (cheap but not free), and override unlock cost to 0 elsewhere, or
   - Split into `unlockMultiplier` and `upgradeMultiplier` arrays.

2. **Unlock pricing is far below earn rate.**  
   Even Triple at 22.5k is ~1% of cumulative earnings by Ch25. To make unlocks feel like commitments, multiply `GetBaseUnlockCoinsForChapter` baseline from 1500 → 4000–6000, *or* gate unlocks behind both currency and a milestone (e.g. "clear Ch5-L20 *and* pay 2.9k for Lucky"). The progression doc §5.1 covers the broader two-config drift; this is a symptom of it.

3. **Cannon stats live in cloud, prices live local.**  
   `unlockAtChapter` from the recommended chapter table above must be edited in Cloud Save's `cannon_stats` custom-id. If the cloud and `EconomyFormulaConfig.cannonMultipliers[]` order disagree, you ship a cannon at the wrong price. Add a startup validator (see Progression doc §5.2).

4. **No "early unlock with gems" path.**  
   The formula calculates a gem alternative (`coinCost / 110`) but `RewardManager` and `UnlockUI` don't currently expose it. Either ship the gem path (give the soft sink for hoarders) or remove `GetCannonUnlockGems`.

---

## 2. Powerups — the 29-strong spin pool

Powerups aren't bought; they're rolled in the slot machine after L5/L10/L15 (mid-chapter spins on slots 1/2/3) and on chapter rollover (slot 0). Unlock = entry in the spin pool. Two gates work jointly:

- **`unlockFromChapter`** — first chapter at which this powerup becomes a possible roll.
- **`spinUnlockLevel`** — first within-chapter spin slot at which it becomes a possible roll. Values: `0, 6, 11, 16` (1-based "after L5/L10/L15" + L1 chapter-start). I.e., `0` = available from chapter-start spin, `6` = only available from the L5+ mid-spin onward, etc.

Both gates must be cleared for a powerup to enter that spin's option pool.

### 2.1 The current pool, by category and rarity

#### Attack (16 powerups)

| Powerup | Rarity | Spin gate | Chapter gate | Initially unlocked? | Effect type |
|---|---|---|---|---|---|
| Power Surge | Common | 16 | 1 | ✓ | AttackIncreasePercent |
| Dual Shot | Common | 16 | 1 | ✓ | ExtraProjectile |
| Ricochet Rounds | Common | 0 | 1 | ✓ | BounceProjectile |
| Piercing Rounds | Rare | 0 | 1 | ✗ | PierceChance |
| Desperation Fury | Rare | 0 | 1 | ✗ | LowHPAttackBoost |
| Lightning Shot | Rare | 6 | 6 | ✗ | ChainLightning |
| Twin Spread | Rare | 6 | 8 | ✗ | SpreadShot |
| Incendiary Shot | Rare | 11 | 7 | ✗ | BurnDamageOverTime |
| Toxic Orb | Rare | 16 | 17 | ✗ | PoisonOrb |
| Ember Orb | Rare | 6 | 18 | ✗ | FireOrb |
| Laser Cannon | Epic | 11 | 11 | ✗ | LaserBeam |
| Frost Orb | Epic | 0 | 18 | ✗ | FreezeOrb |
| Blade Summoner | Epic | 0 | 1 | ✗ | PeriodicMeleeStrike |
| Retaliation Core | Epic | 16 | 13 | ✗ | DamageRetaliationBoost |
| Shadow Turret | Epic | 6 | 16 | ✗ | ShadowCompanion |
| Rocket Barrage | Legendary | 6 | 12 | ✗ | RocketAoE |
| Revenge Pulse | Legendary | 16 | 19 | ✗ | DamageReflectWave |

#### Defense (5 powerups)

| Powerup | Rarity | Spin gate | Chapter gate | Initially unlocked? | Effect type |
|---|---|---|---|---|---|
| Vitality Boost | Common | 16 | 1 | ✓ | MaxHealthIncrease |
| Phase Shield | Rare | 6 | 1 | ✓ | TemporaryInvincibility |
| Instant Heal | Common | 11 | 9 | ✗ | InstantHeal |
| Hit Barrier | Epic | 11 | 19 | ✗ | HitBasedShield |
| Spike Field | Epic | 0 | 16 | ✗ | AreaSpikeDamage |

#### Utility (8 powerups)

| Powerup | Rarity | Spin gate | Chapter gate | Initially unlocked? | Effect type |
|---|---|---|---|---|---|
| Pre-Boss Recovery | Common | 0 | 1 | ✗ | PreBossHealPercent |
| Freeze Breeze (Mana Acceleration asset) | Common | 11 | 15 | ✗ | ManaRegenIncrease |
| Life Steal | Rare | 6 | 14 | ✗ | LifeStealPercent |
| Flame Trail | Rare | 11 | 13 | ✗ | MovementBurnTrail |
| Focus Amplifier | Rare | 11 | 17 | ✗ | StationaryDamageRamp |
| Compact Frame | Epic | 16 | 15 | ✗ | HitboxReduction |
| Second Chance | Legendary | 0 | 20 | ✗ | ReviveWithHealthPercent |

> **Note:** the "Mana Acceleration" file's `displayName` is *Freeze Breeze* — likely a copy-paste from another asset. Either rename the file or the displayName so they match.

### 2.2 Production-ready unlock strategy (vibes-aware)

The principle: **rarity should track when the powerup becomes available, not just how often it's rolled.** The current configs are inconsistent — e.g. Blade Summoner is Epic but available from Ch1, while Toxic Orb is Rare but gated to Ch17. Below is the recommended re-baselining.

#### Recommended chapter-gate ranges by rarity

| Rarity | Earliest chapter | Reason |
|---|---|---|
| Common | 1–4 | Always-available staples; the bedrock of the spin pool. |
| Rare | 5–14 | Unlocked steadily across the Early phase. |
| Epic | 12–22 | Visible in Mid to Late; the meat of meta-build choices. |
| Legendary | 18–28 | Late-game payoff; players should still be unlocking new toys at Ch25+. |

#### Recommended spin-slot gates by rarity

| Rarity | Earliest spin slot | Why |
|---|---|---|
| Common | 0 | Always rollable; broad pool from L1 of a chapter. |
| Rare | 6 (after L5) | Lets the chapter-start spin be heavier on Common to keep early reads readable. |
| Epic | 11 (after L10) | Tie to the mid-chapter mid-boss; "boss spin" feels bigger. |
| Legendary | 16 (after L15) | Final-spin payoff. |

#### Powerup audit — current vs recommended

This table flags every powerup whose rarity disagrees with its gates. Bold rows are the high-impact mismatches.

| Powerup | Rarity | Current Ch | Rec. Ch | Current Spin | Rec. Spin | Action |
|---|---|---|---|---|---|---|
| Power Surge | Common | 1 | 1 | 16 | 0 | Move spin gate to 0; Common should be always-rollable. |
| Dual Shot | Common | 1 | 1 | 16 | 0 | Same as above. |
| Ricochet Rounds | Common | 1 | 1 | 0 | 0 | Keep. ✓ |
| Vitality Boost | Common | 1 | 1 | 16 | 0 | Move spin gate to 0. |
| Pre-Boss Recovery | Common | 1 | 1 | 0 | 0 | Keep. ✓ |
| Instant Heal | Common | 9 | 4 | 11 | 0 | Pull chapter gate forward; Common heals shouldn't gate to Ch9. |
| Mana Acceleration ("Freeze Breeze") | Common | 15 | 4 | 11 | 0 | Same — rarity says Common, gates say late-Mid. |
| **Piercing Rounds** | Rare | 1 | 6 | 0 | 6 | Promote both gates to Rare-band. |
| **Desperation Fury** | Rare | 1 | 8 | 0 | 6 | Same; "low-HP boost" lands harder mid-Early. |
| **Phase Shield** | Rare | 1 | 5 | 6 | 6 | Push chapter gate from 1 → 5; iconic Rare should require some progression. |
| Lightning Shot | Rare | 6 | 6 | 6 | 6 | Keep. ✓ |
| Twin Spread | Rare | 8 | 8 | 6 | 6 | Keep. ✓ |
| Incendiary Shot | Rare | 7 | 9 | 11 | 6 | Pull spin gate forward to 6 (matches other Rares). |
| Lifest Steal | Rare | 14 | 12 | 6 | 6 | Pull chapter gate forward; 14 is on the late side. |
| Flame Trail | Rare | 13 | 13 | 11 | 6 | Pull spin gate to 6; chapter is fine. |
| Toxic Orb | Rare | 17 | 14 | 16 | 6 | Both gates too late for a Rare; pull both forward. |
| Ember Orb | Rare | 18 | 14 | 6 | 6 | Pull chapter gate forward. |
| Focus Amplifier | Rare | 17 | 14 | 11 | 6 | Pull both forward slightly. |
| Laser Cannon | Epic | 11 | 14 | 11 | 11 | Push chapter gate from 11 → 14 (Epic-band start). |
| Frost Orb | Epic | 18 | 18 | 0 | 11 | Pull spin gate up to Epic-band 11. |
| **Blade Summoner** | Epic | 1 | 14 | 0 | 11 | Both gates wrong for Epic; this is the most surprising mismatch. |
| Retaliation Core | Epic | 13 | 14 | 16 | 11 | Pull spin gate forward to 11. |
| Shadow Turret | Epic | 16 | 16 | 6 | 11 | Push spin gate up to Epic-band. |
| Hit Barrier | Epic | 19 | 19 | 11 | 11 | Keep. ✓ |
| Compact Frame | Epic | 15 | 16 | 16 | 11 | Pull spin gate forward. |
| **Spike Field** | Epic | 16 | 18 | 0 | 11 | Push spin gate up; Epic shouldn't be in chapter-start spin. |
| Rocket Barrage | Legendary | 12 | 20 | 6 | 16 | Push both gates significantly later — Legendaries should not unlock mid-Early. |
| Revenge Pulse | Legendary | 19 | 22 | 16 | 16 | Push chapter gate slightly later. |
| **Second Chance** | Legendary | 20 | 22 | 0 | 16 | Push spin gate up — Legendary shouldn't roll on chapter-start spin. |

> **Bold rows** are the four most consequential changes. Re-tuning these alone gets you 80% of the rarity-coherence win.

### 2.3 Powerup rollout cadence — what the player feels

After the recommended re-baselining, the player's experience by chapter:

| Chapter range | New powerups unlocked | Pool size at chapter end |
|---|---|---|
| Ch1–4 | All 7 Common (Power Surge, Dual Shot, Ricochet, Vitality, Pre-Boss Recovery, Instant Heal, Freeze Breeze) | 7 |
| Ch5–9 | 4 Rares (Phase Shield Ch5, Piercing Ch6, Lightning Ch6, Desperation Fury Ch8, Twin Spread Ch8, Incendiary Ch9) | 13 |
| Ch10–14 | 4 Rares + 2 Epics (Lifest Steal Ch12, Flame/Toxic/Focus Ch13-14, Laser Ch14, Blade Summoner Ch14) | 19 |
| Ch15–19 | 5 Epics (Shadow Turret Ch16, Compact Frame Ch16, Frost Orb Ch18, Spike Field Ch18, Hit Barrier Ch19) | 24 |
| Ch20–25 | 1 Epic + 1 Legendary (Rocket Barrage Ch20, Retaliation Core moved to Ch14 already) | 25 |
| Ch26–30 | 2 Legendaries (Revenge Pulse Ch22, Second Chance Ch22) | 27 |

Total accessible by Ch30: **27** of the 29 (with 2 in flight near the end). That's a steady drip-feed where the player is *never* in a chapter without something new to roll for.

### 2.4 Powerup flaws to fix before ship

1. **"Initially unlocked" + chapter-gated is contradictory.**  
   `Power Surge`, `Dual Shot`, `Vitality Boost` and other Commons have `initiallyUnlocked: 1` *and* `unlockFromChapter: 1`. The unlock-from-chapter field is redundant for these. Pick one — keep `initiallyUnlocked` and remove `unlockFromChapter` (or set it to 0) to avoid the impression that the player "unlocks them again" at Ch1.

2. **`spinUnlockLevel` field semantics.**  
   The encoded values 0/6/11/16 are not 0-based slot indices; they're "1-based level after which this slot becomes available." This is confusing. Rename to `firstAvailableAtLevel` or convert to a slot index `firstAvailableAtSpinSlot ∈ {0,1,2,3}` and document in `PowerupConfig`.

3. **Mana Acceleration / Freeze Breeze name mismatch.**  
   The asset filename is `Mana Acceleration.asset` but `displayName: Freeze Breeze`. Fix one to match.

4. **No category-balance check on spin pool.**  
   `GetAvailablePowerUpForSpin` (in `GameProgress.cs`) builds option lists without checking that the player isn't getting all-Attack or all-Defense rolls. Sequential bad-luck protection should at minimum guarantee one Defense or Utility option in any 3-option spin once chapter ≥ 5.

5. **Legendary drop probability.**  
   Spin pool today doesn't surface drop weights — every eligible powerup is equiprobable. Even after the rarity re-baselining, a Ch25 player will see Rocket Barrage in 1/27 of their slot 16 rolls (≈3.7%). That's likely too rare. Layer drop weights by rarity (e.g. Common ×3, Rare ×2, Epic ×1, Legendary ×0.5) to make Legendaries feel earned but reachable.

---

## 3. Action items, ordered by ROI

Highest leverage first.

| # | Item | Owner | Why |
|---|---|---|---|
| 1 | Re-baseline the four bolded powerup rows (§2.2) — Phase Shield, Blade Summoner, Spike Field, Rocket Barrage / Second Chance. | Design | Removes the four most surprising rarity-vs-availability mismatches. |
| 2 | Fix `CANNON_01.coinMultiplier = 0` upgrade-cost bug. Split unlock vs upgrade multipliers. | Eng | Single Shot's whole upgrade tree is currently free. |
| 3 | Multiply baseline unlock cost from 1500 → 4000–6000. | Design + Eng | Otherwise unlocks are <2% of cumulative earn — non-event. |
| 4 | Add startup validator for `CannonDatabase` ↔ `CannonStatsRepository` ↔ `EconomyFormulaConfig.cannonMultipliers[]` consistency. | Eng | Cloud-vs-local drift will absolutely happen otherwise. |
| 5 | Add rarity drop-weights to the spin pool builder. | Eng | Without this, Legendary feel exactly as common as Common. |
| 6 | Wire `EconomyFormulaConfig.GetFirstClearGems` into `RewardManager.HandleLevelCompleted` (replaces flat `gemsOnFirstTimeClear`). | Eng | Already-implemented chapter ramp is going unused. |
| 7 | Add `levelInChapter` factor to in-level coin drops. | Eng | Cheap fix; closes the "L20 pays the same as L1" perception. |
| 8 | Fix Mana Acceleration asset name vs displayName. | Design | 30-second polish item. |

---

*Maintain this doc when:*
- A new cannon or powerup ships.
- `EconomyFormulaConfig.cannonMultipliers[]` changes.
- A rarity, chapter gate, or spin gate moves.
