// ContaminatedBombConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch03/Contaminated Bomb")]
public class ContaminatedBombConfig : BaseAttackConfig
{
    [Header("Drop Points")]
    public string dropParentName = "BombDropPoints";

    [Header("Bomb")]
    public GameObject bombPrefab;
    public float bombGravityScale = 2f;
    public int bombHealth = 5;
    public int bombContactDamage = 15;
    public float bombContactCooldown = 1f;

    [Header("Bounce")]
    [Tooltip("Bomb bounces to this fraction of its drop height before splitting")]
    public float bounceHeightFraction = 0.5f;

    [Header("VFX")]
    public GameObject explosionVfxPrefab;
    [Header("Mini Bomb Arc Spread")]
    public float miniBombSpreadAngle = 90f;
    public float miniBombLaunchSpeed = 6f;
    [Header("Mini Bombs")]
    public GameObject miniBombPrefab;
    public int miniBombCount = 5;
    public int miniBombHealth = 3;
    public float miniBombGravityScale = 2f;
    public float miniBombLifetime = 6f;

    [Header("Mini Bomb Spread (spawned in air)")]
    [Tooltip("Horizontal speed spread between mini-bombs")]
    public float miniBombSpreadSpeed = 3f;
    [Tooltip("Upward speed given to each mini-bomb at spawn")]
    public float miniBombLaunchUpSpeed = 4f;

    [Header("Mini Bomb Jumping")]
    [Tooltip("Upward impulse applied every time a mini-bomb hits the ground")]
    public float miniBombJumpForce = 5f;

    [Header("Mini Bomb Damage")]
    public int miniBombContactDamage = 10;
    public float miniBombContactCooldown = 0.8f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<ContaminatedBombBehaviour>();
        b.SetConfig(this);
        return b;
    }
}