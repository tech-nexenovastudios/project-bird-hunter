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
    public void Initialize(float hp, float enrage)
    {
        maxHealth = hp;
        currentHealth = hp;
        enrageThreshold = enrage;
        hasEnraged = false;
        absorbChance = 0f;
    }

    public void SetDamageAbsorbChance(float chance) => absorbChance = chance;

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        if (absorbChance > 0f && Random.value < absorbChance)
            return;

        currentHealth -= amount;
        Debug.Log("Boss took damage: " + amount + ", current health: " + currentHealth);
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