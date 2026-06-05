# Bird Hunter — Powerup Chest & Collection Design

**Version:** 0.2 (ideation draft — core forks resolved)
**Last Updated:** 2026-06-02
**Status:** 🟡 Ideating — not implemented. This doc captures the design intent before any code is written. The 11 open design forks are now resolved (see §12 Decision log); ⚙️ tuning numbers remain to be locked.
**Scope:** Replace the silent, progression-driven powerup unlock with a **chest-based collection system** featuring a pity guarantee, a reveal moment, and **player-driven powerup upgrades** (duplicates → copies → spend to level up).

This doc pairs with `Cannon_Powerup_Unlock_Plan.md` (current powerup pool, rarities, gates), `Store_and_Inventory_Pricing.md` (currency sinks), `Progression_Reward_Economy.md` (earn rate), and `Reward_Tuning.md`. When the design here is ratified, those docs must be reconciled (the §2 powerup tables in `Cannon_Powerup_Unlock_Plan.md` largely become obsolete — see §9).

> All numbers in this draft are **strawman values for tuning**, flagged ⚙️. They exist to make the design concrete, not to be shipped as-is.

---

## 1. Why change

### 1.1 What we have today

The current powerup system is **stateless and silent**:

- `PowerupGate.IsUnlocked(cfg)` computes unlock on the fly from `PowerupConfig` fields — `initiallyUnlocked`, `unlockFromChapter`, `spinUnlockLevel` — against the player's current chapter/level. **Nothing is persisted.**
- Acquisition = the **slot machine** (`SlotMachineController`): a weighted spin (⚙️ Common 65 / Rare 30 / Legendary 5) at chapter-start and after L5/L10/L15. The "pool" is simply whatever the gate currently permits.
- Player feedback is **grayscale-vs-color cards** (`PowerupLockView`). There is no "you earned X!" moment — powerups just quietly become rollable.
- There is **no chest, loot, pity, or duplicate mechanic** anywhere in the codebase. 29 powerups across Attack / Defense / Utility in 4 rarities (Common / Rare / Epic / Legendary).

### 1.2 The problem

- **No reward moment.** Unlocking a Legendary feels identical to unlocking a Common — both are silent. There's no dopamine beat, no thing to chase.
- **No agency.** The player can't *try* for a powerup; progression hands them out on a fixed schedule.
- **No long-tail progression.** Once a powerup is "unlocked," it's done. There's nothing to keep earning toward it.

### 1.3 The new model in one sentence

> Powerups are **collected** by opening **chests** (with a **pity** floor so rarity feels fair), each collection is a **reveal** moment, and **duplicates become upgrade material** the player spends to **level up** the powerups they own.

---

## 2. Architecture: two layers

The pivotal decision: **chests own, the slot equips.** These are two distinct layers.

```
┌─────────────────────────────────────────────────────────┐
│  LAYER 1 — COLLECTION (new, persistent, meta)            │
│                                                          │
│  Chest sources ──▶ open chest ──▶ pity check ──▶ roll    │
│   • boss drop          (reveal)      (rarity      powerup │
│   • currency buy                      floor)              │
│   • rewarded ad                                          │
│                                                          │
│  Result: you OWN powerup X; dupes give COPIES,           │
│  spent (+coins) to UPGRADE X to a higher LEVEL           │
└────────────────────────────┬────────────────────────────┘
                             │  owned set + levels
                             ▼
┌─────────────────────────────────────────────────────────┐
│  LAYER 2 — EQUIP (existing slot machine, re-pointed)    │
│                                                          │
│  Slot machine rolls ONLY among powerups you already own. │
│  Effect strength = the owned LEVEL of that powerup.      │
└─────────────────────────────────────────────────────────┘
```

**Layer 1 (Collection)** is brand new and is where chests, pity, reveal, and dupe-leveling live. It is persistent (cloud save).

**Layer 2 (Equip)** is the existing `SlotMachineController`, minimally changed: instead of building its option pool from `PowerupGate` (progression), it builds from the player's **owned collection**. The effect value it applies is scaled by the owned powerup's level.

This cleanly separates *"what can I use this run"* (slot, per-run) from *"what do I permanently have"* (collection, meta).

