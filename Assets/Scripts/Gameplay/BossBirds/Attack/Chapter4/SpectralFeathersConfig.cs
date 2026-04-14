// SpectralFeathersConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch04/Spectral Feathers")]
public class SpectralFeathersConfig : BaseAttackConfig
{
    [Header("Spawning")]
    public GameObject featherPrefab;
    [Range(3, 10)]
    public int featherCount = 6;
    public float spawnInterval = 0.25f;

    [Header("Spawn Area")]
    public float spawnWidth = 10f;
    public float spawnYOffset = 2f;

    [Header("Fall")]
    [Tooltip("Gravity strength — higher = faster fall")]
    public float fallSpeed = 1f;

    [Header("Air Resistance")]
    [Tooltip("Drag slows the feather. Higher = slower, floatier. Range per feather is randomized between min and max.")]
    public float dragMin = 0.3f;
    public float dragMax = 0.6f;

    [Header("Lift (creates sideways drift)")]
    [Tooltip("How strongly the feather drifts sideways as it tumbles. Higher = wider swoops.")]
    public float liftMin = 1.5f;
    public float liftMax = 3f;

    [Header("Tumble")]
    [Tooltip("How fast the feather's angle of attack oscillates, causing it to switch drift direction")]
    public float tumbleRateMin = 1.5f;
    public float tumbleRateMax = 3f;

    [Header("Speed Limits")]
    [Tooltip("Max horizontal drift speed — prevents feathers from flying off screen")]
    public float maxHorizontalSpeed = 4f;

    [Header("Damage")]
    public int contactDamage = 12;
    public float damageCooldown = 0.5f;

    [Header("Lifetime")]
    public float destroyBelowY = -6f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SpectralFeathersBehaviour>();
        b.SetConfig(this);
        return b;
    }
}