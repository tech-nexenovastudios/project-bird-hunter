using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.PowerUps;
using UnityEngine;
using Gameplay.Slot;
using UnityUtils;

public enum GameState
{
    Loading,
    Gameplay,
    Slot,
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
        
        public GameObject currentCannon;
        private void Awake()
        {
            Instance = this;
            
            var cam = Camera.main;
            float height = cam.orthographicSize;
            float width  = height * cam.aspect;

            ScreenBounds.minX = -width;
            ScreenBounds.maxX =  width;
            ScreenBounds.minY = -height;
            ScreenBounds.maxY =  height;
        }

        public void ResetGame()
        {
            // Reset to Ch1 L1
            GameProgressManager.Instance.ResetProgress();
            StartGameplay();
        }

        private void OnEnable()
        {
            GameProgressManager.OnProgressChanged += OnProgressChanged;
            GameProgressManager.OnSpinTriggered += OnSpinTriggered;
        }

        private void OnDisable()
        {
            GameProgressManager.OnProgressChanged -= OnProgressChanged;
            GameProgressManager.OnSpinTriggered -= OnSpinTriggered;
        }

        // ──────────────────────────
        // ENTRY POINT: Scene loads
        // ──────────────────────────
        void Start()
        {
            // 1. Check if we need an initial spin for this chapter
            var progress = GameProgressManager.Instance.Data;
            if (IsInitialSpinRequired(progress))
            {
                TriggerInitialSpin();
                return; // Wait for spin to finish before gameplay
            }

            // 2. Normal case: start gameplay immediately
            StartGameplay();
        }

        // ──────────────────────────
        // INITIAL SPIN LOGIC
        // ──────────────────────────
        bool IsInitialSpinRequired(GameProgress progress)
        {
            return progress.IsSpinLevel(progress.currentLevel - 1);
        }

        void TriggerInitialSpin()
        {
            var progress = GameProgressManager.Instance.Data;
            int slotIndex = 0; // Always slot 0 for L1

            var options = progress.GetAvailablePowerUpForSpin(
                progress.currentChapter, slotIndex,
                GameProgressManager.Instance.allPowerups, 3);

            Debug.Log($"🎰 Initial spin for Ch{progress.currentChapter} Slot {slotIndex}");
            OnSpinTriggered(slotIndex, options); // Reuse your existing handler!
        }

        // ──────────────────────────
        // NORMAL SPINS (after level complete)
        // ──────────────────────────
        private void OnSpinTriggered(int slotIndex, PowerupConfig[] powerupConfigs)
        {
            Debug.Log("Spin triggered!");
            Debug.Log($"Slot {slotIndex} | Options: {powerupConfigs.Length}");
            state = GameState.Slot;
            slotMachine.gameObject.SetActive(true);
            slotMachine.Spin(powerupConfigs.ToList());
        }

        // ──────────────────────────
        // START GAMEPLAY
        // ──────────────────────────
        public async void StartGameplay()
        {
            var profile = GameProgressManager.Instance.GetCurrentLevelProfile();
            if (profile == null)
            {
                Debug.LogError("❌ Missing LevelProfile for current progress!");
                return;
            }

            state = GameState.Gameplay;
            
            spawnController.levelProfile = profile;
            spawnController.ResetLevel();

            if (currentCannon == null)
            {
                currentCannon = await cannonSpawner.CannonSpawn();
            }

            // Apply power ups to cannon
            GameProgressManager.Instance.ApplyPowerUpsToCurrentCannon(currentCannon);
        }

        // ──────────────────────────
        // LEVEL COMPLETE (your existing logic)
        // ──────────────────────────
        private void OnProgressChanged(GameProgress progress)
        {
            Debug.Log($"Progress changed to Ch{progress.currentChapter} L{progress.currentLevel}");

            // Optional: if you want to auto-advance after spin completes
            if (slotMachine.gameObject.activeSelf)
            {
                slotMachine.gameObject.SetActive(false);
                StartGameplay();
            }
            
            //StartGameplay();
        }

        // ──────────────────────────
        // PUBLIC CALLS (from your UI)
        // ──────────────────────────
        public void CompleteCurrentLevel(int scoreAchieved)
        {
            GameProgressManager.Instance.CompleteLevel(scoreAchieved);
            // Spin UI will show automatically if this was L7/12/17
        }

        public void OnSpinComplete() // Called by SlotMachineController
        {
            // After spin ends, start the next gameplay
            StartGameplay();
        }

        public bool IsGameActive()
        {
            //TODO: Add logic to check if game is active
            return true;
        }
    }


}