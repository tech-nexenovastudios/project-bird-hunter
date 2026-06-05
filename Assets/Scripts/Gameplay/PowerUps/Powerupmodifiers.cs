using Gameplay.Birds;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.Player;
using ImprovedTimers;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEngine;


namespace Gameplay.PowerUps
{
    // ═════════════════════════════════════════════════════════
    //  CANNON STAT MODIFIER  (consolidated — 7 power-ups)
    //
    //  #4 Vitality Boost, #7 Instant Heal, #8 Power Surge,
    //  #10 Phase Shield, #20 Compact Frame, #27 Hit Barrier,
    //  #29 Second Chance
    //
    //  All follow the same pattern: write a value to ICannon
    //  on activate, revert on deactivate. No wasted fields.
    // ═════════════════════════════════════════════════════════

    public enum CannonStat
    {
        Heal,
        MaxHp,
        AttackPercent,
        Invincible,
        HitboxScale,
        ShieldHits,
        Revive,
        ManaFillRate,
        FireRatePercent
        //Use follwing line of code when its about to increase mana fill rate
        //mana += baseFillRate * (1f + cannon.ManaFillRateBonus / 100f) * Time.deltaTime;
    }


    [Serializable]
        public class CannonStatModifier : ICannonModifier
        {
            public CannonStat stat;
            public float value = 1f;

            [Tooltip("0 = permanent until deactivated")]
            [Min(0f)] public float duration;

            [Header("Heal Settings")]
            [Tooltip("Cooldown between heals (only used for Heal stat)")]
            [Min(0f)] public float healCooldown = 20f;

            [Header("Invincible Settings")]
            [Tooltip("VFX prefab spawned at the cannon while invincibility is active. Destroyed when the duration ends.")]
            public GameObject invincibleVfxPrefab;

            [Tooltip("Local offset (relative to the cannon) where the invincibility VFX is placed.")]
            public Vector3 invincibleVfxOffset = new Vector3(0f, -0.5f, 0f);

            private ICannon cannon;
            private IntervalTimer timer;
            private FrequencyTimer eggWaitTimer;
            private GameObject activeInvincibleVfx;
            private float previousScale;

            // How often the armed shield scans the scene for the first egg.
            private const int EggPollTicksPerSecond = 10;

            private float lastHealTime = -999f;       // ensures first heal works

            public void Activate(ICannon cannon)
            {
                this.cannon = cannon;

                switch (stat)
                {
                    case CannonStat.ManaFillRate:
                        cannon.ManaFillRateBonus += value;
                        break;

                    case CannonStat.Heal:
                        TryHeal();
                        break;

                    case CannonStat.MaxHp:
                        // value is a percent of the cannon's base max HP, not a flat amount.
                        cannon.IncreaseMaxHpByPercent(value);
                        break;

                    case CannonStat.AttackPercent:
                        cannon.AddAttackModifier(0f, value);
                        break;

                    case CannonStat.FireRatePercent:
                        cannon.AddFireRateModifier(value);
                        GameLogger.Log(LogCategory.Powerup, $"[RapidFire] Activated — +{value}% fire rate for this level");
                        break;

                    case CannonStat.Invincible:
                        ActivateInvincible();
                        break;

                    case CannonStat.HitboxScale:
                        previousScale = cannon.HitboxScale;
                        cannon.HitboxScale *= value;
                        break;

                    case CannonStat.ShieldHits:
                        cannon.ShieldHits = Mathf.RoundToInt(value);
                        break;

                    case CannonStat.Revive:
                        cannon.HasRevive = true;
                        cannon.ReviveHealthPercent = value;
                        break;
                }
            }

            private void TryHeal()
            {
                if (Time.time < lastHealTime + healCooldown)
                    return;

                cannon.Heal(Mathf.RoundToInt(value));
                lastHealTime = Time.time;

                if (healCooldown > 0f)
                    GameEvents.FirePowerupCooldownStarted(healCooldown);
            }

            // Activate still runs at the level-intro countdown's "Go!" beat (GameManager defers
            // ApplyPowerUpsToCurrentCannon to OnLevelCountdownGo), but the shield no longer turns
            // on there — it only ARMS a poll that waits for the first object tagged "Egg" to enter
            // the playfield. Tag scan (not GameEvents.OnEggSpawned) so boss egg drops and splits
            // count too, and the shield window isn't wasted on the egg-free opening seconds of a
            // level. Once an egg exists, shield + VFX run for the flat duration window, then
            // Deactivate strips them. The slot is persistent (see GameProgressManager), so this
            // re-arms at the start of every level until the player picks a different powerup.
            private void ActivateInvincible()
            {
                GameLogger.Log(LogCategory.Powerup,
                    "[PhaseShield] Armed — waiting for the first egg to enter the playfield");

                eggWaitTimer = new FrequencyTimer(EggPollTicksPerSecond);
                eggWaitTimer.OnTick = CheckForFirstEgg;
                eggWaitTimer.Start();
            }

