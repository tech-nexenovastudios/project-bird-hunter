using UnityEngine;
using System.Collections.Generic;

public abstract class BaseAttackConfig : ScriptableObject
{
    [Header("Shared Timing")]
    public float cooldown = 3f;
    public float duration = 2f;
    public float damage = 10f;

    [Header("Telegraphing")]
    public GameObject warningPrefab;
    public float warningDuration = 0.5f;

    [Header("VFX")]
    public GameObject vfxPrefab;

    public abstract BaseAttackBehaviour CreateAttack(GameObject parent);

    // Prefabs this attack will pool/instantiate at fire time. SpawnController prewarms these
    // during the boss-spawn countdown so the first use doesn't pay deserialize + shader-compile
    // cost mid-fight. Base collects the shared telegraph/VFX prefabs; subclasses add their own.
    public virtual void CollectPrewarmPrefabs(List<GameObject> into)
    {
        if (warningPrefab != null) into.Add(warningPrefab);
        if (vfxPrefab != null) into.Add(vfxPrefab);
    }
}