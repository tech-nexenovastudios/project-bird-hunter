using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Events;
using Gameplay.Interfaces;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Health
{
    [RequireComponent(typeof(Eggs.Egg))]
    public class EggHealth : MonoBehaviour, IDamageable, IDamageEffect
    {
        [SerializeField] private Eggs.EggTierConfig config;
        private int _currentHp;
        private int _maxHp;
        private bool _isDead;

        private Material _mat;
        private bool _isHitEffectRunning;
        private Coroutine _statusEffectCoroutine;

        private Eggs.Egg _egg;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsAlive => !_isDead;
        public Eggs.EggTierConfig Config => config;
        private readonly List<IEffect<IDamageable>> activeEffects = new();

        public event Action<int, int> OnHpChanged;

        private void Awake()
        {
            _egg = GetComponent<Eggs.Egg>();

            if (_egg != null && _egg.config != null)
                config = _egg.config;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                _mat = sr.material;
        }

        public void Init(Eggs.EggTierConfig tierConfig, int hp)
        {
            config = tierConfig;
            _maxHp = hp;
            int oldHp = _currentHp;
            _currentHp = hp;
            _isDead = false;

            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            if (_mat != null)
            {
                _mat.SetFloat("_HitEffect", 0f);
                _mat.SetInt("_ElectricShock", 0);
            }

            OnHpChanged?.Invoke(oldHp, _currentHp);
        }

        public void TakeDamage(int damage)
        {
            if (_isDead) return;

            int actualDamage = Mathf.Min(damage, _currentHp);
            int oldHp = _currentHp;
            _currentHp -= damage;

            GameEvents.FireEggHit(this, actualDamage, transform.position);
            OnHpChanged?.Invoke(oldHp, _currentHp);
            Debug.Log($"Egg took {actualDamage} damage, HP: {_currentHp}/{_maxHp}");
            if (_currentHp <= 0)
            {
                Die();
            }
            else
            {
                float healthPercent = (float)_currentHp / _maxHp;

                if (_egg != null)
                    _egg.PlayHitReaction(healthPercent);

                StartHitEffect();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead    = true;
            _currentHp = 0;

            if (_statusEffectCoroutine != null)
            {
                StopCoroutine(_statusEffectCoroutine);
                _statusEffectCoroutine = null;
            }

            if (_mat != null)
            {
                _mat.SetFloat("_HitEffect", 0f);
                _mat.SetInt("_ElectricShock", 0);
            }

            int scoreAwarded = config != null ? config.scoreOnDestroy : 0;
            GameEvents.FireEggDestroyed(this, scoreAwarded, transform.position);

            if (_egg != null)
                _egg.PlayDeathSequence();
        }

        private void StartHitEffect()
        {
            if (_isHitEffectRunning || _mat == null) return;
            StartCoroutine(PlayHitEffect());
        }

        private IEnumerator PlayHitEffect()
        {
            _isHitEffectRunning = true;
            float duration = 0.2f;
            float time = 0;

            while (time < duration)
            {
                _mat.SetFloat("_HitEffect", Mathf.Lerp(0, 1, time / duration));
                time += Time.deltaTime;
                yield return null;
            }

            time = duration;
            while (time > 0)
            {
                _mat.SetFloat("_HitEffect", Mathf.Lerp(0, 1, time / duration));
                time -= Time.deltaTime;
                yield return null;
            }

            _mat.SetFloat("_HitEffect", 0f);
            _isHitEffectRunning = false;
        }

        #region IDamageEffect Implementation

        public void ElectricDamage(float applyDamage, float effectTime)
        {
            if (_isDead) return;
            if (_mat != null) _mat.SetInt("_ElectricShock", 1);
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
            StartCoroutine(FreezeRoutine(effectTime));
        }

        private IEnumerator TickDamage(float damage, float duration, float interval)
        {
            float elapsed = 0;
            while (elapsed < duration && !_isDead)
            {
                yield return new WaitForSeconds(interval);
                TakeDamage(Mathf.RoundToInt(damage));
                elapsed += interval;
            }

            if (_mat != null) _mat.SetInt("_ElectricShock", 0);
        }

        private IEnumerator FreezeRoutine(float duration)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) yield break;

            var originalVelocity = rb.linearVelocity;
            var originalType = rb.bodyType;

            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            yield return new WaitForSeconds(duration);

            if (!_isDead)
            {
                rb.bodyType = originalType;
                rb.linearVelocity = originalVelocity;
            }
        }

        #endregion
    }
}