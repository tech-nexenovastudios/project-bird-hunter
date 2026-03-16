using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UI.Scroll;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChapterSelector : MonoBehaviour
{
    [Header("World Database")]
    [SerializeField] ChaptersConfig worlds;

    [Header("UI")]
    [SerializeField] Image worldBGImage;
    [SerializeField] TMP_Text locationText;
    [SerializeField] TMP_Text worldText;
    [SerializeField] TMP_Text chapterText;

    int currentIndex;

    private void OnEnable() => ScrollCarouselEffect.currentLevelChange += ChangeWorld;
    private void OnDisable() => ScrollCarouselEffect.currentLevelChange -= ChangeWorld;


    private void Awake()
    {
        
    }

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
        // Guard: block navigation if chapter is locked
        if (ChapterUnlockManager.Instance != null && !ChapterUnlockManager.Instance.IsUnlocked(currentIndex))
        {
            Debug.Log($"[ChapterSelector] Chapter {currentIndex} is locked.");
            return;
        }
        PlayerPrefs.SetInt("SelectedWorld", currentIndex);
        PlayerPrefs.SetInt("SelectedLevel", 1);
        SceneManager.LoadScene("GamePlay");
    }
}