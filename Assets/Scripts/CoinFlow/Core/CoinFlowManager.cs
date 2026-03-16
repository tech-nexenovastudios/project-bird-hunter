using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinFlowManager : MonoBehaviour
{
    // ── One entry per currency ───────────────────────────
    // You'll set up 3 of these in the Inspector:
    //   Gold  → gold coin prefab  → gold counter icon
    //   Gems  → gem prefab        → gem counter icon
    //   Power → power prefab      → power counter icon

    [SerializeField] private CurrencyFlowEntry[] currencyEntries;


    // ── Runtime lookup ───────────────────────────────────
    // Dictionary lets us instantly find the right prefab/pool/target
    // for any CurrencyType without looping every time.

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

            // Build a separate pool for each currency type.
            // Gold coins go back to the gold pool, gems to the gem pool, etc.
            var pool = new ObjectPoo<CoinEntity>(
                prefab: entry.iconPrefab,
                parent: transform,
                initialSize: entry.config.poolInitialSize,
                onGet: coin => coin.gameObject.SetActive(true),
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


    private void OnEnable()
    {
        GameEvent.OnCurrencyCollected += HandleCurrencyCollected;
    }

    private void OnDisable()
    {
        GameEvent.OnCurrencyCollected -= HandleCurrencyCollected;
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


    private IEnumerator SpawnFlowRoutine(CurrencyType type, Vector2 origin, int totalValue)
    {
        var runtime = runtimeMap[type];
        var config = runtime.config;
        int coinCount = config.coinsPerBurst;

        int valuePerCoin = totalValue / coinCount;
        int remainder = totalValue % coinCount;
        int arrivedCount = 0;

        Vector2 targetPos = runtime.targetUI.position;

        for (int i = 0; i < coinCount; i++)
        {
            CoinEntity coin = runtime.pool.Get();
            int value = valuePerCoin + (i == coinCount - 1 ? remainder : 0);

            coin.Launch(
                type: type,
                origin: origin,
                target: targetPos,
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
}


// ── Inspector data (what you set up per currency) ────────

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


// ── Internal runtime data (not visible in Inspector) ─────

public class CurrencyFlowRuntime
{
    public CoinFlowConfig config;
    public RectTransform targetUI;
    public ObjectPoo<CoinEntity> pool;
}
