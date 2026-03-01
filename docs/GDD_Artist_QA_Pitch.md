# Bird Hunter — GDD for Artists & QA
## Pitch-Ready Game Design Document

**Version:** 1.0  
**Last Updated:** February 2025  
**Audience:** Artists, QA, Stakeholders  
**Genre:** Ball Blast–style cannon shooter with birds & eggs

---

## 1. One-Page Pitch

### Elevator Pitch
**Bird Hunter** is a hypercasual cannon shooter inspired by Ball Blast. Players control a cannon at the bottom, shoot at bouncing eggs (and birds), destroy targets to score, and trigger satisfying split cascades—E4 splits into 2×E3, E3 into 2×E2, and so on until eggs vanish. Birds fly across the sky and lay eggs into the play area, adding tension and variety.

### Core Loop
1. **Move** cannon left/right  
2. **Aim** and **shoot** projectiles at eggs and birds  
3. **Destroy** targets → score + split cascade  
4. **Reach** target score to complete the level  
5. **Proceed** through chapters with powerups and new cannons  

### Key Differentiators
- **Birds + Eggs** — Birds lay eggs; both are shootable (vs Ball Blast’s shapes-only)
- **Physics Eggs** — Eggs bounce off ground and walls (vs static falling shapes)
- **Bouncing Projectiles** — Bullets can bounce to nearby targets
- **Chapters & Levels** — Structured progression with target score (vs endless runs)

---

## 2. Visual Style Guide (Artist Reference)

### Mood & Tone
- **Vibrant, casual, readable** — High contrast for fast-paced gameplay
- **Juicy feedback** — Hit/explode FX should feel punchy
- **Consistent theme** — Per-chapter backgrounds (e.g., desert, forest) via `ThemeManager`

### Color Guidelines
- **Eggs** — Tier-based: E1 (smallest) lightest; E4 (largest) darkest/saturated
- **UI** — High contrast against gameplay; score readable at a glance
- **FX** — Bright bursts on hit; softer particle trails on destroy

### Reference
- Ball Blast: numbered shapes, clear readability, satisfying pops  
- Our twist: egg shapes, bird silhouettes, physics bounce

---

## 3. Art Asset Specifications

### 3.1 Eggs (E1–E4)

| Tier | Current Implementation | Spec | Notes |
|------|------------------------|------|-------|
| **E1** | Base sprite via `EggTierConfig.sprite` | Smallest; no split; disappears on destroy | Final tier in cascade |
| **E2** | Same | Mid-small; splits into 2×E1 | |
| **E3** | Same | Mid-large; splits into 2×E2 | |
| **E4** | Same | Largest; splits into 2×E3 | Entry tier from birds |

**Format:** Sprite, same pivot (center), scalable. Physics uses `PolygonCollider2D`.

**Current:** HP is not shown on eggs.  
**Suggestion:** Add HP/score number overlay (e.g., "4" on E4) for Ball Blast readability. Implement as child UI Text or TMP at egg center; update on hit.

---

### 3.2 Birds (B1–B4)

| Type | Current Implementation | Spec | Notes |
|------|------------------------|------|-------|
| **B1** | `BirdConfig` + prefab | Lay E1; lowest threat | |
| **B2** | Same | Lay E2; medium | |
| **B3** | Same | Lay E3; higher threat | |
| **B4** | Same | Lay E4; highest threat | |

**Format:** Animated sprite or spritesheet; `BoxCollider2D`; flight path follows `BirdConfig` movement (normal, chase, zigzag, etc.).

---

### 3.3 Backgrounds

| Asset | Current Implementation | Spec |
|-------|------------------------|------|
| **Chapter themes** | `ThemeManager` + `ChapterTheme` list; 1 background per chapter | Sprite, full-screen; aspect ratio 16:9 or letterbox-safe |
| **Assignment** | `backgroundRenderer.sprite = GetThemeForChapter(chapterNumber)` | Per-chapter sprite |

