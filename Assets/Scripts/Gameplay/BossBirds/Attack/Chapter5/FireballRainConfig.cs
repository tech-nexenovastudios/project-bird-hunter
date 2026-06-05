// FireballRainConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch05/Fireball Rain")]
public class FireballRainConfig : BaseAttackConfig
{
    [Header("Spawn Point")]
    [Tooltip("Name of a child Transform on the boss where fireballs spawn from.")]
    public string spawnPointName = "FireballSpawnPoint";

    [Header("Fireball")]
    public GameObject fireballPrefab;
    [Tooltip("Fireballs spawned per second.")]
    public float spawnRate = 1f;
    [Tooltip("Initial downward speed when spawned (added on top of gravity).")]
    public float initialDownSpeed = 0f;
    public float gravityScale = 2f;
    [Tooltip("Auto-destroy a fireball after this many seconds if it never hits anything.")]
    public float lifetime = 5f;

    [Header("Damage")]
    public int contactDamage = 10;

    [Header("VFX")]
    public GameObject impactVfxPrefab;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<FireballRainBehaviour>();
        b.SetConfig(this);
        return b;
    }

    public override void CollectPrewarmPrefabs(System.Collections.Generic.List<GameObject> into)
    {
        base.CollectPrewarmPrefabs(into);
        if (fireballPrefab != null) into.Add(fireballPrefab);
        if (impactVfxPrefab != null) into.Add(impactVfxPrefab);
    }
}
