using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Gameplay.PowerUps;
using System.Collections.Generic;

/// <summary>
/// Spawns powerup cards in the editor (no play mode needed).
/// Use the "Spawn Cards" button in the Inspector.
/// Sort: Common → Epic → Rare → Legendary
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

        // Sort: Common(0) → Epic(2) → Rare(1) → Legendary(3)
        int[] sortOrder = { 0, 1, 2, 3 };
        var sorted = new List<PowerupConfig>(database.allPowerups);
        sorted.Sort((a, b) =>
        {
            int orderA = System.Array.IndexOf(sortOrder, (int)a.rarity);
            int orderB = System.Array.IndexOf(sortOrder, (int)b.rarity);
            return orderA.CompareTo(orderB);
        });

        foreach (var config in sorted)
        {
            GameObject prefab = GetPrefabForRarity(config.rarity);
            if (prefab == null)
            {
                Debug.LogWarning($"[PowerupCards] No prefab for {config.rarity}. Skipping '{config.displayName}'.");
                continue;
            }

#if UNITY_EDITOR
            GameObject card = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, container);
#else
            GameObject card = Instantiate(prefab, container);
#endif

            card.name = config.displayName;

            // Find "icon" child (recursive) and set sprite
            var iconTransform = FindDeep(card.transform, "icon");
            if (iconTransform != null)
            {
                var iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && config.icon != null)
                    iconImage.sprite = config.icon;
            }

            // Find "powerUpName" child (recursive) and set text
            var nameTransform = FindDeep(card.transform, "powerUpName");
            if (nameTransform != null)
            {
                var nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                    nameText.text = config.displayName;
            }
        }

        Debug.Log($"[PowerupCards] Spawned {sorted.Count} cards.");
    }

    public void ClearContainer()
    {
        if (container == null) return;

        // DestroyImmediate needed in edit mode
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(container.GetChild(i).gameObject);
            else
                DestroyImmediate(container.GetChild(i).gameObject);
        }
    }

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