**Suggestion:** Add 2–3 background variants per chapter (e.g., day/night, weather) for replay variety. Would require extending `ChapterTheme` to support multiple sprites and a simple rotation rule.

---

### 3.4 Cannon & Projectiles

| Asset | Current Implementation | Spec |
|-------|------------------------|------|
| **Cannon** | Prefab with `CannonFire` | Base + muzzle; should convey fire direction |
| **Bullet** | Prefab; travels `transform.up` | Small, readable; optional trail |
| **BigBullet** | Larger variant | Same style, scaled up |

---

### 3.5 UI Assets

| Element | Current Implementation | Spec |
|---------|------------------------|------|
| **Score text** | TMP; `ScoreManager.scoreText` | Large, readable; "Score: XXXX" |
| **Delta text** | `ScoreUIController.deltaText` (optional) | "+X" pop; short duration |
| **Level/countdown** | Legacy `countdownText` | Countdown during transitions |

**Suggestion:** Add floating "+X" score pop at destroy position (Ball Blast style). Current: only HUD score. Suggestion: world-space TMP at hit point, tween up + fade; subscribe to `OnEggDestroyed` / `OnBirdDestroyed` with score value.

---

## 4. VFX Specifications

### 4.1 Current Implementation
- `HitFXController` subscribes to `OnEggHit`, `OnEggDestroyed`, `OnBirdHit`, `OnBirdDestroyed`
- Optional prefabs: `hitParticlePrefab`, `eggDestroyParticlePrefab`, `birdDestroyParticlePrefab`
- Legacy: `blastparticle` in `EggHealth`; `GamePoolManager.blastParticlePool` for birds
- `Egg` has optional `smokeParticle` on ground bounce

### 4.2 VFX Asset Requirements

| Event | Prefab | Suggested Look | Duration |
|-------|--------|----------------|----------|
| **Egg hit** | `hitParticlePrefab` | Small spark/burst at impact | 0.2–0.5s |
| **Egg destroy** | `eggDestroyParticlePrefab` | Larger burst; optional shell shards | 0.5–1.5s |
| **Bird hit** | Same as egg hit | Small burst | 0.2–0.5s |
| **Bird destroy** | `birdDestroyParticlePrefab` | Feathers + burst | 0.5–1.5s |
| **Ground bounce** | `Egg.smokeParticle` | Dust/smoke puff | 0.3–0.5s |

**Format:** Unity Particle System prefabs; auto-destroy after duration (currently 3s in code; can be shortened for snappier feel).

**Suggestion:** Differentiate egg hit vs destroy intensity. Current: same or similar. Suggestion: lighter hit FX; stronger destroy FX (scale, particle count, color).

---

## 5. Audio Specifications

### 5.1 Current Implementation
- `SFXController` subscribes to hit/destroy events
- Optional clips: `hitSound`, `eggDestroySound`, `birdDestroySound`
- `AudioSource` auto-created if missing

### 5.2 Audio Asset Requirements

| Event | Clip | Suggested Character | Notes |
|-------|------|---------------------|-------|
| **Egg hit** | `hitSound` | Short, punchy; "thud" or "crack" | < 0.2s |
| **Egg destroy** | `eggDestroySound` | Satisfying pop/crack | 0.2–0.4s |
| **Bird hit** | Same as egg hit or variant | Short impact | < 0.2s |
| **Bird destroy** | `birdDestroySound` | Slightly different from egg | 0.2–0.4s |
| **Shoot** | Not in SFXController | Cannon fire; rapid-fire compatible | Staccato |
| **Coin collect** | Not wired | Soft "cha-ching" | For future economy |

**Format:** WAV/MP3; short, low latency; consider pooling for rapid hits.

**Suggestion:** Add shoot SFX to event flow. Current: no shoot event in `GameEvents`. Suggestion: add `OnCannonFired` and wire `SFXController`; optional pitch/volume variation for rapid fire.

