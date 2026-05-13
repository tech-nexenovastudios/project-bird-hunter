using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class RemoveAdsBuyManager : MonoBehaviour
{
    [Header("Buy")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject ownedBadge;

    [Header("Restore (required by App Store review)")]
    [SerializeField] private Button restoreButton;

    [Header("Loading UI (optional)")]
    [SerializeField] private GameObject loadingPanel;

    private bool _isPurchasing;
    private bool _isRestoring;

    private void Start()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnBuyClicked);
            purchaseButton.interactable = false;
        }
        if (restoreButton != null)
            restoreButton.onClick.AddListener(OnRestoreClicked);

        ApplyOwnershipUI();

        if (IAPManager.Instance.IsInitialized)
            OnIAPReady();
        else
            IAPManager.Instance.Initialized += OnIAPReady;
    }

    private void OnDestroy()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(OnBuyClicked);
        if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestoreClicked);
        IAPManager.Instance.Initialized -= OnIAPReady;
        IAPManager.Instance.UnregisterFulfillment(IAPManager.ProductRemoveAds);
    }

    private void OnIAPReady()
    {
        IAPManager.Instance.Initialized -= OnIAPReady;

        IAPManager.Instance.RegisterFulfillment(IAPManager.ProductRemoveAds, FulfillRemoveAds);

        if (priceText != null)
        {
            string price = IAPManager.Instance.GetLocalizedPrice(IAPManager.ProductRemoveAds);
            if (!string.IsNullOrEmpty(price)) priceText.text = price;
        }

        ApplyOwnershipUI();
    }

    private void ApplyOwnershipUI()
    {
        bool owned = IAPManager.Instance.IsProductOwned(IAPManager.ProductRemoveAds);

        if (purchaseButton != null) purchaseButton.gameObject.SetActive(!owned);
        if (ownedBadge != null) ownedBadge.SetActive(owned);

        if (purchaseButton != null && IAPManager.Instance.IsInitialized && !owned)
            purchaseButton.interactable = true;
    }

    // ─── Buttons ───

    private void OnBuyClicked() => BuyAsync().Forget();

    private async UniTaskVoid BuyAsync()
    {
        if (_isPurchasing || !IAPManager.Instance.IsInitialized) return;
        _isPurchasing = true;
        SetLoading(true);
        if (purchaseButton != null) purchaseButton.interactable = false;

        try
        {
            var result = await IAPManager.Instance.PurchaseAsync(IAPManager.ProductRemoveAds);
            if (result == PurchaseResult.Success)
            {
                AudioManager.Instance?.PlayUISuccess();
            }
            else
            {
                Debug.LogWarning($"[RemoveAds] Purchase did not complete: {result}");
                AudioManager.Instance?.PlayUIFail();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoveAds] Purchase error: {ex.Message}");
            AudioManager.Instance?.PlayUIFail();
        }
        finally
        {
            SetLoading(false);
            _isPurchasing = false;
            ApplyOwnershipUI();
        }
    }

    private void OnRestoreClicked()
    {
        if (_isRestoring || !IAPManager.Instance.IsInitialized) return;
        _isRestoring = true;
        SetLoading(true);

        IAPManager.Instance.RestorePurchases(success =>
        {
            _isRestoring = false;
            SetLoading(false);
            ApplyOwnershipUI();
            Debug.Log($"[RemoveAds] Restore complete: success={success}");
        });
    }

    // ─── Fulfillment ───

    private UniTask<bool> FulfillRemoveAds(Product _)
    {
        var ads = ServiceLocator.Get<AdManager>();
        if (ads != null) ads.AdsDisabled = true;
        return UniTask.FromResult(true);
    }

    // ─── Helpers ───

    private void SetLoading(bool active)
    {
        if (loadingPanel != null) loadingPanel.SetActive(active);
    }
}
