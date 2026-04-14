using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sits on the trailVfxPrefab.
/// Scales from zero → full size over <see cref="spreadDuration"/> to mimic spreading poison,
/// then auto-returns itself to the pool after its lifetime expires.
/// </summary>
public class HazardTrailStamp : MonoBehaviour, IPoolable
{
    [Tooltip("How long the stamp takes to reach full size — the 'spreading' feel")]
    [SerializeField] private float spreadDuration = 0.35f;

    [Tooltip("Easing curve for the scale-up (leave as EaseOut for a natural settle)")]
    [SerializeField] private AnimationCurve spreadCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Action<GameObject> onExpiredCallback;
    private Coroutine activeCoroutine;
    private Vector3 targetScale;

    // ── Public API ───────────────────────────────────────────────────

    /// <param name="lifetime">Total seconds before auto-return to pool.</param>
    /// <param name="onExpired">Callback so HazardRollTrail can remove from its list.</param>
    public void Activate(float lifetime, Action<GameObject> onExpired)
    {
        onExpiredCallback = onExpired;
        targetScale = transform.localScale != Vector3.zero
                            ? transform.localScale
                            : Vector3.one;

        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(RunLifetime(lifetime));
    }

    // ── IPoolable ────────────────────────────────────────────────────

    public void OnPoolSpawned()
    {
        transform.localScale = Vector3.zero; // start invisible; Activate() scales it up
        onExpiredCallback = null;
        activeCoroutine = null;
    }

    public void OnPoolDespawned()
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = null;
    }

    // ── Internals ────────────────────────────────────────────────────

    private IEnumerator RunLifetime(float lifetime)
    {
        // ── Phase 1: scale up (spreading) ──
        float elapsed = 0f;
        while (elapsed < spreadDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spreadDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, spreadCurve.Evaluate(t));
            yield return null;
        }
        transform.localScale = targetScale;

        // ── Phase 2: linger ──
        float linger = lifetime - spreadDuration;
        if (linger > 0f)
            yield return new WaitForSeconds(linger);

        // ── Phase 3: return to pool ──
        onExpiredCallback?.Invoke(gameObject);
        PoolManager.Return(gameObject);
    }
}