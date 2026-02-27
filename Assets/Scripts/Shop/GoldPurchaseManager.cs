using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;

public class GoldPurchaseManager : MonoBehaviour
{
    [Header("Purchase Configurations")]
    [SerializeField] private PurchaseCard[] purchaseCards;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Transform loadingIcon;
    [SerializeField] private float rotateSpeed = 300f;

    private bool isLoading = false;

    private void Awake()
    {
        SetupPurchaseButtons();
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void Update()
    {
        if (isLoading && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
    }

    private void Start()
    {
        // Sync Economy config (Auth is already handled by AuthBootstrapper)
        SyncConfig().Forget();
    }

    private async UniTaskVoid SyncConfig()
    {
        SetButtonsInteractable(false);
        ShowLoading();

        try
        {
            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            SetButtonsInteractable(true);
            Debug.Log("[GoldPurchase] Economy config synced.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoldPurchase] Config sync failed: {ex.Message}");
        }
        finally
        {
            HideLoading();
        }
    }

    // ==================== Button Setup ====================

    private void SetupPurchaseButtons()
    {
        for (int i = 0; i < purchaseCards.Length; i++)
        {
            int index = i;
            if (purchaseCards[i].purchaseButton != null)
                purchaseCards[i].purchaseButton.onClick.AddListener(() => OnPurchaseClicked(index));
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        if (purchaseCards == null) return;

        foreach (var card in purchaseCards)
        {
            if (card.purchaseButton != null)
                card.purchaseButton.interactable = state;
        }
    }

    // ==================== Purchase ====================

    private void OnPurchaseClicked(int index)
    {
        ProcessPurchase(index).Forget();
    }

    private async UniTaskVoid ProcessPurchase(int index)
    {
        var card = purchaseCards[index];
        card.purchaseButton.interactable = false;
        ShowLoading();

        try
        {
            MakeVirtualPurchaseResult result =
                await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync(card.virtualPurchaseId);

            if (result != null)
            {
                Debug.Log($"[GoldPurchase] Purchase '{card.purchaseName}' successful!");

                // Log rewards
                if (result.Rewards != null)
                {
                    foreach (var item in result.Rewards.Inventory)
                        Debug.Log($"  Received item: {item.Id}");

                    foreach (var curr in result.Rewards.Currency)
                        Debug.Log($"  Received currency: {curr.Amount} {curr.Id}");
                }

                // Log costs deducted
                if (result.Costs != null)
                {
                    foreach (var curr in result.Costs.Currency)
                        Debug.Log($"  Spent: {curr.Amount} {curr.Id}");
                }

                // Refresh CurrencyManager so UI updates
                await CurrencyManager.Instance.Refresh();
            }
        }
        catch (EconomyException ex)
        {
            switch (ex.ErrorCode)
            {
                case 10504:
                    Debug.LogWarning($"[GoldPurchase] Not enough currency for '{card.purchaseName}'.");
                    CurrencyManager.Instance.NotifyInsufficientFunds(CurrencyType.Gems, 0);
                    break;
                default:
                    Debug.LogError($"[GoldPurchase] Economy error {ex.ErrorCode}: {ex.Message}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GoldPurchase] Error: {ex.Message}");
        }
        finally
        {
            HideLoading();
            card.purchaseButton.interactable = true;
        }
    }

    // ==================== Loading UI ====================

    private void ShowLoading()
    {
        isLoading = true;
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    private void HideLoading()
    {
        isLoading = false;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    // ==================== Cleanup ====================

    private void OnDestroy()
    {
        if (purchaseCards == null) return;

        foreach (var card in purchaseCards)
        {
            if (card.purchaseButton != null)
                card.purchaseButton.onClick.RemoveAllListeners();
        }
    }
}

[System.Serializable]
public class PurchaseCard
{
    public string purchaseName;
    public string virtualPurchaseId;
    public Button purchaseButton;
}