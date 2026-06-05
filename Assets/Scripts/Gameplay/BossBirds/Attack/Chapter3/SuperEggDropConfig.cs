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

    [Header("Split (on kill)")]
    [Tooltip("Children spawned when the SuperEgg is destroyed (E2→E1-style split). 0 disables.")]
    public int splitCount = 2;
    [Tooltip("HP range per child. Match the Radioactive Egg Drop's eggHpMin/Max so children are normal power-1 eggs (contact damage mirrors current HP).")]
    public int splitEggHpMin = 700;
    public int splitEggHpMax = 1000;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SuperEggDropBehaviour>();
        b.SetConfig(this);
        return b;
    }
}
