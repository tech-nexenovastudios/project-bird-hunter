// SuperEggDropBehaviour.cs
using DG.Tweening;
using Gameplay.Eggs;
using UnityEngine;

// Phase 2 boss attack. Same drop pipeline as Power 1, but:
//   - sizeMultiplier > 1 (default 2) makes the egg larger via the base
//     class's GetSizeMultiplier hook (applied to the cloned tier's
//     sizeRange before Egg.Init).
//   - HP range is doubled in the .asset (set in inspector) — read from the
//     inherited eggHpMin/eggHpMax fields, no override needed.
//   - Interval between drops is randomized between intervalMin/Max each
//     cycle by setting cooldownMultiplier at the START of OnExecute. This
//     makes the timer reset that BaseAttackBehaviour.Tick performs right
//     after OnExecute() returns pick up the new multiplier for the next
//     cycle. The very first fire happens at the asset's base cooldown
//     (multiplier defaults to 1 until OnExecute first runs).
//   - On kill, the SuperEgg splits into splitCount normal-sized children
//     (E2→E1-style), each with the power-1 Radioactive egg's HP range so
//     their cannon-contact damage matches a normal drop. Lifetime-timeout
//     cleanup (BossEggLifetime.ExpiredByLifetime) does NOT split — a
//     silently expired egg shouldn't punish the player with two more.
public class SuperEggDropBehaviour : RadioactiveEggDropBehaviour
{
    private SuperEggDropConfig SuperCfg => config as SuperEggDropConfig;

    protected override float GetSizeMultiplier()
    {
        var sc = SuperCfg;
        return sc != null && sc.sizeMultiplier > 0f ? sc.sizeMultiplier : 1f;
    }

    protected override void OnExecute()
    {
        var sc = SuperCfg;
        if (sc != null && config.cooldown > 0f)
        {
            float next = Random.Range(sc.intervalMin, sc.intervalMax);
            ApplyCooldownMultiplier(next / config.cooldown);
        }

        base.OnExecute();
    }

    protected override void SpawnEgg()
    {
        if (config.eggPrefab == null) return;

        Vector3 pos = dropPoint != null ? dropPoint.position : boss.transform.position;
        int hp = Random.Range(config.eggHpMin, config.eggHpMax + 1);

        var egg = SpawnEggAt(pos, hp, GetSizeMultiplier());
        if (egg != null)
            egg.OnTrySplit += HandleSuperEggSplit;
    }

    // Fires from Egg.PlayDeathSequence on bullet kill and cannon contact.
    // Egg.OnPoolRelease clears OnTrySplit, so force-cleans never reach here.
    private void HandleSuperEggSplit(Egg egg)
    {
        if (egg == null) return;
        egg.OnTrySplit -= HandleSuperEggSplit;

        var sc = SuperCfg;
        if (sc == null || sc.splitCount <= 0) return;

        var lifetimeCtl = egg.GetComponent<BossEggLifetime>();
        if (lifetimeCtl != null && lifetimeCtl.ExpiredByLifetime) return;

        for (int i = 0; i < sc.splitCount; i++)
        {
            // Same scatter as SpawnController.HandleEggSplit (E2→E1).
            var offset = i == 0 ? new Vector3(-.5f, 0.5f, 0f) : new Vector3(.5f, 0.5f, 0f);
            int childHp = Random.Range(sc.splitEggHpMin, sc.splitEggHpMax + 1);

            var child = SpawnEggAt(egg.transform.position, childHp, 1f);
            if (child == null) continue;

            child.transform.DOJump(egg.transform.position + offset, 0.5f, 1, 0.5f).SetEase(Ease.OutBack);
        }
    }
}
