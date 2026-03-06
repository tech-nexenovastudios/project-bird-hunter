using System;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Interfaces;

namespace Gameplay.Managers
{
    public class ComboController : MonoBehaviour
    {
        public static ComboController Instance { get; private set; }

        [Header("Combo Settings")]
        [SerializeField] private float comboWindowSeconds = 2f;
        [SerializeField] private float[] comboMultipliers = { 1f, 1.5f, 2f, 2.5f };
        [SerializeField] private int maxComboIndex = 3;

        private int _comboCount;
        private float _lastHitTime;

        public int ComboCount => _comboCount;
        public float ComboMultiplier => _comboCount <= 0 ? 1f : comboMultipliers[Mathf.Min(_comboCount - 1, maxComboIndex)];

        public event Action<int, float> OnComboUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEggHit += OnHit;
            GameEvents.OnEggDestroyed += OnHit;
            GameEvents.OnBirdHit += OnHit;
            GameEvents.OnBirdDestroyed += OnHit;
            GameEvents.OnCannonHit += OnCannonHit;
        }

        private void OnDisable()
        {
            GameEvents.OnEggHit -= OnHit;
            GameEvents.OnEggDestroyed -= OnHit;
            GameEvents.OnBirdHit -= OnHit;
            GameEvents.OnBirdDestroyed -= OnHit;
            GameEvents.OnCannonHit -= OnCannonHit;
        }

        private void Update()
        {
            if (_comboCount > 0 && Time.time - _lastHitTime > comboWindowSeconds)
            {
                _comboCount = 0;
                OnComboUpdated?.Invoke(0, 1f);
            }
        }

        private void OnHit(IDamageable target, int amount, Vector3 position)
        {
            _lastHitTime = Time.time;
            _comboCount = Mathf.Min(_comboCount + 1, maxComboIndex + 1);
            OnComboUpdated?.Invoke(_comboCount, ComboMultiplier);
        }

        private void OnCannonHit(Vector3 position, int damage)
        {
            // Reset combo when cannon gets hit
            _comboCount = 0;
            OnComboUpdated?.Invoke(0, 1f);
        }
    }
}
