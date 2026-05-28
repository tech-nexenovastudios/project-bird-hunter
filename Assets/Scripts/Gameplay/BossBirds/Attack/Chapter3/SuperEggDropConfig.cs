// SuperEggDropConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch03/Super Egg Drop")]
public class SuperEggDropConfig : RadioactiveEggDropConfig
{
    [Header("SuperEgg Modifiers")]
    [Tooltip("Scale multiplier applied to the spawned egg (relative to its prefab tier's natural size).")]
    public float sizeMultiplier = 2f;

    [Header("Random Interval")]
    [Tooltip("Minimum seconds between SuperEgg drops.")]
    public float intervalMin = 5f;
    [Tooltip("Maximum seconds between SuperEgg drops.")]
    public float intervalMax = 10f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SuperEggDropBehaviour>();
        b.SetConfig(this);
        return b;
    }
}
