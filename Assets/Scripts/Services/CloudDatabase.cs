using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Economy;
using UnityEngine;

public class CloudDatabase
{
    // ─── Public Data (read-only access from outside) ───
    public UserData UserData { get; private set; }

    public InventoryData InventoryData { get; private set; }
    public ChapterUnlockStatusData ChapterUnlockStatusData { get; private set; }

    public PowerupUnlockStatusData PowerupUnlockStatusData { get; private set; }
    public ShopData ShopData { get; private set; }

    public bool IsInitialized { get; private set; }
    public string UserId => AuthenticationService.Instance?.PlayerId;

    // ─── Config ───
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 1500;
    private const int TIMEOUT_MS = 15000;

    // ─── Public API ───

    public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            EventBus.Publish(new DataLoadStartedEvent());

            ReportProgress(0.30f, "Loading user data", cancellationToken);
            UserData = await LoadUserDataAsync(cancellationToken);

            ReportProgress(0.60f, "Loading inventory", cancellationToken);
            InventoryData = await LoadInventoryDataAsync(cancellationToken);

            ReportProgress(0.75f, "Loading chapter progress", cancellationToken);
            ChapterUnlockStatusData = await LoadChapterUnlockStatusAsync(cancellationToken);

            ReportProgress(0.80f, "Loading powerup progress", cancellationToken);
            PowerupUnlockStatusData = await LoadPowerupUnlockStatusAsync(cancellationToken);

            ReportProgress(0.90f, "Loading shop", cancellationToken);
            ShopData = await LoadShopDataAsync(cancellationToken);

            ReportProgress(1.00f, "Ready", cancellationToken);

            IsInitialized = true;
            EventBus.Publish(new DataLoadCompletedEvent());
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[CloudDatabase] Initialization cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CloudDatabase] Initialization failed: {ex.Message}\n{ex.StackTrace}");
            EventBus.Publish(new DataLoadFailedEvent
            {
                errorMessage = ex.Message,
                canRetry = true
            });
            throw;
        }
    }


    private async UniTask<UserData> LoadUserDataAsync(CancellationToken ct)
    {
        return await CloudSaveManager.Instance.LoadValueAsync<UserData>(
            CloudKeys.USER_DATA,
            new UserData()
        );
    }


    private async UniTask<InventoryData> LoadInventoryDataAsync(CancellationToken ct)
    {
        var result = await ExecuteWithRetryAsync(
            () => EconomyService.Instance.PlayerInventory.GetInventoryAsync().AsUniTask(),
            "LoadInventory",
            ct
        );

        var data = new InventoryData();
        foreach (var item in result.PlayersInventoryItems)
        {
            data.items.Add(new InventoryItem
            {
                itemKey = item.InventoryItemId,
                quantity = 1,
                instanceId = item.PlayersInventoryItemId
            });
        }
        return data;
    }

    private async UniTask<ChapterUnlockStatusData> LoadChapterUnlockStatusAsync(CancellationToken ct)
    {
        return await CloudSaveManager.Instance.LoadValueAsync<ChapterUnlockStatusData>(
            CloudKeys.CHAPTER_UNLOCK_STATUS,
            new ChapterUnlockStatusData()
        );
    }
    private async UniTask<PowerupUnlockStatusData> LoadPowerupUnlockStatusAsync(CancellationToken ct)
    {
        return await CloudSaveManager.Instance.LoadValueAsync<PowerupUnlockStatusData>(
            CloudKeys.POWERUP_UNLOCK_STATUS,
            new PowerupUnlockStatusData()
        );
    }

    private async UniTask<ShopData> LoadShopDataAsync(CancellationToken ct)
    {
        await ExecuteWithRetryAsync(
            () => EconomyService.Instance.Configuration.SyncConfigurationAsync().AsUniTask(),
            "SyncEconomyConfig",
            ct
        );

        var purchases = EconomyService.Instance.Configuration.GetVirtualPurchases();

        var data = new ShopData();
        foreach (var purchase in purchases)
        {
            var shopItem = new ShopItem { purchaseId = purchase.Id };

            foreach (var cost in purchase.Costs)
            {
                shopItem.costs.Add(new ShopItemCost
                {
                    currencyKey = cost.Item.GetReferencedConfigurationItem().Id,
                    amount = cost.Amount
                });
            }

            foreach (var reward in purchase.Rewards)
            {
                shopItem.rewardItemKeys.Add(reward.Item.GetReferencedConfigurationItem().Id);
            }

            data.items.Add(shopItem);
        }
        return data;
    }

    // ─── Private: Helpers ───

    private void ReportProgress(float progress, string step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        EventBus.Publish(new DataLoadProgressEvent { progress = progress, currentStep = step });
    }

    private async UniTask<T> ExecuteWithRetryAsync<T>(Func<UniTask<T>> operation, string operationName, CancellationToken ct)
    {
        Exception lastException = null;

        for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TIMEOUT_MS);
                return await operation().AttachExternalCancellation(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Debug.LogWarning($"[CloudDatabase] {operationName} failed (attempt {attempt}/{MAX_RETRY_ATTEMPTS}): {ex.Message}");
                if (attempt < MAX_RETRY_ATTEMPTS)
                    await UniTask.Delay(RETRY_DELAY_MS, cancellationToken: ct);
            }
        }

        throw new Exception($"{operationName} failed after {MAX_RETRY_ATTEMPTS} attempts.", lastException);
    }

    private async UniTask ExecuteWithRetryAsync(Func<UniTask> operation, string operationName, CancellationToken ct)
    {
        await ExecuteWithRetryAsync<bool>(async () =>
        {
            await operation();
            return true;
        }, operationName, ct);
    }
}