---

## 3. Chests

### 3.1 Three chest tiers

**Decision (Q1):** ship **three tiers** — Wooden, Silver, Gold — with escalating rarity odds and prices. Tiers give the shop real merchandising surface and let each source hand out an appropriate quality of chest.

⚙️ Per-tier base rarity odds (before pity):

| Tier | Common | Rare | Epic | Legendary |
|---|---|---|---|---|
| **Wooden** | 70% | 25% | 4% | 1% |
| **Silver** | 45% | 35% | 15% | 5% |
| **Gold** | 20% | 40% | 30% | 10% |

### 3.2 Sources → which tier

| Source | Tier(s) | Cost | Cadence (⚙️) | Notes |
|---|---|---|---|---|
| **Boss drop** | ⚙️ Wooden (mini-boss) / Silver (chapter boss) | Free | 1 chest per boss defeated | The headline free path. Ties reward to existing boss encounters (`Gameplay/BossBirds/`). Endless drops are daily-capped (§3.4). |
| **Currency purchase** | All three | ⚙️ coins (Wooden) / gems (Silver, Gold) | On demand, shop; ⚙️ daily purchase cap | Soft+hard sink. Pairs with `Store_and_Inventory_Pricing.md`. |
| **Rewarded ad** | ⚙️ Wooden | 1 ad view | ⚙️ N per day | LevelPlay already integrated (`AdManager`/`AdsManager`). Daily-capped free chest. |

### 3.3 What a chest contains

A chest grants **one powerup** (newly owned, or a duplicate that becomes upgrade material), plus optional currency garnish.

- **Rarity roll** → pick a rarity by the tier's weighted odds, modified by that tier's pity (§4).
- **Powerup roll** → pick a powerup of that rarity from the **chapter-eligible pool** (progression still drip-feeds *which* powerups can appear — see §6).
- **Own-or-level** → if not owned, the player now owns it at L1 (reveal: "NEW!"). If already owned, it's a duplicate → adds copies the player later spends to upgrade it (§5). If the powerup is already maxed, copies convert to coins (§5.3).

### 3.4 Endless mode (Q7)

**Decision:** Endless-mode bosses **do** drop chests, but they count toward a **shared daily cap** so endless can't be farmed for infinite chests. Beyond the cap, endless bosses fall back to coins/score only.

---

## 4. Pity system — per-tier soft pity

**Decision (Q6 + Q2):** **soft pity**, with **separate counters per chest tier**. As a tier's counter climbs toward its floor, the odds for the guaranteed rarity ramp up, hitting 100% at the floor. Each chest tier (Wooden / Silver / Gold) tracks its own counters, so opening Gold chests doesn't advance the pity you've built on Wooden, and vice-versa.

⚙️ Soft-pity floors and ramp (per tier, per rarity guarantee):

| Guarantee | Floor (chests) | Ramp starts at | Resets when… |
|---|---|---|---|
| Rare or better | 5 | 3 | any Rare+ is opened |
| Epic or better | 15 | 10 | any Epic+ is opened |
| Legendary | 40 | 30 | a Legendary is opened |

⚙️ Example ramp for the Epic guarantee (base Epic odds shown for Silver = 15%):

| Chest # since last Epic+ | Epic odds |
|---|---|
| 1–9 | base (15%) |
| 10 | +5% |
| 12 | +15% |
| 14 | +40% |
| 15 | 100% (forced), counter resets |

**Mechanics:**

- Each opened chest increments that **tier's** counters.
- Opening a chest of rarity R resets the counters (for that tier) for R and every tier below it that it satisfies — a Legendary resets Legendary, Epic, and Rare counters.
- Past the ramp-start threshold, the guaranteed rarity's odds rise each chest until the floor forces it.

**Persistence:** counters are stored **per chest tier** in cloud save (§7) — 3 tiers × 3 rarity counters = 9 values. They must survive restarts and sync across devices, or pity is meaningless.

---

## 5. Duplicates & powerup upgrades

Duplicates and upgrades are **one unified track**: a chest duplicate gives you **copies** of a powerup; an **upgrade** is the deliberate action of spending those copies (plus coins) to raise the powerup's **level**, which strengthens its effect. Dupes are the *material*, upgrades are the *spend*.

