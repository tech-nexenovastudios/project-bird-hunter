using System.Globalization;
using Gameplay.Managers;
using UnityEngine;
using UnityEngine.UI;

public class ChapterScrollItem : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private TMPro.TextMeshProUGUI highscore;
    [SerializeField] private TMPro.TextMeshProUGUI bestLevel;

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        int index = transform.GetSiblingIndex();
        int ch = index + 1;

        var unlockService = ServiceLocator.Get<ChapterUnlockService>();
        bool unlocked = unlockService != null && unlockService.IsUnlocked(index);

        if (highscore != null) highscore.gameObject.SetActive(unlocked);
        if (bestLevel != null) bestLevel.gameObject.SetActive(unlocked);
        if (!unlocked) return;

        var progress = GameProgressManager.Instance != null ? GameProgressManager.Instance.Data : null;

        int hs = 0;
        int level = 1;
        if (progress?.chapters != null)
        {
            foreach (var cp in progress.chapters)
            {
                if (cp == null || cp.chapter != ch) continue;
                hs = cp.highScore;
                level = cp.highestLevelReached;
                break;
            }
        }

        if (highscore != null) highscore.text = "highscore: " + hs.ToString("N0", CultureInfo.InvariantCulture);
        if (bestLevel != null) bestLevel.text = "best level: " + level.ToString();
    }
}
