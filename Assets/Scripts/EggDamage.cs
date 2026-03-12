using Gameplay.Interfaces;
using UnityEngine;
using Gameplay.Health;
using Gameplay.Eggs;

public class EggDamage : MonoBehaviour
{
    private Gameplay.Health.EggHealth _eggHealth;

    private void Start()
    {
        _eggHealth = GetComponent<Gameplay.Health.EggHealth>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Vector3 hitPoint = collision.ClosestPoint(transform.position);

            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                int damageAmount = 0;
                
                if (_eggHealth != null)
                {
                    damageAmount = _eggHealth.CurrentHp;
                }
                
                if (damageAmount <= 0)
                {
                    // if (TryGetComponent<Egg>(out var egg))
                    // {
                    //     damageAmount = egg.CurrentHp;
                    // }
                }

                if (damageAmount <= 0) damageAmount = 10;

                Debug.Log($"[EggDamage] Hitting cannon via IDamageable for {damageAmount} damage at {hitPoint}");
                damageable.TakeDamage(damageAmount);
            }

            var damageEffect = collision.GetComponent<IDamageEffect>();
            if (damageEffect != null)
            {
                if (TryGetComponent<Egg>(out var egg) && egg.config != null)
                {
                    if (egg.config.tierId.Contains("Ice"))
                    {
                        damageEffect.FreezeEffect(3f);
                    }
                    else if (egg.config.tierId.Contains("Fire"))
                    {
                        damageEffect.igniteDamage(1f, 3f);
                    }
                }
            }
        }
    }
}
