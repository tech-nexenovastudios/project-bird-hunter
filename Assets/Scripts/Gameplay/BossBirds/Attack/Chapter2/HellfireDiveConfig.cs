// HellfireDiveConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch02/Hellfire Dive")]
public class HellfireDiveConfig : BaseAttackConfig
{
    [Header("Dive")]
    public float diveSpeed = 12f;
    [Tooltip("Y position the phoenix dives down to")]
    public float diveTargetY = -1f;
    public float hoverDurationAtBottom = 0.5f;
    public float returnSpeed = 4f;

    [Header("Fire Column")]
    public GameObject fireColumnPrefab;
    public float columnLifetime = 4f;
    [Tooltip("How wide the fire column visual is")]
    public float columnWidth = 1.5f;

    [Header("Damage")]
    public int columnDamagePerTick = 12;
    public float columnTicksPerSecond = 3f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<HellfireDiveBehaviour>();
        b.SetConfig(this);
        return b;
    }
}