using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinFlowManager : MonoBehaviour
{
    [SerializeField] private CurrencyFlowEntry[] currencyEntries;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private float coinScaleOverride = 0.5f;
   

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

            runtimeMap[entry.currencyType] = new CurrencyFlowRuntime
            {
                config = entry.config,
                targetUI = entry.targetUI,
                pool = pool
            };
        }
    }

    private void OnEnable() => GameEvent.OnCurrencyCollected += HandleCurrencyCollected;
    private void OnDisable() => GameEvent.OnCurrencyCollected -= HandleCurrencyCollected;

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

        // Convert the target UI element to screen, then both origin and target to canvas world space.
        Camera canvasCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? parentCanvas.worldCamera
            : null;

        Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(canvasCam, runtime.targetUI.position);
        Vector3 spawnWorldPos = ScreenToCanvasWorldPos(originScreen, canvasCam);
        Vector3 targetWorldPos = ScreenToCanvasWorldPos(targetScreenPos, canvasCam);

        for (int i = 0; i < coinCount; i++)
        {
            CoinEntity coin = runtime.pool.Get();
            var coinRT = coin.GetComponent<RectTransform>();
            coinRT.position = spawnWorldPos;
            coinRT.localScale = coinScaleOverride == 1f ? Vector3.one : Vector3.one * coinScaleOverride;


            int value = valuePerCoin + (i == coinCount - 1 ? remainder : 0);

            coin.Launch(
                type: type,
                origin: spawnWorldPos,
                target: targetWorldPos,
                cfg: config,
                value: value,
                onComplete: returnedCoin =>
                {
                    runtime.pool.Release(returnedCoin);
                    arrivedCount++;
                    if (arrivedCount >= coinCount)
                        GameEvent.CurrencyBurstComplete(type);
                }
            );

            yield return new WaitForSeconds(config.spawnInterval);
        }
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
    [Tooltip("Which currency this entry handles")]
    public CurrencyType currencyType;

    [Tooltip("The icon prefab (Image + CoinEntity) for this currency")]
    public CoinEntity iconPrefab;

    [Tooltip("The RectTransform icons fly toward (HUD counter icon)")]
    public RectTransform targetUI;

    [Tooltip("Settings for this currency's flow (speed, count, etc.)")]
    public CoinFlowConfig config;
}


public class CurrencyFlowRuntime
{
    public CoinFlowConfig config;
    public RectTransform targetUI;
    public ObjectPoo<CoinEntity> pool;
}