using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Cysharp.Threading.Tasks;
using TMPro;

public class GemsBuyManager : MonoBehaviour, IDetailedStoreListener
{
    [Header("IAP Products")]
    [SerializeField] private GemProduct[] gemProducts;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Transform loadingIcon;
    [SerializeField] private float rotateSpeed = 300f;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private bool _isInitialized = false;
    private bool _isLoading = false;
    private bool _isPurchasing = false;

    private Action<bool> _purchaseCallback;

    private void Awake()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        PopulateProductIds();
    }

    private void Start()
    {
        InitializeIAP();
        SetupButtons();
    }

    private void Update()
    {
        if (_isLoading && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
    }

    // ==================== Auto-Populate IDs ====================

    private void PopulateProductIds()
    {
        long[] gemCounts = { 110000, 40000, 16000, 7000, 500 };

        if (gemProducts == null || gemProducts.Length != gemCounts.Length)
        {
            Debug.LogError($"[GemsBuy] gemProducts array must have {gemCounts.Length} entries.");
            return;
        }

        for (int i = 0; i < gemProducts.Length; i++)
        {
            gemProducts[i].gemAmount = gemCounts[i];
            gemProducts[i].googlePlayProductId = $"{gemCounts[i]}_gems";
        }
    }

    // ==================== IAP Initialization ====================

    private void InitializeIAP()
    {
        ShowLoading();

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        foreach (var product in gemProducts)
        {
            builder.AddProduct(product.googlePlayProductId, ProductType.Consumable);
        }

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensionProvider = extensions;
        _isInitialized = true;

        // Auto-fill localized prices
        foreach (var product in gemProducts)
        {
            var storeProduct = _storeController.products.WithID(product.googlePlayProductId);
            if (storeProduct != null && product.priceText != null)
                product.priceText.text = storeProduct.metadata.localizedPriceString;
        }

        // Handle any pending purchases from previous crashes
        RestorePendingPurchases();

        SetButtonsInteractable(true);
        HideLoading();
        Debug.Log("[GemsBuy] IAP initialized.");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        _isInitialized = false;
        HideLoading();
        Debug.LogError($"[GemsBuy] IAP init failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        _isInitialized = false;
        HideLoading();
        Debug.LogError($"[GemsBuy] IAP init failed: {error} - {message}");
    }

    // ==================== Restore Pending Purchases ====================

    /// <summary>
    /// On init, check if any consumable purchases were paid but not consumed.
    /// Grant gems and consume them.
    /// </summary>
    private void RestorePendingPurchases()
    {
        foreach (var gem in gemProducts)
        {
            var product = _storeController.products.WithID(gem.googlePlayProductId);
            if (product != null && product.hasReceipt && !product.availableToPurchase)
            {
                Debug.Log($"[GemsBuy] Found pending purchase: {gem.googlePlayProductId}. Granting {gem.gemAmount} gems.");
                GrantGemsAndConfirm(product, gem.gemAmount).Forget();
            }
        }
    }

    // ==================== Button Setup ====================

    private void SetupButtons()
    {
        for (int i = 0; i < gemProducts.Length; i++)
        {
            int index = i;
            if (gemProducts[i].purchaseButton != null)
                gemProducts[i].purchaseButton.onClick.AddListener(() => OnBuyClicked(index));
        }

        SetButtonsInteractable(false);
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var product in gemProducts)
        {
            if (product.purchaseButton != null)
                product.purchaseButton.interactable = state;
        }
    }

    // ==================== Purchase Flow ====================

    private void OnBuyClicked(int index)
    {
        ProcessPurchaseFlow(index).Forget();
    }

    private async UniTaskVoid ProcessPurchaseFlow(int index)
    {
        if (!_isInitialized || _isPurchasing) return;

        var gem = gemProducts[index];
        _isPurchasing = true;
        gem.purchaseButton.interactable = false;
        ShowLoading();

        try
        {
            bool success = await WaitForPurchaseAsync(gem.googlePlayProductId);

            if (success)
            {
                Debug.Log($"[GemsBuy] Purchase '{gem.googlePlayProductId}' completed. {gem.gemAmount} gems granted.");
            }
            else
            {
                Debug.LogWarning($"[GemsBuy] Purchase cancelled or failed for '{gem.googlePlayProductId}'.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GemsBuy] Error: {ex.Message}");
        }
        finally
        {
            HideLoading();
            gem.purchaseButton.interactable = true;
            _isPurchasing = false;
        }
    }

    private UniTask<bool> WaitForPurchaseAsync(string productId)
    {
        var tcs = new UniTaskCompletionSource<bool>();
        _purchaseCallback = (success) => tcs.TrySetResult(success);
        _storeController.InitiatePurchase(productId);
        return tcs.Task;
    }

    // ==================== IAP Callbacks ====================

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[GemsBuy] Processing purchase: {productId}");

        // Find matching gem product
        GemProduct matchedGem = null;
        foreach (var gem in gemProducts)
        {
            if (gem.googlePlayProductId == productId)
            {
                matchedGem = gem;
                break;
            }
        }

        if (matchedGem != null)
        {
            // Grant gems and then notify callback
            GrantGemsAndNotify(args.purchasedProduct, matchedGem.gemAmount).Forget();
        }
        else
        {
            Debug.LogWarning($"[GemsBuy] Unknown product purchased: {productId}");
            _purchaseCallback?.Invoke(false);
            _purchaseCallback = null;
        }

        // Return Complete to immediately consume the purchase
        // This prevents "already owned" error
        return PurchaseProcessingResult.Complete;
    }

    private async UniTaskVoid GrantGemsAndNotify(Product product, long gemAmount)
    {
        bool granted = await GrantGems(gemAmount);
        _purchaseCallback?.Invoke(granted);
        _purchaseCallback = null;
    }

    private async UniTaskVoid GrantGemsAndConfirm(Product product, long gemAmount)
    {
        bool granted = await GrantGems(gemAmount);

        if (granted)
        {
            _storeController.ConfirmPendingPurchase(product);
            Debug.Log($"[GemsBuy] Pending purchase confirmed: {product.definition.id}");
        }
    }

    /// <summary>
    /// Grants gems via CurrencyManager. Returns true on success.
    /// </summary>
    private async UniTask<bool> GrantGems(long gemAmount)
    {
        try
        {
            await CurrencyManager.Instance.AddGems(gemAmount);
            await CurrencyManager.Instance.Refresh();
            Debug.Log($"[GemsBuy] Granted {gemAmount} gems.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GemsBuy] Failed to grant {gemAmount} gems: {ex.Message}");
            return false;
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        Debug.LogWarning($"[GemsBuy] Purchase failed: {product.definition.id} — {reason}");
        _purchaseCallback?.Invoke(false);
        _purchaseCallback = null;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogWarning($"[GemsBuy] Purchase failed: {product.definition.id} — {failureDescription.reason}: {failureDescription.message}");
        _purchaseCallback?.Invoke(false);
        _purchaseCallback = null;
    }

    // ==================== Loading UI ====================

    private void ShowLoading()
    {
        _isLoading = true;
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    private void HideLoading()
    {
        _isLoading = false;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    // ==================== Public API ====================

    public string GetLocalizedPrice(string productId)
    {
        if (!_isInitialized) return "";
        var product = _storeController.products.WithID(productId);
        return product?.metadata.localizedPriceString ?? "";
    }

    // ==================== Cleanup ====================

    private void OnDestroy()
    {
        foreach (var product in gemProducts)
        {
            if (product.purchaseButton != null)
                product.purchaseButton.onClick.RemoveAllListeners();
        }
    }
}

[System.Serializable]
public class GemProduct
{
    [HideInInspector] public string googlePlayProductId;
    [HideInInspector] public long gemAmount;

    [Header("Assign in Inspector")]
    public Button purchaseButton;
    public TextMeshProUGUI priceText;
}