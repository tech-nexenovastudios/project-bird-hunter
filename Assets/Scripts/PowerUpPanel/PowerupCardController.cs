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

    public void Initialise(string id, PowerupRarity rarity)
    {
        PowerupId = id;
        Rarity = rarity;
        ResolveUIReferences();
    }

    private void Awake()
    {
        if (RootImage == null)
            ResolveUIReferences();
    }

    private void Start()
    {
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

    private void AutoResolveIdentity()
    {
        var cfg = PowerupGate.FindByNameOrId(gameObject.name);
        if (cfg == null) return;

        PowerupId = cfg.id;
        Rarity = cfg.rarity;
        Debug.Log($"[PowerupCard] '{gameObject.name}' auto-resolved → id='{cfg.id}', rarity={cfg.rarity}");
    }

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
