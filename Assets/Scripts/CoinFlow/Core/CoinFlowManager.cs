using System.Collections;
using Gameplay.Events;
using UnityEngine;

// ───────────────────────────────────────────────────────────
// PURPOSE: Listens for coin-collect events, orchestrates the
//          full burst-and-fly sequence using pooled coins.
//
// SETUP (Inspector):
//   1. Drag your CoinFlowConfig asset into "Config"
//   2. Drag your coin UI prefab into "Coin Prefab"
//   3. Drag the RectTransform of your coin counter icon
//      (the target coins fly toward) into "Target UI"
//
// HIERARCHY:
//   Canvas (Screen Space - Overlay)
//   ├── CoinCounter (top of screen)
//   │   └── CoinIcon  ← drag this into "Target UI"
//   └── CoinFlowManager (this script)
//       └── (pooled coins spawn as children here)
// ───────────────────────────────────────────────────────────

public class CoinFlowManager : MonoBehaviour
{
    [Header("═══ References ═══")]

    [Tooltip("The ScriptableObject with all coin-flow settings")]
    [SerializeField] private CoinFlowConfig config;

    [Tooltip("Prefab: a UI Image with CoinEntity script attached")]
    [SerializeField] private CoinEntity coinPrefab;

    [Tooltip("The RectTransform coins fly toward (coin icon in HUD)")]
    [SerializeField] private RectTransform targetUI;


    // ── Internal State ───────────────────────────────────
    private ObjectPoo<CoinEntity> pool;
    private Camera mainCam;


    private void Awake()
    {
        mainCam = Camera.main;

        // Build the pool. Coins start hidden (SetActive false).
        pool = new ObjectPoo<CoinEntity>(
            prefab: coinPrefab,
            parent: transform,
            initialSize: config.poolInitialSize,
            onGet: coin => coin.gameObject.SetActive(true),
            onRelease: coin => coin.gameObject.SetActive(false)
        );
    }


    // ── Subscribe / Unsubscribe ──────────────────────────
    // ALWAYS unsubscribe in OnDisable to prevent memory leaks
    // and errors from destroyed objects still listening.

    private void OnEnable()
    {
        GameEvent.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        GameEvent.OnCoinCollected -= HandleCoinCollected;
    }


    // ── Event Handler ────────────────────────────────────

    /// <param name="screenPos">
    /// Where on screen the coins burst from.
    /// If triggered from a world-space object (e.g., a chest),
    /// convert with: Camera.main.WorldToScreenPoint(chest.position)
    /// </param>
    /// <param name="totalValue">
    /// Total coins earned. Divided evenly across the burst.
    /// </param>
    private void HandleCoinCollected(Vector2 screenPos, int totalValue)
    {
        StartCoroutine(SpawnBurstRoutine(screenPos, totalValue));
    }


    private IEnumerator SpawnBurstRoutine(Vector2 origin, int totalValue)
    {
        int coinCount = config.coinsPerBurst;

        // Divide total value across coins.
        // Example: 100 coins / 10 icons = each icon worth 10.
        // Remainder goes to last coin so nothing is lost.
        int valuePerCoin = totalValue / coinCount;
        int remainder = totalValue % coinCount;

        // Track how many coins from THIS burst have arrived
        int arrivedCount = 0;

        // Target position (coin counter icon's screen position)
        Vector2 targetPos = targetUI.position;

        for (int i = 0; i < coinCount; i++)
        {
            CoinEntity coin = pool.Get();

            int value = valuePerCoin + (i == coinCount - 1 ? remainder : 0);

            coin.Launch(
                origin: origin,
                target: targetPos,
                cfg: config,
                value: value,
                onComplete: returnedCoin =>
                {
                    pool.Release(returnedCoin);
                    arrivedCount++;

                    if (arrivedCount >= coinCount)
                    {
                        GameEvent.CoinBurstComplete();
                    }
                }
            );

            // Stagger: wait before launching next coin
            yield return new WaitForSeconds(config.spawnInterval);
        }
    }
}