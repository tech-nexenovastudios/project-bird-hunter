using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CannonHealthBar : MonoBehaviour
{
    [Header("Bar References")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image damageBar;
    //[SerializeField] private TMPro.TextMeshProUGUI healthText;

    [Header("Damage Bar Settings")]
    [SerializeField] private float damageBarDelay = 0.6f;
    [SerializeField] private float damageBarDuration = 0.5f;

    private Material _healthBarMaterial;  // instanced material so we don't affect other UI
    private Tweener _healthTween;
    private Tweener _damageTween;
    private Sequence _damageSequence;

    private static readonly int ShaderValue = Shader.PropertyToID("_Value");

    public void Init(Image healthBarImg, Image damageBarImg )
    {
        healthBar = healthBarImg;
        damageBar = damageBarImg;
       // healthText = text;

        // Create a per-instance material so we don't mutate the shared asset
        if (healthBar != null)
        {
            _healthBarMaterial = new Material(healthBar.material);
            healthBar.material = _healthBarMaterial;
            _healthBarMaterial.SetFloat(ShaderValue, 1f);
        }

        if (damageBar != null) damageBar.fillAmount = 1f;
       // if (healthText != null) healthText.text = "100%";
    }

    public void UpdateHealth(int currentHp, int maxHp)
    {
        if (maxHp <= 0) return;

        float fill = Mathf.Clamp01((float)currentHp / maxHp);

        //if (healthText != null)
        //    healthText.text = Mathf.RoundToInt(fill * 100f) + "%";

        AnimateHealthBar(fill);
    }

    private void AnimateHealthBar(float targetFill)
    {
        if (healthBar == null) return;

        // ── Health bar + shader value animate together
        _healthTween?.Kill();

        float currentFill = healthBar.fillAmount;

        _healthTween = DOTween
            .To(
                () => currentFill,
                v =>
                {
                    currentFill = v;
                    healthBar.fillAmount = v;

                    // Drive the shader _Value property (0–1)
                    if (_healthBarMaterial != null)
                        _healthBarMaterial.SetFloat(ShaderValue, v);
                },
                targetFill,
                0.15f
            )
            .SetEase(Ease.OutCubic);

        // ── Damage bar lingers then catches up
        _damageSequence?.Kill();
        _damageSequence = DOTween.Sequence();
        _damageSequence.AppendInterval(damageBarDelay);
        _damageSequence.Append(
            damageBar
                .DOFillAmount(targetFill, damageBarDuration)
                .SetEase(Ease.InOutCubic)
        );
    }

    private void OnDestroy()
    {
        _healthTween?.Kill();
        _damageSequence?.Kill();

        // Clean up the instanced material
        if (_healthBarMaterial != null)
            Destroy(_healthBarMaterial);
    }
}