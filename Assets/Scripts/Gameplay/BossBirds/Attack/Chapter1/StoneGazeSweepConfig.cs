// StoneGazeSweepConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch01/Stone Gaze Sweep")]
public class StoneGazeSweepConfig : BaseAttackConfig
{
    [Header("Beam")]
    public string firePointName = "BeamFirePoint";
    public float beamLength = 20f;
    public float beamWidth = 0.4f;
    public float warmupDuration = 0.5f;

    [Header("Petrify")]
    [Tooltip("How long a projectile freezes before being destroyed")]
    public float petrifyDelay = 0.3f;
    public GameObject petrifyVfxPrefab;

    [Header("Laser Visual")]
    public GameObject laserVisualPrefab;
    public Color coreColor = Color.white;
    public Color glowColor = new Color(0.4f, 0.8f, 0.2f, 0.7f);
    public Color haloColor = new Color(0.4f, 0.8f, 0.2f, 0.12f);
    public float widthMultiplier = 1f;

    [Header("Damage")]
    public int damagePerTick = 8;
    public float ticksPerSecond = 4f;

    [Header("Rotation")]
    public bool rotateBeforeShoot = true;
    public float rotationSpeed = 180f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<StoneGazeSweepBehaviour>();
        b.SetConfig(this);
        return b;
    }
}