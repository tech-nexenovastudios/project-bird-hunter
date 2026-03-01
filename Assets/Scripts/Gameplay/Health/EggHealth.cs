using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Events;
using Gameplay.Eggs;

namespace Gameplay.Health
{
    [RequireComponent(typeof(Egg))]
    public class EggHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EggTierConfig config;
        private int _currentHp;
        private int _maxHp;
        private bool _isDead;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsAlive => !_isDead;

        private void Awake()
        {
            var egg = GetComponent<Egg>();
            if (egg != null && egg.config != null)
                config = egg.config;
        }

        public void Init(EggTierConfig tierConfig, int hp)
        {
            config = tierConfig;
            _maxHp = hp;
            _currentHp = hp;
            _isDead = false;
        }

        public void TakeDamage(int damage, Vector3 hitPoint)
        {
            if (_isDead) return;

            int actualDamage = Mathf.Min(damage, _currentHp);
            _currentHp -= damage;

            GameEvents.FireEggHit(this, actualDamage, hitPoint);

            if (_currentHp <= 0)
            {
                _isDead = true;
                int scoreAwarded = config != null ? config.scoreOnDestroy : 0;
                GameEvents.FireEggDestroyed(this, scoreAwarded, transform.position);

                var egg = GetComponent<Egg>();
                egg?.OnDestroyed?.Invoke(egg);
            }
        }
    }
}
