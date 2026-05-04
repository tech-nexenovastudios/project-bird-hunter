# Bird Hunter — Shop Pricing

**Version:** 1.0
**Last Updated:** 2026-05-04
**Scope:** Canonical pricing for everything the player can spend or buy in the shop. Numbers below are derived from `EconomyFormulaConfig`, `GameplayRewardConfig`, `GemsBuyManager`, and the Unity Economy Dashboard slots wired in `GoldPurchaseManager` / `PowerPurchaseManager`.

This doc pairs with `Cannon_Powerup_Unlock_Plan.md` (gameplay-side unlocks) and `Progression_Reward_Economy.md` (earn-rate model). When any of those change, regenerate this doc.

---

## 0. Currency model

| Currency | ID (Unity Economy) | Where it comes from | Where it's spent |
|---|---|---|---|
| **Coins** | `GOLD` | In-level drops, level completion, ad x2, chests | Cannon unlocks, cannon upgrades |
| **Gems** | `GEM` | First-time clears, in-level drops (3–10%), ad reward, IAP | Gem-bundle gold packs, gem-bundle power packs, optional gem-unlock for cannons |
| **Power** | `POWER` | Refund on completion (20–30%), in-level drops (4–10%), ad refill, gem purchase | Level entry (5/run) |

**Soft → hard rate (anchor):** `1 gem = 110 coins`. Documented here and in `docs/cloud-save/cannon_stats.json`. There is no runtime constant for it — every gem-vs-coin price (gold packs, gem-alt for cannon unlocks) is hand-anchored to this rate. If you change it, search `coinsPerGem` and `1 gem = 110` across docs.

---

## 1. IAP — Gem packs (real money)

Source: `Assets/Scripts/Shop/GemsBuyManager.cs:51`. Product IDs are formed as `{gemCount}_gems` and the actual price is fetched from Google Play / App Store at runtime (`OnInitialized`, line 85).

| Tier | Pack | Product ID | Target USD¹ | $/100 gems | Position |
|---|---|---|---|---|---|
| 1 | **500 gems** | `500_gems` | $0.99 | $0.198 | Starter, low commit |
| 2 | **7,000 gems** | `7000_gems` | $4.99 | $0.071 | "Most popular" — sweet spot |
| 3 | **16,000 gems** | `16000_gems` | $9.99 | $0.062 | Mid value pack |
| 4 | **40,000 gems** | `40000_gems` | $19.99 | $0.050 | Whale entry |
| 5 | **110,000 gems** | `110000_gems` | $49.99 | $0.045 | Top tier |

¹ **Target** USD prices — set in Google Play Console / App Store Connect, not in code. Verify against the live store config; storefronts display localized prices via `storeProduct.metadata.localizedPrice`.

### 1.1 Coin-equivalent value of each gem pack

Using the in-game rate `1 gem = 110 coins`:

| Pack | Gems | Coins (equivalent) | What it unlocks (post-fix) |
|---|---|---|---|
| 500 gems | 500 | 55,000 | Rapid Fire (Ch3) + Lucky (Ch6) with change |
| 7,000 gems | 7,000 | 770,000 | All Early/Mid cannons + most upgrades to L7 |
| 16,000 gems | 16,000 | 1,760,000 | Roughly Ch1–Ch20 fully maxed |
| 40,000 gems | 40,000 | 4,400,000 | Triple unlock + most cannons to L10 |
| 110,000 gems | 110,000 | 12,100,000 | "I want everything maxed forever" |

### 1.2 Pricing flaw watch

- **Pack 1 ($0.99 / 500 gems)** is 4× more expensive per gem than Pack 5. That ratio is fine — most F2P stores use 4–10× spread to make whales feel rewarded — but it's worth verifying mid-funnel: too steep and Pack 1 feels like a trap; too flat and Pack 5 has no pull.
- The gap between Pack 2 (7k) and Pack 3 (16k) is narrow ($0.009/100 gems). Either widen Pack 3 to ~20k or tighten Pack 2 to ~5k so each tier feels distinct.

---

## 2. Gold packs (gems → coins)

Source: `Assets/Scripts/Shop/GoldPurchaseManager.cs`. Each card is a `MakeVirtualPurchaseAsync(virtualPurchaseId)` call against a Unity Economy virtual purchase configured in the Dashboard.

**Recommended catalog**, anchored on `coinsPerGem = 110` (then the bigger packs add a small bonus to incentivize stacking):

