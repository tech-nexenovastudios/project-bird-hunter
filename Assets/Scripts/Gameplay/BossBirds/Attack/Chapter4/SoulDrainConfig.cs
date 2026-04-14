// SoulDrainConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch04/Soul Drain")]
public class SoulDrainConfig : BaseAttackConfig
{
    [Header("Telegraph")]
    public GameObject telegraphPrefab;
    public float telegraphDuration = 0.8f;

    [Header("Tether Line")]
    [Tooltip("LaserBeamVisual prefab — the vertical beam connecting boss to ground")]
    public GameObject tetherLinePrefab;
    public float tetherWidth = 1.2f;
    public float tetherActiveDuration = 3f;

    [Header("Tether Colors")]
    public Color coreColor = new Color(0.6f, 0.2f, 1f, 1f);
    public Color glowColor = new Color(0.5f, 0.1f, 0.9f, 0.5f);
    public Color haloColor = new Color(0.4f, 0.1f, 0.8f, 0.1f);
    public float widthMultiplier = 1f;

    [Header("Damage")]
    public int damagePerTick = 8;
    public float ticksPerSecond = 3f;

    [Header("Debuff")]
    [Range(0.1f, 1f)]
    public float speedDebuffMultiplier = 0.6f;

    [Header("Ground")]
    public GameObject groundImpactVfxPrefab;
    public float groundY = -3.5f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SoulDrainBehaviour>();
        b.SetConfig(this);
        return b;
    }
}