### 5.1 The upgrade loop

```
chest dupe ──▶ +copies of powerup X ──▶ (on Collection screen)
                                          spend copies + coins ──▶ X levels up
                                                                    (effect ↑)
```

- Each owned powerup has a **level** (L1…Lmax, ⚙️ Lmax = 5) and a **copies** balance (the upgrade material).
- A chest duplicate adds ⚙️ 1 copy to that powerup's balance.
- The player **chooses** when to upgrade (active, on the Collection screen) — leveling is **not** automatic. This gives agency and a coin sink, and lets the player prioritize which powerups to invest in.
- An upgrade consumes **copies + coins** (Q9 — coins, reusing the cannon-upgrade sink; no new currency); the cost ramps by target level and rarity.
- **Max level is chapter-capped (Q10):** a powerup can't exceed the level its rarity is allowed at the player's current chapter, so a lucky early Legendary can't be maxed at Ch3 and trivialize early content.

⚙️ Chapter cap on max reachable level:

| Player chapter | Max level allowed |
|---|---|
| 1–4 | L2 |
| 5–9 | L3 |
| 10–17 | L4 |
| 18+ | L5 |

⚙️ Copies + coins required per upgrade:

| To level | Common (copies / coins) | Rare | Epic | Legendary |
|---|---|---|---|---|
| L2 | 2 / 500 | 3 / 900 | 4 / 1,600 | 5 / 3,000 |
| L3 | 4 / 1,200 | 5 / 2,000 | 6 / 3,500 | 8 / 6,500 |
| L4 | 8 / 2,800 | 10 / 4,500 | 12 / 7,500 | 15 / 13,000 |
| L5 | 15 / 6,000 | 18 / 9,500 | 22 / 15,000 | 30 / 26,000 |

> **Decision (Q9):** upgrades cost **coins**, the same currency as cannon upgrades (`Store_and_Inventory_Pricing.md` §5.1). This adds a second meaningful coin sink and keeps the player's mental model simple — no new "shard" currency.

### 5.2 Effect scaling

**Decision (Q11):** each powerup defines its **own per-level curve in config**, not one global formula. `PowerupConfig.baseValue` is the **L1** value; the config carries a per-level array (or curve-type enum) so additive effects (+% damage), cooldown reductions, and proc-chance effects can each scale correctly. ⚙️ A sensible default for additive effects is `value(level) = baseValue × (1 + 0.20 × (level − 1))` (+20%/level, +80% at L5), but the config can override it per powerup.

> **Design tension:** upgrades add power. We must make sure a fully-leveled common doesn't trivialize content, that the **slot equip** (Layer 2) reads the owned level when applying effects, and that the chapter cap (§5.1) holds the curve in check early. Flag for the economy/balance pass.

### 5.3 Maxed-dupe overflow (Q3)

**Decision:** once a powerup is at its true max (L5), further duplicate copies **auto-convert to coins** at a ⚙️ fixed rate (e.g. 1 copy → 200 / 400 / 800 / 1,500 coins by rarity). This keeps chests rewarding even for a near-complete collection, with no new currency. (Note: this triggers only at the absolute L5 cap, not at the chapter cap — copies still bank toward future levels while chapter-capped.)

---

## 6. Interaction with existing chapter gates

Today two gates govern availability: `unlockFromChapter` and `spinUnlockLevel`. In the new model:

- **`spinUnlockLevel` is retired.** The L5/L10/L15 "spin slot" gating no longer governs ownership; the slot machine (Layer 2) just rolls among owned powerups.
- **`unlockFromChapter` is repurposed** as the **chest-eligibility gate**: a powerup can only *appear in a chest* once the player has reached its chapter. This keeps the progression drip — early chests skew Common, and new rarities/powerups enter the chest pool as the player advances — without it being the unlock mechanism itself.
- **`initiallyUnlocked`** powerups are **granted at L1 on first boot** (seed the collection), so new players have a starting kit and the slot machine isn't empty.

This means `Cannon_Powerup_Unlock_Plan.md` §2 (spin-gate tables) is superseded for the *acquisition* story but its **chapter-gate + rarity** columns remain the source for chest eligibility. Reconcile in §9.

