using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelProgressTracker : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image levelFillImage; // Using Image with Fill type instead of Slider

    private LevelData currentLevelData; // Will be set dynamically by GameManager

    // Egg type counts based on the formula
    private int totalE1Eggs;
    private int totalE2Eggs;
    private int totalE3Eggs;
    private int totalE4Eggs;
    private int totalEggs;

    // Current destroyed egg counts
    private int destroyedE1Eggs;
    private int destroyedE2Eggs;
    private int destroyedE3Eggs;
    private int destroyedE4Eggs;

    public static LevelProgressTracker Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Don't calculate here since level data will be set by GameManager
        UpdateLevelFill();
    }

    private void OnEnable()
    {
        // Subscribe to egg destruction events
        EggHealth.OnEggDestroyed += OnEggDestroyed;
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        EggHealth.OnEggDestroyed -= OnEggDestroyed;
    }

    /// <summary>
    /// Called when a bird is destroyed before spawning its egg
    /// </summary>
    public void OnUnspawnedEggDestroyed(EggType eggType)
    {
        // Use the same logic as OnEggDestroyed
        OnEggDestroyed(eggType);
    }

    /// <summary>
    /// Calculate total eggs that will be spawned based on the formula:
    /// (n1+2*n2+4*n3+8*n4)*E1 + (n2+2*n3+4*n4)*E2 + (n3+2*n4)*E3 + n4*E4
    /// This method reads the egg type directly from the Bird's Egg field
    /// </summary>
    private void CalculateTotalEggs()
    {
        int n1 = 0, n2 = 0, n3 = 0, n4 = 0;

        // Count birds by their egg type
        foreach (BirdSpawnEntry entry in currentLevelData.birdsToSpawn)
        {
            // Get the Bird component from the prefab
            var birdScript = entry.birdPrefab.GetComponent<BirdEggSpawner>();

            if (birdScript != null)
            {
                // Access the public Egg field and get its EggHealth component
                if (birdScript.Egg != null)
                {
                    EggHealth eggHealth = birdScript.Egg.GetComponent<EggHealth>();
                    if (eggHealth != null)
                    {
                        switch (eggHealth.eggType)
                        {
                            case EggType.E1:
                                n1 += entry.count;
                                break;
                            case EggType.E2:
                                n2 += entry.count;
                                break;
                            case EggType.E3:
                                n3 += entry.count;
                                break;
                            case EggType.E4:
                                n4 += entry.count;
                                break;
                            case EggType.GoldenEgg:
                                // Handle golden egg if needed
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Bird prefab {entry.birdPrefab.name} has Egg assigned but no EggHealth component found!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Bird prefab {entry.birdPrefab.name} has no Egg assigned in the inspector!");
                }
            }
            else
            {
                Debug.LogWarning($"Bird prefab {entry.birdPrefab.name} missing Bird component!");
            }
        }

        // Apply the formula
        totalE1Eggs = (n1 + 2 * n2 + 4 * n3 + 8 * n4);
        totalE2Eggs = (n2 + 2 * n3 + 4 * n4);
        totalE3Eggs = (n3 + 2 * n4);
        totalE4Eggs = n4;

        totalEggs = totalE1Eggs + totalE2Eggs + totalE3Eggs + totalE4Eggs;

        Debug.Log($"Bird Counts: n1(B1)={n1}, n2(B2)={n2}, n3(B3)={n3}, n4(B4)={n4}");
        Debug.Log($"Total Eggs Calculated: E1={totalE1Eggs}, E2={totalE2Eggs}, E3={totalE3Eggs}, E4={totalE4Eggs}, Total={totalEggs}");
    }

    /// <summary>
    /// Called when an egg is destroyed
    /// </summary>
    private void OnEggDestroyed(EggType eggType)
    {
        switch (eggType)
        {
            case EggType.E1:
                destroyedE1Eggs++;
                break;
            case EggType.E2:
                destroyedE2Eggs++;
                break;
            case EggType.E3:
                destroyedE3Eggs++;
                break;
            case EggType.E4:
                destroyedE4Eggs++;
                break;
        }

        UpdateLevelFill();
        CheckLevelCompletion();
    }

    /// <summary>
    /// Update the level fill UI based on destroyed eggs
    /// </summary>
    private void UpdateLevelFill()
    {
        if (levelFillImage == null) return;

        int totalDestroyedEggs = destroyedE1Eggs + destroyedE2Eggs + destroyedE3Eggs + destroyedE4Eggs;
        float fillAmount = totalEggs > 0 ? (float)totalDestroyedEggs / totalEggs : 0f;

        levelFillImage.fillAmount = fillAmount;

        Debug.Log($"Level Progress: {totalDestroyedEggs}/{totalEggs} ({fillAmount * 100:F1}%)");
    }

    /// <summary>
    /// Check if level is completed
    /// </summary>
    private void CheckLevelCompletion()
    {
        int totalDestroyedEggs = destroyedE1Eggs + destroyedE2Eggs + destroyedE3Eggs + destroyedE4Eggs;

        if (totalDestroyedEggs >= totalEggs)
        {
            OnLevelCompleted();
        }
    }

    /// <summary>
    /// Called when level is completed
    /// </summary>
    private void OnLevelCompleted()
    {
        Debug.Log("Level Completed!");
        // Add your level completion logic here
        // e.g., show completion UI, load next level, etc.
    }

    /// <summary>
    /// Public method to set level data (called by GameManager when loading levels)
    /// </summary>
    public void SetLevelData(LevelData levelData)
    {
        currentLevelData = levelData;
        ResetProgress();
        CalculateTotalEggs();
        UpdateLevelFill();
    }

    /// <summary>
    /// Reset progress counters
    /// </summary>
    public void ResetProgress()
    {
        destroyedE1Eggs = 0;
        destroyedE2Eggs = 0;
        destroyedE3Eggs = 0;
        destroyedE4Eggs = 0;

        totalE1Eggs = 0;
        totalE2Eggs = 0;
        totalE3Eggs = 0;
        totalE4Eggs = 0;
        totalEggs = 0;
    }

    /// <summary>
    /// Get current progress as percentage
    /// </summary>
    public float GetProgressPercentage()
    {
        int totalDestroyedEggs = destroyedE1Eggs + destroyedE2Eggs + destroyedE3Eggs + destroyedE4Eggs;
        return totalEggs > 0 ? (float)totalDestroyedEggs / totalEggs : 0f;
    }

    /// <summary>
    /// Get detailed progress info
    /// </summary>
    public string GetProgressInfo()
    {
        int totalDestroyedEggs = destroyedE1Eggs + destroyedE2Eggs + destroyedE3Eggs + destroyedE4Eggs;
        return $"Progress: {totalDestroyedEggs}/{totalEggs} eggs destroyed\n" +
               $"E1: {destroyedE1Eggs}/{totalE1Eggs}\n" +
               $"E2: {destroyedE2Eggs}/{totalE2Eggs}\n" +
               $"E3: {destroyedE3Eggs}/{totalE3Eggs}\n" +
               $"E4: {destroyedE4Eggs}/{totalE4Eggs}";
    }
}