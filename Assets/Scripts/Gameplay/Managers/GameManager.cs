//using System.Linq;
//using Gameplay.PowerUps;
//using UnityEngine;
//using Gameplay.Events;
//using Gameplay.Player;
//using TMPro;

//public enum GameState
//{
//    Loading,
//    Gameplay,
//    Slot,
//    GameOver,
//}

//namespace Gameplay.Managers
//{
//    public class GameManager : MonoBehaviour
//    {
//        public static GameManager Instance;

//        public Slot.SlotMachineController slotMachine;
//        public SpawnController spawnController;
//        public CannonSpawner cannonSpawner;

//        public GameState state;

//        public TextMeshProUGUI playerLevelupText;
//        public TextMeshProUGUI playerXPText;
//        public TextMeshProUGUI currentLevelText;

//        public GameObject currentCannon;

//        // ADDED: guard so OnProgressChanged never triggers StartGameplay more than once per completion
//        private bool _pendingLevelStart = false;

//        private void Awake()
//        {
//            Instance = this;

//            var cam = Camera.main;
//            float height = cam.orthographicSize;
//            float width = height * cam.aspect;

//            ScreenBounds.minX = -width;
//            ScreenBounds.maxX = width;
//            ScreenBounds.minY = -height;
//            ScreenBounds.maxY = height;
//        }

//        private void OnEnable()
//        {
//            GameProgressManager.OnProgressChanged += OnProgressChanged;
//            GameProgressManager.OnSpinTriggered += OnSpinTriggered;
//            GameEvents.OnPlayerDeath += OnPlayerDeath;
//            XPManager.Instance.OnXPAdded += OnXPAdded;
//            XPManager.Instance.OnPlayerLevelUp += OnPlayerLevelUp;
//        }

//        private void OnDisable()
//        {
//            GameProgressManager.OnProgressChanged -= OnProgressChanged;
//            GameProgressManager.OnSpinTriggered -= OnSpinTriggered;
//            GameEvents.OnPlayerDeath -= OnPlayerDeath;
//        }

//        private void OnPlayerLevelUp(int level)
//            => playerLevelupText.text = $"Level Up! {level}";

//        private void OnXPAdded(int xp, int amountAdded)
//            => playerXPText.text = $"XP: {xp}";

//        // ──────────────────────────
//        // ENTRY POINT
//        // ──────────────────────────

//        void Start()
//        {
//            GameProgressManager.Instance.LoadProgress();

//            var progress = GameProgressManager.Instance.Data;

//            if (IsInitialSpinRequired(progress))
//            {
//                TriggerInitialSpin(progress);
//                return; // StartGameplay() called by OnSpinComplete() after player picks
//            }

//            StartGameplay();
//        }

//        // ──────────────────────────
//        // INITIAL SPIN
//        // ──────────────────────────
//        bool IsInitialSpinRequired(GameProgress progress)
//        {
//            Debug.Log($"IsInitialSpinRequired? Ch{progress.currentChapter} L{progress.currentLevel} Spins: {progress.playerSpins}");
//            return progress.playerSpins == 0;
//        }

//        void TriggerInitialSpin(GameProgress progress)
//        {
//            var options = progress.GetAvailablePowerUpForSpin(
//                progress.currentChapter, 0,
//                GameProgressManager.Instance.allPowerups, 3);

//            Debug.Log($"🎰 Initial spin Ch{progress.currentChapter} L{progress.currentLevel} → Slot {0}");
//            OnSpinTriggered(0, options);
//            // StartGameplay() will be called by OnSpinComplete() when player picks
//        }

//        // ──────────────────────────
//        // SPIN TRIGGERED (show UI)
//        // ──────────────────────────
//        private void OnSpinTriggered(int slotIndex, PowerupConfig[] powerupConfigs)
//        {
//            state = GameState.Slot;

//            if (currentCannon != null)
//            {
//                var baseCannon = currentCannon.GetComponent<BaseCannon>();
//                if (baseCannon != null) baseCannon.StopFiring();
//            }

//            slotMachine.gameObject.SetActive(true);
//            slotMachine.Spin(powerupConfigs.ToList());
//        }

//        // ──────────────────────────
//        // START GAMEPLAY
//        // ──────────────────────────
//        public async void StartGameplay()
//        {
//            _pendingLevelStart = false;

