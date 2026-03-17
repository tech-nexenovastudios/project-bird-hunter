//using System;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using TMPro;
//using System.Threading.Tasks;

//public class GameManager : MonoBehaviour
//{
//    public SpriteRenderer backgroundRenderer;
//    public BirdSpawner birdSpawner;
//    private LevelData currentLevel;
//    public SlotMachineController slotMachineController;
//    public GameObject SlotMachine;
//    public CannonFire cannon;
//    public static Action LevelEnd;

//    [SerializeField] TMP_Text levelNumberText;

//    [Header("Countdown UI")]
//    public TextMeshProUGUI countdownText;


//    [SerializeField] CannonSpawner cannonSpawner;
   
//    public void Start()
//    {
//        int chapterNumber = PlayerPrefs.GetInt("SelectedChapter", 1);
//        int levelNumber = PlayerPrefs.GetInt("SelectedLevel", 1);
//        //await cannonSpawner.CannonSpawn();

//        var go = cannonSpawner.SpawnCannon();
//        //cannon = cannonSpawner.AssignValueCannon(go);

//        // TRIGGER 1: Show at the very start of Level 1
//        if (levelNumber == 1)
//        {
//            SlotMachine.SetActive(true);
//            slotMachineController.ShowAndSpin();
//            // Note: LoadLevel will be called by the SlotMachine's countdown after it finishes
//        }
//        else
//        {
//            LoadLevel(chapterNumber, levelNumber);
//        }

//    }

//    public void LoadLevel(int chapterNumber, int levelNumber)
//    {
//        string levelPath = $"Levels/Chapter{chapterNumber:D2}/Level_{levelNumber:D2}";
//        LevelData level = Resources.Load<LevelData>(levelPath);
//        levelNumberText.text = "Level: " + levelNumber.ToString();

//        if (level == null)
//        {
//            Debug.LogError($"LevelData not found at path: {levelPath}");
//            return;
//        }

//        currentLevel = level;

//        //leveltackprogressbar
//        if (LevelProgressTracker.Instance != null)
//        {
//            LevelProgressTracker.Instance.SetLevelData(currentLevel);
//        }

//        // Update background
//        if (ThemeManager.Instance != null)
//        {
//            backgroundRenderer.sprite = ThemeManager.Instance.GetThemeForChapter(chapterNumber);
//        }

//        // Start bird spawning
//        birdSpawner.StartSpawning(currentLevel);

//        //  Start cannon again after level is loaded
//        if (cannon != null)
//            cannon.StartFiring();
//    }

//    public void OnAllBirdsDestroyed()
//    {

//        int chapterNumber = PlayerPrefs.GetInt("SelectedChapter", 1);
//        int currentLevelNum = currentLevel.levelNumber; // Level we just finished
//        int nextLevel = currentLevelNum + 1;

//        LevelManager.Instance?.UpdateValue();
//        LevelEnd?.Invoke();

//        if (nextLevel > 20)
//        {
//            SceneManager.LoadScene("SampleScene");
//        }
//        else
//        {
//            PlayerPrefs.SetInt("SelectedLevel", nextLevel);

//            if (cannon != null)
//                cannon.StopFiring();

//            // TRIGGER 2, 3, 4: End of Level 5, 10, and 15
//            if (currentLevelNum == 5 || currentLevelNum == 10 || currentLevelNum == 15)
//            {
//                SlotMachine.SetActive(true);
//                slotMachineController.ShowAndSpin();
//            }
//            else
//            {
//                // Regular transition for all other levels
//                StartCoroutine(LoadNextLevelWithCountdown(chapterNumber, nextLevel));
//            }
//        }
//    }

//    //  method for countdown during regular level transitions
//    private IEnumerator LoadNextLevelWithCountdown(int chapter, int level)
//    {
//        if (countdownText != null)
//        {
//            countdownText.gameObject.SetActive(true);

//            for (int count = 3; count > 0; count--)
//            {
//                countdownText.text = $"Loading level: {level} in {count} seconds";

//                // Animate countdown text
//                countdownText.transform.localScale = Vector3.one * 1.5f;
//                float elapsed = 0f;
//                while (elapsed < 0.5f)
//                {
//                    elapsed += Time.deltaTime;
//                    countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / 0.5f);
//                    yield return null;
//                }

//                yield return new WaitForSeconds(0.5f);
//            }

//            countdownText.text = "";
//            countdownText.gameObject.SetActive(false);
//        }
//        else
//        {
//            // Fallback if countdown text is not assigned - just wait 3 seconds
//            yield return new WaitForSeconds(3f);
//        }

//        LoadLevel(chapter, level);
//    }
//    private void OnDisable()
//    {
//        ClearPool();
//    }

//    public void ClearPool()
//    {
//        GamePoolManager.blastParticlePool.Clear();
//    }

//}