using Gameplay.Interfaces;
using UnityEngine;

public class BossHealthHandler : MonoBehaviour, IDamageablee
{
    private float maxHealth;
    private float currentHealth;
    private float enrageThreshold;
    private bool hasEnraged;
    private float absorbChance;

    public event System.Action<float> OnHealthChanged;
    public event System.Action OnDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float NormalizedHealth => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    /// <summary>Initialize at full HP (start of phase 1, or fresh level-20 test spawn).</summary>
    public void Initialize(float hp, float enrage) => Initialize(hp, hp, enrage);

    /// <summary>Initialize with separate max + starting HP — used when boss returns mid-fight.</summary>
    public void Initialize(float maxHp, float startHp, float enrage)
    {
        maxHealth = Mathf.Max(1f, maxHp);
        currentHealth = Mathf.Clamp(startHp, 0f, maxHealth);
        enrageThreshold = enrage;
        hasEnraged = false;
        absorbChance = 0f;

        OnHealthChanged?.Invoke(NormalizedHealth);
    }

    public void SetDamageAbsorbChance(float chance) => absorbChance = chance;

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        if (absorbChance > 0f && Random.value < absorbChance)
            return;

        currentHealth -= amount;
        float normalized = Mathf.Clamp01(currentHealth / maxHealth);
        OnHealthChanged?.Invoke(normalized);

        if (!hasEnraged && normalized <= enrageThreshold && enrageThreshold > 0f)
        {
            hasEnraged = true;
        }

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath?.Invoke();
        }
    }
}
