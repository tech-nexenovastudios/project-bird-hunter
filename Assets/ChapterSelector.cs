using Cysharp.Threading.Tasks;
using Gameplay.Managers;
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

    private void Start()
    {
        PersistantData.Instance.UpdateChaptersConfig(worlds);
        ChangeWorld(1);
    }

    public void ChangeWorld(int index)
    {
        currentIndex = index - 1;

        ChapterData data = worlds.GetChapterData(currentIndex);

        worldText.text = $"WORLD: {data.worldName.ToUpper()}";
        chapterText.text = $"CHAPTER {currentIndex + 1}";
        locationText.text = $"LOCATION: {currentIndex + 1}/{worlds.GetWorldCount()}";
    }

    public void LoadCurrentWorld()
    {
        if (ChapterUnlockManager.Instance != null && !ChapterUnlockManager.Instance.IsUnlocked(currentIndex))
        {
            Debug.Log($"[ChapterSelector] Chapter {currentIndex} is locked.");
            return;
        }
        
        PersistantData.Instance.SetChapterIndex(currentIndex);
        
        SceneManager.LoadScene("GamePlayScene");
    }
}