//            var profile = GameProgressManager.Instance.GetCurrentLevelProfile();
//            if (profile == null)
//            {
//                Debug.LogError("❌ Missing LevelProfile for current progress!");
//                return;
//            }

//            state = GameState.Gameplay;

//            ScoreManager.Instance?.ResetLevel();
//            LevelCompletionController.Instance?.ResetForNewLevel();
//            XPManager.Instance?.ResetForNewLevel();
//            spawnController.levelProfile = profile;
//            spawnController.ResetLevel();

//            if (currentCannon == null)
//            {
//                currentCannon = await cannonSpawner.CannonSpawn();
//            }
//            else
//            {
//                var baseCannon = currentCannon.GetComponent<BaseCannon>();
//                //if (baseCannon != null) baseCannon.StartFiring();
//            }

//            GameProgressManager.Instance.ApplyPowerUpsToCurrentCannon(currentCannon);
//        }
//        // ──────────────────────────
//        // LEVEL COMPLETE
//        // ──────────────────────────
//        public void CompleteCurrentLevel(int scoreAchieved)
//        {
//            _pendingLevelStart = true;
//            GameProgressManager.Instance.CompleteLevel(scoreAchieved);
//        }

//        // FIXED: only handles mid-game progress advances, NOT initial spin
//        private void OnProgressChanged(GameProgress progress)
//        {
//            Debug.Log($"Progress changed to Ch{progress.currentChapter} L{progress.currentLevel}");

//            currentLevelText.text = $"Level {progress.currentLevel}";

//            if (!_pendingLevelStart) return; // ignore SaveProgress duplicate fires
//            _pendingLevelStart = false;

//            // If spin is showing, wait — OnSpinComplete() will call StartGameplay()
//            if (slotMachine.gameObject.activeSelf) return;

//            StartGameplay();
//        }
//        // ──────────────────────────
//        // SPIN COMPLETE — single exit point for ALL spins (initial + mid-game)
//        // ──────────────────────────
//        public void OnSpinComplete() // Called by SlotMachineController after player picks powerup
//        {
//            slotMachine.gameObject.SetActive(false);
//            StartGameplay(); // always safe — works for both initial and mid-game spins
//        }

//        // ──────────────────────────
//        // MISC
//        // ──────────────────────────

//        public void ResetGame()
//        {
//            GameProgressManager.Instance.ResetProgress();
//            StartGameplay();
//        }

//        public bool IsGameActive() => state == GameState.Gameplay;

//        private void OnPlayerDeath()
//        {
//            Debug.Log("Game Over!");
//            state = GameState.GameOver;
//        }
//    }
//}

using System.Collections;
using System.Linq;
using Gameplay.PowerUps;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Player;
using TMPro;

public enum GameState
{
    Loading,
    Gameplay,
    Slot,
    Paused,
    GameOver,
}