            private void CheckForFirstEgg()
            {
                if (GameObject.FindGameObjectWithTag(TagManager.EggTag) == null) return;

                StopEggWaitTimer();
                StartInvincibleWindow();
            }

            private void StartInvincibleWindow()
            {
                if (cannon == null) return;

                cannon.IsInvincible = true;
                SpawnInvincibleVfx();

                float window = duration > 0f ? duration : 5f;
                GameLogger.Log(LogCategory.Powerup,
                    $"[PhaseShield] First egg in scene — shield ON, invincible for {window:F0}s");

                timer = new IntervalTimer(window, window);
                timer.OnTimerStop = Deactivate;
                timer.Start();
            }

            private void SpawnInvincibleVfx()
            {
                if (invincibleVfxPrefab == null || cannon == null) return;
                if (activeInvincibleVfx != null) return;

                activeInvincibleVfx = UnityEngine.Object.Instantiate(
                    invincibleVfxPrefab,
                    cannon.Transform);
                activeInvincibleVfx.transform.localPosition = invincibleVfxOffset;
                activeInvincibleVfx.transform.localRotation = Quaternion.identity;
            }

            private void DespawnInvincibleVfx()
            {
                if (activeInvincibleVfx == null) return;
                UnityEngine.Object.Destroy(activeInvincibleVfx);
                activeInvincibleVfx = null;
            }

            private void StopActiveTimer()
            {
                if (timer == null) return;
                // ImprovedTimers.Timer.Stop() invokes OnTimerStop unconditionally — assign empty
                // delegates instead of null to detach our handlers without throwing NRE.
                timer.OnInterval = delegate { };
                timer.OnTimerStop = delegate { };
                timer.Stop();
                timer = null;
            }

            private void StopEggWaitTimer()
            {
                if (eggWaitTimer == null) return;
                eggWaitTimer.OnTick = delegate { };
                eggWaitTimer.OnTimerStop = delegate { };
                eggWaitTimer.Stop();
                eggWaitTimer = null;
            }

            public void Deactivate()
            {
                StopActiveTimer();
                StopEggWaitTimer();
                DespawnInvincibleVfx();

                if (cannon == null) return;

                switch (stat)
                {
                    case CannonStat.ManaFillRate:
                        cannon.ManaFillRateBonus -= value;
                        break;

                    case CannonStat.AttackPercent:
                        cannon.RemoveAttackModifier(0f, value);
                        break;

                    case CannonStat.FireRatePercent:
                        cannon.RemoveFireRateModifier(value);
                        GameLogger.Log(LogCategory.Powerup, $"[RapidFire] Deactivated — -{value}% fire rate (level transition / displaced)");
                        break;

                    case CannonStat.Invincible:
                        cannon.IsInvincible = false;
                        GameLogger.Log(LogCategory.Powerup,
                            "[PhaseShield] Shield OFF (5s window ended / level transition / displaced)");
                        break;

                    case CannonStat.HitboxScale:
                        cannon.HitboxScale = previousScale;
                        break;

                    case CannonStat.ShieldHits:
                        cannon.ShieldHits = 0;
                        break;

                    case CannonStat.Revive:
                        cannon.HasRevive = false;
                        cannon.ReviveHealthPercent = 0f;
                        break;
                }

                cannon = null;
            }
        }
    
    // ═════════════════════════════════════════════════════════
    //  BULLET MODIFIER  (consolidated — 6 power-ups)
    //
    //  #5 Ricochet, #6 Twin Spread, #11 Laser,
    //  #12 Dual Shot, #13 Pierce, #14 Rocket
    //
    //  IProjectileModifier already separates ModifyBullet()
    //  from ExtraProjectiles. Each mode uses the right subset.
    // ═════════════════════════════════════════════════════════

    public enum BulletModType
    {
        AddBounce,
        AddPierce,
        SpreadShot,
        ExtraShot,
        SetRocket
    }

    [Serializable]
    public class BulletModifier : IProjectileModifier
    {
        public BulletModType mode;
        public float value = 1f;

        [Header("Rocket Settings (only used when mode = SetRocket)")]
        public GameObject rocketPrefab;
        public float rocketSpeeed = 10f;

        [Min(0.5f)] public float rocketInterval = 4f;
        [Min(0f)] public float intervalVariance = 1f;

        public bool IsActive { get; private set; }

        public int ExtraProjectiles => mode switch
        {
            BulletModType.SpreadShot => 2,
            BulletModType.ExtraShot => 1,
            _ => 0
        };

