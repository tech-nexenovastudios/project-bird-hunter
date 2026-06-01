using UnityEngine;

// Compiles the boss + VFX shader variants up-front so the first boss render doesn't stall the
// frame compiling them mid-fight. WarmUp() is a synchronous main-thread call (GPU pipeline-state
// creation can't be threaded or time-sliced), so it's fired once per session behind the level-start
// countdown rather than during the boss spawn — moving the cost to a moment the player can't act.
public static class ShaderWarmup
{
    const string ResourcesCollectionName = "BossShaderVariants";

    static bool _warmed;

    /// <summary>
    /// Warms the given collection once. Falls back to Resources/BossShaderVariants.shadervariants
    /// (built by Tools ▸ Bird Hunter ▸ Build Boss Shader Variant Collection) when none is assigned.
    /// No-op on subsequent calls — variants stay resident for the app's lifetime.
    /// </summary>
    public static void WarmBossVariants(ShaderVariantCollection collection)
    {
        if (_warmed) return;

        if (collection == null)
            collection = Resources.Load<ShaderVariantCollection>(ResourcesCollectionName);

        if (collection == null)
        {
            Debug.LogWarning("[ShaderWarmup] No boss ShaderVariantCollection assigned or found in " +
                             $"Resources/{ResourcesCollectionName}.shadervariants — skipping warmup. " +
                             "Build one via Tools ▸ Bird Hunter ▸ Build Boss Shader Variant Collection.");
            return;
        }

        _warmed = true;
        collection.WarmUp();
        Debug.Log($"[ShaderWarmup] Warmed {collection.variantCount} boss shader variants.");
    }
}
