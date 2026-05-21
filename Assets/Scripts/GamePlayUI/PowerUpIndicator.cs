using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpIndicator : MonoBehaviour, ICooldownIndicator
{
    [Header("Visuals")]
    [SerializeField] private Image powerUpIcon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private Transform bgVFXSpawnPoint;

    [Header("FX")]
    [SerializeField] private GameObject spawnVFXPrefab;
    [SerializeField] private GameObject destructionVFXPrefab;

    [Header("Pulse")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.6f;

    [Header("Cooldown Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private Tween _pulseTween;
    private Tween _cooldownTween;

    public void Bind(NotificationRequest request)
    {
        if (powerUpIcon != null && request.icon != null) powerUpIcon.sprite = request.icon;
        if (cooldownFill != null && request.icon != null) cooldownFill.sprite = request.icon;

        SpawnVfx(spawnVFXPrefab);
        SetActiveVisual();
        StartPulse();
    }

    public void StartCooldown(float duration)
    {
        if (duration <= 0f || cooldownFill == null) return;
        StopPulse();
        _cooldownTween?.Kill();

        if (powerUpIcon != null) powerUpIcon.color = cooldownColor;
        cooldownFill.color = activeColor;
        cooldownFill.fillAmount = 0f;

        _cooldownTween = cooldownFill
            .DOFillAmount(1f, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { SetActiveVisual(); StartPulse(); });
    }

    public void Hide()
    {
        StopPulse();
        _cooldownTween?.Kill();
        SpawnVfx(destructionVFXPrefab);
        gameObject.SetActive(false);
    }

    private void SetActiveVisual()
    {
        if (powerUpIcon != null) powerUpIcon.color = activeColor;
        if (cooldownFill != null) { cooldownFill.fillAmount = 1f; cooldownFill.color = activeColor; }
    }

    private void StartPulse()
    {
        _pulseTween?.Kill();
        transform.localScale = Vector3.one;
        _pulseTween = transform
            .DOScale(pulseScale, pulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopPulse()
    {
        _pulseTween?.Kill();
        transform.localScale = Vector3.one;
    }

    private void SpawnVfx(GameObject prefab)
    {
        if (prefab == null) return;
        Vector3 pos = bgVFXSpawnPoint != null ? bgVFXSpawnPoint.position : transform.position;
        var go = Instantiate(prefab, bgVFXSpawnPoint);
        //go.transform.parent = bgVFXSpawnPoint;
        
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
        _cooldownTween?.Kill();
    }
}
