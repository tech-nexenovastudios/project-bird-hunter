using System.Collections.Generic;
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

    // ─── Runtime ───
    private readonly Dictionary<string, PowerupCardController> cards = new();
    private PowerupUnlockService unlockService;

    // ─── Lifecycle ───

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (grayscaleMaterial == null)
            Debug.LogWarning("[PowerupLockView] Grayscale material not assigned.");
    }

    private void Start()
    {
        unlockService = ServiceLocator.Get<PowerupUnlockService>();

        if (unlockService == null || !unlockService.IsReady)
        {
            Debug.LogError("[PowerupLockView] PowerupUnlockService not available or not ready.");
            return;
        }

        RefreshAllCards();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PowerupUnlockedEvent>(OnPowerupUnlocked);
        EventBus.Subscribe<PowerupLockedEvent>(OnPowerupLocked);
        EventBus.Subscribe<PowerupStatesLoadedEvent>(OnStatesLoaded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PowerupUnlockedEvent>(OnPowerupUnlocked);
        EventBus.Unsubscribe<PowerupLockedEvent>(OnPowerupLocked);
        EventBus.Unsubscribe<PowerupStatesLoadedEvent>(OnStatesLoaded);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─── Event handlers ───

    private void OnPowerupUnlocked(PowerupUnlockedEvent evt)
    {
        if (cards.TryGetValue(evt.powerupId, out var card))
            ApplyUnlockedVisual(card);
    }

    private void OnPowerupLocked(PowerupLockedEvent evt)
    {
        if (cards.TryGetValue(evt.powerupId, out var card))
            ApplyLockedVisual(card);
    }

    private void OnStatesLoaded(PowerupStatesLoadedEvent evt)
    {
        RefreshAllCards();
    }

    // ─── Card registration ───

    public void RegisterCard(PowerupCardController card)
    {
        if (card == null || string.IsNullOrEmpty(card.PowerupId))
        {
            Debug.LogWarning("[PowerupLockView] RegisterCard — null or empty id.");
            return;
        }

        cards[card.PowerupId] = card;

        bool unlocked = unlockService != null && unlockService.IsUnlocked(card.PowerupId);
        ApplyVisual(card, unlocked);
    }

    public void UnregisterCard(PowerupCardController card)
    {
        if (card != null) cards.Remove(card.PowerupId);
    }

    public void RefreshAllCards()
    {
        if (unlockService == null) return;
        foreach (var kvp in cards)
            ApplyVisual(kvp.Value, unlockService.IsUnlocked(kvp.Key));
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