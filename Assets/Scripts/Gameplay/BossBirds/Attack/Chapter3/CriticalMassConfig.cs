// CriticalMassConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch03/Critical Mass")]
public class CriticalMassConfig : BaseAttackConfig
{
    [Header("Orbit")]
    [Tooltip("World-space center the raven orbits around")]
    public Vector2 orbitCenter = new Vector2(0f, 1f);
    [Tooltip("Horizontal radius of the ellipse")]
    public float orbitRadiusX = 3f;
    [Tooltip("Vertical radius of the ellipse")]
    public float orbitRadiusY = 2.5f;
    public float orbitSpeed = 1.2f; // radians per second

    [Header("Fragment Dropping")]
    public GameObject fragmentPrefab;   // glow/idle state
    public GameObject explosionPrefab;  // blast VFX
    public float dropInterval = 1.4f;   // seconds between drops
    [Tooltip("How long the fragment glows before exploding")]
    public float warningDuration = 1.8f;
    [Tooltip("How long the explosion VFX lingers before pool return")]
    public float explosionLingerDuration = 0.5f;

    [Header("Explosion")]
    public float explosionRadius = 2f;
    public int explosionDamage = 20;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<CriticalMassBehaviour>();
        b.SetConfig(this);
        return b;
    }
}