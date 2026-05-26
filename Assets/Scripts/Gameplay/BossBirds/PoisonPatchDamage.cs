// PoisonPatchDamage.cs
using Gameplay.Player;
using UnityEngine;

/// <summary>
/// Damage-over-time for a poison trail patch (the PoisonBubbles stamps dropped by
/// HazardRollTrail as the boss egg rolls). While the player's cannon overlaps the patch,
/// it takes <see cref="damagePerTick"/> damage once every <see cref="tickInterval"/> seconds,
/// for as long as the patch is alive (HazardTrailStamp returns it to the pool at end of life).
///
/// Detection uses a Physics2D.OverlapCircle at the patch's world position rather than a
/// trigger collider, because the PoisonBubbles visual root is rotated/scaled for 3D particles
/// — a 2D collider on it would behave unpredictably. Polling at the patch centre is robust
/// and layer-independent.
/// </summary>
public class PoisonPatchDamage : MonoBehaviour, IPoolable
{
    [Tooltip("World-unit radius around the patch centre that counts as 'standing in it'.")]
    [SerializeField] private float damageRadius = 0.6f;

    [Tooltip("Seconds between damage ticks while the player stays in the patch.")]
    [SerializeField] private float tickInterval = 1f;

    private int damagePerTick;
    private float tickTimer;
    private bool isActive;

    private readonly Collider2D[] _hits = new Collider2D[8];

    /// <summary>Called by HazardRollTrail right after the patch spawns. dmgPerSecond comes from the attack config.</summary>
    public void Init(int dmgPerSecond)
    {
        damagePerTick = Mathf.Max(0, dmgPerSecond);
        tickTimer = tickInterval;
        isActive = damagePerTick > 0;

        Debug.Log($"[PoisonPatch] Spawned at {transform.position} — {damagePerTick} dmg every {tickInterval}s, radius {damageRadius}. Active={isActive}");
    }

    private void Update()
    {
        if (!isActive) return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f) return;
        tickTimer = tickInterval;

        var cannon = FindCannonInPatch();
        if (cannon != null)
        {
            cannon.TakeDamage(damagePerTick);
            Debug.Log($"[PoisonPatch] Player in patch at {transform.position} → dealt {damagePerTick} dmg. Cannon HP now {cannon.CurrentHp}/{cannon.MaxHp}");
        }
        else
        {
            Debug.Log($"[PoisonPatch] tick at {transform.position} → no player within radius {damageRadius}");
        }
    }

    private BaseCannon FindCannonInPatch()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, damageRadius, _hits);
        for (int i = 0; i < count; i++)
        {
            var hit = _hits[i];
            if (hit == null) continue;
            if (hit.CompareTag("Player") && hit.TryGetComponent<BaseCannon>(out var cannon))
                return cannon;
        }
        return null;
    }

    // ── IPoolable ──
    public void OnPoolSpawned()
    {
        isActive = false;
        tickTimer = tickInterval;
    }

    public void OnPoolDespawned()
    {
        isActive = false;
        Debug.Log($"[PoisonPatch] Expired/returned to pool at {transform.position} — damage stopped.");
    }
}
