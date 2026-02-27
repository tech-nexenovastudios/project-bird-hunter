
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using Gameplay.Managers;
using Gameplay.PowerUps;
using Random = UnityEngine.Random;

public class SlotMachineController : MonoBehaviour
{
    [Header("Reel Setup")]
    public RowScroll[] reels;

    [Header("UI Panels")]
    public GameObject slotMachinePanel;
    public TextMeshProUGUI countdownText;

    [Header("References")]
    //public GameManager gameManager;
    private GameProgressManager gameManager;
    //public SlotPowerHolder_SO powers;

    // UPDATED WEIGHTS FROM YOUR IMAGE
    [Header("Rarity Weights (Must total 100)")]
    [Range(0, 100)] public float commonChance = 65f;
    [Range(0, 100)] public float rareChance = 30f;
    [Range(0, 100)] public float legendaryChance = 5f;

    [Header("Animation Settings")]
    public float delayBetweenReels = 0.4f;

    // Cache indices by rarity for fast lookup
    private List<int> commonIndices = new List<int>();
    private List<int> rareIndices = new List<int>();
    private List<int> legendaryIndices = new List<int>();

    private void Start()
    {
        CategorizePowerUps();
    }
    
    private void Awake()
    {
        gameManager = GameProgressManager.Instance;
        GameProgressManager.OnSpinTriggered += OnShowPowerupSpin;
        Debug.Log("Awake");
    }

    private void OnDestroy()
    {
        GameProgressManager.OnSpinTriggered -= OnShowPowerupSpin;
    }

    private void CategorizePowerUps()
    {
        commonIndices.Clear();
        rareIndices.Clear();
        legendaryIndices.Clear();

        var powers = GameProgressManager.Instance.allPowerups;
        
        for (int i = 0; i < powers.Length; i++)
        {
            if (powers[i] == null) continue;

            switch (powers[i].rarity)
            {
                case PowerupRarity.Common:
                    commonIndices.Add(i);
                    break;
                case PowerupRarity.Rare:
                    rareIndices.Add(i);
                    break;
                case PowerupRarity.Epic:
                case PowerupRarity.Legendary:
                    legendaryIndices.Add(i);
                    legendaryIndices.Add(i);
                    break;
            }
        }

        // Debugging to ensure your lists are populated correctly
        Debug.Log($"Categorized: Common({commonIndices.Count}), Rare({rareIndices.Count}), Legendary({legendaryIndices.Count})");
    }

    public void ShowAndSpin()
    {
        slotMachinePanel.SetActive(true);
        if (countdownText) countdownText.gameObject.SetActive(false);
        StartCoroutine(PanelEntranceAnimation());
    }

    private void OnShowPowerupSpin(int slotIndex, PowerupConfig[] options)
    {
        Debug.Log($"Spin triggered! Slot {slotIndex} | Options: {options.Length}");
        slotMachinePanel.SetActive(true);
        if (countdownText) countdownText.gameObject.SetActive(false);
        StartCoroutine(PanelEntranceAnimation());
    }
    private IEnumerator PanelEntranceAnimation()
    {
        slotMachinePanel.transform.localScale = Vector3.zero;
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            slotMachinePanel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, BounceEaseOut(t));
            yield return null;
        }
        slotMachinePanel.transform.localScale = Vector3.one;
        Debug.Log("Spinning!");
        yield return new WaitForSeconds(0.3f);
        SpinAllReels();
    }

    private float BounceEaseOut(float t)
    {
        if (t < (1f / 2.75f)) return 7.5625f * t * t;
        else if (t < (2f / 2.75f)) return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
        else if (t < (2.5f / 2.75f)) return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
        else return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
    }

    public void SpinAllReels()
    {
        // Re-categorize just in case
        CategorizePowerUps();

        List<int> selectedResults = new List<int>();

        // We need 1 unique result per reel
        for (int i = 0; i < reels.Length; i++)
        {
            int index = GetWeightedRandomIndex(selectedResults);
            selectedResults.Add(index);
        }

        Debug.Log($"Spin Results: {string.Join(",", selectedResults)}");
        StartCoroutine(StartReelsWithStagger(selectedResults));
    }

    // CORE LOGIC: Weighted Random Selection
    private int GetWeightedRandomIndex(List<int> excludeIndices)
    {
        // Ensure total weight is calculated correctly
        float totalWeight = commonChance + rareChance + legendaryChance;
        float randomPoint = Random.Range(0, totalWeight);

        List<int> targetList = null;

        // 1. Determine Rarity Tier
        if (randomPoint < commonChance)
        {
            targetList = commonIndices;
        }
        else if (randomPoint < commonChance + rareChance)
        {
            targetList = rareIndices;
        }
        else
        {
            targetList = legendaryIndices;
        }

        // 2. Filter out already selected items to avoid duplicates
        List<int> validOptions = targetList.Where(x => !excludeIndices.Contains(x)).ToList();

        // FALLBACK: If we ran out of items in this rarity
        if (validOptions.Count == 0)
        {
            // Combine all valid indices from all categories
            List<int> allValid = new List<int>();
            allValid.AddRange(commonIndices);
            allValid.AddRange(rareIndices);
            allValid.AddRange(legendaryIndices);

            validOptions = allValid.Where(x => !excludeIndices.Contains(x)).ToList();
        }

        // Safety: If absolutely no items are left
        if (validOptions.Count == 0) return 0;

        // 3. Pick random item from valid filtered list
        return validOptions[Random.Range(0, validOptions.Count)];
    }

    private IEnumerator StartReelsWithStagger(List<int> results)
    {
        for (int r = 0; r < reels.Length; r++)
        {
            if (r < results.Count)
            {
                reels[r].resultIndex = results[r];
                //eels[r].powerUp = powers;
                reels[r].StartSpin();
            }

            if (r < reels.Length - 1)
                yield return new WaitForSeconds(delayBetweenReels);
        }
    }

    public void OnUserSelectsIcon()
    {
        StartCoroutine(PanelExitAnimation());
    }

    private IEnumerator PanelExitAnimation()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = slotMachinePanel.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            slotMachinePanel.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            yield return null;
        }

        slotMachinePanel.SetActive(false);
        slotMachinePanel.transform.localScale = Vector3.one;

        int chapter = PlayerPrefs.GetInt("SelectedChapter", 1);
        int level = PlayerPrefs.GetInt("SelectedLevel", 1);
        StartCoroutine(LoadNextLevelWithCountdown(chapter, level));
    }

    private IEnumerator LoadNextLevelWithCountdown(int chapter, int level)
    {
        countdownText.gameObject.SetActive(true);

        for (int count = 3; count > 0; count--)
        {
            countdownText.text = $"Loading level: {level} in {count} seconds";

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / 0.5f);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }

        countdownText.text = "";
        countdownText.gameObject.SetActive(false);
        gameManager.LoadLevelProfile(chapter, level);
    }
}