---

## 7. Persistence — the big new requirement

**This is the largest architectural change.** Today nothing about powerups is saved. The new system needs persistent, cloud-synced state.

Proposed new DTO (`Assets/Scripts/Data/`), e.g. `PowerupCollectionData`:

```
PowerupCollectionData
  ├─ entries: List<PowerupOwnership>
  │     ├─ powerupId      (string, matches PowerupConfig.id)
  │     ├─ level          (int, 1..Lmax)
  │     └─ copies         (int, upgrade material spent to level up)
  ├─ pity: List<TierPity>            // one per chest tier (Q2 — per-tier)
  │     ├─ tier            (enum: Wooden / Silver / Gold)
  │     ├─ sinceRare       (int)
  │     ├─ sinceEpic       (int)
  │     └─ sinceLegendary  (int)
  ├─ chestInventory                  // unopened chests, by tier
  │     ├─ wooden          (int)
  │     ├─ silver          (int)
  │     └─ gold            (int)
  ├─ dailyChestCounters              // resets daily; for ad + endless caps (§3.4)
  │     ├─ adChestsToday   (int)
  │     ├─ endlessChestsToday (int)
  │     └─ resetDateUtc    (string)
  ├─ totalChestsOpened    (int, analytics / future use)
  └─ migratedV1           (bool, set once the grandfather pass runs — §10/§12 Q4)
```

- New `CloudKeys` entry (e.g. `POWERUP_COLLECTION`), loaded during boot alongside `InventoryData` / `CurrencyData` and persisted via `CloudSaveManager`.
- `PowerupGate.IsUnlocked(id)` changes from *"computed from progression"* to *"is `id` in the owned collection."* Every current caller (`PowerupLockView`, `SlotMachineController`, card UI) keeps working through the same API — only the implementation behind it changes.
- **Migration (Q4 — grandfather):** existing players have no collection. On first load post-update, seed `initiallyUnlocked` powerups at L1 **and grandfather** every powerup whose old chapter/spin gate the player has already cleared — granting each at L1 — so nothing the player could already roll is taken away. Guard with the `migratedV1` flag so it runs exactly once.

---

## 8. UX — the reveal moment

The whole point is to stop being silent. Minimum beats:

1. **Chest grant feedback** — when a chest is awarded (boss kill / purchase / ad), show it landing in an inventory ("1 chest ready to open").
2. **Open animation** — chest opens, builds anticipation (rarity-colored burst; bigger for higher rarity). Reuse the rarity palette already in `PowerupLockView` (Common `#98F3AF`, Rare `#F8E64B`, Epic `#EAB3FF`, Legendary `#FF9B94`).
3. **Reveal card** — the powerup card flips in, tagged **NEW** (first-time) or **+1 / LEVEL UP → Ln** (duplicate). Show the effect delta on level-up.
4. **Pity nudge (optional)** — a subtle "X chests until guaranteed Epic" progress hint to drive engagement.

Where does opening happen? **Decision (Q5 — both):** boss-drop chests offer an **immediate post-run open popup** (open it right after the win for the dopamine beat), and **all** chests also accumulate in a **main-menu Collection screen** where the player can open stored/purchased chests, batch-open, and browse the collection. The Collection screen shows all 29 powerups as owned/locked with levels and copy progress, replacing the passive grayscale grid with an active collection book.

### 8.1 Upgrade UX

The Collection screen is also where **upgrades** happen. Each owned powerup card shows:

- Current **level** and the **effect value** at that level.
- A **copies bar** (`have / needed` for next level) and the **coin cost**.
- An **Upgrade** button — affordable when copies + coins are met; otherwise it shows what's missing ("need 3 more copies").
- An **effect-delta preview** on the upgrade button/popup ("Damage 120 → 144").

This is the spend moment that mirrors the existing cannon-upgrade UX, so it should feel familiar to players who already upgrade cannons.

---

## 9. Doc reconciliation checklist (when ratified)