        // Progressive DPS uplift derived from this mod's serialized `value`.
        // Spread/Extra return 1f — the volley's damage is split equally across all
        // bullets (BaseCannon.SpawnBullet), so extra bullets don't add total DPS.
        public float DpsMultiplier => mode switch
        {
            // Each bounce ~40% effective rehit on a fresh target. Scales linearly with bounce count.
            BulletModType.AddBounce  => 1f + 0.4f * value,
            // value is instakill chance (0..1). Expected DPS = base / (1 - p), capped at 5×
            // so designer-set value=1.0 doesn't yield a 100× multiplier that swamps the gate.
            BulletModType.AddPierce  => Mathf.Min(5f, 1f / Mathf.Max(0.01f, 1f - Mathf.Clamp01(value))),
            BulletModType.SpreadShot => 1f,
            BulletModType.ExtraShot  => 1f,
            // Rocket fires every rocketInterval seconds at attack × value damage.
            // Normalized against an assumed baseline fire rate of 4 shots/sec.
            BulletModType.SetRocket  => 1f + (value / Mathf.Max(0.5f, rocketInterval * 4f)),
            _ => 1f
        };

        private float[] cachedAngles;
        private ICannon cannon;
        private float rocketTimer;

        public void Activate()
        {
            IsActive = true;
            rocketTimer = 0f;  // Fire on first available tick
            UnityEngine.Debug.Log($"[BulletModifier] Activated. Mode={mode}, IsActive={IsActive}");
        }

        public void Deactivate()
        {
            IsActive = false;
            cannon = null;
        }

        public float[] GetExtraAngles()
        {
            switch (mode)
            {
                case BulletModType.SpreadShot:
                    cachedAngles ??= new float[2];
                    cachedAngles[0] = -value;
                    cachedAngles[1] = value;
                    return cachedAngles;
                case BulletModType.ExtraShot:
                    // Dual Shot — two bullets symmetric around the gun's aim.
                    // The primary bullet is redirected to -value in ModifyBullet,
                    // and we hand out only +value here so the extra spawn flies
                    // on the other side. Total: 2 bullets, no straight shot.
                    cachedAngles ??= new float[1];
                    cachedAngles[0] = value;
                    return cachedAngles;
                default:
                    return System.Array.Empty<float>();
            }
        }

        public void ModifyBullet(BaseBullet bullet, int cannonAttack)
        {
            switch (mode)
            {
                case BulletModType.AddBounce:
                    if (bullet is BouncingBullet b) b.bounceCount += Mathf.RoundToInt(value);
                    break;
                case BulletModType.AddPierce:
                    bullet.instantKillChance = value;
                    break;
                case BulletModType.ExtraShot:
                    // Re-aim the primary bullet to -value so the dual-shot pair fans
                    // symmetrically. CreateBullet has already set transform.rotation
                    // and the rigidbody's velocity from the gun's firing direction —
                    // we rotate on top of that and recompute linearVelocity so both
                    // physics-based bullets and Update-driven SimpleBullets fly the
                    // new heading.
                    bullet.transform.rotation *= Quaternion.Euler(0f, 0f, -value);
                    var rb = bullet.GetComponent<Rigidbody2D>();
                    if (rb != null)
                        rb.linearVelocity = bullet.transform.up * bullet.bulletSpeed;
                    break;
            }
        }

        public void Tick(ICannon cannon)
        {
            if (!IsActive)
            {
                // Uncomment below if you never see ANY log from Tick
                // UnityEngine.Debug.Log("[BulletModifier] Tick skipped — not active");
                return;
            }
            if (cannon == null)
            {
                UnityEngine.Debug.Log("[BulletModifier] Tick skipped — cannon is null");
                return;
            }
            if (mode != BulletModType.SetRocket) return;

            this.cannon = cannon;

            rocketTimer -= Time.deltaTime;
            if (rocketTimer <= 0f)
            {
                SpawnRocket();
                rocketTimer = rocketInterval + UnityEngine.Random.Range(-intervalVariance, intervalVariance);
                UnityEngine.Debug.Log($"[BulletModifier] Next rocket in {rocketTimer:F1}s");
            }
        }

        private void SpawnRocket()
        {
            if (rocketPrefab == null || cannon == null) return;

            GameObject go = UnityEngine.Object.Instantiate(
                rocketPrefab,
                cannon.Transform.position,
                Quaternion.identity
            );

            BaseBullet bullet = go.GetComponent<BaseBullet>();
            if (bullet != null)
            {
                bullet.damage = cannon.CurrentAttack * value;
                bullet.bulletSpeed = rocketSpeeed; ;  // ← ADD THIS (or whatever speed you want)
            }
        }
    }


