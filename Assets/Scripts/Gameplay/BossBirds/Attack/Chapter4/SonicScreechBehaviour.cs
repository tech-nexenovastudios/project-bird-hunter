using System.Collections;
using Gameplay.Player;
using Gameplay.VFX;
using UnityEngine;

/// <summary>
/// Boss-4 add: after a brief telegraph, emits an expanding shockwave ring from the boss with a
/// screen shake. The cannon takes damage once, as the ring's edge sweeps over it — so the player
/// must move clear of the wave's path during the wind-up.
/// </summary>
public class SonicScreechBehaviour : BaseAttackBehaviour
{
    private SonicScreechConfig config;

    public void SetConfig(SonicScreechConfig cfg) => config = cfg;

    protected override void OnExecute()
    {
        NotifyAttackStarted();
        StartCoroutine(ScreechSequence());
    }

    private IEnumerator ScreechSequence()
    {
        Vector3 origin = boss.transform.position;
        var cannon = GameObject.FindWithTag("Player")?.transform;
        var cannonComp = cannon != null ? cannon.GetComponent<BaseCannon>() : null;

        if (config.telegraphDuration > 0f)
            yield return new WaitForSeconds(config.telegraphDuration);

        GameObject ring = config.ringPrefab != null ? PoolManager.Get(config.ringPrefab, origin) : null;

        CameraShakeController.Shake(config.shakeDuration, config.shakeStrength);

        bool hasDamaged = false;
        float elapsed = 0f;
        while (elapsed < config.expandDuration)
        {
            float radius = (elapsed / config.expandDuration) * config.maxRadius;

            if (ring != null)
                ring.transform.localScale = Vector3.one * (radius * 2f);

            // One hit, as the expanding edge crosses the cannon.
            if (!hasDamaged && cannonComp != null)
            {
                float dist = Vector2.Distance(origin, cannon.position);
                if (Mathf.Abs(dist - radius) <= config.ringThickness)
                {
                    cannonComp.TakeDamage(config.contactDamage);
                    hasDamaged = true;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ring != null) PoolManager.Return(ring);
        isRunning = false;
        NotifyAttackComplete();
    }

    public override void OnStop() { StopAllCoroutines(); isRunning = false; }
    public override void OnCleanup() => OnStop();
}