| Card | Gems cost | Coins granted | Effective rate | Bonus vs base |
|---|---|---|---|---|
| Small | 50 | 5,500 | 110/gem | 0% |
| Medium | 200 | 24,000 | 120/gem | +9% |
| Large | 800 | 104,000 | 130/gem | +18% |
| Mega | 2,000 | 280,000 | 140/gem | +27% |

> **Soft-launch action:** confirm the Dashboard `virtualPurchaseId`s match the row order in `purchaseCards[]` — Inspector mismatches will silently grant the wrong amount. See `Soft_Launch_Verification.md` §4.

---

## 3. Power packs (gems → power)

Source: `Assets/Scripts/Shop/PowerPurchaseManager.cs`.

Each level costs `powerCostPerAttempt = 5` (`GameplayRewardConfig.cs:50`) with a 20–30% refund chance, so expected net spend per level is ~3.5–4 power. A 5-power refill = ~1.4 levels, a 25-power = ~7 levels.

**Recommended catalog:**

| Card | Gems cost | Power granted | Levels of play (avg) |
|---|---|---|---|
| Small | 10 | 5 | 1.4 |
| Medium | 35 | 25 | 7 |
| Large | 60 | 60 | 17 |
| Mega | 150 | 200 | 57 |

**Ad alternative:** `GameplayRewardConfig.coinMultiplierForAd = 2`, `powerPerAdRefill = 5`, `gemsPerAdReward = 10`. Watching one rewarded ad = 5 power = 1 small pack. So the Small pack at 10 gems must feel meaningfully *better* than waiting/watching, otherwise nobody buys it. Two ways to make that gap real: (a) make Small grant 7 power instead of 5; (b) add a tiny gem bonus on the Small pack.

---

## 4. Cannon unlocks (post-fix)

**Source of truth:** Unity Cloud Save → `cannon_stats` custom-id → per-cannon `baseCoinsRequired` / `baseGemsRequired`. Runtime reads these directly via `CannonInventoryService.GetUnlockCoinCost`. There is no longer a formula in `EconomyFormulaConfig` for unlock prices — values are designer-set and shipped via `docs/cloud-save/cannon_stats.json`.

Why cloud, not formula: live-ops can re-tune any cannon without a client patch, and there's no two-config drift risk.

| Tier | Cannon | Cloud sub-key | Unlock Ch | Coins | Gems (alt) |
|---|---|---|---|---|---|
| 1 | Single Shot | `SingleShotCannon` | 1 | **0** | 0 |
| 2 | Rapid Fire | `RapidFireCannon` | 3 | **6,000** | 55 |
| 3 | Lucky | `LuckyCannon` | 6 | **9,500** | 87 |
| 4 | Double Cannon | `DoubleCannon` | 10 | **16,000** | 146 |
| 5 | Shotgun | `ShotgunCannon` | 14 | **24,000** | 219 |
| 6 | Big Bartha | `BigBarthaCannon` | 20 | **44,500** | 405 |
| 7 | Triple | `TripleCannon` | 25 | **75,000** | 682 |

Spend as % of cumulative earnings at the unlock chapter:

| Tier | Spend at unlock | Cumulative earned | % of total |
|---|---|---|---|
| 2 | 6,000 | ~58k | 10.3% |
| 3 | 9,500 | ~145k | 6.6% |
| 4 | 16,000 | ~290k | 5.5% |
| 5 | 24,000 | ~590k | 4.1% |
| 6 | 44,500 | ~1.34M | 3.3% |
| 7 | 75,000 | ~1.74M | 4.3% |

This is the target band — meaningful but not blocking. Anything <2% reads as "free", anything >15% reads as "I have to grind first."

> **Pre-fix snapshot for context:** the old formula baseline of 1500 produced ~1–2% spend bands (Triple at Ch25 was ~22.5k coins ≈ 1.3% of cumulative earn). The values above were chosen as designer-friendly round numbers in the 3–10% band and baked into cloud save as the single source of truth. See `Cannon_Powerup_Unlock_Plan.md` §1.6 and `docs/cloud-save/cannon_stats.json`.

---

## 5. Cannon upgrades (L1 → L10)

Source: `EconomyFormulaConfig.GetUpgradeCostCoins(cannonId, toLevel)`. Per-level base × `coinMultiplier`. Materials follow `coinMaterials` curve × `matMultiplier`.

