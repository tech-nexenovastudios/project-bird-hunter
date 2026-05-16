using System.Collections;
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
            GameEvents.OnChapterCompleted += OnChapterCompletedHandler;
        }

        private void OnDisable()
        {
            GameProgressManager.OnProgressChanged -= OnProgressChanged;
            GameProgressManager.OnSpinTriggered -= OnSpinTriggered;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnPauseToggled -= OnPauseToggled;
            GameEvents.OnChapterCompleted -= OnChapterCompletedHandler;
        }

        // Pause gameplay during the chapter-end animation. The spin still
        // starts later (TriggerSpin → OnSpinTriggered also sets state=Slot),
        // and StartGameplay restores Gameplay state once the spin closes.
        // This guarantees OnProgressChanged's GameState.Slot guard skips
        // StartGameplay during the animation window.
        private void OnChapterCompletedHandler(int newChapterNumber)
        {
            state = GameState.Slot;
        }

        // ───────── Entry point ─────────
        private void Start()
        {
            StartCoroutine(InitWithDelay());
        }

        private IEnumerator InitWithDelay()
        {
            yield return null;

            GameProgressManager.Instance.LoadProgress();

            // New Gameplay scene = new run. RewardManager is DontDestroyOnLoad so its
            // run counters survive scene loads; we explicitly clear them here.
            RewardManager.Instance?.ResetForNewRun();

            var progress = GameProgressManager.Instance.Data;

            if (IsSpinPending(progress))
            {
                TriggerPendingSpin(progress);
                yield break;
            }

            StartGameplay();
        }

        // ───────── Spin pending check ─────────
        private bool IsSpinPending(GameProgress progress)
        {
            Debug.Log($"IsSpinPending? Ch{progress.currentChapter} L{progress.currentLevel} Spins:{progress.playerSpins}");

            if (progress.playerSpins == 0)
            {
                Debug.Log("→ Fresh game spin");
                return true;
            }

            if (progress.currentLevel == 1)
            {
                var slot0 = progress.chapterSlots[0];
                bool slot0Empty = slot0 == null || string.IsNullOrEmpty(slot0.equippedPowerupId);
                Debug.Log($"→ At L1, slot0 empty: {slot0Empty}");
                return slot0Empty;
            }

            return false;
        }

        private void TriggerPendingSpin(GameProgress progress)
        {
            var options = progress.GetAvailablePowerUpForSpin(
                progress.currentChapter, 0,
                GameProgressManager.Instance.allPowerups, 3);

            Debug.Log($"Pending spin → Ch{progress.currentChapter} L{progress.currentLevel}");
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

            var chapterCfg = GameProgressManager.Instance.GetCurrentChapterConfig();
            if (chapterCfg == null)
            {
                int ch = GameProgressManager.Instance.CurrentChapter;
                Debug.LogError($"[GameManager] Missing ChapterProgressionConfig for chapter {ch}. " +
                               $"Ensure Assets/Resources/Data/ChapterProgressions/Chapter{ch}.asset exists.");
                return;
            }

            state = GameState.Gameplay;

            int levelIdx = GameProgressManager.Instance.CurrentLevel - 1;
            int priorAttempts = GameProgressManager.Instance.RegisterLevelAttempt(
                GameProgressManager.Instance.CurrentChapter,
                GameProgressManager.Instance.CurrentLevel);

            spawnController.Configure(chapterCfg, levelIdx, priorAttempts);
            spawnController.ResetLevel();

            ScoreManager.Instance?.ResetLevel(spawnController.TargetScore);
            LevelCompletionController.Instance?.ResetForNewLevel();

            RewardManager.Instance?.ResetForNewLevel();

            if (currentCannon == null)
                currentCannon = await cannonSpawner.CannonSpawn();

            // ── Inject live cannon into caster BEFORE applying powerups ──
            var baseCannon = currentCannon.GetComponent<BaseCannon>();
            if (baseCannon != null)
                CannonPowerUpCaster.Instance.SetCannon(baseCannon);
            else
                Debug.LogWarning("⚠️ StartGameplay: BaseCannon not found on currentCannon.");

            GameProgressManager.Instance.ApplyPowerUpsToCurrentCannon(currentCannon);

            GameEvents.FireGameLevelUpdated(GameProgressManager.Instance.CurrentLevel);
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
            Debug.Log(isPaused ? "Paused" : "Resumed");
        }

        // ───────── Reset ─────────
        public void ResetGame()
        {
            GameProgressManager.Instance.ResetProgress();
            RewardManager.Instance?.ResetForNewRun();
            StartGameplay();
        }

        // ───────── Helpers ─────────
        public bool IsGameActive() => state == GameState.Gameplay;

        private void OnPlayerDeath()
        {
            Debug.Log("Game Over");
            state = GameState.GameOver;
        }
    }
}