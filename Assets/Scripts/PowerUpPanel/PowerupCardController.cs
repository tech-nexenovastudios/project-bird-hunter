using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.PowerUps;

/// <summary>
/// Per-card component.
/// Identity resolution priority:
///   1. Spawner calls Initialise(id, rarity)  — explicit, always correct
///   2. If PowerupId is still empty in Start() — auto-resolve from the
///      database by matching this GameObject's name against config.displayName
///      or config.id, so cards placed directly in the scene still work.
/// </summary>
public class PowerupCardController : MonoBehaviour
{
    [HideInInspector] public string PowerupId;
    [HideInInspector] public PowerupRarity Rarity;

    public Image RootImage { get; private set; }
    public Image IconImage { get; private set; }
    public Image HeaderAssetImage { get; private set; }
    public TextMeshProUGUI HeaderText { get; private set; }

    // ── Called by spawner right after AddComponent ────────────────────────────
    public void Initialise(string id, PowerupRarity rarity)
    {
        PowerupId = id;
        Rarity = rarity;
        ResolveUIReferences();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (RootImage == null)
            ResolveUIReferences();
    }

    private void Start()
    {
        // If the spawner never called Initialise (card placed directly in scene)
        // try to find identity from the database using the GameObject name.
        if (string.IsNullOrEmpty(PowerupId))
            AutoResolveIdentity();

        if (string.IsNullOrEmpty(PowerupId))
        {
            Debug.LogWarning($"[PowerupCard] '{name}' — could not resolve PowerupId. " +
                             "Make sure the GameObject name matches config.displayName or config.id, " +
                             "or spawn cards through PowerupCardSpawner.");
            return;
        }

        PowerupLockView.Instance?.RegisterCard(this);
    }

    private void OnDestroy()
    {
        PowerupLockView.Instance?.UnregisterCard(this);
    }

    // ── Identity auto-resolve ─────────────────────────────────────────────────
    /// <summary>
    /// Searches the PowerupDatabase (loaded from the LockManager) for a config
    /// whose displayName or id matches this GameObject's name.
    /// </summary>
    private void AutoResolveIdentity()
    {
        var db = PowerupLockView.Instance?.GetDatabase();
        if (db == null) return;

        string goName = gameObject.name;

        foreach (var cfg in db.allPowerups)
        {
            if (cfg.displayName == goName || cfg.id == goName)
            {
                PowerupId = cfg.id;
                Rarity = cfg.rarity;
                return;
            }
        }

        // Fallback: strip "(Clone)" suffix Unity appends on Instantiate
        string stripped = goName.Replace("(Clone)", "").Trim();
        foreach (var cfg in db.allPowerups)
        {
            if (cfg.displayName == stripped || cfg.id == stripped)
            {
                PowerupId = cfg.id;
                Rarity = cfg.rarity;
                return;
            }
        }
    }

    // ── Per-card test hooks ───────────────────────────────────────────────────
    [ContextMenu("Test / Lock This Card")]
    public void TestLock() => PowerupLockView.Instance?.LockPowerup(PowerupId);

    [ContextMenu("Test / Unlock This Card")]
    public void TestUnlock() => PowerupLockView.Instance?.UnlockPowerup(PowerupId);

    [ContextMenu("Test / Toggle This Card")]
    public void TestToggle()
    {
        var mgr = PowerupLockView.Instance;
        if (mgr == null) return;
        if (mgr.IsUnlocked(PowerupId)) mgr.LockPowerup(PowerupId);
        else mgr.UnlockPowerup(PowerupId);
    }

    // ── UI reference resolver ─────────────────────────────────────────────────
    private void ResolveUIReferences()
    {
        RootImage = GetComponent<Image>();

        var iconT = FindDeep(transform, "icon");
        if (iconT != null) IconImage = iconT.GetComponent<Image>();

        var headerT = FindDeep(transform, "headerAsset");
        if (headerT != null)
        {
            HeaderAssetImage = headerT.GetComponent<Image>();
            HeaderText = headerT.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private static Transform FindDeep(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName) return child;
            var result = FindDeep(child, targetName);
            if (result != null) return result;
        }
        return null;
    }
}