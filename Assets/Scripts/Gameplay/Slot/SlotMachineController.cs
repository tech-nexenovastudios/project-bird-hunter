using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Slot
{
    public class SlotMachineController : MonoBehaviour
    {
        [SerializeField] private ReelController[] reels;
        [SerializeField] private List<PowerupConfig> symbolLibrary;

        // Tracks which reel the player last selected so we can un-highlight it
        private ReelController _selectedReel;

        private void Start()
        {
            foreach (var reel in reels)
            {
                reel.symbolList = symbolLibrary;
                reel.InitializeReel();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnSpinStarted += Spin;
        }

        private void OnDisable()
        {
            GameEvents.OnSpinStarted -= Spin;

            foreach (var reel in reels)
            {
                reel.OnPowerupSelected = null;
                reel.SetHighlight(false);
                // Kill only this reel's scroll tween, not everything in the scene
                if (reel.content != null)
                    reel.content.DOKill();
            }

            _selectedReel = null;
            // ← DOTween.KillAll() removed — it was killing the notification panel tween
        }

        // ───────── Spin ─────────
        private void Spin(List<PowerupConfig> resultsPerReel)
        {
            // Reset selection state for a fresh spin
            if (_selectedReel != null)
            {
                _selectedReel.SetHighlight(false);
                _selectedReel = null;
            }

            for (int i = 0; i < reels.Length; i++)
            {
                var result = resultsPerReel[i];
                var index = i;
                var reel = reels[i]; // capture for lambda

                reels[i].OnPowerupSelected = null;

                // Capture reel reference so OnReelSelected knows which reel was clicked.
                // NOTE: We intentionally do NOT clear this listener after first selection
                // so the player can change their mind and pick a different reel.
                reels[i].OnPowerupSelected += (config) => OnReelSelected(reel, config);

                DOVirtual.DelayedCall(i * 0.12f, () => reels[index].SpinToResult(result));
            }
        }

        // ───────── Player selects / re-selects a reel ─────────
        private void OnReelSelected(ReelController clickedReel, PowerupConfig selected)
        {
            // Un-highlight previously selected reel (if different)
            if (_selectedReel != null && _selectedReel != clickedReel)
                _selectedReel.SetHighlight(false);

            // Highlight the newly selected reel
            _selectedReel = clickedReel;
            clickedReel.SetHighlight(true);

            // GameEvents.FirePowerupCommitted is already fired inside ReelController's
            // click listener — no need to fire it again here.
        }

        // ───────── Called by UI confirm button via SlotMachineScreen ─────────
        public void CommitSelection()
        {
            var selected = GameProgressManager.Instance.LastSelectedPowerup;
            if (selected == null)
            {
                Debug.LogWarning("⚠️ CommitSelection: LastSelectedPowerup is null.");
                return;
            }
            GameProgressManager.Instance.ClearLastSelectedPowerup();
            GameManager.Instance.OnSpinComplete();
        }
    }
}