# Bird Hunter — Store & Inventory Pricing (Telemetry-Calibrated)

**Version:** 1.0
**Last Updated:** 2026-06-02
**Status:** 🟢 Authoritative pricing doc. Recomputed from the 2026-06-02 game logs (`GameLogs/economy.log`, 143 level clears). **Supersedes `Shop_Pricing.md`** (which was calibrated against assumed earn rates that telemetry contradicts — see §2). Reconcile `Reward_Tuning.md` / `Progression_Reward_Economy.md` to the real rates here.

**Scope:** every currency sink and source — IAP gem packs, gem→gold/power virtual packs, remove-ads, cannon unlocks + upgrades, powerups, and (future) chests — priced against **measured** player income.

> **Currencies:** **GOLD** (soft, abundant), **GEM** (premium), **POWER** (in-level energy, not an economy currency). Conversion anchor: **1 gem = 110 gold** (`EconomyFormulaConfig.coinsPerGem`).

---

## 1. Telemetry anchors (the foundation — everything below derives from these)

Measured over 143 level clears, 5 chapters, single-shot to upgrade lvl35:

| Metric | Measured value | Source |
|---|---|---|
| Gold per egg | **27.2 avg** (15–40) | 1,900 `egg_destroyed` events |
| Eggs per level | **~14** (9–24) | per-level grouping |
| Gold per level (eggs) | **~380** | 27.2 × 14 |
| Gold per level (completion) | **flat 340** (every level) | 143 `level_completion` |
| **Total gold per level** | **~700** (575–980) | combined |
| **Steady-state earn rate** | **702 gold/level** | cumulative ÷ levels (stable from L10→L143) |
| Gems per first-clear | **+4** | 91 `first_time_clear` |
| Gem drop (per egg) | **+1, rare** (~1.6% of eggs) | 31 `egg_gem_drop` |
| **Organic gem rate** | **~4 gems / new level** (first playthrough) | combined |

**Cannon upgrade cost curve (measured, single-shot, mult 0.6):** matches `600 × 1.11^(L-2) × mult` exactly. lvl2 = 360 → lvl35 = 11,271. Cumulative L2→L35 = **110,463 gold**.

> These are **one player / one device** — directional, not statistically final. But the earn rate is dead-flat across 143 levels, so it's a reliable planning anchor.

---

## 2. ⚠️ The headline finding — income is flat, costs are geometric

**The economy has a structural mismatch that pricing must address, not paper over.**

- **Income is flat.** ~702 gold/level from L1 to L143, across all 5 chapters. Per-egg gold never scaled (stayed ~27). **The intended income ramp in `Reward_Tuning.md` (Early ~737 → Mid ~2,015 → Late ~3,740) is NOT occurring** — either per-chapter egg-gold scaling isn't wired, or the player never hit the chapters where it kicks in. Flat completion bonus (340) compounds this.
- **Upgrade costs are geometric** (×1.11/level). So the *grind per single upgrade* explodes:

| Upgrade to | Cost | = levels of income (@702) |
|---|---|---|
| lvl2 | 360 | **0.5** |
| lvl10 | 830 | 1.2 |
| lvl20 | 2,356 | 3.4 |
| lvl35 | 11,271 | **16.1** |
| lvl50 (projected) | ~50,400 | **~72** |
| lvl100 (projected) | ~1.3 B | absurd |

**Consequence:** the `Cannon_DPS_and_Egg_Workload_Design.md` goal — "upgrade every 2–3 levels" — holds only to ~lvl15. By lvl35 the player grinds **16 levels per single upgrade**; the death-loop cadence is broken. `maxUpgradeLevel = 100` is unreachable without IAP by orders of magnitude.

**This is a design decision, not just a number tweak — pick a path in §7 before locking prices.**

---

## 3. Time-to-afford — every SKU at the REAL rate (702 gold/lvl, 4 gems/lvl)

This is the single most useful table: how long each purchase actually takes a non-paying player.

### 3.1 Cannon unlocks (gold path)

| Cannon | Price (gold) | Levels to afford | `Shop_Pricing` intended unlock | Reality vs intent |
|---|---|---|---|---|
| Rapid Fire | 6,000 | **8.6** | Ch3 | OK |
| Lucky | 9,500 | **13.5** | Ch6 | OK |
| Double | 16,000 | **22.8** | Ch10 | OK-ish |
| Shotgun | 24,000 | **34.2** | Ch14 | slow |
| Big Bartha | 44,500 | **63.4** | Ch20 | **very slow** |
| Triple | 75,000 | **106.8** | Ch25 | **way too slow** |
| **All 6** | **175,000** | **249 levels** | — | a full game of income just to own them |

> Under the *assumed* ramp these sat at 8–14% of cumulative earnings. Under *flat* income they're far heavier — Triple alone is ~107 levels of pure saving (≈ the whole 5-chapter run we logged). **If income stays flat, mid/late unlocks must come down (§7).**

