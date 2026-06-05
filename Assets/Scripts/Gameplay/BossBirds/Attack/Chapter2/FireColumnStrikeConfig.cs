// FireColumnStrikeConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch02/Fire Column Strike")]
public class FireColumnStrikeConfig : BaseAttackConfig
{
    [Header("Fire Column")]
    [Tooltip("The fire column prefab. Spawns at the boss's 'FireBlockSpawnPosition' child and " +
             "drops straight down to the cannon's level.")]
    public GameObject fireColumnPrefab;
    [Tooltip("How long a column stays alive before it's destroyed (seconds).")]
    public float columnLifetime = 3f;
    [Tooltip("Delay after a column is destroyed before the next one spawns (seconds).")]
    public float respawnDelay = 2f;
    [Tooltip("Upper cap on the fire column's contact width. The actual damage width follows " +
             "the beam's rendered visuals each tick; this only caps it (and is the fallback " +
             "before particles appear).")]
    public float columnWidth = 1.5f;

    [Header("Damage")]
    public int columnDamagePerTick = 12;
    public float columnTicksPerSecond = 3f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<FireColumnStrikeBehaviour>();
        b.SetConfig(this);
        return b;
    }

    public override void CollectPrewarmPrefabs(System.Collections.Generic.List<GameObject> into)
    {
        base.CollectPrewarmPrefabs(into);
        if (fireColumnPrefab != null) into.Add(fireColumnPrefab);
    }
}
