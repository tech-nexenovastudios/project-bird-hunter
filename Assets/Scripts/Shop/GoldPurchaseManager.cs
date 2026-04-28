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
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void Update()
    {
        if (isLoading && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
    }

    private void Start() => SyncConfig().Forget();

    private async UniTaskVoid SyncConfig()
    {
        SetButtonsInteractable(false);
        ShowLoading();

        try
        {
            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            SetButtonsInteractable(true);
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
            if (card.purchaseButton != null) card.purchaseButton.interactable = state;
    }

    private void OnPurchaseClicked(int index) => ProcessPurchase(index).Forget();

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
                // ── Convert button world position → screen pixels ──
                Canvas canvas = card.purchaseButton.GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? canvas.worldCamera : null;
                Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(
                    cam, card.purchaseButton.transform.position);

                int goldAmount = 0;
                if (result.Rewards != null)
                {
                    foreach (var curr in result.Rewards.Currency)
                        if (curr.Id == "GOLD") goldAmount = (int)curr.Amount;
                }

                if (goldAmount > 0)
                    GameEvent.CurrencyCollected(CurrencyType.Gold, buttonScreenPos, goldAmount);

                await CurrencyManager.Instance.Refresh();
            }
        }
        catch (EconomyException ex)
        {
            switch (ex.ErrorCode)
            {
                case 10504:
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

    private void ShowLoading()
    {
        isLoading = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    private void HideLoading()
    {
        isLoading = false;
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (purchaseCards == null) return;
        foreach (var card in purchaseCards)
            if (card.purchaseButton != null) card.purchaseButton.onClick.RemoveAllListeners();
    }
}