using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Gameplay.Events;
using Gameplay.Managers;
using Gameplay.PowerUps;
using Gameplay.UI;

namespace Gameplay.Slot
{
    public class SlotMachineController : MonoBehaviour
    {
        [SerializeField] private ReelController[] reels;  // Assign 3 reels
        [SerializeField] private List<PowerupConfig> symbolLibrary;  // All icons

        void Start()
        {
            foreach (var reel in reels)
            {
                reel.symbolList = symbolLibrary;  // Share library
                reel.InitializeReel();
            }
        }

        // Call this with your predetermined results!
        public void Spin(List<PowerupConfig> resultsPerReel)
        {
            for (int i = 0; i < reels.Length; i++)
            {
                var reel       = reels[i];
                var reelResult = resultsPerReel[i];
                var delay = i * 0.12f;
                var i1 = i;
                
                reel.OnPowerupSelected = null;
                reel.OnPowerupSelected += OnPowerupSelected;
                
                DOVirtual.DelayedCall(delay, () => reels[i1].SpinToResult(reelResult));
            }
        }
        
        private void OnPowerupSelected(PowerupConfig selected)
        {
            GameProgressManager.Instance.PlayerSelectedPowerup(selected);
            foreach (var reel in reels)
            {
                reel.OnPowerupSelected = null;
            }
            GameProgressManager.Instance.ApplyPowerUpsToCannon();
            GameEvents.FireSpinComplete(); 
        }

        private void OnDisable()
        {
            DOTween.KillAll();
        }
    }

}