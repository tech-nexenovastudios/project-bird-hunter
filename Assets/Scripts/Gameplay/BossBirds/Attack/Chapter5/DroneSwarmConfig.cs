// DroneSwarmConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch05/Drone Swarm")]
public class DroneSwarmConfig : BaseAttackConfig
{
    [Header("Spawning")]
    [Range(2, 4)]
    public int droneCount = 2;
    public float spawnInterval = 4f;
    [Tooltip("Drones spawn at this radius around boss")]
    public float spawnRadius = 1f;

    [Header("Scale & Health (relative to boss)")]
    [Tooltip("Drone scale = boss scale * this value")]
    public float scaleRatio = 0.1f;
    [Tooltip("Drone HP = boss max HP * this value")]
    public float healthRatio = 0.1f;

    [Header("Drone Prefab")]
    public GameObject dronePrefab;

    [Header("Tracking")]
    [Tooltip("How fast drones move toward cannon")]
    public float moveSpeed = 2f;
    [Tooltip("Drones accelerate smoothly instead of snapping to target")]
    public float trackingStiffness = 3f;
    [Tooltip("Minimum distance drones keep from each other to prevent stacking")]
    public float separationRadius = 1f;
    public float separationForce = 2f;

    [Header("Hover")]
    [Tooltip("Drones bob up/down slightly while tracking")]
    public float hoverAmplitude = 0.3f;
    public float hoverSpeed = 2f;

    [Header("Damage")]
    public int contactDamage = 10;
    [Tooltip("Cooldown between damage ticks when overlapping cannon")]
    public float damageCooldown = 1f;

    [Header("Death")]
    public GameObject deathVfxPrefab;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<DroneSwarmBehaviour>();
        b.SetConfig(this);
        return b;
    }
}