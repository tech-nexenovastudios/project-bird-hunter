using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class GemsBuyManager : MonoBehaviour
{
    [Header("IAP Products")]
    [SerializeField] private GemProduct[] gemProducts;

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Transform loadingIcon;
    [SerializeField] private float rotateSpeed = 300f;

    private bool _isLoading;
    private bool _isPurchasing;

    // Fixed gem amounts mapped 1:1 to indices in the gemProducts array.
    private static readonly long[] GemCounts = { 7500, 2800, 1300, 600, 320, 100 };

    private void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        PopulateProductIds();
    }

    private void Start()
    {
        SetupButtons();
        SetButtonsInteractable(false);

        if (IAPManager.Instance.IsInitialized)
            OnIAPReady();
        else
            IAPManager.Instance.Initialized += OnIAPReady;
    }

    private void OnDestroy()
    {
        foreach (var product in gemProducts)
        {
            if (product?.purchaseButton != null)
                product.purchaseButton.onClick.RemoveAllListeners();
            if (product != null)
                IAPManager.Instance.UnregisterFulfillment(product.productId);
        }
        IAPManager.Instance.Initialized -= OnIAPReady;
    }

    private void Update()
    {
        if (_isLoading && loadingIcon != null)
            loadingIcon.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
    }

    // ─── Setup ───

    private void PopulateProductIds()
    {
        if (gemProducts == null || gemProducts.Length != GemCounts.Length)
        {
            Debug.LogError($"[GemsBuy] gemProducts must have {GemCounts.Length} entries.");
            return;
        }

        for (int i = 0; i < gemProducts.Length; i++)
        {
            gemProducts[i].gemAmount = GemCounts[i];
            gemProducts[i].productId = $"{GemCounts[i]}_gems";
        }
    }

    private void SetupButtons()
    {
        for (int i = 0; i < gemProducts.Length; i++)
        {
            int index = i;
            if (gemProducts[i].purchaseButton != null)
                gemProducts[i].purchaseButton.onClick.AddListener(() => OnBuyClicked(index));
        }
    }

    private void OnIAPReady()
    {
        IAPManager.Instance.Initialized -= OnIAPReady;

        foreach (var gem in gemProducts)
        {
            IAPManager.Instance.RegisterFulfillment(gem.productId, p => GrantGemsForProduct(gem, p));

            if (gem.priceText != null)
            {
                string price = IAPManager.Instance.GetLocalizedPrice(gem.productId);
                if (!string.IsNullOrEmpty(price)) gem.priceText.text = price;
            }
        }

        SetButtonsInteractable(true);
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (var product in gemProducts)
        {
            if (product?.purchaseButton != null)
                product.purchaseButton.interactable = state;
        }
    }

    // ─── Purchase Flow ───

    private void OnBuyClicked(int index) => ProcessPurchaseFlow(index).Forget();

    private async UniTaskVoid ProcessPurchaseFlow(int index)
    {
        if (_isPurchasing || !IAPManager.Instance.IsInitialized) return;

        var gem = gemProducts[index];
        _isPurchasing = true;
        gem.purchaseButton.interactable = false;
        ShowLoading();

        // Capture button screen position BEFORE async gap so the gem flow
        // effect originates from the actual tapped button.
        gem.lastButtonScreenPos = ResolveButtonScreenPosition(gem.purchaseButton);

        try
        {
            var result = await IAPManager.Instance.PurchaseAsync(gem.productId);
            if (result == PurchaseResult.Success)
            {
                Debug.Log($"[GemsBuy] '{gem.productId}' purchased — {gem.gemAmount} gems granted.");
                AudioManager.Instance?.PlayUISuccess();
            }
            else
            {
                Debug.LogWarning($"[GemsBuy] '{gem.productId}' did not complete: {result}");
                AudioManager.Instance?.PlayUIFail();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GemsBuy] Purchase error: {ex.Message}");
            AudioManager.Instance?.PlayUIFail();
        }
        finally
        {
            HideLoading();
            gem.purchaseButton.interactable = true;
            _isPurchasing = false;
        }
    }

    private static Vector2 ResolveButtonScreenPosition(Button button)
    {
        var canvas = button.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.WorldToScreenPoint(cam, button.transform.position);
    }

    // ─── Fulfillment (called by IAPManager) ───

    private async UniTask<bool> GrantGemsForProduct(GemProduct gem, Product _)
    {
        Vector2 origin = gem.lastButtonScreenPos == default
            ? new Vector2(Screen.width / 2f, Screen.height / 2f)
            : gem.lastButtonScreenPos;

        try
        {
            await CurrencyManager.Instance.AddGems(gem.gemAmount);
            await CurrencyManager.Instance.Refresh();
            GameEvent.CurrencyCollected(CurrencyType.Gems, origin, (int)gem.gemAmount);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GemsBuy] Failed to grant {gem.gemAmount} gems: {ex.Message}");
            return false;
        }
    }

    // ─── UI Helpers ───

    private void ShowLoading()
    {
        _isLoading = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    private void HideLoading()
    {
        _isLoading = false;
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public string GetLocalizedPrice(string productId) =>
        IAPManager.Instance.GetLocalizedPrice(productId);
}

[System.Serializable]
public class GemProduct
{
    [HideInInspector] public string productId;
    [HideInInspector] public long gemAmount;
    [HideInInspector] public Vector2 lastButtonScreenPos;

    [Header("Assign in Inspector")]
    public Button purchaseButton;
    public TextMeshProUGUI priceText;
}
