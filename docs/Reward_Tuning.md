# Bird Hunter — Reward Tuning

**Version:** 1.0
**Last Updated:** 2026-05-04
**Scope:** Why the reward system was over-paying coins and gems, and how the live `GameplayRewardConfig` was re-tuned to extend playtime.

This doc pairs with:
- `Shop_Pricing.md` — the cost side (cannons, upgrades, IAP).
- `Progression_Reward_Economy.md` — the source-of-truth model.
- `Cannon_Powerup_Unlock_Plan.md` — unlock chapters and prices.

---

## 1. The over-pay diagnosis

The reward system has **two configs that both define coin/gem rewards**:

| Config | Asset | Used at runtime? |
|---|---|---|
| `GameplayRewardConfig` | `Assets/Resources/Data/New Gameplay Reward Config.asset` | **Yes** — `RewardManager` exclusively |
| `EconomyFormulaConfig` (reward methods) | `Assets/Resources/Data/EconomyFormulaConfig.asset` | **No** — dead code (cannon upgrade formula is the only live path) |

Verified — every reward callsite goes through `GameplayRewardConfig`:

```text
RewardManager.cs:80    rewardConfig.GetRandomCoinDrop(chapter)
RewardManager.cs:87    rewardConfig.GetRandomGemDrop(chapter)
RewardManager.cs:133   rewardConfig.GetLevelCompletionCoins(...)
RewardManager.cs:141   rewardConfig.GetFirstTimeClearGems()
RewardManager.cs:147   rewardConfig.GetPowerRefund()
```

So `GameplayRewardConfig` is the only knob that matters.

### 1.1 Earn-rate model (pre-fix)

`RewardManager.EstimateExpectedCoinsForLevel` (line 230) baselines **15 eggs per level**. Using that with the original asset values:

| Phase | Drops × 15 | Completion (avg perf) | Per level | Per chapter (×20) | Cumulative |
|---|---|---|---|---|---|
| Early (Ch1–10) | 55 × 15 = 825 | 550 + 75 = 625 | **1,450** | 29,000 | 290,000 |
| Mid (Ch11–20) | 150 × 15 = 2,250 | 1,320 + 180 = 1,500 | **3,750** | 75,000 | 1,040,000 |
| Late (Ch21–30) | 325 × 15 = 4,875 | 2,750 + 375 = 3,125 | **8,000** | 160,000 | **2,640,000** |

**Total cost to max everything** (cloud-priced, post-unlock-fix):
- Unlocks: 0 + 6,000 + 9,500 + 16,000 + 24,000 + 44,500 + 75,000 = **175,000**
- Upgrades L1→L10 across 7 cannons: **379,964**
- **Total: ~555,000 coins.**

A Ch30 player held **~5× the coins they would ever spend**. By **Ch15** they could already afford everything in the game including all upgrades. This is the over-rewarding the user observed.

### 1.2 Gems — same picture

| Source | Per level | Lifetime (Ch1–30, 600 levels) |
|---|---|---|
| First-time clear | flat 10 | 6,000 |
| Early in-level (3% × 1) | 0.45 | 90 |
| Mid in-level (5% × 1.5) | 1.125 | 225 |
| Late in-level (8% × 2.5) | 3.0 | 600 |
| **Total (no ads)** | | **~6,915** |

Gem alt-cost for **all 7 cannons**: 0 + 55 + 87 + 146 + 219 + 405 + 682 = **1,594 gems**. Lifetime supply was **4.3× target spend**. Players had no reason to buy gems.

---

## 2. The cuts

Target: **Ch30 cumulative coins ≈ 1.2M** (≈ 2× target spend — a comfortable buffer, but no "max everything by Ch15"). Apply ~50% cuts across all three phases. Tighten gems further so cannon gem-alt becomes a meaningful sink.

### 2.1 Coin drops

| Phase | Old | New | Cut |
|---|---|---|---|
| Early | 30–80 | **15–40** | −50% |
| Mid | 100–200 | **50–110** | −46% |
| Late | 250–400 | **110–200** | −55% |

### 2.2 Completion bonus

| Phase | Old base + perf | New base + perf | Cut |
|---|---|---|---|
| Early | 550 + 50–100 | **280 + 30–60** | −50% |
| Mid | 1,320 + 120–240 | **700 + 80–150** | −47% |
| Late | 2,750 + 250–500 | **1,200 + 150–280** | −56% |

### 2.3 Gems

| Field | Old | New | Cut |
|---|---|---|---|
| `gemsOnFirstTimeClear` | 10 | **4** | −60% |
| Early drop chance / amount | 3% × 1 | **2% × 1** | −33% |
| Mid drop chance / amount | 5% × 1–2 | **3% × 1** | −60% |
| Late drop chance / amount | 8% × 2–3 | **5% × 1–2** | −63% |
| `gemsPerAdReward` | 10 | **5** | −50% |

### 2.4 Power

| Field | Old | New | Cut |
|---|---|---|---|
| `powerRefundChance` | 25% | **20%** | −20% |
| `powerRefundAmount` | 5 | 5 | unchanged |

Power is the energy gate. A small cut to refund chance compounds across 600 levels, slowing the burn modestly without making the player sit watching a timer.

---

## 3. New earn-rate model (post-fix)

