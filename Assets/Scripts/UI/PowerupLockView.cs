using System.Collections.Generic;
using Gameplay.Events;
using Gameplay.Levels;
using Gameplay.PowerUps;
using UnityEngine;
using UnityEngine.UI;

public class PowerupLockView : MonoBehaviour
{
    public static PowerupLockView Instance { get; private set; }

    [Header("Grayscale Material")]
    [SerializeField] private Material grayscaleMaterial;

    // ─── Rarity colors ───
    private static readonly Color ColCommon = Hex("98F3AF");
    private static readonly Color ColRare = Hex("F8E64B");
    private static readonly Color ColEpic = Hex("EAB3FF");
    private static readonly Color ColLegendary = Hex("FF9B94");

    private readonly Dictionary<string, PowerupCardController> cards = new();

    // ─── Lifecycle ───
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (grayscaleMaterial == null)
            Debug.LogWarning("[PowerupLockView] Grayscale material not assigned.");
    }

    private void OnEnable()
    {
        GameEvents.OnGameLevelUpdated += OnLevelUpdated;
        RefreshAllCards();
    }

    private void OnDisable()
    {
        GameEvents.OnGameLevelUpdated -= OnLevelUpdated;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnLevelUpdated(LevelProfile _, int __) => RefreshAllCards();

    // ─── Database access (used by PowerupCardController.AutoResolveIdentity) ───
    /// <summary>
    /// Returns the shared PowerupDatabase loaded by PowerupGate from Resources.
    /// No serialized field needed — the gate owns the single load point.
    /// </summary>
    public PowerupDatabase GetDatabase() => PowerupGate.Database;

    // ─── Lock / Unlock API ───
    /// <summary>
    /// Forces the locked visual on a card regardless of gate state.
    /// Useful for editor context-menu tests. Does NOT mutate unlock state
    /// (PowerupGate is stateless — truth comes from config + chapter progress).
    /// </summary>
    public void LockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (cards.TryGetValue(id, out var card))
            ApplyLockedVisual(card);
    }

    /// <summary>
    /// Forces the unlocked visual on a card regardless of gate state.
    /// Useful for editor context-menu tests.
    /// </summary>
    public void UnlockPowerup(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (cards.TryGetValue(id, out var card))
            ApplyUnlockedVisual(card);
    }

    /// <summary>
    /// Delegates to PowerupGate for the authoritative unlock check.
    /// </summary>
    public bool IsUnlocked(string id) => PowerupGate.IsUnlocked(id);

    // ─── Card registration ───
    public void RegisterCard(PowerupCardController card)
    {
        if (card == null || string.IsNullOrEmpty(card.PowerupId))
        {
            Debug.LogWarning("[PowerupLockView] RegisterCard — null or empty id.");
            return;
        }
        cards[card.PowerupId] = card;
        ApplyVisual(card, PowerupGate.IsUnlocked(card.PowerupId));
    }

    public void UnregisterCard(PowerupCardController card)
    {
        if (card != null) cards.Remove(card.PowerupId);
    }

    public void RefreshAllCards()
    {
        foreach (var kvp in cards)
            ApplyVisual(kvp.Value, PowerupGate.IsUnlocked(kvp.Key));
    }

    // ─── Visuals ───
    private void ApplyVisual(PowerupCardController card, bool unlocked)
    {
        if (unlocked) ApplyUnlockedVisual(card);
        else ApplyLockedVisual(card);
    }

    public void ApplyLockedVisual(PowerupCardController card)
    {
        if (card == null) return;
        SetMat(card.RootImage, grayscaleMaterial);
        SetMat(card.IconImage, grayscaleMaterial);
        SetMat(card.HeaderAssetImage, grayscaleMaterial);
        if (card.HeaderText != null) card.HeaderText.color = Color.white;
    }

    public void ApplyUnlockedVisual(PowerupCardController card)
    {
        if (card == null) return;
        SetMat(card.RootImage, null);
        SetMat(card.IconImage, null);
        SetMat(card.HeaderAssetImage, null);
        if (card.HeaderText != null)
            card.HeaderText.color = RarityColour(card.Rarity);
    }

    private static void SetMat(Image img, Material mat)
    {
        if (img != null) img.material = mat;
    }

    private static Color RarityColour(PowerupRarity r) => r switch
    {
        PowerupRarity.Common => ColCommon,
        PowerupRarity.Rare => ColRare,
        PowerupRarity.Epic => ColEpic,
        PowerupRarity.Legendary => ColLegendary,
        _ => Color.white
    };

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}