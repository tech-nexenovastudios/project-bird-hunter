using BirdHunter.Inventory.Data;
using BirdHunter.Inventory.Stats;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Gameplay-only utility. Reads the equipped cannon snapshot from cloud
/// and builds a configured StatSheet.
/// Does NOT require CannonInventoryService to be in the scene.
/// </summary>
public static class CannonLoader
{
    private static readonly object UpgradeSource = new { tag = "Upgrade" };
    private static CannonProgressionConfig _cachedProgression;

    private static CannonProgressionConfig GetProgression()
    {
        if (_cachedProgression == null)
            _cachedProgression = Resources.Load<CannonProgressionConfig>("CannonProgressionConfig");
        return _cachedProgression;
    }

    public static async UniTask<EquippedCannonSnapshot> LoadSnapshotAsync()
    {
        try
        {
            var snapshot = await CloudSaveManager.Instance.LoadValueAsync<EquippedCannonSnapshot>(
                CloudKeys.EQUIPPED_CANNON_SNAPSHOT,
                new EquippedCannonSnapshot()
            );
            return snapshot;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CannonLoader] Snapshot load failed: {ex.Message}");
            return new EquippedCannonSnapshot();
        }
    }

    public static async UniTask<string> GetEquippedCannonKeyAsync()
    {
        var snapshot = await LoadSnapshotAsync();
        return snapshot.cannonKey;
    }

    public static async UniTask<StatSheet> BuildStatSheetForEquippedAsync()
    {
        var snapshot = await LoadSnapshotAsync();
        if (string.IsNullOrEmpty(snapshot.cannonKey))
        {
            Debug.LogWarning("[CannonLoader] No equipped cannon in snapshot.");
            return null;
        }

        await CannonStatsRepository.Instance.LoadAsync();

        var dto = CannonStatsRepository.Instance.GetByKey(snapshot.cannonKey);
        if (dto == null)
        {
            Debug.LogError($"[CannonLoader] No DTO found for key '{snapshot.cannonKey}'.");
            return null;
        }

        var sheet = dto.BuildStatSheet();
        ApplyUpgradeModifier(sheet, snapshot.level);
        return sheet;
    }

    private static void ApplyUpgradeModifier(StatSheet sheet, int level)
    {
        var progression = GetProgression();
        if (progression == null)
        {
            Debug.LogError("[CannonLoader] CannonProgressionConfig not found in Resources.");
            return;
        }

        sheet.RemoveBySource(UpgradeSource);
        if (level <= 1) return;

        int steps = level - 1;
        sheet.Add(new StatModifier(StatType.Damage, steps * progression.damagePerLevelPct, StatModOp.PctAdd, UpgradeSource));
        sheet.Add(new StatModifier(StatType.Health, steps * progression.healthPerLevelPct, StatModOp.PctAdd, UpgradeSource));
        sheet.Add(new StatModifier(StatType.FireRate, steps * progression.fireRatePerLevelPct, StatModOp.PctAdd, UpgradeSource));
        sheet.Add(new StatModifier(StatType.MoveSpeed, steps * progression.moveSpeedPerLevelPct, StatModOp.PctAdd, UpgradeSource));
    }
}