    // ═════════════════════════════════════════════════════════
    //  BULLET ELEMENT MODIFIER  (separate — different purpose)
    //
    //  #2 Lightning Shot, #3 Incendiary Shot
    //  (and future: Fire Bullet, Frost Bullet, Poison Bullet…)
    //
    //  Attaches an IEffect<IEntity> to every bullet fired.
    //  When the bullet hits a target, the effect triggers
    //  (chain lightning, burn DOT, freeze, etc).
    //
    //  Each bullet gets a FRESH clone of the template effect
    //  so DOT timers don't conflict between bullets.
    //
    //  This is separate from BulletModifier because it does
    //  something fundamentally different: BulletModifier changes
    //  bullet properties (bounce, damage, count). This one
    //  attaches runtime behavior that fires on impact.
    // ═════════════════════════════════════════════════════════

    [Serializable]
    public class BulletElementModifier : IProjectileModifier
    {
        [Tooltip("The effect to attach to each bullet. Gets cloned per bullet.")]
        [SerializeReference] public IEffect<IEntity> effectTemplate;

        [Tooltip("Optional: override bullet damage when this element is active.")]
        public bool overrideDamage;
        [Min(0)] public float damageOverride;

        [Tooltip("VFX prefab to attach to the bullet (lightning sparks, fire trail, ice crystals…).")]
        public GameObject elementVfxPrefab;

        public bool IsActive { get; private set; }
        public int ExtraProjectiles => 0;

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;
        public float[] GetExtraAngles() => Array.Empty<float>();

        // On-hit effects (DoT, chain, AoE) add per-hit damage that the bullet's base
        // damage doesn't capture. We estimate uplift from the effectTemplate's own
        // fields so designer tuning flows through automatically. Uplift is expressed
        // as a fraction of a nominal baseline (10) — close enough for the gate's
        // purpose without coupling to live cannon attack.
        public float DpsMultiplier
        {
            get
            {
                const float baseline = 10f;
                float uplift = effectTemplate switch
                {
                    DamageEffect d            => d.damageAmount / baseline,
                    DamageOverTimeEffect dot  => dot.tickInterval > 0f
                                                 ? (dot.damagePerTick * (dot.duration / dot.tickInterval)) / baseline
                                                 : 0f,
                    MultiHitDamageEffect mh   => (mh.hitCount * mh.damagePerHit) / baseline,
                    ChainLightningEffect cl   => cl.baseDamage * (1f + cl.damageDecay * (cl.maxChains - 1)) / baseline,
                    AreaDamageEffect ad       => ad.damage / baseline,
                    FreezeEffect fz           => 0.2f + (fz.damagePerTick * fz.duration / Mathf.Max(0.1f, fz.tickInterval)) / baseline,
                    _                          => 0.15f
                };
                return 1f + Mathf.Max(0f, uplift);
            }
        }

        public void ModifyBullet(BaseBullet bullet, int cannonAttack)
        {
            if (effectTemplate != null)
            {
                IEffect<IEntity> clone = CloneEffect(effectTemplate);
                if (clone != null) bullet.AddOnHitEffect(clone);
            }

            if (overrideDamage)
                bullet.damage = damageOverride;

            if (elementVfxPrefab != null)
            {
                // FIX: Ensure the VFX is instantiated at the bullet's local zero 
                // to prevent it spawning far away if the bullet is already moving.
                GameObject vfx = UnityEngine.Object.Instantiate(
                    elementVfxPrefab,
                    bullet.transform.position,
                    bullet.transform.rotation,
                    bullet.transform);

                // Ensure scale is correct and it's on a visible Z layer
                vfx.transform.localPosition = Vector3.zero;

                bullet.AttachElementVfx(vfx);
            }
        }

        private static IEffect<IEntity> CloneEffect(IEffect<IEntity> template)
        {
            if (template == null) return null;

            try
            {
                string json = JsonUtility.ToJson(template);
                object clone = JsonUtility.FromJson(json, template.GetType());
                return clone as IEffect<IEntity>;
            }
            catch
            {
                return Activator.CreateInstance(template.GetType()) as IEffect<IEntity>;
            }
        }
    }

    // ═════════════════════════════════════════════════════════
    //  REACTIVE EFFECTS  (split — each has different state,
    //  different triggers, different event signatures)
    // ═════════════════════════════════════════════════════════

    // ── #16 Retaliation Core ─────────────────────────────────
    // State: boost timer + isBoosted flag
    // Trigger: GameEvents.OnCannonHit

    [Serializable]
    public class RetaliationEffect : IReactiveEffect
    {
        [Range(1f, 200f)] public float attackBoostPercent = 100f;
        [Min(0.1f)] public float boostDuration = 5f;

        private ICannon cannon;
        private bool isBoosted;
        private float boostTimer;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            GameEvents.OnCannonHit += OnDamaged;
        }

