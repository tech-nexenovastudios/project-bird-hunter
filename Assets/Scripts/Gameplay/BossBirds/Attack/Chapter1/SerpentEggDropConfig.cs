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

    [Header("Egg Stats")]
    public float eggHealth = 50f;
    public float eggLifetime = 10f;

    [Header("Rolling")]
    public float rollSpeed = 2.5f;
    public bool randomDirection = true;

    [Header("Contact Damage")]
    public int contactDamage = 15;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SerpentEggDropBehaviour>();
        b.SetConfig(this);
        return b;
    }
}