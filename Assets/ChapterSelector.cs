using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChapterSelector : MonoBehaviour
{
    [Header("World Database")]
    [SerializeField] ChaptersConfig worlds;

    [Header("UI")]
    [SerializeField] TMP_Text locationText;
    [SerializeField] TMP_Text worldText;
    [SerializeField] TMP_Text chapterText;

    int currentIndex;

    private void OnEnable() => ScrollCarouselEffect.currentLevelChange += ChangeWorld;
    private void OnDisable() => ScrollCarouselEffect.currentLevelChange -= ChangeWorld;

    private void Start() => ChangeWorld(1);

    public void ChangeWorld(int index)
    {
        currentIndex = index - 1;

        ChapterData data = worlds.GetWorldData(currentIndex);

        worldText.text = $"WORLD: {data.worldName.ToUpper()}";
        chapterText.text = $"CHAPTER {currentIndex + 1}";
        locationText.text = $"LOCATION: {currentIndex + 1}/{worlds.GetWorldCount()}";
    }

    public void LoadCurrentWorld()
    {
        var unlockService = ServiceLocator.Get<ChapterUnlockService>();
        if (unlockService != null && !unlockService.IsUnlocked(currentIndex))

            PlayerPrefs.SetInt("SelectedWorld", currentIndex);
        PlayerPrefs.SetInt("SelectedLevel", 1);

        ChapterData data = worlds.GetWorldData(currentIndex);
        ChapterEnvironmentApplier.SetChapterData(data);

        SceneManager.LoadScene("GamePlayScene");
    }
}