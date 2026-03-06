using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Events;
using Gameplay.Birds;

namespace Gameplay.Health
{
    [RequireComponent(typeof(BaseBird))]
    public class BirdHealth : MonoBehaviour, IDamageable, IDamageEffect
    {
        [SerializeField] private BirdConfig config;
        [SerializeField] private int scoreOnDeath = 10;

        private int _currentHp;
        private int _maxHp;
        private bool _isDead;
        private BaseBird _baseBird;
        private Coroutine _statusEffectCoroutine;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _baseBird = GetComponent<BaseBird>();
            if (_baseBird != null && _baseBird.config != null)
                config = _baseBird.config;
        }

        public void Init(BirdConfig birdConfig, int hp)
        {
            config = birdConfig;
            _maxHp = hp;
            _currentHp = hp;
            _isDead = false;

            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }
        }

        public void TakeDamage(int damage, Vector3 hitPoint)
        {
            if (_isDead) return;

            int actualDamage = Mathf.Min(damage, _currentHp);
            _currentHp -= damage;

            GameEvents.FireBirdHit(this, actualDamage, hitPoint);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _currentHp = 0;
            
            int scoreAwarded = scoreOnDeath;
            GameEvents.FireBirdDestroyed(this, scoreAwarded, transform.position);

            if (_baseBird != null)
                _baseBird.ForceKill();
            else
                Destroy(gameObject);
        }

        #region IDamageEffect Implementation

        public void ElectricDamage(float applyDamage, float effectTime)
        {
            if (_isDead) return;
            _statusEffectCoroutine = StartCoroutine(TickDamage(applyDamage, effectTime, 1f));
        }

        public void igniteDamage(float applyDamage, float effectTime)
        {
            if (_isDead) return;
            _statusEffectCoroutine = StartCoroutine(TickDamage(applyDamage, effectTime, 1f));
        }

        public void PoisonDamage(float applyDamage, float effectTime)
        {
            if (_isDead) return;
            _statusEffectCoroutine = StartCoroutine(TickDamage(applyDamage, effectTime, 1f));
        }

        public void FreezeEffect(float effectTime)
        {
            if (_isDead) return;
            // Birds might need specific freeze logic in BaseBird, but we can pause movement here
            StartCoroutine(FreezeRoutine(effectTime));
        }

        private System.Collections.IEnumerator TickDamage(float damage, float duration, float interval)
        {
            float elapsed = 0;
            while (elapsed < duration && !_isDead)
            {
                yield return new WaitForSeconds(interval);
                TakeDamage(Mathf.RoundToInt(damage), transform.position);
                elapsed += interval;
            }
        }

        private System.Collections.IEnumerator FreezeRoutine(float duration)
        {
            if (_baseBird == null) yield break;
            
            // Assuming BaseBird has a way to pause/resume
            // If not, we can at least try to manipulate Rigidbody if it exists
            var rb = GetComponent<Rigidbody2D>();
            Vector2 originalVelocity = Vector2.zero;
            RigidbodyType2D originalType = RigidbodyType2D.Dynamic;
            
            if (rb != null)
            {
                originalVelocity = rb.linearVelocity;
                originalType = rb.bodyType;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            yield return new WaitForSeconds(duration);

            if (!_isDead && rb != null)
            {
                rb.bodyType = originalType;
                rb.linearVelocity = originalVelocity;
            }
        }

        #endregion
    }
}
