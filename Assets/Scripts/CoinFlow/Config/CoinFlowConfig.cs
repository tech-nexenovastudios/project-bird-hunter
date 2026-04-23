using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "CoinFlowConfig", menuName = "CoinFlow/Config")]
public class CoinFlowConfig : ScriptableObject
{
    [Header("═══ Coin Count ═══")]
    public int minCoins = 5;
    public int maxCoins = 15;
    [Range(5, 30)]
    public int coinsPerBurst = 10;

    [Header("═══ Timing ═══")]
    [Range(0.01f, 0.15f)]
    public float spawnInterval = 0.05f;

    [Range(0.3f, 2.0f)]
    public float flightDuration = 0.6f;

    [Header("═══ Path ═══")]
    [Tooltip("Small horizontal spread so coins don't stack on one line. " +
             "Keep this LOW — 15 to 40 pixels max.")]
    [Range(0f, 50f)]
    public float spreadWidth = 25f;

    [Tooltip("InOutSine or OutQuad work best for direct paths")]
    public Ease dotweenFlightEase = Ease.InOutSine;

    [Header("═══ Scale ═══")]
    [Range(0.3f, 1.5f)]
    public float startScale = 1.0f;

    [Range(0.2f, 1.0f)]
    public float endScale = 0.5f;

    [Header("═══ Pool ═══")]
    [Range(10, 60)]
    public int poolInitialSize = 20;
}