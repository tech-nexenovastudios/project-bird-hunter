# Bird Hunter — Soft Launch Verification

**Version:** 1.0
**Last Updated:** 2026-05-04
**Scope:** Pre-soft-launch verification checklist for the live build. Anything that touches money, progression, cloud-save, or first-run experience belongs here. Run this end-to-end before flipping to a soft-launch territory.

This doc lives next to:
- `Shop_Pricing.md` — what the player should be charged
- `Cannon_Powerup_Unlock_Plan.md` — when content should unlock
- `Progression_Reward_Economy.md` — how coins/gems/power flow

If a row in the checklist disagrees with one of those docs, **the docs are the contract**. Fix the build to match.

---

## 1. How to use this checklist

Each section is a phase. Walk through them in order on a clean, signed device build (not the Editor) before the soft-launch ship.

For each row:
- **Pass:** record the number you observed and tick the box.
- **Fail:** open a blocker bug; do not ship until resolved or explicitly waived by Design + Eng leads.
- **Waived:** leave a one-line justification next to the row.

Use the same checklist on every soft-launch territory build (KSA / PH / VN / CA, etc.). Numbers must be reproducible across two consecutive runs on a fresh install.

---

## 2. Pre-flight (Editor + cloud)

Cannon unlock prices are read at runtime from Cloud Save `cannon_stats`. The `EconomyFormulaConfig` formula is a designer tool only. Most pre-flight is therefore cloud-side.

| # | Check | How | Expected |
|---|---|---|---|
| 2.1 | Cloud Save `cannon_stats` custom-id has 7 entries | Unity Cloud Save dashboard → Game Data | sub-keys `SingleShotCannon`, `RapidFireCannon`, `LuckyCannon`, `DoubleCannon`, `ShotgunCannon`, `BigBarthaCannon`, `TripleCannon` |
| 2.2 | Each entry's pricing fields match `docs/cloud-save/cannon_stats.json` | Compare `unlockAtChapter`, `baseCoinsRequired`, `baseGemsRequired` per sub-key | Exact match — see §3 below |
| 2.3 | Each entry's gameplay-stat fields are non-zero | Dashboard | `baseDamage`, `baseHealth`, `baseFireRate`, `baseBulletSpeed`, `baseCannonSpeed` all > 0 |
| 2.4 | `baseGemsRequired ≈ ceil(baseCoinsRequired / 110)` per cannon | `docs/cloud-save/cannon_stats.json` and dashboard | matches the table in §3 below |
| 2.5 | All 7 cannons have an entry in `CannonDatabase.asset` | Open asset | 7 entries, `cannonKey`s match cloud sub-keys exactly |
| 2.6 | `CannonInventoryService.GetUnlockCoinCost` reads DTO, not formula | grep `GetUnlockCoinCost` | returns `dto.baseCoinsRequired` directly |
| 2.7 | All 29 powerups present in `PowerupDatabase.asset` | Open asset | 29 entries; no duplicate IDs |
| 2.8 | Mana Acceleration / Freeze Breeze name fix applied | `Assets/Resources/Data/PowerUps/Mana Acceleration.asset` | `displayName` matches filename |
| 2.9 | No unstaged edits in `Assets/Resources/Data/` | `git status` | Clean — config drift kills reproducibility |
| 2.10 | `docs/cloud-save/cannon_stats.json` last-updated date matches the latest cloud push | compare `_meta.lastUpdated` and dashboard last-modified | Same day or doc newer |

---

## 3. Cannon unlock pricing (live device)

For each cannon, verify the displayed unlock cost matches the cloud value. Test on a fresh install — local cache should be empty so the price is loaded straight from cloud.

**Test:** progress to the listed chapter, open the Cannon shop, screenshot the unlock cost. Numbers must equal the corresponding entry in `docs/cloud-save/cannon_stats.json`.

| Cannon | Cloud sub-key | Chapter | Expected coins | Expected gems alt | ☐ |
|---|---|---|---|---|---|
| Single Shot | `SingleShotCannon` | 1 | 0 (owned at start) | 0 | ☐ |
| Rapid Fire | `RapidFireCannon` | 3 | 6,000 | 55 | ☐ |
| Lucky | `LuckyCannon` | 6 | 9,500 | 87 | ☐ |
| Double Cannon | `DoubleCannon` | 10 | 16,000 | 146 | ☐ |
| Shotgun | `ShotgunCannon` | 14 | 24,000 | 219 | ☐ |
| Big Bartha | `BigBarthaCannon` | 20 | 44,500 | 405 | ☐ |
| Triple | `TripleCannon` | 25 | 75,000 | 682 | ☐ |