        public void Deactivate()
        {
            GameEvents.OnCannonHit -= OnDamaged;
            if (isBoosted && cannon != null)
                cannon.RemoveAttackModifier(0f, attackBoostPercent);
            isBoosted = false;
            cannon = null;
        }

        public void Tick()
        {
            if (!isBoosted) return;
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                cannon?.RemoveAttackModifier(0f, attackBoostPercent);
                isBoosted = false;
            }
        }

        private void OnDamaged(int damage)
        {
            if (cannon == null) return;
            if (!isBoosted)
            {
                cannon.AddAttackModifier(0f, attackBoostPercent);
                isBoosted = true;
            }
            boostTimer = boostDuration;
        }
    }

    // ── #18 Life Steal ───────────────────────────────────────
    // State: proc chance gating
    // Trigger: GameEvents.OnBirdDestroyed (IDamageable, int, Vector3)

    [Serializable]
    public class LifeStealEffect : IReactiveEffect
    {
        [Range(10f, 100f)] public float healPercent = 50f;
        [Range(1f, 100f)] public float procChance = 25f;

        private ICannon cannon;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            GameEvents.OnEggDestroyed += OnEnemyKilled;
            
        }

        public void Deactivate()
        {
            GameEvents.OnEggDestroyed -= OnEnemyKilled;
            cannon = null;
        }

        public void Tick() { }

        private void OnEnemyKilled(IDamageable killed, int score, Vector3 pos)
        {
            if (cannon == null || killed == null) return;
            if (UnityEngine.Random.value <= procChance)
            {
                int heal = Mathf.Max(1, Mathf.CeilToInt(killed.MaxHp * healPercent));
                cannon.Heal(heal);
            }
        }
    }

    // ── #28 Revenge Pulse ────────────────────────────────────
    // State: none — instant AoE on every hit
    // Trigger: GameEvents.OnCannonHit
    // Uses: Physics2D.OverlapCircleAll for area damage

    [Serializable]
    public class RevengeWaveEffect : IReactiveEffect
    {
        [Min(0)] public int waveDamage = 40;
        [Min(0.1f)] public float waveRadius = 50f;

        private ICannon cannon;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            GameEvents.OnCannonHit += OnDamaged;
        }

        public void Deactivate()
        {
            GameEvents.OnCannonHit -= OnDamaged;
            cannon = null;
        }

        public void Tick() { }

        private void OnDamaged(int damage)
        {
            if (cannon == null) return;
            Collider2D[] cols = Physics2D.OverlapCircleAll(cannon.Transform.position, waveRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                IEntity e = cols[i].GetComponent<IEntity>();
                if (e != null && e.IsAlive)
                    e.TakeDamage(waveDamage);
            }
        }
    }

    // ── #23 Stationary Damage Ramp ───────────────────────────
    // Unique: If stationary for 'stayTime', grant a flat 50% damage boost.
    // Movement immediately resets the timer and removes the boost.

    [Serializable]
    public class StationaryDamageRampEffect : IReactiveEffect
    {
        [Tooltip("How long the player must stand still to get the boost.")]
        public float stayTime = 3f;

        [Tooltip("The percentage boost to apply (0.5 = 50%).")]
        public float attackBoostPercent = 0.5f;

        private ICannon cannon;
        private bool isBoostActive;
        private float stationaryTimer;
        private Vector3 lastPos;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            isBoostActive = false;
            stationaryTimer = 0f;
            lastPos = cannon.Transform.position;
        }

        public void Tick()
        {
            if (cannon == null) return;

            Vector3 currentPos = cannon.Transform.position;
            // Check if moved (using a small threshold for floating point jitter)
            bool moved = (currentPos - lastPos).sqrMagnitude > 0.0001f;
            lastPos = currentPos;

            if (moved)
            {
                // Reset everything if they move
                if (isBoostActive)
                {
                    cannon.RemoveAttackModifier(0f, attackBoostPercent);
                    isBoostActive = false;
                    UnityEngine.Debug.Log("[Ramp] Moved! Bonus Removed.");
                }
                stationaryTimer = 0f;
            }
            else
            {
                // Standstill logic
                if (!isBoostActive)
                {
                    stationaryTimer += Time.deltaTime;

                    if (stationaryTimer >= stayTime)
                    {
                        cannon.AddAttackModifier(0f, attackBoostPercent);
                        isBoostActive = true;
                        UnityEngine.Debug.Log("[Ramp] 3s Stationary! 50% Damage Boost Applied.");
                    }
                }
            }
        }

        public void Deactivate()
        {
            if (cannon != null && isBoostActive)
            {
                cannon.RemoveAttackModifier(0f, attackBoostPercent);
            }
            isBoostActive = false;
            stationaryTimer = 0f;
            cannon = null;
        }
    }
    /// <summary>
    /// SUMMON EFFECTS
    /// </summary>
    [Serializable]
    public class BladeStrikeEffect : ISummonEffect
    {
        public GameObject strikePrefab;

        [Min(0.1f)] public float checkInterval = 0.5f;  // how often to scan for eggs
        [Min(1f)] public float bladeSpeed = 12f;
        [Min(90f)] public float rotationSpeed = 720f;
        [Min(0.5f)] public float searchRadius = 50f;
        [Min(0)] public int bladeDamage = 30;

        [Tooltip("Local offset from cannon where blade sits when attached")]
        public Vector3 localAttachOffset = new Vector3(1f, 0f, 0f);

        private ICannon cannon;
        private SpinningSword activeBlade;
        private float checkTimer;

        private static readonly Collider2D[] searchBuffer = new Collider2D[16];

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            checkTimer = 0f;   // scan immediately on first tick

            SpawnBlade();
        }

        public void Tick()
        {
            if (cannon == null) return;

            // Blade was destroyed externally — respawn
            if (activeBlade == null)
                SpawnBlade();

            // Only launch if blade is currently attached (idle)
            if (activeBlade == null) return;

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = checkInterval;

            // Blade is already flying or returning — don't launch again
            // SpinningSword.Launch() guards against double-launch internally

            Transform nearestEgg = FindNearestEgg(cannon.Transform.position);
            if (nearestEgg != null)
            {
                UnityEngine.Debug.Log($"[BladeStrike] Egg found at {nearestEgg.position} — launching blade.");
                activeBlade.Launch(nearestEgg.position);
            }
        }

        private void SpawnBlade()
        {
            if (strikePrefab == null || cannon == null) return;

            Vector3 spawnPos = cannon.Transform.TransformPoint(localAttachOffset);
            var go = UnityEngine.Object.Instantiate(strikePrefab, spawnPos, Quaternion.Euler(0f, 0f, -11f));

            // Attach to cannon immediately
            go.transform.SetParent(cannon.Transform);

            activeBlade = go.AddComponent<SpinningSword>();
            activeBlade.Init(cannon.Transform, localAttachOffset, bladeSpeed, rotationSpeed, bladeDamage);

            UnityEngine.Debug.Log("[BladeStrike] Blade spawned and attached to cannon.");
        }

        private Transform FindNearestEgg(Vector3 from)
        {
            int count = Physics2D.OverlapCircleNonAlloc(from, searchRadius, searchBuffer);

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (searchBuffer[i] == null) continue;
                if (!searchBuffer[i].CompareTag("Egg")) continue;

                float dist = ((Vector2)searchBuffer[i].transform.position - (Vector2)from).sqrMagnitude;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = searchBuffer[i].transform;
                }
            }

            return nearest;
        }

        public void Deactivate()
        {
            if (activeBlade != null)
                UnityEngine.Object.Destroy(activeBlade.gameObject);

            activeBlade = null;
            cannon = null;
        }
    }

    // ── #22 Shadow Turret ────────────────────────────────────
    // Clones the player's cannon at 0.8x scale as a greyscale
    // shadow. NOT parented — follows the cannon with smooth
    // delay (Ball Blast style). Deals x% damage per bullet.
    //
    // REPLACE the existing ShadowTurretEffect in AllPowerUps.cs

    [Serializable]
    public class ShadowTurretEffect : ISummonEffect
    {
        public Vector3 spawnOffset = new(2f, 0f, 0f);

        [Tooltip("Shadow deals this % of the cannon's current damage per bullet.")]
        [Range(1f, 100f)] public float damagePercent = 50f;

        [Tooltip("Scale multiplier relative to current cannon size.")]
        [Range(0.1f, 1f)] public float scaleMultiplier = 0.8f;

        [Tooltip("How quickly the shadow follows the cannon. Lower = more delay.")]
        [Range(1f, 20f)] public float followSpeed = 5f;

        private GameObject instance;

        public void Activate(ICannon cannon)
        {
            if (cannon == null) return;
            Deactivate();

            BaseCannon sourceCannonMb = cannon as BaseCannon;
            if (sourceCannonMb == null) return;

            // Clone the cannon GameObject for identical visuals
            Vector3 spawnPos = cannon.Transform.position + spawnOffset;
            instance = UnityEngine.Object.Instantiate(
                sourceCannonMb.gameObject,
                spawnPos,
                cannon.Transform.rotation
            );
            // NOT parented — lives independently in the scene

            // Strip conflicting components
            BaseCannon clonedCannon = instance.GetComponent<BaseCannon>();
            if (clonedCannon != null)
                UnityEngine.Object.Destroy(clonedCannon);

            Rigidbody2D clonedRb = instance.GetComponent<Rigidbody2D>();
            if (clonedRb != null)
                UnityEngine.Object.Destroy(clonedRb);

            Collider2D clonedCol = instance.GetComponent<Collider2D>();
            if (clonedCol != null)
                UnityEngine.Object.Destroy(clonedCol);

            // Scale to 0.8x of the original cannon's current scale
            instance.transform.localScale = cannon.Transform.localScale * scaleMultiplier;

            // Add shadow behavior
            ShadowCannon shadow = instance.AddComponent<ShadowCannon>();
            shadow.Init(cannon, damagePercent, spawnOffset, followSpeed);
        }

        public void Tick() { }

        public void Deactivate()
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            instance = null;
        }
    }


    // ── #15 Flame Trail ──────────────────────────────────────
    // Movement-based: drops patches when cannon moves far enough

    [Serializable]
    public class FlameTrailEffect : ISummonEffect
    {
        public GameObject firePatchPrefab;
        [Min(0.1f)] public float patchLifetime = 6f;
        [Min(0.05f)] public float dropInterval = 0.3f;
        [Min(0.01f)] public float minMoveDistance = 0.2f;

        private ICannon cannon;
        private Vector3 lastDropPos;
        private float dropTimer;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            lastDropPos = cannon.Transform.position;
            dropTimer = 0f;
        }

        public void Tick()
        {
            if (cannon == null || firePatchPrefab == null) return;
            dropTimer -= Time.deltaTime;

            if (Vector3.Distance(cannon.Transform.position, lastDropPos) >= minMoveDistance
                && dropTimer <= 0f)
            {
                var cannonPos = cannon.Transform.position;
                cannonPos.y -= 0.4f;
                var go = UnityEngine.Object.Instantiate(
                    firePatchPrefab, cannonPos, Quaternion.identity);
                UnityEngine.Object.Destroy(go, patchLifetime);
                lastDropPos = cannon.Transform.position;
                dropTimer = dropInterval;
            }
        }

        public void Deactivate() => cannon = null;
    }
    // ── #21 Ground Spike ───────────────────────────────────
    // Spawns spike prefabs across 25% of ground from left.
    // Each spike damages Eggs on contact. A shared tracker
    // ensures each Egg takes damage only once per second
    // no matter how many spikes it touches.

    [Serializable]
    public class GroundSpikeEffect : ISummonEffect
    {
        public GameObject spikePrefab;

        [Tooltip("Damage per second = this % of cannon's current attack.")]
        [Range(1f, 100f)] public float damagePercent = 50f;

        [Tooltip("How much of the ground width to cover (0.25 = 25%).")]
        [Range(0.05f, 1f)] public float groundCoverPercent = 0.25f;

        [Tooltip("Spacing between spikes (world units).")]
        [Min(0.1f)] public float spikeSpacing = 1f;

        [Tooltip("Y position of the spikes (ground level).")]
        public float groundY = -4f;

        private readonly List<GameObject> spawnedSpikes = new();
        private float tickTimer;
        private const float TickInterval = 1f;

        public void Activate(ICannon cannon)
        {
            if (spikePrefab == null || cannon == null) return;
            Deactivate();

            tickTimer = TickInterval;

            Camera cam = Camera.main;
            if (cam == null) return;

            float leftEdge = cam.ViewportToWorldPoint(Vector3.zero).x;
            float rightEdge = cam.ViewportToWorldPoint(Vector3.right).x;
            float zoneWidth = (rightEdge - leftEdge) * groundCoverPercent;
            float zoneEnd = leftEdge + zoneWidth;

            float x = leftEdge + spikeSpacing * 0.5f;
            while (x < zoneEnd)
            {
                GameObject go = UnityEngine.Object.Instantiate(
                    spikePrefab, new Vector3(x, groundY, 0f), Quaternion.identity);

                GroundSpikes spike = go.GetComponent<GroundSpikes>();
                if (spike != null)
                    spike.Init(cannon, damagePercent);

                spawnedSpikes.Add(go);
                x += spikeSpacing;
            }
        }

        public void Tick()
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer <= 0f)
            {
                GroundSpikes.ResetDamageTracker();
                tickTimer = TickInterval;
            }
        }

        public void Deactivate()
        {
            for (int i = spawnedSpikes.Count - 1; i >= 0; i--)
            {
                if (spawnedSpikes[i] != null)
                    UnityEngine.Object.Destroy(spawnedSpikes[i]);
            }
            spawnedSpikes.Clear();
            GroundSpikes.ResetDamageTracker();
        }
    }





    // ═════════════════════════════════════════════════════════
    //  UNIQUE CANNON MODIFIERS
    // ═════════════════════════════════════════════════════════

    // ── #9 Low HP ATK Boost ──────────────────────────────────
    // HP is checked ONCE at level start (Activate runs each level via
    // ApplyPowerUpsToCurrentCannon). The boost state is frozen for the whole
    // level — mid-level HP changes do NOT toggle it. Deactivate on the next
    // level transition removes the boost before the fresh evaluation.

    [Serializable]
    public class LowHpAttackBoostModifier : ICannonModifier
    {
        [Range(0f, 1f)] public float hpThreshold = 0.5f;
        [Range(10f, 200f)] public float percentBonus = 30f;

        private ICannon cannon;
        private bool isBoosted;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            GameLogger.Log(LogCategory.Powerup,
                $"[DesperationFury] Level-start HP check: {cannon.CurrentHp}/{cannon.MaxHp} (threshold ≤{hpThreshold * 100f:F0}%)");
            EvaluateBoost();
        }

        public void EvaluateBoost()
        {
            if (cannon == null || cannon.MaxHp <= 0) return;
            bool should = (float)cannon.CurrentHp / cannon.MaxHp <= hpThreshold;

            if (should && !isBoosted)
            {
                cannon.AddFireRateModifier(percentBonus);
                isBoosted = true;
                GameLogger.Log(LogCategory.Powerup,
                    $"[DesperationFury] HP low → boost ON: +{percentBonus}% fire rate, frozen for the whole level");
            }
            else if (!should && isBoosted)
            {
                cannon.RemoveFireRateModifier(percentBonus);
                isBoosted = false;
                GameLogger.Log(LogCategory.Powerup, $"[DesperationFury] Boost OFF: -{percentBonus}% fire rate");
            }
            else if (!should)
            {
                GameLogger.Log(LogCategory.Powerup, "[DesperationFury] HP above threshold → no boost this level");
            }
        }

        public void Deactivate()
        {
            if (isBoosted && cannon != null)
            {
                cannon.RemoveFireRateModifier(percentBonus);
                GameLogger.Log(LogCategory.Powerup,
                    $"[DesperationFury] Deactivated — -{percentBonus}% fire rate (level transition / displaced)");
            }
            isBoosted = false;
            cannon = null;
        }
    }

    // ── #17 Pre-Boss Recovery ─────────────────────────────────
    // Deferred heal. Unlike every other powerup, this does NOT take effect on the level
    // it is equipped on. It arms a one-shot listener for OnBossSpawned — which only fires
    // on a boss level (L10/L20) — and heals the cannon the instant the boss enters, i.e.
    // "recovery before the boss battle". Picked at L5/L6 it sits pending (the indicator is
    // shown greyed) across every level until the boss appears at L10.
    //
    // Persistence + consumption live in GameProgressManager: the slot is re-equipped each
    // level while pending, then consumed once OnPreBossRecoveryActivated fires so the heal
    // can't repeat on the chapter's second boss (L20).
    [Serializable]
    public class PreBossHealModifier : ICannonModifier
    {
        // PowerupConfig.effectType value that tags this powerup. Lets the UI / progress
        // manager recognise it without a hard reference to this class.
        public const string ConfigEffectType = "PreBossHealPercent";

        [Range(1f, 100f)] public float healPercent = 50f;

        private ICannon cannon;
        private bool subscribed;

        public void Activate(ICannon cannon)
        {
            this.cannon = cannon;
            // Re-armed on every level transition while pending — subscribe only once.
            if (subscribed) return;

            // The live chapter bosses are BossBirdController, which raises BossEventBus.OnBossSpawned
            // (NOT the legacy GameEvents.OnBossSpawned that the old BossBird class fired). Listening to
            // the wrong bus is why this heal never triggered at L10.
            var bus = BossEventBus.Instance;
            if (bus == null) return; // can't arm yet; Activate runs again next level while pending
            bus.OnBossSpawned += OnBossSpawned;
            subscribed = true;
        }

        public void Deactivate()
        {
            if (subscribed)
            {
                var bus = BossEventBus.Instance;
                if (bus != null) bus.OnBossSpawned -= OnBossSpawned;
                subscribed = false;
            }
            cannon = null;
        }

        private void OnBossSpawned(string bossName)
        {
            // One-shot: stop listening before healing so a phase-2 re-spawn can't double-heal
            // before GameProgressManager has consumed the slot.
            if (subscribed)
            {
                var bus = BossEventBus.Instance;
                if (bus != null) bus.OnBossSpawned -= OnBossSpawned;
                subscribed = false;
            }
            TriggerHeal();
            GameEvents.FirePreBossRecoveryActivated();
        }

        private void TriggerHeal()
        {
            // healPercent is 1..100 — heal that percentage of max HP.
            if (cannon != null && cannon.MaxHp > 0)
                cannon.Heal(Mathf.CeilToInt(cannon.MaxHp * healPercent / 100f));
        }
    }
}