using Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterSelector : MonoBehaviour
{
    [Header("World Database")]
    [SerializeField] ChaptersConfig worlds;

    [Header("UI")]
    [SerializeField] TMP_Text chapterText;

    int currentIndex;

    private void OnEnable() => ScrollCarouselEffect.currentLevelChange += ChangeWorld;
    private void OnDisable() => ScrollCarouselEffect.currentLevelChange -= ChangeWorld;

    public void ChangeWorld(int oneBasedIndex)
    {
        currentIndex = oneBasedIndex - 1;
        ChapterData data = worlds.GetWorldData(currentIndex);
        if (data != null) chapterText.text = data.chapterName.ToUpper();
    }

    public void LoadCurrentWorld()
    {
        var unlockService = ServiceLocator.Get<ChapterUnlockService>();
        if (unlockService != null && !unlockService.IsUnlocked(currentIndex)) return;

        GameProgressManager.Instance?.SelectChapter(currentIndex + 1);

        ChapterData data = worlds.GetWorldData(currentIndex);
        if (data != null) ChapterEnvironmentApplier.SetChapterData(data);

        SceneManager.LoadScene("GamePlayScene");
    }
}