// RadioactiveEggDropConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch03/Radioactive Egg Drop")]
public class RadioactiveEggDropConfig : BaseAttackConfig
{
    [Header("Drop Point")]
    [Tooltip("Name of the single transform on the boss whose position eggs are dropped from. If not found, falls back to the boss's own transform position.")]
    public string dropPointName = "EggDropPoint";

    [Header("Egg")]
    [Tooltip("Egg prefab — must have Egg + EggHealth + Rigidbody2D + Collider2D, with an EggTierConfig already assigned on the prefab.")]
    public GameObject eggPrefab;

    [Tooltip("Minimum HP for a dropped egg (rolled per egg).")]
    public int eggHpMin = 100;

    [Tooltip("Maximum HP for a dropped egg (rolled per egg, inclusive).")]
    public int eggHpMax = 150;

    [Tooltip("Sorting order passed to Egg.Init.")]
    public int eggSortingIndex = 0;

    [Header("Lifetime")]
    [Tooltip("Egg is auto-destroyed this many seconds after spawn (silent — no reward, no coin-flow). Until then it only dies if the player shoots it or it collides with the cannon.")]
    public float eggLifetime = 10f;

    [Header("Boss Pause")]
    [Tooltip("How long the boss stops moving each drop so the player can read the telegraph. Cycle = cooldown + stopDuration.")]
    public float stopDuration = 0.15f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<RadioactiveEggDropBehaviour>();
        b.SetConfig(this);
        return b;
    }
}