namespace Gameplay.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public Slot.SlotMachineController slotMachine;
        public SpawnController spawnController;
        public CannonSpawner cannonSpawner;

        public GameState state;

        public TextMeshProUGUI playerLevelupText;
        public TextMeshProUGUI playerXPText;
        public TextMeshProUGUI currentLevelText;

        public GameObject currentCannon;

        private bool _pendingLevelStart = false;

        // ───────── Unity ─────────
        private void Awake()
        {
            Instance = this;

            var cam = Camera.main;
            float h = cam.orthographicSize;
            float w = h * cam.aspect;

            ScreenBounds.minX = -w;
            ScreenBounds.maxX = w;
            ScreenBounds.minY = -h;
            ScreenBounds.maxY = h;
        }

        private void OnEnable()
        {
            GameProgressManager.OnProgressChanged += OnProgressChanged;
            GameProgressManager.OnSpinTriggered += OnSpinTriggered;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            GameEvents.OnPauseToggled += OnPauseToggled;
            GameEvents.OnBackToMenu += OnBackToMenu;
            XPManager.Instance.OnXPAdded += OnXPAdded;
            XPManager.Instance.OnPlayerLevelUp += OnPlayerLevelUp;
        }

        private void OnDisable()
        {
            GameProgressManager.OnProgressChanged -= OnProgressChanged;
            GameProgressManager.OnSpinTriggered -= OnSpinTriggered;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnPauseToggled -= OnPauseToggled;
            GameEvents.OnBackToMenu -= OnBackToMenu;
        }

        // ───────── XP / Level display ─────────
        private void OnPlayerLevelUp(int level)
            => playerLevelupText.text = $"Level Up! {level}";

        private void OnXPAdded(int xp, int amountAdded)
            => playerXPText.text = $"XP: {xp}";

        // ───────── Entry point ─────────
        private void Start()
        {
            // Wait one frame so all OnEnable subscriptions on UI scripts complete first
            StartCoroutine(InitWithDelay());
        }

        private IEnumerator InitWithDelay()
        {
            yield return null; // one frame delay — guarantees all UI OnEnable has run

            GameProgressManager.Instance.LoadProgress();

            var progress = GameProgressManager.Instance.Data;

            if (IsInitialSpinRequired(progress))
            {
                TriggerInitialSpin(progress);
                yield break;
            }

            StartGameplay();
        }

        // ───────── Initial spin ─────────
        private bool IsInitialSpinRequired(GameProgress progress)
        {
            Debug.Log($"IsInitialSpinRequired? Ch{progress.currentChapter} L{progress.currentLevel} Spins:{progress.playerSpins}");
            return progress.playerSpins == 0;
        }

        private void TriggerInitialSpin(GameProgress progress)
        {
            var options = progress.GetAvailablePowerUpForSpin(
                progress.currentChapter, 0,
                GameProgressManager.Instance.allPowerups, 3);

            Debug.Log($"🎰 Initial spin Ch{progress.currentChapter} L{progress.currentLevel}");
            OnSpinTriggered(0, options);
        }

        // ───────── Spin triggered ─────────
        private void OnSpinTriggered(int slotIndex, PowerupConfig[] options)
        {
            state = GameState.Slot;

            if (currentCannon != null)
            {
                var cannon = currentCannon.GetComponent<BaseCannon>();
                if (cannon != null) cannon.StopFiring();
            }

            GameEvents.FireSpinTriggered(options);
        }

        // ───────── Start gameplay ─────────
        public async void StartGameplay()
        {
            _pendingLevelStart = false;

            var profile = GameProgressManager.Instance.GetCurrentLevelProfile();
            if (profile == null)
            {
                Debug.LogError("❌ Missing LevelProfile!");
                return;
            }

            state = GameState.Gameplay;

            ScoreManager.Instance?.ResetLevel();
            LevelCompletionController.Instance?.ResetForNewLevel();
            XPManager.Instance?.ResetForNewLevel();

            spawnController.levelProfile = profile;
            spawnController.ResetLevel();

            if (currentCannon == null)
                currentCannon = await cannonSpawner.CannonSpawn();

            GameProgressManager.Instance.ApplyPowerUpsToCurrentCannon(currentCannon);

            // Fire AFTER everything ready — UI is guaranteed subscribed by now
            GameEvents.FireGameLevelUpdated(profile, GameProgressManager.Instance.CurrentLevel);
        }

        // ───────── Level complete ─────────
        public void CompleteCurrentLevel(int scoreAchieved)
        {
            _pendingLevelStart = true;
            GameProgressManager.Instance.CompleteLevel(scoreAchieved);
        }

        private void OnProgressChanged(GameProgress progress)
        {
            Debug.Log($"Progress → Ch{progress.currentChapter} L{progress.currentLevel}");
            currentLevelText.text = $"Level {progress.currentLevel}";

            if (!_pendingLevelStart) return;
            _pendingLevelStart = false;

            if (state == GameState.Slot) return;

            StartGameplay();
        }

        // ───────── Spin complete ─────────
        public void OnSpinComplete()
        {
            StartGameplay();
        }

        // ───────── Pause / Resume ─────────
        private void OnPauseToggled(bool isPaused)
        {
            state = isPaused ? GameState.Paused : GameState.Gameplay;
            Debug.Log(isPaused ? "⏸ Paused" : "▶ Resumed");
        }

        // ───────── Back to menu ─────────
        private void OnBackToMenu()
        {
            state = GameState.Loading;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        // ───────── Reset ─────────
        public void ResetGame()
        {
            GameProgressManager.Instance.ResetProgress();
            StartGameplay();
        }

        // ───────── Helpers ─────────
        public bool IsGameActive() => state == GameState.Gameplay;

        private void OnPlayerDeath()
        {
            Debug.Log("💀 Game Over");
            state = GameState.GameOver;
        }
    }
}