---

## 6. QA Test Cases

### 6.1 Core Gameplay

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-01 | Egg hit reduces HP | Shoot egg; verify HP decreases | Egg takes damage; no destroy if HP > 0 | P0 |
| QA-02 | Egg destroy on HP = 0 | Shoot egg until HP = 0 | Egg destroyed; split or vanish; score increases | P0 |
| QA-03 | Split cascade E4→E3→E2→E1 | Destroy E4, then E3s, E2s, E1s | Correct split count and tier each step | P0 |
| QA-04 | Bird hit/destroy | Shoot bird until destroyed | Bird takes damage; on destroy, score increases; bird removed | P0 |
| QA-05 | Score display updates | Destroy eggs/birds | Score UI updates; `ScoreManager.CurrentScore` correct | P0 |
| QA-06 | Level complete | Reach target score | Level complete triggered; next level or slot machine | P0 |

### 6.2 Projectiles

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-07 | Bullet hits egg | Fire at egg | Bullet deals damage; bounces if enabled | P0 |
| QA-08 | Bullet bounce | Fire at egg; bullet bounces | Bullet seeks nearby target; doesn’t re-hit same target | P1 |
| QA-09 | Bullet pierce | With pierce chance, hit egg | Egg destroyed; bullet continues or stops per config | P2 |
| QA-10 | BigBullet one-shot | Fire BigBullet at egg | Egg destroyed; bullet destroyed | P1 |

### 6.3 FX & Audio

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-11 | Hit FX plays | Shoot egg/bird | Hit particle spawns at impact | P1 |
| QA-12 | Destroy FX plays | Destroy egg/bird | Destroy particle spawns; auto-removed | P1 |
| QA-13 | Hit SFX plays | Shoot egg/bird | Hit sound plays | P1 |
| QA-14 | Destroy SFX plays | Destroy egg/bird | Destroy sound plays | P1 |
| QA-15 | Rapid fire | Shoot rapidly | No audio overlap/clipping; FX performant | P2 |

### 6.4 Spawning & Pressure

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-16 | Birds spawn | Start level; wait | Birds spawn at configured interval | P0 |
| QA-17 | Birds lay eggs | Let birds fly | Eggs appear at bird positions | P0 |
| QA-18 | Pressure cap | Let eggs accumulate | Spawn rate / pressure respects `pressureMax` | P1 |
| QA-19 | Relief mode | Build pressure then clear eggs | Relief mode reduces spawn rate as intended | P2 |

### 6.5 Cannon & Control

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-20 | Cannon moves | Move input | Cannon moves left/right within bounds | P0 |
| QA-21 | Cannon aims | Aim input | Fire direction matches aim | P0 |
| QA-22 | Cannon fires | Fire input | Projectile spawns and travels | P0 |
| QA-23 | Theme change | Load new chapter | Background updates per chapter | P1 |

### 6.6 Edge Cases & Regression

| ID | Test Case | Steps | Expected | Priority |
|----|-----------|-------|----------|----------|
| QA-24 | Multiple rapid hits | Hit same egg multiple times quickly | HP correct; no double-count score | P0 |
| QA-25 | Destroy during split | Destroy egg as split spawns | No duplicate score; no orphan objects | P1 |
| QA-26 | Scene reload | Complete level; reload scene | No duplicate managers; clean state | P1 |
| QA-27 | Missing prefabs | Remove FX/audio prefabs | No null refs; graceful fallback | P2 |

---

## 7. Suggestions & Improvements

### 7.1 HP/Score Display on Eggs

**Current:** HP is internal; no visual feedback except destroy.  
**Suggestion:** Add a number (HP or score) on each egg, Ball Blast style. Improves readability and feedback.  
**Implementation:** Child TMP on egg prefab; `EggHealth` or `Egg` updates text on `TakeDamage` / `Init`.

