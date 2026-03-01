using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Events;
using Gameplay.Birds;

namespace Gameplay.Health
{
    [RequireComponent(typeof(BaseBird))]
    public class BirdHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private BirdConfig config;
        [SerializeField] private int scoreOnDeath = 10;

        private int _currentHp;
        private int _maxHp;
        private bool _isDead;
        private BaseBird _baseBird;

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
        }

        public void TakeDamage(int damage, Vector3 hitPoint)
        {
            if (_isDead) return;

            int actualDamage = Mathf.Min(damage, _currentHp);
            _currentHp -= damage;

            GameEvents.FireBirdHit(this, actualDamage, hitPoint);

            if (_currentHp <= 0)
            {
                _isDead = true;
                int scoreAwarded = scoreOnDeath;
                GameEvents.FireBirdDestroyed(this, scoreAwarded, transform.position);

                if (_baseBird != null)
                    _baseBird.ForceKill();
                else
                    Destroy(gameObject);
            }
        }
    }
}
