using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Movement Config")]
public class BossMovementConfig : ScriptableObject
{
    public enum MovementPattern
    {
        HorizontalSweep, Erratic, Hovering,
        DiveAndReturn, LowFlyby, StopAndAttack
    }

    public MovementPattern pattern;
    public float speed = 3f;
    public float hoverAmplitude = 0.5f;
    public float diveSpeed = 8f;
    public float pauseDuration = 1.5f;

    [Header("Screen Bounds")]
    [Tooltip("Fallback X padding for Spine bosses (no SpriteRenderer). Tune per boss.")]
    public float screenEdgePadding = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Vertical position anchor: 0=bottom, 1=top. 0.75 = upper quarter of screen.")]
    public float hoverVerticalAnchor = 0.75f;

    [Header("Legacy Manual Bounds (Erratic only)")]
    [Tooltip("Only used by Erratic pattern as a secondary clamp. Leave default if unused.")]
    public Vector2 boundsMin = new(-7f, 2f);
    public Vector2 boundsMax = new(7f, 5f);
}