If any number differs, file a bug and check in this order:
1. Cloud Save `cannon_stats[<sub-key>].baseCoinsRequired` matches `docs/cloud-save/cannon_stats.json`.
2. Cloud Save `cannon_stats[<sub-key>].unlockAtChapter` matches the chapter column.
3. The shop UI is reading via `CannonInventoryService.GetUnlockCoinCost(key)` and not from a hard-coded value or the deprecated formula path.
4. The device's Cloud Save cache isn't stale — clear app data and re-fetch.

### 3.1 No-formula sanity check

Confirm that no unlock-formula symbols exist at all. From the repo root:

```bash
grep -rn "GetCannonUnlockCoins\|GetCannonUnlockGems\|GetBaseUnlockCoinsForChapter\|unlockCoinMultiplier" Assets --include="*.cs"
```

Expected: **zero matches.** These were deleted when unlock pricing moved to cloud save. Any hit means a stale call site or a partial revert.

---

## 4. Cannon upgrade pricing (live device)

Spot-check L2, L7, and L10 for one Common, one Mid, one Late cannon. The numbers below are from `GetUpgradeCostCoins(cannonId, toLevel)`.

| Cannon | L2 (coins) | L7 (coins, mats) | L10 (coins, mats) | ☐ |
|---|---|---|---|---|
| CANNON_02 (Rapid Fire) | 664 | 4,152 / 1 mat | 6,668 / 5 mats | ☐ |
| CANNON_05 (Shotgun) | 1,195 | 7,474 / 2 mats | 12,002 / 8 mats | ☐ |
| CANNON_07 (Triple) | 2,324 | 14,532 / 3 mats | 23,338 / 15 mats | ☐ |

CANNON_01 was previously free to upgrade (multiplier was 0). Confirm L2 now costs **398 coins** (664 × 0.6).

---

## 5. Shop — IAP & virtual purchases

Run these against the **live store** (Google Play Internal Testing track / TestFlight). Do **not** test on the Editor.

| # | Check | Pass criteria |
|---|---|---|
| 5.1 | All 6 gem packs initialize | `[GemsBuy] IAP initialized.` log appears; no `OnInitializeFailed` and no length-mismatch error from `PopulateProductIds` |
| 5.2 | Each gem pack shows correct localized price | Tap each card, verify `priceText` matches store listing for the new SKUs (`100_gems`, `320_gems`, `600_gems`, `1300_gems`, `2800_gems`, `7500_gems`) |
| 5.3 | Buy 100-gem pack ($0.99) end-to-end | Wallet increments by exactly 100; receipt confirms; pending-purchase logic clears |
| 5.3a | Buy 320-gem pack ($2.99) end-to-end | Wallet increments by exactly 320 — confirms the new funnel-fill tier wired correctly |
| 5.3b | Old SKUs (`500_gems`, `7000_gems`, `16000_gems`, `40000_gems`, `110000_gems`) are removed/hidden | Play Console / App Store Connect | None purchasable; no test account can buy them |
| 5.3c | Shop UI shows 6 cards | Open shop scene at runtime | Six pack cards visible, none missing or stacked atop each other |
| 5.4 | Restore pending purchase | Force-close mid-purchase; relaunch; gems credited via `RestorePendingPurchases` |
| 5.5 | Buy gold pack (gems → coins) | Each `purchaseCards[i].virtualPurchaseId` matches Dashboard slot in catalog order |
| 5.6 | Buy power pack (gems → power) | Same — order matters |
| 5.7 | Insufficient gems handling | Try buying gold pack with 0 gems; UI surfaces "not enough gems" via `OnInsufficientFunds` |
| 5.8 | Remove Ads IAP | After purchase, `PlayerPrefs.GetInt("AdsRemoved") == 1`; interstitials no longer shown |
| 5.9 | Receipt validation server-side | Confirm Unity Economy server logs the `MakeVirtualPurchaseAsync` for each test buy |
| 5.10 | Currency cap behavior | Add `long.MaxValue / 2` coins; verify wallet UI doesn't overflow or roll negative |

> **Catalog-ID mismatch is the #1 silent failure mode.** If `purchaseCards[2]` is wired to the "Mega gold pack" UI but its `virtualPurchaseId` points to the "Small gold pack" Dashboard entry, the player pays Mega prices and gets Small rewards — and nothing in code throws. Verify each row by buying a small amount on a test account.

