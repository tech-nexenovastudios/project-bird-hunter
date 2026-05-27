// SoulDrainConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch04/Soul Drain")]
public class SoulDrainConfig : BaseAttackConfig
{
    [Header("Telegraph")]
    public GameObject telegraphPrefab;
    public float telegraphDuration = 0.8f;

    [Header("Beam Line")]
    [Tooltip("LaserBeamVisual prefab � the beam fired from the boss toward the cannon")]
    public GameObject tetherLinePrefab;
    [Tooltip("Beam thickness used for the hit test (cannon must be within ~2x this of the beam line).")]
    public float tetherWidth = 0.4f;
    public float tetherActiveDuration = 3f;
    [Tooltip("Maximum beam length. Should be long enough to reach the cannon from the boss.")]
    public float beamLength = 30f;
    [Tooltip("Optional child transform on the boss to fire from. Empty = a rotating pivot is created at the boss origin.")]
    public string firePointName = "";
    [Tooltip("Seconds the beam widens in before reaching full strength.")]
    public float warmupDuration = 0.5f;

    [Header("Tracking")]
    [Tooltip("If true, the beam first rotates to lock onto the cannon before firing.")]
    public bool rotateBeforeShoot = true;
    [Tooltip("Degrees/sec used for the initial lock-on rotation (before firing).")]
    public float rotationSpeed = 180f;
    [Tooltip("Degrees/sec the beam tracks the cannon while firing (like Stone Gaze Sweep). " +
             "Lower = more delay, easier to dodge.")]
    public float trackingRotationSpeed = 45f;

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