// AuraOfDamnationConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch02/Aura of Damnation")]
public class AuraOfDamnationConfig : BaseAttackConfig
{
    [Header("Fire Rings")]
    public GameObject fireRingPrefab;
    public float ringSpawnInterval = 2f;
    public float ringMaxRadius = 8f;
    public float ringLifetime = 3f;

    [Header("Collision")]
    [Tooltip("Starting collision radius when ring spawns")]
    public float collisionRadiusStart = 0.5f;
    [Tooltip("Final collision radius at end of lifetime")]
    public float collisionRadiusEnd = 3f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<AuraOfDamnationBehaviour>();
        b.SetConfig(this);
        return b;
    }
}