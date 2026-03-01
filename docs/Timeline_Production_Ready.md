# Bird Hunter — Development Timeline
## Current State → Production-Ready QA Sign-Off

**Version:** 1.1  
**Last Updated:** February 2025  
**Team:** 3 Devs | 4 Artists | 2 Animators | VFX Team  
**Deadline:** 20 days art + integration | 10 days QA + release = **30 days total**

---

## 1. Current Development State (Baseline)

### Done
| Area | Status | Notes |
|------|--------|-------|
| Main Menu UI | Complete | Done |
| Core architecture | Complete | IDamageable, GameEvents, ScoreManager, event-driven flow |
| Egg/Bird health | Complete | EggHealth, BirdHealth; split cascade; physics |
| Projectiles | Complete | Bullet, BigBullet, Sword; IDamageable + legacy fallback |
| Spawn logic | Complete | Birds spawn; lay eggs; pressure/relief; adaptive rate |
| Score flow | Complete | ScoreManager; OnEggDestroyed/BirdDestroyed; level score |
| FX/Audio stubs | Complete | HitFXController, SFXController, ScoreUIController |
| Level flow | Complete | Chapters, levels, target score, slot machine, powerups |
| Theme system | Complete | ThemeManager; per-chapter backgrounds |

### Pending
| Area | Status | Notes |
|------|--------|-------|
| HP display on eggs | Not implemented | Ball Blast–style number on egg |
| Floating score pop | Not implemented | "+X" at destroy position |
| Combo system | Not implemented | Rapid-hit multiplier |
| Cannon damage/loss | Not implemented | Dodging, game over on hit |
| Shoot SFX | Not wired | No OnCannonFired event |
| Coin events | Legacy only | Not in event flow |
| Full art assets | Placeholder/legacy | Eggs, birds, FX, UI need production art |
| VFX prefabs | Stub/legacy | Hit, destroy, bounce particles |
| Animation polish | Basic | Bird flight; egg bounce |
| QA execution | Not started | Test cases defined; not run/signed |

---

## 2. Team Allocation Overview

| Discipline | Headcount | Primary Focus |
|------------|-----------|---------------|
| **Devs** | 3 | Mechanics, integration, bug fixing, tooling |
| **Artists** | 4 | 2D art: eggs, birds, backgrounds, UI, icons |
| **Animators** | 2 | Bird flight, egg bounce, cannon, UI |
| **VFX** | Team | Particle FX: hit, destroy, trails, screen effects |

*VFX team size assumed flexible; tasks can be split or shared with Artists.*

---

## 3. Timeline Overview

**Actual deadline (FYI):** 20 days art + integration | 10 days QA + release = **30 days total**

```
Day   1  5 10 15 20 25 30
      |--|--|--|--|--|--|
Art + Integration (20 days)     ████████████████████
QA + Release (10 days)                      ██████████
```

**Compressed schedule:** Art, VFX, animation, and integration run in parallel within the 20-day window. QA + bug fix + release within 10 days.

**Asset scope:** 30 chapter BGs | 7 cannons | 8+ bullets | powerup VFX (~10–15) | 29–30 powerup icons.

---

## 4. Phase Breakdown

*With 20-day art+integration and 10-day QA+release:* Phases 1–5 fit in days 1–20; Phases 6–7 in days 21–30. Week estimates below are for reference; adjust to day-level as needed.

### Phase 1: Feature Completion (Days 1–3)
**Owner:** Devs (3)  
**Goal:** Ship remaining mechanics for MVP polish.

| Task | Dev | Est. | Notes |
|------|-----|------|-------|
| HP display on eggs | Dev 1 | 3d | TMP on egg; EggHealth updates on hit |
| Floating score pop | Dev 1 | 2d | World-space "+X"; subscribe to destroy events |
| Combo system | Dev 2 | 4d | ComboController; multiplier; decay timer |
| Cannon damage / loss | Dev 2 | 3d | CannonHealth; collision with eggs/birds; game over |
| OnCannonFired + shoot SFX | Dev 3 | 1d | Event + SFXController wiring |
| Coin events (optional) | Dev 3 | 2d | OnCoinDropped, OnCoinCollected; event integration |

**Deliverables:** Feature-complete gameplay; all GDD mechanics implemented.  
**Handoff:** Design/dev review; Art can start from approved specs.

---

