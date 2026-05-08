using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

public enum PurchaseResult
{
    Success,
    Cancelled,
    Failed,
    NotInitialized,
    AlreadyProcessing,
    InvalidReceipt,
}

/// <summary>
/// Singleton owner of Unity IAP. Initializes once, validates receipts, and
/// routes ProcessPurchase to feature handlers (gem packs, remove ads, etc.).
/// </summary>
public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    // ─── Product Catalog ───
    // Same SKU strings must exist in both App Store Connect and Google Play.
    private static readonly (string id, ProductType type)[] Catalog =
    {
        ("100_gems",  ProductType.Consumable),
        ("320_gems",  ProductType.Consumable),
        ("600_gems",  ProductType.Consumable),
        ("1300_gems", ProductType.Consumable),
        ("2800_gems", ProductType.Consumable),
        ("7500_gems", ProductType.Consumable),
        ("remove_ads", ProductType.NonConsumable),
    };

    public const string ProductRemoveAds = "remove_ads";

    // ─── Singleton ───
    private static IAPManager _instance;
    public static IAPManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindAnyObjectByType<IAPManager>();
            if (_instance != null) return _instance;
            var go = new GameObject(nameof(IAPManager));
            _instance = go.AddComponent<IAPManager>();
            return _instance;
        }
    }

    // ─── State ───
    public bool IsInitialized { get; private set; }
    public event Action Initialized;

    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;
    private CrossPlatformValidator _validator;

    private readonly Dictionary<string, Func<Product, UniTask<bool>>> _fulfillment = new();
    private readonly Dictionary<string, UniTaskCompletionSource<PurchaseResult>> _pendingPurchases = new();

    private const string PrefOwnedPrefix = "iap.owned.";

    // ─── Lifecycle ───

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        ServiceLocator.Register<IAPManager>(this);
        Initialize();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        ServiceLocator.Unregister<IAPManager>();
        _instance = null;
    }

    // ─── Public API ───

    /// <summary>
    /// Register a fulfillment handler for a product ID. Handler must return
    /// true once the entitlement is delivered (gems granted, ads disabled).
    /// Returning false leaves the receipt as Pending so it retries next launch.
    /// </summary>
    public void RegisterFulfillment(string productId, Func<Product, UniTask<bool>> handler)
    {
        _fulfillment[productId] = handler;
    }

    public void UnregisterFulfillment(string productId)
    {
        _fulfillment.Remove(productId);
    }

    public async UniTask<PurchaseResult> PurchaseAsync(string productId)
    {
        if (!IsInitialized) return PurchaseResult.NotInitialized;
        if (_pendingPurchases.ContainsKey(productId)) return PurchaseResult.AlreadyProcessing;

        var product = _storeController.products.WithID(productId);
        if (product == null || !product.availableToPurchase)
        {
            Debug.LogWarning($"[IAPManager] Product unavailable: {productId}");
            return PurchaseResult.Failed;
        }

        var tcs = new UniTaskCompletionSource<PurchaseResult>();
        _pendingPurchases[productId] = tcs;
        _storeController.InitiatePurchase(product);

        return await tcs.Task;
    }

    public string GetLocalizedPrice(string productId)
    {
        if (!IsInitialized) return string.Empty;
        var product = _storeController.products.WithID(productId);
        return product?.metadata?.localizedPriceString ?? string.Empty;
    }

    public string GetIsoCurrencyCode(string productId)
    {
        if (!IsInitialized) return string.Empty;
        var product = _storeController.products.WithID(productId);
        return product?.metadata?.isoCurrencyCode ?? string.Empty;
    }

    /// <summary>
    /// True if a non-consumable has been purchased. Persisted across launches.
    /// </summary>
    public bool IsProductOwned(string productId)
    {
        return PlayerPrefs.GetInt(PrefOwnedPrefix + productId, 0) == 1;
    }

    /// <summary>
    /// Required for App Store review when shipping non-consumables.
    /// On Apple, triggers the StoreKit restore flow. On Google, purchases
    /// are restored automatically at init — this is a no-op there.
    /// </summary>
    public void RestorePurchases(Action<bool> onComplete = null)
    {
        if (!IsInitialized)
        {
            onComplete?.Invoke(false);
            return;
        }

#if UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || UNITY_STANDALONE_OSX
        var apple = _extensionProvider?.GetExtension<IAppleExtensions>();
        if (apple == null)
        {
            onComplete?.Invoke(false);
            return;
        }
        apple.RestoreTransactions((success, error) =>
        {
            if (!success) Debug.LogWarning($"[IAPManager] Restore failed: {error}");
            onComplete?.Invoke(success);
        });
#else
        onComplete?.Invoke(true);
#endif
    }

    // ─── Initialization ───

    private void Initialize()
    {
        if (IsInitialized) return;

        var module = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);
        foreach (var (id, type) in Catalog)
            builder.AddProduct(id, type);

        BuildValidator();

        UnityPurchasing.Initialize(this, builder);
    }

    private void BuildValidator()
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
        try
        {
            byte[] google = null;
            byte[] apple = null;

            try { google = GooglePlayTangle.Data(); } catch { /* tangle absent */ }
            try { apple = AppleTangle.Data(); } catch { /* tangle absent */ }

            bool hasGoogle = google != null && google.Length > 0;
            bool hasApple = apple != null && apple.Length > 0;

            if (!hasGoogle && !hasApple)
            {
                Debug.LogWarning("[IAPManager] No tangles found. Receipt validation disabled. " +
                                 "Generate tangles via Window → Unity IAP → Receipt Validation Obfuscator.");
                _validator = null;
                return;
            }

            _validator = new CrossPlatformValidator(
                hasGoogle ? google : new byte[0],
                hasApple ? apple : new byte[0],
                Application.identifier);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[IAPManager] Could not build receipt validator: {ex.Message}");
            _validator = null;
        }