- `Cannon_Powerup_Unlock_Plan.md` §2 — mark spin-gate strategy obsolete; keep chapter-gate + rarity as chest-eligibility source.
- `Store_and_Inventory_Pricing.md` — add chest SKUs (currency + IAP if any) and the rewarded-ad chest to the sinks/sources tables (see its §5.3).
- `Progression_Reward_Economy.md` / `Reward_Tuning.md` — fold chest drops (boss + ad) into the earn-rate model; account for powerup-level power creep.
- `EndlessMode_Design.md` — decide whether bosses in endless also drop chests (could be a farm exploit — cap or scale).

---

## 10. Remaining open items

The 11 design forks are resolved (§12). What's left is **tuning + a couple of UX details**, not architecture:

1. **Lock the ⚙️ numbers** — per-tier odds (§3.1), soft-pity ramps/floors (§4), upgrade copies+coins costs (§5.1), chapter-cap thresholds (§5.1), overflow conversion rates (§5.3), chest prices, and daily caps (ad + endless). Needs the economy pass against `Reward_Tuning.md` / `Progression_Reward_Economy.md`.
2. **Free-vs-paid balance target** — set the intended split, e.g. ⚙️ a non-spending player should reach ~70% collection at L2–L3 average by Ch30 from boss + ad chests alone, with paid chests accelerating, not gating.
3. **Pity-progress UI** — show "X chests until guaranteed Epic" per tier? (Nice engagement hook; small build.)
4. **Boss-drop tier mapping** — confirm which bosses drop Wooden vs Silver (mini-boss vs chapter boss is the strawman in §3.2).
5. **Chest-tier visual identity** — art/VFX per tier for the open animation (rarity palette already exists in `PowerupLockView`).

---

## 11. Build order (once design is locked — NOT started)

A rough sequence, smallest-risk-first. Listed for scoping only; no code exists yet.

1. **Data + persistence** — `PowerupCollectionData` DTO, `CloudKeys` entry, boot load/save, migration seed.
2. **Repoint unlock** — `PowerupGate.IsUnlocked` reads the collection; seed `initiallyUnlocked`; verify slot machine + lock visuals still work off owned set.
3. **Chest core** — chest open logic: rarity roll → pity → powerup roll → own-or-level. Pure logic, unit-testable, no UX.
4. **Sources** — wire boss drop, shop purchase, rewarded-ad grants to award chests.
5. **Upgrades + level scaling** — copies-spend upgrade logic, per-level effect curve, slot equip applies owned level to effect value.
6. **UX** — chest inventory, open animation, reveal card, collection screen, **upgrade button + effect-delta preview**.
7. **Tuning + analytics** — replace ⚙️ strawman numbers with tuned values; log chest opens / rarity / pity hits / upgrade spends.

---

## 12. Decision log

The 11 open design forks, resolved 2026-06-02 (ideation session):

| # | Question | Decision | Section |
|---|---|---|---|
| Q1 | Chest tiers | **Three tiers** — Wooden / Silver / Gold, escalating odds & prices | §3.1 |
| Q2 | Pity scope | **Per-tier counters** (each tier tracks its own pity) | §4, §7 |
| Q3 | Maxed-dupe overflow | **Auto-convert to coins** at the L5 cap | §5.3 |
| Q4 | Migration policy | **Grandfather** every powerup the player's progression already cleared (+ seed `initiallyUnlocked`) | §7 |
| Q5 | Opening flow | **Both** — post-run popup for boss drops + main-menu Collection screen | §8 |
| Q6 | Pity style | **Soft pity** — odds ramp up toward the floor | §4 |
| Q7 | Endless boss drops | **Yes, but daily-capped** (shared cap, then coins/score only) | §3.4 |
| Q8 | Free-vs-paid balance | Target set in tuning pass (free reaches majority of collection by Ch30) | §10.2 |
| Q9 | Upgrade currency | **Coins** (reuse cannon-upgrade sink; no new currency) | §5.1 |
| Q10 | Upgrade gating | **Chapter-capped** max level | §5.1 |
| Q11 | Effect curve | **Per-powerup curve in config** (additive default, override per powerup) | §5.2 |

---

*Maintain this doc through ideation. Promote to a versioned spec (drop the 🟡 status, lock the ⚙️ numbers) before implementation begins.*
