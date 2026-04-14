// IrradiationTrailConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch03/Irradiation Trail")]
public class IrradiationTrailConfig : BaseAttackConfig
{
    [Header("Cloud Spawning")]
    public GameObject radiationCloudPrefab;
    public float spawnInterval = 0.8f;
    public int maxCloudsActive = 6;

    [Header("Cloud Behaviour")]
    public float cloudStartRadius = 2f;
    public float cloudShrinkSpeed = 0.15f;
    public float cloudMinRadius = 0.3f;
    public float cloudLifetime = 8f;

    [Header("Damage")]
    public int cloudDamagePerTick = 5;
    public float cloudTicksPerSecond = 2f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<IrradiationTrailBehaviour>();
        b.SetConfig(this);
        return b;
    }
}