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
            Debug.Log("[SlotMachine] OnEnable — subscribed to OnSpinStarted.");
        }

        private void OnDisable()
        {
            GameEvents.OnSpinStarted -= Spin;
            Debug.Log("[SlotMachine] OnDisable — unsubscribed from OnSpinStarted.");

            foreach (var reel in reels)
            {
                reel.OnPowerupSelected = null;
                reel.SetHighlight(false);
                if (reel.content != null)
                    reel.content.DOKill();
            }

            _selectedReel = null;
        }

        // ───────── Spin ─────────
        private void Spin(List<PowerupConfig> resultsPerReel)
        {
            Debug.Log($"[SlotMachine] Spin() called. Results count: {resultsPerReel?.Count}");

            if (_selectedReel != null)
            {
                _selectedReel.SetHighlight(false);
                _selectedReel = null;
            }

            for (int i = 0; i < reels.Length; i++)
            {
                var result = resultsPerReel[i];
                Debug.Log($"[SlotMachine] Reel[{i}] assigned result: {result?.displayName ?? "NULL"}");

                var index = i;
                var reel = reels[i];

                reels[i].OnPowerupSelected = null;
                reels[i].OnPowerupSelected += (config) => OnReelSelected(reel, config);

                DOVirtual.DelayedCall(i * 0.12f, () => reels[index].SpinToResult(result));
            }

            if (reels.Length > 0)
            {
                float totalSpinDuration = (reels.Length - 1) * 0.12f + reels[reels.Length - 1].spinDuration;
                DOVirtual.DelayedCall(totalSpinDuration, GameEvents.FireSpinAnimationCompleted);
            }
        }

        private void OnReelSelected(ReelController clickedReel, PowerupConfig selected)
        {
            Debug.Log($"[SlotMachine] OnReelSelected — powerup: {selected?.displayName ?? "NULL"} | id: {selected?.id ?? "NULL"}");

            if (_selectedReel != null && _selectedReel != clickedReel)
            {
                Debug.Log("[SlotMachine] Un-highlighting previously selected reel.");
                _selectedReel.SetHighlight(false);
            }

            _selectedReel = clickedReel;
            clickedReel.SetHighlight(true);

            // ── THIS was the missing call ──
            GameProgressManager.Instance.PlayerSelectedPowerup(selected);
        }

        // ───────── Confirm button ─────────
        public void CommitSelection()
        {
            Debug.Log("[SlotMachine] CommitSelection() called.");

            var selected = GameProgressManager.Instance.LastSelectedPowerup;

            if (selected == null)
            {
                Debug.LogWarning("[SlotMachine] ⚠️ CommitSelection: LastSelectedPowerup is NULL — player hasn't selected a reel yet.");
                return;
            }

            Debug.Log($"[SlotMachine] Committing powerup: '{selected.displayName}' (id: {selected.id})");

            GameProgressManager.Instance.ClearLastSelectedPowerup();
            Debug.Log("[SlotMachine] LastSelectedPowerup cleared. Calling OnSpinComplete...");

            GameManager.Instance.OnSpinComplete();
        }
    }
}