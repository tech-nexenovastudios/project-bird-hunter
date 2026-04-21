using UnityEngine;

[CreateAssetMenu(
    fileName = "CannonProgressionConfig",
    menuName = "BirdHunter/Cannon Progression Config"
)]
public class CannonProgressionConfig : ScriptableObject
{
    [Header("Progression Curve (percent per level)")]
    [Tooltip("Applied as PctAdd. Level N adds (N-1) * this.")]
    public float damagePerLevelPct = 0.10f;
    public float healthPerLevelPct = 0.10f;
    public float fireRatePerLevelPct = 0.05f;
    public float moveSpeedPerLevelPct = 0.02f;
}