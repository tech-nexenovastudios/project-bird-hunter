// BossEggLifetime.cs
using Gameplay.Eggs;
using Gameplay.Health;
using UnityEngine;

// Attached at runtime by the boss-egg spawners (Radioactive / Super Egg Drop).
// Four jobs:
//
// 1. Lifetime cap — egg auto-destroys N seconds after spawn (silent death,
//    no FireEggDestroyed → no coin reward, no coin-flow VFX).
//
// 2. Reward suppression on bullet death — hooks EggHealth.OnHpChanged so
//    that when HP hits 0 from bullets we MarkDeadSilent before
//    EggHealth.Die() runs. Die() guards on _isDead → FireEggDestroyed is
//    skipped. Per-hit score from OnEggHit still fires; only the on-destroy
//    reward + coin-flow toast are suppressed.
//
// 3. Cannon-contact damage = current HP — clones the tier config once per
//    egg and mirrors cannonDamage to eggHealth.CurrentHp every frame.
//    Egg.OnTriggerEnter2D reads the clone, so a single TakeDamage call
//    deals exactly currentHp to the cannon. One call keeps shield-absorb
//    semantics intact.
//
// 4. Size override — applies a sizeMultiplier to the cloned tier's sizeRange
//    BEFORE Egg.Init runs, so ApplyPersonality locks the multiplied scale
//    into _originalScale (which TickPersonalityPulse then modulates around).
//    Spawners MUST call Init on this component before calling Egg.Init for
//    the size override to take effect.
//
// The shared tier asset on disk is untouched — only the per-egg clone is
// modified, so normal eggs spawned by birds keep their default behaviour.
public class BossEggLifetime : MonoBehaviour, IPoolable
{
    private float lifetime;
    private float spawnTime;
    private bool ticking;

    private Egg egg;
    private EggHealth eggHealth;
    private bool subscribed;

    private EggTierConfig clonedConfig;
    private Vector2 originalSizeRange;
    private bool capturedOriginal;

    public void Init(float lifetimeSeconds, float sizeMultiplier = 1f)
    {
        lifetime = Mathf.Max(0f, lifetimeSeconds);
        spawnTime = Time.time;
        ticking = lifetime > 0f;

        if (egg == null) egg = GetComponent<Egg>();
        if (eggHealth == null) eggHealth = GetComponent<EggHealth>();

        EnsurePerEggConfig(sizeMultiplier);
        Subscribe();
    }

    private void EnsurePerEggConfig(float sizeMultiplier)
    {
        if (egg == null || egg.config == null) return;

        if (clonedConfig == null)
        {
            // Capture the prefab's original sizeRange BEFORE the first clone so
            // re-acquires from a different spawner with a different size mult
            // compute relative to the prefab's values, not last cycle's clone.
            originalSizeRange = egg.config.sizeRange;
            capturedOriginal = true;
            clonedConfig = Instantiate(egg.config);
            clonedConfig.name = egg.config.name + " (BossEggClone)";
        }

        if (capturedOriginal && sizeMultiplier > 0f)
            clonedConfig.sizeRange = originalSizeRange * sizeMultiplier;

        if (egg.config != clonedConfig)
            egg.config = clonedConfig;
    }

    private void Update()
    {
        if (clonedConfig != null && eggHealth != null && eggHealth.IsAlive)
            clonedConfig.cannonDamage = eggHealth.CurrentHp;

        if (!ticking) return;
        if (Time.time - spawnTime < lifetime) return;
        ticking = false;
        SilentKill();
    }

    private void Subscribe()
    {
        if (subscribed || eggHealth == null) return;
        eggHealth.OnHpChanged += HandleHpChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || eggHealth == null) return;
        eggHealth.OnHpChanged -= HandleHpChanged;
        subscribed = false;
    }

    private void HandleHpChanged(int oldHp, int newHp)
    {
        if (newHp > 0) return;
        ticking = false;
        SilentKill();
    }

    private void SilentKill()
    {
        if (eggHealth != null && eggHealth.IsAlive)
            eggHealth.MarkDeadSilent();

        if (egg != null)
            egg.PlayDeathSequence();
    }

    public void OnPoolSpawned() { ticking = false; spawnTime = 0f; }

    public void OnPoolDespawned()
    {
        Unsubscribe();
        ticking = false;
    }

    private void OnDisable() => Unsubscribe();

    private void OnDestroy()
    {
        if (clonedConfig != null)
        {
            Destroy(clonedConfig);
            clonedConfig = null;
        }
    }
}
