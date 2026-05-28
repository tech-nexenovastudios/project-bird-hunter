// AuraOfDamnationConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch02/Aura of Damnation")]
public class AuraOfDamnationConfig : BaseAttackConfig
{
    [Header("Fire Rings")]
    public GameObject fireRingPrefab;
    [Tooltip("Seconds between ring throws. The attack emits a continuous stream (no cooldown gap) — " +
             "one ring per interval, overlapping with earlier rings that are still alive.")]
    public float ringSpawnInterval = 2f;
    [Tooltip("Travel speed of each thrown ring (world units/sec).")]
    public float ringSpeed = 5f;
    [Tooltip("Uniform size multiplier applied to the spawned ring (and its child rings). " +
             "1 = the prefab's authored size; larger = bigger ring.")]
    public float ringScale = 1f;
    [Tooltip("VFX spawned at the cannon's position when a ring hits it. Auto-returned to the pool.")]
    public GameObject destroyVfxPrefab;
    [Tooltip("Seconds before the destroy VFX is returned to the pool.")]
    public float destroyVfxLifetime = 1.5f;
    public float ringMaxRadius = 8f;
    public float ringLifetime = 3f;

    [Header("Collision")]
    [Tooltip("Starting collision radius when ring spawns")]
    public float collisionRadiusStart = 0.5f;
    [Tooltip("Final collision radius at end of lifetime")]
    public float collisionRadiusEnd = 3f;

    [Header("Aim Before Shooting")]
    [Tooltip("If true, the ring spawn point rotates to lock onto the cannon before emitting rings " +
             "(like Stone Gaze Sweep's rotateBeforeShoot). Rings then travel toward the locked position.")]
    public bool rotateBeforeShoot = true;
    [Tooltip("Degrees/sec for the lock-on rotation before firing.")]
    public float rotationSpeed = 180f;
    [Tooltip("Max seconds spent rotating to aim before firing anyway.")]
    public float aimMaxDuration = 1f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<AuraOfDamnationBehaviour>();
        b.SetConfig(this);
        return b;
    }
}