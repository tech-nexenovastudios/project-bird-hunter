#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Scans the boss + VFX prefab folders, gathers every shared material, and writes a
// ShaderVariantCollection covering their shaders/keywords. ShaderWarmup warms this at runtime so
// the first boss render doesn't compile variants mid-fight.
//
// This is a best-effort starting collection (it adds the materials' current keyword sets across the
// URP pass types). For exact coverage, play through the bosses with Project Settings ▸ Graphics ▸
// Shader Loading tracking on, then "Save to asset" over the same file to capture the real variants.
public static class BossShaderVariantBuilder
{
    const string OutputPath = "Assets/Resources/BossShaderVariants.shadervariants";

    static readonly string[] PrefabFolders =
    {
        "Assets/Prefab/BossBirds",
        "Assets/VFX/BossBird",
        "Assets/BossBirdsVFX_Prefabs",
        "Assets/Resources/Birds/BossBirds",
        "Assets/Prefab/Birds/BossBird",
    };

    // URP renders through the ScriptableRenderPipeline pass types; the others cover any legacy/
    // built-in materials that slipped in. Invalid (shader, passType) combos throw on construct and
    // are skipped.
    static readonly PassType[] PassTypes =
    {
        PassType.ScriptableRenderPipeline,
        PassType.ScriptableRenderPipelineDefaultUnlit,
        PassType.Normal,
        PassType.ShadowCaster,
    };

    [MenuItem("Tools/Bird Hunter/Build Boss Shader Variant Collection")]
    public static void Build()
    {
        var materials = CollectMaterials();
        if (materials.Count == 0)
        {
            Debug.LogWarning("[BossShaderVariantBuilder] No materials found in the boss/VFX prefab folders.");
            return;
        }

        var svc = new ShaderVariantCollection();
        int variantAdds = 0;

        foreach (var mat in materials)
        {
            if (mat == null || mat.shader == null) continue;
            string[] keywords = mat.shaderKeywords;

            foreach (var pass in PassTypes)
            {
                variantAdds += TryAdd(svc, mat.shader, pass, keywords);
                variantAdds += TryAdd(svc, mat.shader, pass, System.Array.Empty<string>());
            }
        }

        WriteAsset(svc);
        Debug.Log($"[BossShaderVariantBuilder] Built {OutputPath} — {materials.Count} materials, " +
                  $"{svc.shaderCount} shaders, {svc.variantCount} variants ({variantAdds} adds).");
        Selection.activeObject = svc;
    }

    static HashSet<Material> CollectMaterials()
    {
        var materials = new HashSet<Material>();

        foreach (var folder in PrefabFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat != null) materials.Add(mat);
            }
        }

        return materials;
    }

    static int TryAdd(ShaderVariantCollection svc, Shader shader, PassType pass, string[] keywords)
    {
        try
        {
            var variant = new ShaderVariantCollection.ShaderVariant(shader, pass, keywords);
            if (!svc.Contains(variant))
            {
                svc.Add(variant);
                return 1;
            }
        }
        catch
        {
            // (shader, passType, keywords) isn't a valid variant for this shader — skip it.
        }
        return 0;
    }

    static void WriteAsset(ShaderVariantCollection svc)
    {
        var dir = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            Directory.CreateDirectory(dir);

        if (AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(OutputPath) != null)
            AssetDatabase.DeleteAsset(OutputPath);

        AssetDatabase.CreateAsset(svc, OutputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
