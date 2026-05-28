// SuperEggDropBehaviour.cs
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
}
