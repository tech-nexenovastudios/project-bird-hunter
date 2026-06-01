using UnityEngine;

[CreateAssetMenu(menuName = "BossBird/Attacks/Ch04/Sonic Screech")]
public class SonicScreechConfig : BaseAttackConfig
{
    [Header("Ring Prefab")]
    [Tooltip("A sprite (e.g. a hollow ring) scaled from 0 outward as the shockwave expands. " +
             "Assumed ~1 world-unit diameter at scale 1.")]
    public GameObject ringPrefab;

    [Header("Telegraph")]
    [Tooltip("Wind-up before the wave expands, so the player can reposition.")]
    public float telegraphDuration = 0.6f;

    [Header("Shockwave")]
    public float maxRadius = 8f;
    public float expandDuration = 0.8f;
    [Tooltip("The cannon takes damage when the ring's edge passes within this distance of it.")]
    public float ringThickness = 0.8f;
    public int contactDamage = 15;

    [Header("Screen Shake")]
    public float shakeDuration = 0.4f;
    public float shakeStrength = 0.4f;

    public override BaseAttackBehaviour CreateAttack(GameObject parent)
    {
        var b = parent.AddComponent<SonicScreechBehaviour>();
        b.SetConfig(this);
        return b;
    }

    public override void CollectPrewarmPrefabs(System.Collections.Generic.List<GameObject> into)
    {
        base.CollectPrewarmPrefabs(into);
        if (ringPrefab != null) into.Add(ringPrefab);
    }
}
