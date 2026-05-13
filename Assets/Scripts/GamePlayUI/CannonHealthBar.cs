
using BirdHunter.Inventory.Stats;
using DG.Tweening;
using Gameplay.Events;
using UnityEngine;
using UnityEngine.UI;

public class CannonHealthBar : MonoBehaviour
{
    [SerializeField] private Image _healthBar;
    [SerializeField] private Image _damageBar;

    [Header("Damage Bar Settings")]
    [SerializeField] private float damageBarDelay = 0.6f;
    [SerializeField] private float damageBarDuration = 0.5f;

    [Header("Damage Punch")]
    [SerializeField] private float damagePunchStrength = 0.08f;
    [SerializeField] private float damagePunchDuration = 0.22f;

    private Material _healthBarMaterial;
    private Tweener _healthTween;
    private Sequence _damageSequence;
    private RectTransform _rootRect;
    private int _lastHp = -1;

    private float _maxHealth = 100f;

    private static readonly int ShaderValue = Shader.PropertyToID("_Health");

    private bool _initialized = false;

    public void Init(StatSheet stats)
    {
        // Only create instanced material if the bar has a material that uses _Value
        if (_healthBar.material != null && _healthBar.material.HasProperty(ShaderValue))
        {
            _healthBarMaterial = new Material(_healthBar.material);
            _healthBar.material = _healthBarMaterial;
            _healthBarMaterial.SetFloat(ShaderValue, 1f);
        }

        _healthBar.fillAmount = 1f;
        if (_damageBar != null) _damageBar.fillAmount = 1f;

        _maxHealth = stats != null ? stats.Get(StatType.Health) : 100f;

        _initialized = true;
    }

    private void OnEnable()
    {
        GameEvents.OnCannonStatsUpdated += Init;
        GameEvents.OnCannonHealthChanged += UpdateHealth;  // replaces OnCannonHit
    }

    private void OnDisable()
    {
        GameEvents.OnCannonStatsUpdated -= Init;
        GameEvents.OnCannonHealthChanged -= UpdateHealth;
    }

    
    public void UpdateHealth(int currentHp, int maxHp)
    {
        if (!_initialized)
        {
            Debug.LogWarning("[CannonHealthBar] UpdateHealth called before Init!");
            return;
        }
        if (maxHp <= 0) return;

        if (_lastHp >= 0 && currentHp < _lastHp) PunchOnDamage();
        _lastHp = currentHp;

        float fill = Mathf.Clamp01((float)currentHp / maxHp);
        AnimateHealthBar(fill);
    }

    private void PunchOnDamage()
    {
        if (_rootRect == null) _rootRect = transform as RectTransform;
        if (_rootRect == null) return;
        _rootRect.DOComplete();
        _rootRect.DOPunchScale(Vector3.one * damagePunchStrength, damagePunchDuration, 8, 0.5f);
    }

    private void AnimateHealthBar(float targetFill)
    {
        if (_healthBar == null) return;

        // Kill existing tweens before starting new ones
        _healthTween?.Kill();
        _damageSequence?.Kill();

        float startFill = _healthBar.fillAmount;

        _healthTween = DOTween.To(
            () => startFill,
            v =>
            {
                startFill = v;
                _healthBar.fillAmount = v;
                _healthBarMaterial?.SetFloat(ShaderValue, v);
            },
            targetFill,
            0.15f
        ).SetEase(Ease.OutCubic);

        if (_damageBar != null)
        {
            _damageSequence = DOTween.Sequence();
            _damageSequence.AppendInterval(damageBarDelay);
            _damageSequence.Append(
                _damageBar.DOFillAmount(targetFill, damageBarDuration)
                          .SetEase(Ease.InOutCubic)
            );
        }
    }

    private void OnDestroy()
    {
        _healthTween?.Kill();
        _damageSequence?.Kill();

        if (_healthBarMaterial != null)
            Destroy(_healthBarMaterial);
    }
}