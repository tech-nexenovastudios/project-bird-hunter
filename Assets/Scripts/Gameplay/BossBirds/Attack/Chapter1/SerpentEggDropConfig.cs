// SerpentEggDropConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch01/Serpent Egg Drop")]
public class SerpentEggDropConfig : BaseAttackConfig
{
    [Header("Drop")]
    public string dropParentName = "HazardDropPosition";
    public GameObject eggPrefab;
    public int dropCount = 1;
    public float dropInterval = 0.4f;
    [Tooltip("Gravity scale applied to the egg while falling (before it lands and starts rolling). Lower = slower fall.")]
    public float dropGravityScale = 1.2f;

    [Header("Egg Stats")]
    public float eggLifetime = 10f;
    public GameObject deathVfxPrefab;

    [Header("Rolling")]
    public float rollSpeed = 2.5f;
    public bool randomDirection = true;
    [Tooltip("Max world-units the egg rolls after landing before it stops in place. Set < half screen width.")]
    public float maxRollDistance = 4f;

    [Header("Contact Damage")]
    public int contactDamage = 15;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SerpentEggDropBehaviour>();
        b.SetConfig(this);
        return b;
    }
}