using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CoinFlowManager : MonoBehaviour
{
    [SerializeField] private CurrencyFlowEntry[] currencyEntries;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private float coinScaleOverride = 0.5f;
    [SerializeField] private float spawnRadius = 30f;

    [Header("Target Bounce Animation")]
    [SerializeField] private float bounceScale = 1.1f;
    [SerializeField] private float bounceInDuration = 0.18f;
    [SerializeField] private float bounceOutDuration = 0.28f;

    [Header("Coin Flow SFX")]
    [SerializeField] private AudioSource coinSfxSource;
    [SerializeField] private AudioClip coinFlowSfx;
    [Range(0f, 1f)] [SerializeField] private float coinSfxVolume = 1f;
    [Range(-3f, 3f)] [SerializeField] private float coinSfxPitch = 1f;

    private Dictionary<CurrencyType, CurrencyFlowRuntime> runtimeMap;

    private void Awake()
    {
        runtimeMap = new Dictionary<CurrencyType, CurrencyFlowRuntime>();

        foreach (var entry in currencyEntries)
        {
            if (runtimeMap.ContainsKey(entry.currencyType))
            {
                Debug.LogError($"[CoinFlowManager] Duplicate entry for {entry.currencyType}!");
                continue;
            }

            var pool = new ObjectPoo<CoinEntity>(
                prefab: entry.iconPrefab,
                parent: transform,
                initialSize: entry.config.poolInitialSize,
                onGet: coin =>
                {
                    coin.gameObject.SetActive(true);
                    coin.GetComponent<RectTransform>().localScale = Vector3.one;
                },
                onRelease: coin => coin.gameObject.SetActive(false)
            );

            // Cache the parent we'll bounce — immediate parent of the target UI
            Transform bounceTarget = entry.targetUI != null && entry.targetUI.parent != null
                ? entry.targetUI.parent
                : null;

            runtimeMap[entry.currencyType] = new CurrencyFlowRuntime
            {
                config = entry.config,
                targetUI = entry.targetUI,
                pool = pool,
                bounceTarget = bounceTarget,
                bounceOriginalScale = bounceTarget != null ? bounceTarget.localScale : Vector3.one
            };
        }
    }

    private void OnEnable()
    {
        GameEvent.OnCurrencyCollected += HandleCurrencyCollected;
        // coinSfxSource is owned by this manager (not AudioManager.sfxSource), so it
        // would ignore the pause-panel slider unless we sync explicitly — same pattern
        // we use for SFXController.
        AudioManager.OnSFXVolumeChanged += ApplyCoinSfxVolume;
        ApplyCoinSfxVolume(AudioManager.Instance != null
            ? (AudioManager.Instance.IsSFXEnabled() ? AudioManager.Instance.GetSFXVolume() : 0f)
            : PlayerPrefs.GetFloat("SFXVol", 1f));
    }

    private void OnDisable()
    {
        GameEvent.OnCurrencyCollected -= HandleCurrencyCollected;
        AudioManager.OnSFXVolumeChanged -= ApplyCoinSfxVolume;
    }

    private void ApplyCoinSfxVolume(float vol)
    {
        if (coinSfxSource != null) coinSfxSource.volume = Mathf.Clamp01(vol);
    }

    private void HandleCurrencyCollected(CurrencyType type, Vector2 screenPos, int totalValue)
    {
        if (!runtimeMap.ContainsKey(type))
        {
            Debug.LogWarning($"[CoinFlowManager] No entry configured for {type}!");
            return;
        }
        StartCoroutine(SpawnFlowRoutine(type, screenPos, totalValue));
    }

    private IEnumerator SpawnFlowRoutine(CurrencyType type, Vector2 originScreen, int totalValue)
    {
        var runtime = runtimeMap[type];
        var config = runtime.config;

        int coinCount = Mathf.Clamp(
            Mathf.RoundToInt(totalValue * UnityEngine.Random.Range(0.15f, 0.20f)),
            config.minCoins,
            config.maxCoins
        );

        int valuePerCoin = totalValue / coinCount;
        int remainder = totalValue % coinCount;
        int arrivedCount = 0;

        Camera canvasCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera : null;

        Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(canvasCam, runtime.targetUI.position);
        Vector3 spawnWorldPos = ScreenToCanvasWorldPos(originScreen, canvasCam);
        Vector3 targetWorldPos = ScreenToCanvasWorldPos(targetScreenPos, canvasCam);

        for (int i = 0; i < coinCount; i++)
        {
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 coinSpawnWorldPos = ScreenToCanvasWorldPos(originScreen + randomOffset, canvasCam);

            CoinEntity coin = runtime.pool.Get();
            var coinRT = coin.GetComponent<RectTransform>();
            coinRT.position = coinSpawnWorldPos;
            coinRT.localScale = coinScaleOverride == 1f ? Vector3.one : Vector3.one * coinScaleOverride;

            int value = valuePerCoin + (i == coinCount - 1 ? remainder : 0);

            if (i % 2 == 0) PlayCoinFlowSfx();

            coin.Launch(
                type: type,
                origin: coinSpawnWorldPos,
                target: targetWorldPos,
                cfg: config,
                value: value,
                onComplete: returnedCoin =>
                {
                    runtime.pool.Release(returnedCoin);

                    // Trigger bounce on first arrival; refresh while coins keep arriving
                    BounceTargetActive(runtime);

                    arrivedCount++;
                    if (arrivedCount >= coinCount)
                    {
                        StopCoinFlowSfx();
                        BounceTargetSettle(runtime);
                        GameEvent.CurrencyBurstComplete(type);
                    }
                }
            );

            yield return new WaitForSeconds(config.spawnInterval);
        }
    }

    private void PlayCoinFlowSfx()
    {
        if (coinSfxSource != null && coinFlowSfx != null)
        {
            coinSfxSource.pitch = coinSfxPitch == 0f ? 1f : coinSfxPitch;
            coinSfxSource.PlayOneShot(coinFlowSfx, coinSfxVolume);
            return;
        }

        AudioManager.Instance?.PlayCoinCollect();
    }

    private void StopCoinFlowSfx()
    {
        if (coinSfxSource == null) return;
        coinSfxSource.Stop();
    }

    private void BounceTargetActive(CurrencyFlowRuntime runtime)
    {
        if (runtime.bounceTarget == null) return;

        // Already in "scaled up" state? Don't re-tween.
        if (runtime.bounceTween != null && runtime.bounceTween.IsActive() && !runtime.isSettling) return;

        runtime.bounceTween?.Kill();
        runtime.isSettling = false;

        runtime.bounceTween = runtime.bounceTarget
            .DOScale(runtime.bounceOriginalScale * bounceScale, bounceInDuration)
            .SetEase(Ease.OutQuad);
    }

    private void BounceTargetSettle(CurrencyFlowRuntime runtime)
    {
        if (runtime.bounceTarget == null) return;

        runtime.bounceTween?.Kill();
        runtime.isSettling = true;

        runtime.bounceTween = runtime.bounceTarget
            .DOScale(runtime.bounceOriginalScale, bounceOutDuration)
            .SetEase(Ease.OutBack);
    }

    private Vector3 ScreenToCanvasWorldPos(Vector2 screenPos, Camera canvasCam)
    {
        if (parentCanvas == null) return screenPos;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, screenPos, canvasCam, out Vector3 worldPoint);
        return worldPoint;
    }
}


[Serializable]
public class CurrencyFlowEntry
{
    public CurrencyType currencyType;
    public CoinEntity iconPrefab;
    public RectTransform targetUI;
    public CoinFlowConfig config;
}


public class CurrencyFlowRuntime
{
    public CoinFlowConfig config;
    public RectTransform targetUI;
    public ObjectPoo<CoinEntity> pool;
    public Transform bounceTarget;
    public Vector3 bounceOriginalScale;
    public Tween bounceTween;
    public bool isSettling;
}