### 3.2 Cannon upgrades (per cannon, to typical levels)

| To level | Cost (mult 0.6) | Levels to afford (single upgrade) | Cumulative from L1 |
|---|---|---|---|
| L5 | 492 | 0.7 | 1,696 (2.4 lvls) |
| L10 | 830 | 1.2 | 5,100 (7.3 lvls) |
| L20 | 2,356 | 3.4 | 20,499 (29 lvls) |
| L35 | 11,271 | 16.1 | 110,463 (**157 lvls**) |

### 3.3 Gem sinks at organic gem rate (~4/level)

| Item | Gems | Levels to afford |
|---|---|---|
| Rapid Fire (gem unlock) | 55 | 13.8 |
| Triple (gem unlock) | 682 | **170** |
| Wooden chest (⚙️ est. 50) | 50 | 12.5 |
| Gold chest (⚙️ est. 250) | 250 | 62.5 |

Gems are scarce organically (~4/level, first playthrough only). The gem path is a **convenience/IAP lane**, not a realistic F2P route for big items.

---

## 4. Store pricing — IAP & virtual packs

### 4.1 IAP gem packs (real money → gems)
Product IDs: `{amount}_gems` (`GemsBuyManager.cs:19`). Prices are store-localized; reference USD:

| Pack | Gems | USD (ref) | = gold @110 | = levels of income | $ / 1k gold |
|---|---|---|---|---|---|
| Starter | 100 | $0.99 | 11,000 | 15.7 | $0.090 |
| Small | 320 | $2.99 | 35,200 | 50 | $0.085 |
| Medium | 600 | $4.99 | 66,000 | 94 | $0.076 |
| Large | 1,300 | $9.99 | 143,000 | 204 | $0.070 |
| Mega | 2,800 | $19.99 | 308,000 | 439 | $0.065 |
| Whale | 7,500 | $49.99 | 825,000 | 1,176 | $0.061 |

**Value curve is healthy** — larger packs give more gold/$ (10% → ↑), the standard increasing-value ladder. No change needed to the ladder shape.

### 4.2 Gem → gold virtual packs (Unity Economy Dashboard)

| Pack | Gems | Gold | Gold per gem |
|---|---|---|---|
| Small | 50 | 5,500 | 110 |
| Medium | 200 | 24,000 | 120 |
| Large | 800 | 104,000 | 130 |
| Mega | 2,000 | 280,000 | 140 |

Volume bonus 110→140 gold/gem. Consistent with the 110 anchor; fine.

### 4.3 Gem → power virtual packs
Small 10→5 · Medium 35→25 · Large 60→60 · Mega 150→200. (Power is a session resource; out of the core economy loop — leave as-is.)

### 4.4 Remove Ads
$4.99 one-time. Standard; keep. (Pairs with rewarded-ad income — see §6.4.)

---

## 5. Inventory pricing — current state & formulas

### 5.1 Cannons
- **Unlock:** cloud `cannon_stats.json` `baseCoinsRequired` / `baseGemsRequired` (single source of truth). Values in §3.1.
- **Upgrade:** `600 × 1.11^(toLevel-2) × cannonMultiplier` (`EconomyFormulaConfig`). Per-cannon `coinMultiplier` tiers cost (Single-shot 0.6 = cheapest). Materials free L2–L4, ramp L5–L10.
- **⚠️ `CANNON_01.coinMultiplier`:** flagged as `0` historically (free) — confirm it's `0.6` for upgrades (telemetry shows paid upgrades at 0.6, so this is fixed in the live build).

### 5.2 Powerups (29)
**Not currency-priced today** — acquired via the slot machine, gated by `unlockFromChapter` + `spinUnlockLevel`. No sink. **This changes** if `Powerup_Chest_Collection_Design.md` ships (chests + dupe-upgrades become the powerup sink — upgrades cost coins there). Until then, powerups contribute **zero** to the economy sink side.

### 5.3 Chests (future — `Powerup_Chest_Collection_Design.md`)
Chest SKUs (Wooden/Silver/Gold via coins/gems/ad) are **not yet priced**. When that system lands, price them here. ⚙️ Strawman for planning: Wooden ~2,000 gold / 50 gems · Silver ~150 gems · Gold ~250 gems. Also the star-milestone chests (`Chapter_Unlock_Panel_Design.md §3.4`) are *free* grants — account for them as a source.

---

## 6. The full economy ledger (sources vs sinks, at real rates)

### 6.1 Sources (per level, non-paying)
| Source | Gold/level | Gems/level |
|---|---|---|
| Egg drops | ~380 | ~0.25 |
| Level completion | 340 | — |
| First-clear | — | 4 (first playthrough) |
| **Total** | **~700** | **~4** |

