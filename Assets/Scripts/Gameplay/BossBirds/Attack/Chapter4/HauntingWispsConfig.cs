using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch04/Haunting Wisps")]
public class HauntingWispsConfig : BaseAttackConfig
{
    [Header("Wisp Prefab")]
    [Tooltip("Must be tagged 'BossBird' with a Collider2D so player bullets can shoot it down.")]
    public GameObject wispPrefab;

    [Header("Spawning")]
    public int minCount = 2;
    public int maxCount = 3;
    [Tooltip("Wisps spawn within this radius around the boss.")]
    public float spawnRadius = 1f;

    [Header("Wisp Stats")]
    [Tooltip("Flat HP per wisp — keep low so a couple of shots clears them.")]
    public float wispHP = 30f;
    public int contactDamage = 10;

    [Header("Homing")]
    [Tooltip("Units/sec the wisp drifts toward the cannon.")]
    public float moveSpeed = 2.5f;
    [Tooltip("A volley self-expires after this many seconds if its wisps aren't killed/contacted.")]
    public float wispLifetime = 8f;

    [Header("Enrage")]
    [Tooltip("Extra wisps per volley once the boss enrages.")]
    public int enrageExtraCount = 2;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<HauntingWispsBehaviour>();
        b.SetConfig(this);
        return b;
    }
}