#endif
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensionProvider = extensions;
        IsInitialized = true;

#if UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS || UNITY_STANDALONE_OSX
        var apple = extensions.GetExtension<IAppleExtensions>();
        apple?.RegisterPurchaseDeferredListener(p =>
            Debug.Log($"[IAPManager] Apple deferred (Ask to Buy): {p.definition.id}"));
#endif

        // Reconcile non-consumable ownership from receipts (Google restores at init,
        // Apple requires explicit RestorePurchases but receipts persist if previously processed).
        ReconcileNonConsumables();

        Debug.Log("[IAPManager] Initialized.");
        Initialized?.Invoke();
    }

    private void ReconcileNonConsumables()
    {
        foreach (var (id, type) in Catalog)
        {
            if (type != ProductType.NonConsumable) continue;
            var product = _storeController.products.WithID(id);
            if (product != null && product.hasReceipt)
                PlayerPrefs.SetInt(PrefOwnedPrefix + id, 1);
        }
        PlayerPrefs.Save();
    }

    public void OnInitializeFailed(InitializationFailureReason error) =>
        OnInitializeFailed(error, null);

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        IsInitialized = false;
        Debug.LogError($"[IAPManager] Init failed: {error} {message}");
    }

    // ─── Purchase Processing ───

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        var product = args.purchasedProduct;
        var productId = product.definition.id;

        if (!ValidateReceipt(product))
        {
            ResolvePending(productId, PurchaseResult.InvalidReceipt);
            Debug.LogError($"[IAPManager] Receipt validation FAILED: {productId}");
            return PurchaseProcessingResult.Complete; // Drop tampered receipts.
        }

        if (!_fulfillment.TryGetValue(productId, out var handler))
        {
            Debug.LogWarning($"[IAPManager] No fulfillment registered for {productId}; leaving Pending.");
            return PurchaseProcessingResult.Pending;
        }

        FulfillAsync(productId, product, handler).Forget();
        return PurchaseProcessingResult.Pending;
    }

    private async UniTaskVoid FulfillAsync(string productId, Product product, Func<Product, UniTask<bool>> handler)
    {
        bool fulfilled = false;
        try
        {
            fulfilled = await handler(product);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAPManager] Fulfillment threw for {productId}: {ex.Message}");
        }

        if (fulfilled)
        {
            if (product.definition.type == ProductType.NonConsumable)
            {
                PlayerPrefs.SetInt(PrefOwnedPrefix + productId, 1);
                PlayerPrefs.Save();
            }
            _storeController.ConfirmPendingPurchase(product);
            ResolvePending(productId, PurchaseResult.Success);
        }
        else
        {
            // Leave receipt pending for retry on next launch.
            ResolvePending(productId, PurchaseResult.Failed);
        }
    }

    private bool ValidateReceipt(Product product)
    {
        if (_validator == null) return true; // Validator unavailable — trust the store.
#if UNITY_ANDROID || UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
        try
        {
            _validator.Validate(product.receipt);
            return true;
        }
        catch (IAPSecurityException)
        {
            return false;
        }
#else
        return true;
#endif
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) =>
        HandlePurchaseFailure(product?.definition?.id, reason.ToString());

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) =>
        HandlePurchaseFailure(product?.definition?.id, $"{description?.reason}: {description?.message}");

    private void HandlePurchaseFailure(string productId, string detail)
    {
        if (string.IsNullOrEmpty(productId)) return;
        Debug.LogWarning($"[IAPManager] Purchase failed for {productId}: {detail}");

        var result = detail != null && detail.Contains("UserCancelled", StringComparison.OrdinalIgnoreCase)
            ? PurchaseResult.Cancelled
            : PurchaseResult.Failed;
        ResolvePending(productId, result);
    }

    private void ResolvePending(string productId, PurchaseResult result)
    {
        if (_pendingPurchases.TryGetValue(productId, out var tcs))
        {
            _pendingPurchases.Remove(productId);
            tcs.TrySetResult(result);
        }
    }
}