### Phase 2: Art Production (Days 1–14, within 20-day window)
**Owner:** Artists (4)  
**Goal:** Production art for eggs, birds, 30 chapter BGs, 7 cannons, 8+ bullets, 29–30 powerup icons.

| Task | Artist | Weeks | Notes |
|------|--------|-------|-------|
| Egg sprites E1–E4 | Art 1 | 1–2 | Final tier art; optional HP overlay template |
| Bird sprites B1–B4 | Art 2 | 2–3 | Static or sheet; Animator will animate |
| Chapter backgrounds (30) | Art 1–2 | 4–6 | 1 per chapter; ThemeManager; 16:9 or letterbox-safe |
| Cannon sprites (7) | Art 3 | 2–3 | Base + muzzle; 7 distinct cannons |
| Bullet sprites (8+) | Art 4 | 2 | Bullet 1–7, Big Bullet; distinct per type |
| UI: score, combo, HUD | Art 1–2 | 3–4 | Score panel, delta pop, combo UI |
| Powerup icons (29–30) | Art 1–2 | 3–4 | **Blocked by VFX** — must align with powerup particle FX; greybox OK for early integration |

**Deliverables:** Final sprites; source files; import settings documented.  
**Dependencies:** Art 1–2 feeds Anim; powerup icons wait for VFX reference.

---

### Phase 3: Animation Polish (Days 5–15, within 20-day window)
**Owner:** Animators (2)  
**Goal:** Bird flight, egg bounce, 7 cannons, slot spin, UI animations.

| Task | Animator | Weeks | Notes |
|------|----------|-------|-------|
| Bird flight B1–B4 | Anim 1 | 2–3 | Flap, glide; per BirdConfig |
| Egg bounce / wobble | Anim 2 | 1–2 | Optional; physics can suffice |
| Cannon fire / recoil (7) | Anim 1 | 2 | Muzzle flash; recoil; per cannon |
| Powerup slot spin | Anim 2 | 1 | Slot machine reel animation |
| UI: score pop, combo | Anim 2 | 1–2 | Score delta; combo "x2" reveal |

**Deliverables:** Final animation clips; controller setup; integration notes.  
**Dependencies:** Requires Art 1–2; Dev integration in Phase 5.

---

### Phase 4: VFX Production (Days 3–16, within 20-day window)
**Owner:** VFX Team  
**Goal:** Core gameplay FX; powerup-specific FX (~10–15); bullet trails. **Powerup icons depend on these.**

| Task | Owner | Weeks | Notes |
|------|-------|-------|-------|
| Hit particle (egg/bird) | VFX | 1 | Small burst; HitFXController.hitParticlePrefab |
| Egg destroy particle | VFX | 1–2 | Larger burst; optional shards |
| Bird destroy particle | VFX | 1–2 | Feathers + burst |
| Ground bounce smoke | VFX | 0.5 | Egg.smokeParticle |
| Bullet trails (per type) | VFX | 1–2 | Optional per bullet type |
| Powerup FX (fire, lightning, frost, etc.) | VFX | 3–4 | ~10–15 effect types; **Art uses as reference for powerup icons** |
| Screen shake (optional) | VFX | 0.5 | On destroy; CameraController |

**Deliverables:** Prefab VFX; assigned in HitFXController/Egg; powerup FX for icon alignment.  
**Dependencies:** Powerup icons (Art) block on powerup VFX completion.

---

### Phase 5: Integration & Polish (Days 10–20)
**Owner:** Devs (2) + Art (1) + Anim (1)  
**Goal:** Merge art, animation, VFX; 30 BGs, 7 cannons, bullets, powerup icons; balance and polish.

| Task | Owner | Weeks | Notes |
|------|-------|-------|-------|
| Art import & prefab setup | Dev 1 | 2 | Eggs, birds, 30 BGs, 7 cannons, 8+ bullets, powerup icons |
| Animation controller setup | Dev 1 + Anim | 1 | Bird, 7 cannons, slot spin, UI |
| VFX prefab assignment | Dev 2 | 1 | HitFXController; Egg; powerup FX |
| Audio assignment | Dev 2 | 0.5 | SFXController; shoot, hit, destroy |
| Balancing pass | Dev 3 | 1 | Spawn rate, pressure, target score |
| Performance pass | Dev 3 | 0.5 | Pooling; draw calls; frame rate |

