using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.PowerUps;
using System.Collections.Generic;

/// <summary>
/// Spawns powerup cards from the database.
/// Works in Edit Mode (Spawn Cards button) and Play Mode.
///
/// After spawning each card it:
///   1. Sets the icon sprite and display name
///   2. Adds / finds a PowerupCardController on the card
///   3. Calls Initialise(id, rarity) so the controller knows its identity
///   4. Registers the card with PowerupLockView so locked/unlocked visual applies
///      immediately — no waiting for async load.
/// </summary>
public class PowerupCardSpawner : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PowerupDatabase database;

    [Header("Card Prefabs")]
    [SerializeField] private GameObject commonPrefab;
    [SerializeField] private GameObject rarePrefab;
    [SerializeField] private GameObject epicPrefab;
    [SerializeField] private GameObject legendaryPrefab;

    [Header("Spawn Target")]
    [Tooltip("Parent with GridLayoutGroup")]
    [SerializeField] private Transform container;

    // ─────────────────────────────────────────────────────────────────────
    // Editor button entry point
    // ─────────────────────────────────────────────────────────────────────

    public void SpawnAll()
    {
        if (database == null || database.allPowerups == null || database.allPowerups.Count == 0)
        {
            Debug.LogError("[PowerupCards] Database is null or empty.");
            return;
        }

        if (container == null)
        {
            Debug.LogError("[PowerupCards] Container is not assigned.");
            return;
        }

        ClearContainer();

        // Sort: Common(0) → Rare(1) → Epic(2) → Legendary(3)
        var sorted = new List<PowerupConfig>(database.allPowerups);
        sorted.Sort((a, b) => ((int)a.rarity).CompareTo((int)b.rarity));

        foreach (var config in sorted)
        {
            GameObject prefab = GetPrefabForRarity(config.rarity);
            if (prefab == null)
            {
                Debug.LogWarning($"[PowerupCards] No prefab for {config.rarity}. Skipping '{config.displayName}'.");
                continue;
            }

            // ── Instantiate ───────────────────────────────────────────────
#if UNITY_EDITOR
            GameObject card = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, container);
#else
            GameObject card = Instantiate(prefab, container);
#endif
            card.name = config.displayName;

            // ── Icon sprite ───────────────────────────────────────────────
            var iconTransform = FindDeep(card.transform, "icon");
            if (iconTransform != null)
            {
                var iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && config.icon != null)
                    iconImage.sprite = config.icon;
            }

            // ── Display name ──────────────────────────────────────────────
            var nameTransform = FindDeep(card.transform, "powerUpName");
            if (nameTransform != null)
            {
                var nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = config.displayName;
            }

            // ── Lock / Unlock wiring ──────────────────────────────────────
            var ctrl = card.GetComponent<PowerupCardController>();
            if (ctrl == null)
                ctrl = card.AddComponent<PowerupCardController>();

            // Give the controller its identity (id + rarity).
            // Must happen before RegisterCard so the view can look up state.
            ctrl.Initialise(config.id, config.rarity);

            // In Play Mode the controller's Start() will call RegisterCard.
            // In Edit Mode (or if Start hasn't fired yet) we call it manually
            // so the visual applies immediately after spawning.
            if (Application.isPlaying && PowerupLockView.Instance != null)
            {
                // Force re-register in case this card was already in the dictionary
                // from a previous spawn — ensures the visual is always current.
                PowerupLockView.Instance.RegisterCard(ctrl);
            }
        }

        Debug.Log($"[PowerupCards] Spawned {sorted.Count} cards.");
    }

    // ─────────────────────────────────────────────────────────────────────

    public void ClearContainer()
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(container.GetChild(i).gameObject);
            else
                DestroyImmediate(container.GetChild(i).gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    private GameObject GetPrefabForRarity(PowerupRarity rarity)
    {
        return rarity switch
        {
            PowerupRarity.Common => commonPrefab,
            PowerupRarity.Rare => rarePrefab,
            PowerupRarity.Epic => epicPrefab,
            PowerupRarity.Legendary => legendaryPrefab,
            _ => commonPrefab
        };
    }

    private Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            var result = FindDeep(child, childName);
            if (result != null)
                return result;
        }
        return null;
    }
}