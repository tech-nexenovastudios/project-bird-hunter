using System;
using System.Linq;
using Gameplay.PowerUps;
using UnityEngine;
using Gameplay.Events;
using Gameplay.Player;
using Gameplay.UI;
using TMPro;

public enum GameState
{
    Loading,
    Gameplay,
    Slot,
    GameOver,
}

namespace Gameplay.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public ChaptersConfig chaptersConfig;
        public Slot.SlotMachineController slotMachine;
        public SpawnController            spawnController;
        public CannonSpawner              cannonSpawner;

        public GameState state;

        public GameObject currentCannon;

        private bool _pendingLevelStart = false;
        
        public ChapterData chapterData;

        private void Awake()
        {
            Instance = this;

            var cam    = Camera.main;
            float height = cam.orthographicSize;
            float width  = height * cam.aspect;

            ScreenBounds.minX = -width;
            ScreenBounds.maxX =  width;
            ScreenBounds.minY = -height;
            ScreenBounds.maxY =  height;
        }

        private void OnEnable()
        {
            GameProgressManager.OnProgressChanged += OnProgressChanged;
            GameProgressManager.OnSpinTriggered   += OnSpinTriggered;
            GameEvents.OnPlayerDeath              += OnPlayerDeath;
            // XPManager.Instance.OnXPAdded          += OnXPAdded;
            // XPManager.Instance.OnPlayerLevelUp    += OnPlayerLevelUp;
        }

        private void OnDisable()
        {
            GameProgressManager.OnProgressChanged -= OnProgressChanged;
            GameProgressManager.OnSpinTriggered   -= OnSpinTriggered;
            GameEvents.OnPlayerDeath              -= OnPlayerDeath;
        }

        // private void OnPlayerLevelUp(int level)
        //     => playerLevelupText.text = $"Level Up! {level}";
        //
        // private void OnXPAdded(int xp, int amountAdded)
        //     => playerXPText.text = $"XP: {xp}";

        // ──────────────────────────
        // ENTRY POINT
        // ──────────────────────────

        private async void Start()
        {
            try
            {
                var chapterIndex = PlayerPrefs.GetInt("SelectedChapter", 1);
                
                chapterData = chaptersConfig.GetWorldData(chapterIndex - 1);
                
                LevelReferences.Instance.SetChapterData(chapterData);
                
                GameProgressManager.Instance.LoadProgress();
            
                var progress = GameProgressManager.Instance.Data;

                if (IsInitialSpinRequired(progress))
                {
                    TriggerInitialSpin(progress);
                    return; // StartGameplay() called by OnSpinComplete() after player picks
                }

                currentCannon = await cannonSpawner.CannonSpawn();
            
                StartGameplay();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Start() failed: {e.Message}");
            }
        }

        bool IsInitialSpinRequired(GameProgress progress)
        {
            Debug.Log($"IsInitialSpinRequired? Ch{progress.currentChapter} L{progress.currentLevel} Spins: {progress.playerSpins}");
            return progress.playerSpins == 0;
        }

        void TriggerInitialSpin(GameProgress progress)
        {
            var options = progress.GetAvailablePowerUpForSpin(
                progress.currentChapter, 0,
                GameProgressManager.Instance.allPowerups, 3);

            Debug.Log($"🎰 Initial spin Ch{progress.currentChapter} L{progress.currentLevel} → Slot {0}");
            OnSpinTriggered(0, options);
            // StartGameplay() will be called by OnSpinComplete() when player picks
        }

        private void OnSpinTriggered(int slotIndex, PowerupConfig[] powerupConfigs)
        {
            state = GameState.Slot;

            if (currentCannon != null)
            {
                var baseCannon = currentCannon.GetComponent<ICannonBase>();
                if (baseCannon != null) baseCannon.StopFiring();
            }

            slotMachine.Spin(powerupConfigs.ToList());
        }
        
        public void StartGameplay()
        {
            try
            {
                _pendingLevelStart = false;

                var profile = GameProgressManager.Instance.GetCurrentLevelProfile();
                if (profile == null)
                {
                    Debug.LogError("❌ Missing LevelProfile for current progress!");
                    return;
                }

                state = GameState.Gameplay;

                ScoreManager.Instance?.ResetLevel();
                
                LevelCompletionController.Instance?.ResetForNewLevel();
                
                XPManager.Instance?.ResetForNewLevel();
                
                spawnController.levelProfile = profile;
                spawnController.ResetLevel();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] StartGameplay() failed: {e.Message}");
            }
        }
        public void CompleteCurrentLevel(int scoreAchieved)
        {
            _pendingLevelStart = true;
            GameProgressManager.Instance.CompleteLevel(scoreAchieved);
        }

        private void OnProgressChanged(GameProgress progress)
        {
            Debug.Log($"Progress changed to Ch{progress.currentChapter} L{progress.currentLevel}");
            
            GameEvents.FireGameLevelUpdated(GameProgressManager.Instance.GetCurrentLevelProfile(), progress.currentLevel);
            
            if (!_pendingLevelStart) return;
            _pendingLevelStart = false;

            if (state == GameState.Slot) return;

            StartGameplay();
        }

        // ──────────────────────────
        // MISC
        // ──────────────────────────

        public void ResetGame()
        {
            GameProgressManager.Instance.ResetProgress();
            StartGameplay();
        }

        public bool IsGameActive() => state == GameState.Gameplay;

        private void OnPlayerDeath()
        {
            Debug.Log("Game Over!");
            state = GameState.GameOver;
        }
    }
}