**Deliverables:** Integrated build; all assets in place; playable end-to-end.  
**Handoff:** Build to QA for Phase 6.

---

### Phase 6: QA Execution (Days 21–27)
**Owner:** QA (+ Dev support)  
**Goal:** Execute test cases; log bugs; regress.

| Days | Focus | Notes |
|------|-------|-------|
| 21–23 | Core gameplay (QA-01–06) | Egg/bird hit, destroy, split, score |
| 23–25 | Projectiles + FX/Audio (QA-07–15) | Bounce, pierce, VFX, SFX |
| 25–27 | Spawn + Cannon + Edge (QA-16–27) | 7 cannons, pressure, control, regressions |
| 27–30 | Full regression + release | All platforms; 30 BGs; powerup icons; performance |

**Deliverables:** Bug list; pass/fail per test case; QA report.  
**Reference:** `docs/GDD_Artist_QA_Pitch.md` Section 6.

---

### Phase 7: Bug Fix & QA Sign-Off (Days 25–30)
**Owner:** Devs (all) + QA  
**Goal:** Fix P0/P1 bugs; re-test; obtain sign-off.

| Days | Focus | Notes |
|------|-------|-------|
| 25–27 | P0 bug fix | Crashes; blockers; critical logic |
| 27–29 | P1 bug fix | UX; polish; important flows |
| 29–30 | P2 triage + release | Fix high-value; defer rest; QA sign-off; release |

**Deliverables:** Production-ready build; QA sign-off; release notes draft.

---

## 5. Milestone Summary

| Milestone | Target (30-day schedule) | Criteria |
|-----------|--------------------------|----------|
| **M1: Feature Complete** | Day 2–3 | HP display, floating score, combo, cannon loss, shoot SFX |
| **M2: Art + Integration Complete** | Day 20 | Eggs, birds, 30 BGs, 7 cannons, 8+ bullets, 29–30 powerup icons; VFX; integrated build |
| **M3: QA Complete** | Day 27 | All test cases run; bug list finalized |
| **M4: Production Ready** | Day 30 | P0/P1 resolved; QA sign-off; release |

---

## 6. Dependency Map (30-Day Schedule)

```
Days 1–3:   Phase 1 (Dev) → M1: Feature Complete
Days 1–20:  Phase 2 (Art) | Phase 3 (Anim) | Phase 4 (VFX) — parallel
Days 10–20: Phase 5 (Integration) — overlaps as assets land
Day 20:     M2: Art + Integration Complete
Days 21–30: Phase 6 (QA) + Phase 7 (Bug Fix)
Day 30:     M4: Production Ready
```

**Key dependencies:**
- Animation needs bird/egg art
- Powerup icons (Art) depend on powerup VFX (VFX) — icons must visually match particle FX
- Integration needs Art, Anim, VFX
- QA needs stable integration build

---

## 7. Asset Scope Summary

| Asset | Count | Owner |
|-------|-------|-------|
| Chapter backgrounds | 30 | Art |
| Cannons | 7 | Art |
| Bullets | 8+ | Art |
| Powerup icons | 29–30 | Art (after VFX) |
| Powerup FX | ~10–15 | VFX |

---

## 8. Risk & Buffer

| Risk | Mitigation | Buffer |
|------|------------|--------|
| Art scope creep | Lock asset list at M1 | +1 week Art |
| VFX iteration | Early greybox; iterate | +1 week VFX |
| QA bug volume | Fix as you go in Phase 5 | +1 week Phase 7 |
| Scope changes | Freeze features at M1 | — |

**30-day constraint:** 20 days art + integration; 10 days QA + release. No buffer — scope must be tightly scoped or parallelized.

---

## 9. Weekly Cadence (Suggested)

| Day | Activity |
|-----|----------|
| Mon | Sprint planning; priority alignment |
| Tue–Thu | Execution |
| Fri | Integration; build to QA/stakeholders |
| Bi-weekly | Sprint review; retrospective |

---

## 10. Sign-Off Checklist

- [ ] M1: Feature complete by Day 3 (Dev lead)
- [ ] M2: Art + integration complete by Day 20 (Dev + Art lead)
- [ ] M3: QA complete by Day 27 (QA lead)
- [ ] M4: Production ready by Day 30 (QA sign-off; release)

---

*30-day schedule: 20 days art + integration; 10 days QA + release. Art, VFX, and Anim run in parallel.*