### 6.2 Sinks
| Sink | Cost | Notes |
|---|---|---|
| Cannon unlocks | 175k gold total | one-time |
| Cannon upgrades | geometric, unbounded | the dominant gold sink |
| Chests (future) | TBD | new sink |
| Powerups | 0 today | becomes a sink only via chest system |

### 6.3 The imbalance
At flat 700 gold/level, the **only** scaling sink is cannon upgrades — and it scales *faster than income*, so past ~lvl35 the player is permanently gold-starved on upgrades while unlocks (175k) sit unaffordable. There's no gem sink at all until chests ship. **The economy currently has one real sink (upgrades) that outruns the one flat source (levels).**

### 6.4 Rewarded-ad income (not in the 700/level)
Telemetry showed large unexplained balance jumps (e.g. +47k at one point) — likely ad/grant income. Rewarded ads (2× coins/level, etc.) are a real income lever **not** captured in the 702/level steady rate; factor them into the §7 target as a "with-ads" multiplier (⚙️ assume +30–50% income for an ad-watching player).

---

## 7. Recommendations — pick a path, then lock numbers

The flat-income/geometric-cost mismatch (§2) must be resolved. Three options, not mutually exclusive:

### Path A — Make income scale (preferred; matches original design)
Wire per-chapter egg-gold scaling and a scaling completion bonus so income ramps roughly with cost.
- ⚙️ Target: egg gold ×1.15/chapter, completion bonus `340 × 1.12^(chapter-1)`.
- Result: income ~700 (Ch1) → ~2,000 (Ch10) → ~5,000 (Ch20), tracking `Reward_Tuning.md`'s intent — and making the §3.1 unlock prices land at their designed 8–14% bands again.
- **Still doesn't fix L50+ upgrades** (cost outruns even a ramping income), so pair with Path B's curve softening for the long tail.

### Path B — Soften the upgrade cost curve
Lower the growth factor so a single upgrade never exceeds ~3 levels of income.
- ⚙️ Drop `c` from **1.11 → 1.08** past ~L20, or cap per-upgrade cost at `~4 × current-level-income`.
- Keeps the death-loop cadence (`Cannon_DPS` §5) intact into the late game.
- Re-evaluate `maxUpgradeLevel = 100` — at any sane curve, L100 is a whale/super-late goal; consider a soft cap or a separate prestige track.

### Path C — Re-price the inventory to flat income (fastest, if income won't change)
If income stays flat, cut mid/late prices to keep affordability sane:
- ⚙️ Big Bartha 44,500 → **~28,000** (~40 lvls), Triple 75,000 → **~42,000** (~60 lvls).
- Add a real **gem sink** (chests, or gem→upgrade) so the premium currency has somewhere to go.

**Recommended:** **A + B together** — scale income to restore the unlock affordability bands, and soften the upgrade curve so the late game stays a 2–3-level cadence. Re-run a telemetry capture after, and re-lock §3 with the new numbers.

---

## 8. Action items

| # | Item | Owner | Why |
|---|---|---|---|
| 1 | Confirm whether per-chapter egg-gold scaling is wired; if not, implement (Path A). | Eng | Telemetry shows flat ~27 gold/egg all game — the income ramp is missing. |
| 2 | Decide Path A / B / C (recommend A+B); set the income & cost targets. | Design | Resolves the §2 mismatch before any price is locked. |
| 3 | Re-price Big Bartha / Triple to the chosen income model. | Design | Currently 63 / 107 levels to afford at real rate. |
| 4 | Add a gem sink (chests, or gem-for-upgrade) — gems currently have no organic use. | Design | No premium-currency sink exists today. |
| 5 | Re-baseline `maxUpgradeLevel = 100` against the softened curve (soft cap / prestige). | Design | L100 is ~1.3 B gold today — unreachable. |
| 6 | Capture fresh telemetry after changes; re-lock §1 and §3. | Eng + Design | Single-device data — validate at scale. |
| 7 | Reconcile `Reward_Tuning.md` / `Progression_Reward_Economy.md` to the real flat rate, or to the post-fix ramp. | Design | Those docs assert a ramp telemetry doesn't show. |

---

## 9. Doc reconciliation
- **`Shop_Pricing.md`** — superseded by this doc. Recommend deletion (its prices assume the unverified income ramp). Update `CLAUDE.md`'s reference to point here.
- **`Reward_Tuning.md`** — keep, but mark its Early/Mid/Late coin-ramp as *target, not measured* until Path A is implemented and re-captured.
- **`Powerup_Chest_Collection_Design.md`** — when ratified, its chest/upgrade SKUs fold into §5.3 + §6.

---

*All ⚙️ values are strawman targets pending a Path decision (§7) and a fresh capture. The measured §1 anchors are the only hard numbers here.*