---

## 6. Earn-rate sanity check

Play a fresh install through Ch1–Ch3 (≈60 levels) on a stopwatch. Compare to the modeled earn rate from `Reward_Tuning.md` §3 (15-eggs-per-level baseline from `RewardManager.EstimateExpectedCoinsForLevel`). Numbers below assume the **post-cut** `GameplayRewardConfig` is the live asset.

| Window | Expected coins (cumulative) | Expected gems | Expected power refunds |
|---|---|---|---|
| End Ch1 (L20) | ~13,000–16,500 | ~3–6 | ~4 levels worth |
| End Ch2 (L40) | ~26,500–32,500 | ~6–12 | ~8 levels worth |
| End Ch3 (L60) | ~40,000–48,500 | ~10–17 | ~12 levels worth |
| End Ch10 (L200) | ~135,000–160,000 | ~50–80 | ~40 levels worth |
| End Ch20 (L400) | ~530,000–570,000 | ~140–180 | ~80 levels worth |
| End Ch30 (L600) | ~1,250,000–1,350,000 | ~280–340 | ~120 levels worth |

If actuals are **<80% or >120%** of expected, the live config drifted from the doc. Check in this order:
1. `Assets/Resources/Data/New Gameplay Reward Config.asset` matches Reward_Tuning §2.
2. `RewardManager.EstimateExpectedCoinsForLevel`'s 15-eggs constant hasn't changed.
3. Player isn't running on a save migrated from a pre-cut build (force fresh install for this test).

---

## 7. Powerup unlock chapters (live device)

Run a chapter-by-chapter spin pool sweep on a save-state preset (one save file per chapter).

For each save, perform the L0 (chapter-start) spin and the L5/L10/L15 mid-chapter spins. Verify:

| Spin slot | Expected available rarities | Expected pool size |
|---|---|---|
| L0 (Ch1) | Common only | 5 (Power Surge, Dual Shot, Ricochet, Vitality, Pre-Boss Recovery) |
| L0 (Ch5) | + Phase Shield (Rare) | 6 |
| L0 (Ch10) | + Common heal/mana now in pool | 7 |
| L5 mid-spin (Ch7) | + Rare effects unlocked at Ch5–7 | 8–10 |
| L10 mid-spin (Ch12) | + first Epic (Laser/Blade) | 11–13 |
| L15 mid-spin (Ch20) | + Legendary tier | 16–18 |

The exact pool depends on whether you've shipped the §2.2 re-baselining from `Cannon_Powerup_Unlock_Plan.md`. Either way, **no Legendary should appear before Ch18, no Epic before Ch11.**

### 7.1 Pool-balance check

For 30 consecutive Ch12 mid-chapter spins, log the rarity distribution. Without weights, every eligible powerup is equiprobable — that's the bug to confirm. Expected after weight fix:

| Rarity | Expected % in eligible pool | After weight (recommended) |
|---|---|---|
| Common | uniform | 3× weight (~50%) |
| Rare | uniform | 2× weight (~30%) |
| Epic | uniform | 1× weight (~15%) |
| Legendary | uniform | 0.5× weight (~5%) |

---

## 8. First-run experience

Test on a fresh install with cloud sign-in disabled, then again with cloud sign-in enabled.

| # | Check | Pass criteria |
|---|---|---|
| 8.1 | Tutorial completes without crash | Reach Ch1-L5 without the app force-closing |
| 8.2 | Single Shot owned, others locked | Cannon shop shows CANNON_01 owned, CANNON_02–07 locked |
| 8.3 | Starter wallet | Coins = configured starter amount; gems = 0 (or starter grant) |
| 8.4 | First-time clear gem reward | Beat Ch1-L1; gem count increases per `GameplayRewardConfig.gemsOnFirstTimeClear` (currently 10) |
| 8.5 | First chapter completion bonus | Beat Ch1; verify chapter-end coin/gem/chest grants |
| 8.6 | Cloud save backup happens | Force-close after Ch1; reinstall; sign in; progress restored |
| 8.7 | No "phantom unlocks" | Reinstall without sign-in; player should NOT see CANNON_03+ unlocked |

---

## 9. Telemetry & live-ops

Confirm these events fire on every relevant action. Without them, soft-launch data is unreadable.

