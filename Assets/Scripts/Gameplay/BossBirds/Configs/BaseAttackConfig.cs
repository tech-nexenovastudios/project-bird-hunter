using UnityEngine;

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
}