| Phase | Drops × 15 | Completion (avg) | Per level | Per chapter | Cumulative |
|---|---|---|---|---|---|
| Early (Ch1–10) | 27.5 × 15 = 412 | 280 + 45 = 325 | **737** | 14,740 | 147,400 |
| Mid (Ch11–20) | 80 × 15 = 1,200 | 700 + 115 = 815 | **2,015** | 40,300 | 550,400 |
| Late (Ch21–30) | 155 × 15 = 2,325 | 1,200 + 215 = 1,415 | **3,740** | 74,800 | **1,298,400** |

| Phase | Gems/level | Per chapter | Cumulative (lifetime) |
|---|---|---|---|
| First-clear (4 × 600 levels) | – | – | 2,400 |
| Early (2% × 1) | 0.30 | 6 | 60 |
| Mid (3% × 1) | 0.45 | 9 | 90 |
| Late (5% × 1.5) | 1.125 | 22.5 | 225 |
| **Total (no ads)** | | | **~2,775** |

---

## 4. Spend-vs-earn validation

How tight is the new economy at each unlock chapter?

| Chapter | Cumulative coins | Total target spend at that point¹ | Spend / Earn |
|---|---|---|---|
| Ch3 | 44,200 | 6,000 (CANNON_02) | 14% |
| Ch6 | 88,400 | 21,000 (+ CANNON_03 + CANNON_02 to L5) | 24% |
| Ch10 | 147,400 | 60,000 (+ CANNON_04 + 2 cannons L7) | 41% |
| Ch14 | 308,600 | 130,000 (+ CANNON_05 + 3 cannons L8) | 42% |
| Ch20 | 550,400 | 290,000 (+ CANNON_06 + most cannons L9) | 53% |
| Ch25 | 924,400 | 460,000 (+ CANNON_07 + most cannons L10) | 50% |
| Ch30 | 1,298,400 | 555,000 (everything maxed) | 43% |

¹ Target spend = unlocks paid + a reasonable upgrade-along-the-way pace. Not "everything maxed at this chapter" — just what the player would realistically have bought.

The spend-to-earn ratio is healthy throughout — players always have enough to make a meaningful purchase, but never enough to max-everything-and-coast. That's the playtime extension.

### 4.1 What this changes for the player

- **Ch1–Ch10:** broadly unchanged. New player still earns more than they spend, gets the unlock-and-upgrade dopamine loop.
- **Ch11–Ch20:** slowdown is most visible here. Previously a Mid-phase player drowned in coins; now they have to pick: unlock the next cannon, or upgrade what they have. That choice is the game.
- **Ch21–Ch30:** Triple unlock at Ch25 (75k coins) finally feels like a real spend (~4 chapters' earn rate vs the prior 0.5 chapter). Maxing all upgrades requires playing through Ch30.

### 4.2 Gem-pack pull

Lifetime gem income drops from ~7k to ~2.8k. Now any player who wants to skip the grind on a cannon (gem alt-path) has to either grind further or buy a gem pack. The 500-gem starter pack ($0.99) finally has a clear use case: "a Mid-phase cannon unlock, today, instead of next week."

---

## 5. What we did **not** change

- **Cannon upgrade costs** — already healthy (`EconomyFormulaConfig.GetUpgradeCostCoins`). The cuts above slow how fast you can afford them, which is the right lever.
- **Chest payouts** (`EconomyFormulaConfig.GetChestCoinsMin/Max`) — flagged as dead code (no runtime caller). If chests get wired up later, halve the base from 3,000 to 1,500 first. Tracked separately.
- **Force-destroy penalty** (`forceDestroyRewardMultiplier: 0.5`, suppress flags) — already a meaningful drag on grind-by-letting-grace-expire; left as-is.
- **Ad coin multiplier** (`coinMultiplierForAd: 2`) — keep. Ads are the only non-paid bypass; a 2× bonus is industry standard.

---

## 6. Soft-launch validation

Add these rows to `Soft_Launch_Verification.md` §6 — earn-rate sanity check should now match:

| Window | Old cumulative | **New cumulative (target)** |
|---|---|---|
| End Ch1 (L20) | ~14k–18k | **~7k–9k** |
| End Ch2 (L40) | ~36k–46k | **~18k–22k** |
| End Ch3 (L60) | ~58k–72k | **~30k–46k** |
| End Ch10 | ~290k | **~145k–155k** |
| End Ch20 | ~1.04M | **~540k–565k** |
| End Ch30 | ~2.64M | **~1.25M–1.35M** |

A soft-launch player who's earning ≥30% above these is on the old curve — confirm `New Gameplay Reward Config.asset` shipped correctly.

---

## 7. Open follow-ups

| # | Item | Owner |
|---|---|---|
| 1 | Delete dead reward methods from `EconomyFormulaConfig.cs` (drift risk; methods listed in §1) | Eng |
| 2 | Wire up chests with halved base (3,000 → 1,500) if/when the chest UI ships | Eng + Design |
| 3 | Watch Day 7/14 retention for "stuck at Mid phase" — if >40% of Ch15 players churn for economy reasons, soften Mid completion bonus by +20% | Live-ops |
| 4 | Migrate `GameplayRewardConfig` to Remote Config so live-ops can re-tune without a client patch (same play as the cannon prices → cloud) | Eng |

---

*Maintain this doc when:*
- A `GameplayRewardConfig` field changes.
- The 15-eggs-per-level baseline in `RewardManager.EstimateExpectedCoinsForLevel` changes.
- A new reward source (login bonus, daily reward, battlepass) is added.
