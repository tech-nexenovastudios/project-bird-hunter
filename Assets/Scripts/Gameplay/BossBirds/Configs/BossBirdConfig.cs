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
    public float maxHealth = 500f;
    public float contactDamage = 20f;
    public float phase2HealthMultiplier = 1.5f;

    [Header("Movement")]
    public BossMovementConfig phase1Movement;
    public BossMovementConfig phase2Movement;

    [Header("Attacks")]
    public BaseAttackConfig phase1Attack;
    public BaseAttackConfig phase2Attack;

    [Header("Enrage")]
    [Range(0f, 1f)] public float enrageThreshold = 0.3f;
    public float enrageSpeedMultiplier = 1.3f;

    [Header("Rewards")]
    public int scoreValue = 1000;
    public GameObject deathVFX;
}