---

### 7.2 Floating Score Pop

**Current:** Score updates only in HUD; no world-space feedback.  
**Suggestion:** Spawn "+X" text at destroy position; tween up and fade.  
**Implementation:** `ScoreUIController` or new `FloatingScoreView` subscribes to `OnEggDestroyed` / `OnBirdDestroyed`; uses score value from event payload.

---

### 7.3 Hit vs Destroy FX Differentiation

**Current:** Same or similar FX for hit and destroy; `hitParticlePrefab` vs `eggDestroyParticlePrefab` but behavior similar.  
**Suggestion:** Lighter hit FX (small burst); stronger destroy FX (larger burst, more particles, optional shards).  
**Implementation:** Artist provides distinct prefabs; `HitFXController` already supports separate prefabs; tune scale/count in prefabs.

---

### 7.4 Shoot SFX Integration

**Current:** No shoot event in `GameEvents`; shoot SFX not wired to event system.  
**Suggestion:** Add `OnCannonFired` to `GameEvents`; `SFXController` subscribes and plays shoot clip.  
**Implementation:** Fire event from `CannonFire` or bullet spawn; `SFXController` plays shoot clip with optional pitch variation.

---

### 7.5 Combo Visual Feedback

**Current:** No combo system; rapid hits have no extra feedback.  
**Suggestion:** Combo multiplier for rapid hits; "x2", "x3" pop; optional screen shake or color flash.  
**Implementation:** `ComboController` subscribes to `OnEggHit` / `OnBirdHit`; tracks time since last hit; multiplies score and drives UI/UX feedback.

---

### 7.6 Cannon Damage/Loss Condition

**Current:** Cannon has no health; eggs/birds hitting cannon have no consequence (or handled only in legacy flow).  
**Suggestion:** Add cannon health; eggs/birds hitting cannon deal damage; 0 HP = game over (Ball Blast dodging mechanic).  
**Implementation:** `CannonHealth` (or extend existing); collision with eggs/birds; `OnCannonHit` or new `OnCannonDamaged`; UI for health.

---

### 7.7 Background Variety Per Chapter

**Current:** One background per chapter via `ThemeManager`.  
**Suggestion:** 2–3 variants per chapter (e.g., time of day, weather) for variety.  
**Implementation:** Extend `ChapterTheme` with multiple sprites; pick by level index or random.

---

### 7.8 Coin Drop & Collect VFX

**Current:** `Coin.cs` exists; economy partially in legacy flow; no events for coins.  
**Suggestion:** Coins drop at destroy position; cannon must move to collect; VFX on drop and collect.  
**Implementation:** Add `OnCoinDropped`, `OnCoinCollected` to `GameEvents`; coin prefab with collect radius; FX at drop and collect.

---

## 8. Asset Checklist (Artist Handoff)

### Required for MVP
- [ ] Egg sprites E1–E4 (with optional HP overlay)
- [ ] Bird sprites/animations B1–B4
- [ ] Hit particle prefab
- [ ] Egg destroy particle prefab
- [ ] Bird destroy particle prefab
- [ ] Hit SFX
- [ ] Egg destroy SFX
- [ ] Bird destroy SFX
- [ ] Chapter background(s)
- [ ] Cannon sprite/prefab
- [ ] Bullet sprite/prefab

### Nice to Have
- [ ] Floating "+X" score pop asset
- [ ] Shoot SFX
- [ ] Combo "x2", "x3" UI
- [ ] Coin drop/collect VFX
- [ ] Background variants per chapter

---

## 9. QA Sign-Off Template

| Phase | Date | QA Lead | Notes |
|-------|------|---------|-------|
| Core gameplay | | | |
| FX/Audio | | | |
| Edge cases | | | |
| Regression | | | |
| Release candidate | | | |

---

*This document is intended for artists and QA. For technical architecture, see `GDD_Event-Driven_Architecture.md`.*
