using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using UnityEngine;

namespace Gameplay.Slot
{
    /// <summary>
    /// Drives the reels: starts staggered spins on OnSpinStarted, manages one-of-N highlight
    /// when the player taps a reel, and equips the chosen powerup on Confirm. Confirm button
    /// invokes <see cref="CommitSelection"/> via UnityEvent in the prefab.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        private enum State { Idle, Spinning, AwaitingChoice, Confirming }

        private const float ReelStaggerSeconds = 0.12f;

        [SerializeField] private ReelController[] reels;
        [SerializeField] private List<PowerupConfig> symbolLibrary;

        private State _state = State.Idle;
        private ReelController _selectedReel;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            foreach (var reel in reels)
            {
                if (reel == null) continue;
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
                if (reel == null) continue;
                reel.OnPowerupSelected = null;
                reel.SetHighlight(false);
                if (reel.content != null) reel.content.DOKill();
            }

            _selectedReel = null;
            _state = State.Idle;
        }

        // ─── Spin ───────────────────────────────────────────────────────────

        private void Spin(List<PowerupConfig> resultsPerReel)
        {
            if (resultsPerReel == null || resultsPerReel.Count != reels.Length)
            {
                Debug.LogError($"[SlotMachine] Spin received {resultsPerReel?.Count ?? 0} results for {reels.Length} reels.");
                return;
            }

            ClearSelection();
            _state = State.Spinning;

            for (int i = 0; i < reels.Length; i++)
            {
                int idx = i;
                var reel = reels[i];
                var result = resultsPerReel[i];

                reel.OnPowerupSelected = null;
                reel.OnPowerupSelected = config => OnReelSelected(reel, config);

                DOVirtual.DelayedCall(idx * ReelStaggerSeconds, () => reel.SpinToResult(result));
            }

            float allReelsCompleteTime = (reels.Length - 1) * ReelStaggerSeconds + reels[^1].spinDuration;
            DOVirtual.DelayedCall(allReelsCompleteTime, OnAllReelsStopped);
        }

        private void OnAllReelsStopped()
        {
            if (_state != State.Spinning) return;
            _state = State.AwaitingChoice;
            GameEvents.FireSpinAnimationCompleted();
        }

        // ─── Selection (player tapped a reel) ───────────────────────────────

        private void OnReelSelected(ReelController clickedReel, PowerupConfig selected)
        {
            if (selected == null) return;

            if (_selectedReel != null && _selectedReel != clickedReel)
                _selectedReel.SetHighlight(false);

            _selectedReel = clickedReel;
            clickedReel.SetHighlight(true);

            GameProgressManager.Instance.PlayerSelectedPowerup(selected);
            // Surfaces "a reel was tapped" to listeners (UI confirm button enable, SFX, etc.).
            GameEvents.FirePowerupCommitted(selected);
        }

        private void ClearSelection()
        {
            if (_selectedReel != null) _selectedReel.SetHighlight(false);
            _selectedReel = null;
        }

        // ─── Confirm button (wired via UnityEvent in the prefab) ────────────

        public void CommitSelection()
        {
            if (_state == State.Confirming) return;

            var selected = GameProgressManager.Instance.LastSelectedPowerup;
            if (selected == null)
            {
                Debug.LogWarning("[SlotMachine] CommitSelection: no powerup selected — confirm ignored.");
                return;
            }

            _state = State.Confirming;
            GameProgressManager.Instance.ClearLastSelectedPowerup();
            GameManager.Instance.OnSpinComplete();
        }
    }
}