#### Per-level base (mult = 1.0)

| toLevel | Base coins | Base mats |
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
| **L1→L10 total** | **31,402** | **12** |

#### Total cost L1 → L10 per cannon (post-fix)

| Tier | Cannon | `coinMult` | `matMult` | Coins to L10 | Mats to L10 |
|---|---|---|---|---|---|
| 1 | Single Shot | 0.6 | 0.6 | 18,841 | 7 |
| 2 | Rapid Fire | 1.0 | 1.0 | 31,402 | 12 |
| 3 | Lucky | 1.2 | 1.0 | 37,682 | 12 |
| 4 | Double Cannon | 1.5 | 1.2 | 47,103 | 14 |
| 5 | Shotgun | 1.8 | 1.5 | 56,524 | 18 |
| 6 | Big Bartha | 2.5 | 2.0 | 78,505 | 24 |
| 7 | Triple | 3.5 | 3.0 | 109,907 | 36 |

**Total to fully max all 7 cannons:** ~380k coins + ~123 materials.

---

## 6. Chests

Source: `EconomyFormulaConfig.GetChestCoinsMin/Max(chapter)`. Random within `[min, max]` where `max = min × 1.5`.

| Chapter | Min coins | Max coins | Median |
|---|---|---|---|
| Ch1 | 3,000 | 4,500 | 3,750 |
| Ch5 | 3,778 | 5,667 | 4,722 |
| Ch10 | 5,061 | 7,591 | 6,326 |
| Ch15 | 6,471 | 9,706 | 8,088 |
| Ch20 | 7,961 | 11,941 | 9,951 |
| Ch25 | 9,510 | 14,265 | 11,887 |
| Ch30 | 11,107 | 16,660 | 13,884 |

Chest coin payouts roughly equal one Mid-phase level's earnings — acceptable as a "weekly reward" or campaign-completion drop.

---

## 7. Ad rewards

Source: `GameplayRewardConfig.cs:55-58` and `Services/AdManager.cs`.

| Ad type | Reward | Cooldown / cap |
|---|---|---|
| Coin x2 boost | Multiplies completion coins by 2 | Once per level completion |
| Power refill | +5 power | Configurable; recommend cooldown of 2–4h |
| Gem reward | +10 gems | Once per day (cap recommended) |
| Remove Ads (IAP) | Persistently removes interstitials | One-time purchase |

**Recommended Remove-Ads price:** $4.99 — same tier as the 7k gem pack, so the player faces a real choice between currency and convenience.

---

## 8. Daily / login / one-time bundles (recommended additions)

Not currently in the code. If you ship them, target these envelopes:

| Bundle | Suggested contents | Suggested price | Cadence |
|---|---|---|---|
| Daily reward | 200–500 coins; 1–2 gems on streak day 7 | Free | Daily |
| New player starter | 5,000 coins + 50 gems + 10 power | $0.99 (limited 7d) | Once per account |
| Weekend gem deal | 1.5× gems on the 7k or 16k pack | Same USD | Fri–Sun |
| Cannon unlock bundle | Ch6 Lucky cannon + 30 gems | $1.99 | Once after Ch5 cleared |

The starter bundle in particular fixes a soft-launch funnel risk: the gem packs jump from $0.99 (500 gems) straight to $4.99 (7k gems), with no $1.99 step between.

---

## 9. Pricing principles for tuning

1. **Coins are abundant; gems are precious.** Target a 50–80× gem-to-coin abundance ratio in earn-side flow. (Earn ~10k coins per Mid-phase level vs 1–3 gems.)
2. **Unlocks should sit in the 3–10% of-cumulative-earn band.** Below 2% feels meaningless; above 15% feels gating. See §4.
3. **Bigger packs always have a per-unit discount.** ~10–25% extra over the smallest pack is the standard ladder.
4. **Every pack must beat the ad alternative.** If 1 ad = 5 power, the Small power pack better feel like more than that.
5. **Always have a $0.99–1.99 IAP step.** Conversion-rate optimization 101 — without it, the funnel jumps straight from "free user" to "$4.99 user."

---

*Maintain this doc when:*
- A gem-pack count changes in `GemsBuyManager.PopulateProductIds`.
- A virtual purchase ID is added/changed in the Unity Economy Dashboard.
- `EconomyFormulaConfig.cannonMultipliers[]` or the unlock-base constant changes.
- An IAP catalog row is added in `IAPProductCatalog.json`.
