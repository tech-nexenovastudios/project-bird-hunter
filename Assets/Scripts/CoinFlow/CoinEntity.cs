using System;
using System.Collections;
using Gameplay.Events;
using UnityEngine;

// ───────────────────────────────────────────────────────────
// PURPOSE: Self-contained coin icon. Knows how to:
//          1. Burst outward from origin
//          2. Fly along a curved arc to the target
//          3. Fire arrival event
//          4. Return itself to the pool
//
// ATTACH TO: The coin prefab (a UI Image on a RectTransform).
// ───────────────────────────────────────────────────────────

[RequireComponent(typeof(RectTransform))]
public class CoinEntity : MonoBehaviour
{
    private RectTransform rect;
    private CoinFlowConfig config;
    private Action<CoinEntity> releaseCallback;    // pool return
    private int coinValue;

    // Cached to avoid repeated GetComponent
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }


    /// <summary>
    /// Called by CoinFlowManager to kick off this coin's full animation.
    /// </summary>
    public void Launch(
        Vector2 origin,          // screen-space start
        Vector2 target,          // screen-space end (coin counter)
        CoinFlowConfig cfg,
        int value,
        Action<CoinEntity> onComplete)
    {
        config = cfg;
        coinValue = value;
        releaseCallback = onComplete;

        rect.position = origin;
        rect.localScale = Vector3.one * config.startScale;

        StartCoroutine(AnimateRoutine(origin, target));
    }


    private IEnumerator AnimateRoutine(Vector2 origin, Vector2 target)
    {
        // ── PHASE 1: Burst outward ────────────────────────
        // Pick a random direction and push the coin outward.
        // This creates the "explosion" scatter before flight.

        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector2 burstTarget = origin + randomDir * config.burstRadius;

        float elapsed = 0f;
        while (elapsed < config.burstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / config.burstDuration);

            // Ease-out for burst (fast start, gentle stop)
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rect.position = Vector2.Lerp(origin, burstTarget, eased);

            yield return null;   // wait one frame
        }


        // ── PHASE 2: Curved flight to target ──────────────
        // The coin now swoops from its burst position to the
        // coin counter UI. An arc offset makes it curve.

        Vector2 flightStart = burstTarget;

        // Perpendicular direction for the arc bend
        Vector2 direction = (target - flightStart).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        // Randomize arc side and strength for organic feel
        float arcOffset = config.arcStrength
                        * UnityEngine.Random.Range(0.5f, 1.0f)
                        * (UnityEngine.Random.value > 0.5f ? 1f : -1f);

        Vector2 controlPoint = Vector2.Lerp(flightStart, target, 0.5f)
                             + perpendicular * arcOffset;

        elapsed = 0f;
        while (elapsed < config.flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / config.flightDuration);

            // Use the designer's AnimationCurve for easing
            float curved = config.flightCurve.Evaluate(t);

            // Quadratic Bézier: start → control → end
            // This creates a smooth arc instead of a straight line.
            Vector2 a = Vector2.Lerp(flightStart, controlPoint, curved);
            Vector2 b = Vector2.Lerp(controlPoint, target, curved);
            rect.position = Vector2.Lerp(a, b, curved);

            // Scale shrinks as coin approaches target
            float scale = Mathf.Lerp(config.startScale, config.endScale, curved);
            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        // ── PHASE 3: Arrival ──────────────────────────────
        rect.position = target;
        GameEvent.CoinArrived(coinValue);

        // Return self to pool
        releaseCallback?.Invoke(this);
    }
}