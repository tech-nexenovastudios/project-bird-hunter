using UnityEngine;

// ───────────────────────────────────────────────────────────
// PURPOSE: Single source of truth for ALL coin-flow settings.
//          Change values here — nothing else needs editing.
//
// HOW TO USE:
//   1. Right-click in Project window
//   2. Create → CoinFlow → Config
//   3. Name it "DefaultCoinFlowConfig"
//   4. Drag it into CoinFlowManager's inspector slot
// ───────────────────────────────────────────────────────────

[CreateAssetMenu(
    fileName = "CoinFlowConfig",
    menuName = "CoinFlow/Config",
    order = 0
)]
public class CoinFlowConfig : ScriptableObject
{
    [Header("═══ Coin Count ═══")]

    [Tooltip("How many coin icons fly toward the counter per trigger")]
    [Range(5, 30)]
    public int coinsPerBurst = 10;


    [Header("═══ Timing ═══")]

    [Tooltip("Seconds between each coin's launch (stagger effect)")]
    [Range(0.01f, 0.15f)]
    public float spawnInterval = 0.04f;

    [Tooltip("Total seconds each coin takes to reach the target")]
    [Range(0.3f, 2.0f)]
    public float flightDuration = 0.7f;


    [Header("═══ Spread — Initial Burst ═══")]

    [Tooltip("Coins burst outward this many pixels from the origin before flying to target")]
    [Range(20f, 200f)]
    public float burstRadius = 80f;

    [Tooltip("Seconds the burst-outward phase takes (before curving to target)")]
    [Range(0.05f, 0.4f)]
    public float burstDuration = 0.15f;


    [Header("═══ Flight Path Curve ═══")]

    [Tooltip("Controls the flight arc. Default ease-in-out gives a satisfying swoop.")]
    public AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Maximum sideways pixel offset at the midpoint of flight (creates arc)")]
    [Range(0f, 300f)]
    public float arcStrength = 120f;


    [Header("═══ Visual ═══")]

    [Tooltip("Coin icon starts at this scale")]
    [Range(0.3f, 1.5f)]
    public float startScale = 1.0f;

    [Tooltip("Coin icon ends at this scale when reaching target")]
    [Range(0.2f, 1.0f)]
    public float endScale = 0.5f;


    [Header("═══ Pool ═══")]

    [Tooltip("Pre-created coins sitting in memory ready to go. " +
             "Set this to your largest expected burst size.")]
    [Range(10, 60)]
    public int poolInitialSize = 20;
}