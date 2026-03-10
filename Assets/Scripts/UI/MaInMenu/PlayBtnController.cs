using UnityEngine;

public class PlayBtnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ChapterSelector chapterSelector;

    private bool _pendingLoad = false;

    public void OnPlayButtonPressed()
    {
        if (AdManager.Instance == null || !AdManager.Instance.IsInitialized || AdManager.Instance.AdsDisabled)
        {
            // No ad manager or ads disabled — just load directly
            chapterSelector.LoadCurrentWorld();
            return;
        }

        if (AdManager.Instance.IsInterstitialReady)
        {
            _pendingLoad = true;
            AdManager.Instance.OnInterstitialDismissed += HandleInterstitialDismissed;
            AdManager.Instance.OnInterstitialUnavailable += HandleInterstitialUnavailable;
            AdManager.Instance.ShowInterstitial();
        }
        else
        {
            // Ad not ready — don't block the player, load directly
            chapterSelector.LoadCurrentWorld();
        }
    }

    private void HandleInterstitialDismissed()
    {
        if (!_pendingLoad) return;
        Cleanup();
        chapterSelector.LoadCurrentWorld();
    }

    private void HandleInterstitialUnavailable()
    {
        if (!_pendingLoad) return;
        Cleanup();
        chapterSelector.LoadCurrentWorld();
    }

    private void Cleanup()
    {
        _pendingLoad = false;
        AdManager.Instance.OnInterstitialDismissed -= HandleInterstitialDismissed;
        AdManager.Instance.OnInterstitialUnavailable -= HandleInterstitialUnavailable;
    }

    private void OnDestroy()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.OnInterstitialDismissed -= HandleInterstitialDismissed;
        AdManager.Instance.OnInterstitialUnavailable -= HandleInterstitialUnavailable;
    }
}