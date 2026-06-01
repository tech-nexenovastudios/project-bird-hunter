using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "BossBird/Bird Config")]
public class BossBirdConfig : ScriptableObject
{
    [Header("Identity")]
    public string bossName;
    public Sprite bossSprite;
    public RuntimeAnimatorController animatorController;

    [Header("Stats")]
    [Tooltip("Max health is rolled randomly within this range (inclusive of both x and y) each time the boss spawns.")]
    public Vector2Int maxHealthRange = new Vector2Int(500, 500);
    public float contactDamage = 20f;

    [Tooltip("Fraction of max health the boss returns with at level 20 (phase 2), after retreating at 50% in phase 1.")]
    [Range(0f, 1f)] public float returnHealthPercent = 0.75f;

    /// <summary>
    /// Rolls a random max health within <see cref="maxHealthRange"/> (inclusive of both ends).
    /// Call once per spawn and cache the result so all phases share the same base value.
    /// </summary>
    public float RollMaxHealth()
    {
        int lo = Mathf.Min(maxHealthRange.x, maxHealthRange.y);
        int hi = Mathf.Max(maxHealthRange.x, maxHealthRange.y);
        return Random.Range(lo, hi + 1); // upper bound is exclusive for ints, so +1 to include hi
    }

    [Header("Movement")]
    public BossMovementConfig phase1Movement;
    public BossMovementConfig phase2Movement;

    [Header("Attacks")]
    public BaseAttackConfig phase1Attack;
    public BaseAttackConfig phase2Attack;

    [Tooltip("Extra attacks active in EVERY phase (spawned alongside the phase attack in both " +
             "the level-10 mid-boss and the level-20 final encounter).")]
    public List<BaseAttackConfig> extraAttacks = new();

    [Header("Enrage")]
    [Range(0f, 1f)] public float enrageThreshold = 0.3f;
    public float enrageSpeedMultiplier = 1.3f;

    [Header("Rewards")]
    public int scoreValue = 1000;
    public GameObject deathVFX;

    // Every prefab this boss may instantiate during the fight (death VFX + per-attack pooled
    // prefabs for the phases that will actually run). SpawnController prewarms these during the
    // spawn countdown so the first use doesn't hitch.
    public void CollectPrewarmPrefabs(List<GameObject> into, bool isLevel20)
    {
        if (deathVFX != null) into.Add(deathVFX);
        if (phase1Attack != null) phase1Attack.CollectPrewarmPrefabs(into);
        if (isLevel20 && phase2Attack != null) phase2Attack.CollectPrewarmPrefabs(into);
        if (extraAttacks != null)
            for (int i = 0; i < extraAttacks.Count; i++)
                if (extraAttacks[i] != null) extraAttacks[i].CollectPrewarmPrefabs(into);
    }
}