| Event | When | Required params |
|---|---|---|
| `cannon_unlocked` | Player buys a cannon | cannonId, chapter, coinsSpent, gemsSpent |
| `cannon_upgraded` | Level-up purchase | cannonId, fromLevel, toLevel, coinsSpent, matsSpent |
| `iap_purchase` | Successful gem-pack buy | productId, gemsGranted, localizedPrice, currencyCode |
| `gold_pack_purchase` | Gem→coin virtual buy | virtualPurchaseId, coinsGranted, gemsSpent |
| `power_pack_purchase` | Gem→power virtual buy | virtualPurchaseId, powerGranted, gemsSpent |
| `level_started` | Begin level | chapter, level, powerSpent |
| `level_completed` | Successful clear | chapter, level, performanceRatio, coinsAwarded, gemsAwarded |
| `level_failed` | Fail | chapter, level, deathCause |
| `ad_watched` | Rewarded ad finishes | placement, rewardType, rewardAmount |
| `insufficient_funds` | Spend rejected | currency, attemptedAmount, walletBalance |

If your analytics tool is GameAnalytics / Firebase / Unity Analytics, the param names below map 1:1 to event params — keep that mapping consistent across builds.

---

## 10. Soft-launch KPIs to watch (Day 1 / Day 7)

These are the dials that will tell you whether the pricing changes work. Pull them daily during soft-launch and chart against the targets.

| KPI | Target | Tells you |
|---|---|---|
| % of players who unlock CANNON_02 by Ch3 | > 70% | Pricing isn't gating |
| % of players who unlock CANNON_03 by Ch6 | > 50% | Mid-funnel pricing healthy |
| Median coin balance at end of Ch10 | 80k–150k | Earn rate is in band |
| % of D1 players who view the shop | > 40% | Discoverability is fine |
| % of D1 players who make at least 1 IAP | 1–4% | Industry-standard conversion |
| % of D7 retained who have spent gems on a gold pack | 25–50% | Gem economy has a real sink |
| Median session count to first IAP | 3–6 | Funnel isn't too steep |
| Crash rate | < 1% | Standard ship gate |

If unlock rates are too high (>95% by Ch3 or >85% by Ch6), unlocks are still too cheap — re-tune the `unlockCoinMultiplier` upward by 25–40%. If too low (<50% by Ch3), they're too expensive — back off the base from 5000 toward 4000.

---

## 11. Rollback & hotfix plan

If a number is wrong in production:

| What's wrong | How to roll back | Time to ship |
|---|---|---|
| Cannon unlock price | Cloud Save → `cannon_stats` → edit `baseCoinsRequired` / `baseGemsRequired` for the affected sub-key — no client ship | Minutes |
| Cannon unlock chapter | Cloud Save → `cannon_stats` → edit `unlockAtChapter` — no client ship | Minutes |
| Gem pack USD price | Google Play / App Store Connect → save — no client ship | Minutes |
| Powerup gating | Edit `PowerupDatabase.asset` → patch build | Hours (client patch) |
| Server-side virtual-purchase reward | Unity Economy Dashboard → save — no client ship | Minutes |
| Upgrade curve shape | Edit `EconomyFormulaConfig.asset` → patch build | Hours (client patch) |

**Lesson:** cannon unlock pricing is now fully cloud-driven — soft-launch tuning of any individual cannon is a Dashboard edit. Powerup gating and the upgrade-cost curve still require client patches; after soft-launch, plan to migrate them to Remote Config so live-ops can tune without rebuilding.

---

## 12. Sign-off

A soft-launch ship requires sign-off from each role. Use this exact list — partial sign-offs do not ship.

- [ ] **Engineering lead:** Pre-flight (§2), validators in place (§3.1), telemetry firing (§9)
- [ ] **Design lead:** Cannon prices verified (§3, §4), shop catalog matches Shop_Pricing.md (§5), powerup gates verified (§7)
- [ ] **Live-ops / Producer:** KPIs instrumented (§10), rollback plan understood (§11)
- [ ] **QA lead:** Full first-run pass clean on 3 devices, two consecutive runs reproduce all numbers (§3, §4, §6, §8)
- [ ] **Final go/no-go:** Date, build number, commit hash recorded.

---

*Maintain this doc when:*
- A new shop SKU, IAP product, or virtual purchase ships.
- Pricing constants change (`baseUnlock`, `coinsPerGem`, multipliers).
- A new analytics event is added or renamed.
- The cannon or powerup roster changes.
