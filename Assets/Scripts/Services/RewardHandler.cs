// ============================================================================
// RewardHandler.cs — Connects Ad Rewards to Unity Economy CurrencyManager
// ============================================================================


using UnityEngine;
using Cysharp.Threading.Tasks;

public class RewardHandler : MonoBehaviour
{
    private void OnEnable()
    {
        RewardedAdOffer.OnRewardClaimed += HandleRewardClaimed;
        RewardedAdOffer.OnAdProgressUpdated += HandleAdProgress;
    }

    private void OnDisable()
    {
        RewardedAdOffer.OnRewardClaimed -= HandleRewardClaimed;
        RewardedAdOffer.OnAdProgressUpdated -= HandleAdProgress;
    }

    // ========================================================================
    // REWARD GRANTING —
    // ========================================================================

    private void HandleRewardClaimed(string rewardType, int amount)
    {
        // Fire-and-forget async call using UniTask
        GrantRewardAsync(rewardType, amount).Forget();
    }

    private async UniTaskVoid GrantRewardAsync(string rewardType, int amount)
    {
        // Make sure CurrencyManager is loaded before granting
        if (!CurrencyManager.Instance.IsLoaded)
        {
            Debug.LogWarning("[RewardHandler] CurrencyManager not loaded yet. Loading now...");
            await CurrencyManager.Instance.LoadBalances();
        }

        switch (rewardType)
        {
            case "Gold":
                await CurrencyManager.Instance.AddGold(amount);
                Debug.Log($"[RewardHandler] Granted {amount} Gold! New balance: {CurrencyManager.Instance.Gold}");
                break;

            case "Gems":
                await CurrencyManager.Instance.AddGems(amount);
                Debug.Log($"[RewardHandler] Granted {amount} Gems! New balance: {CurrencyManager.Instance.Gems}");
                break;

            case "Power":
                await CurrencyManager.Instance.AddPower(amount);
                Debug.Log($"[RewardHandler] Granted {amount} Power! New balance: {CurrencyManager.Instance.Power}");
                break;

            // ============================================================
            // ADD MORE REWARD TYPES HERE AS YOUR GAME GROWS:
            // ============================================================
            //
            // case "Lives":
            //     await SomeManager.Instance.AddLives(amount);
            //     break;
            //
            // case "Skin":
            //     await InventoryManager.Instance.UnlockSkin(amount);
            //     break;
            //
            // case "Booster":
            //     await BoosterManager.Instance.AddBooster(amount);
            //     break;
            //

            default:
                Debug.LogWarning($"[RewardHandler] Unknown reward type: {rewardType} x{amount}");
                break;
        }
    }

    // ========================================================================
    // AD PROGRESS — For UI feedback, sounds, animations
    // ========================================================================

    private void HandleAdProgress(string rewardType, int adsWatched, int adsRequired)
    {
        Debug.Log($"[RewardHandler] {rewardType}: {adsWatched}/{adsRequired} ads watched.");

        // TODO: Play a progress sound
        // Example: AudioManager.Instance.PlaySound("ad_progress");

        if (adsWatched >= adsRequired)
        {
            // TODO: Play a big reward celebration
            // Example: AudioManager.Instance.PlaySound("reward_claimed");
            // Example: ParticleManager.Instance.PlayCoinBurst();
        }
    }
}