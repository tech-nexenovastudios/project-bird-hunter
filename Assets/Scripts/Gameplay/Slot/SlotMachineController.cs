//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using DG.Tweening;
//using Gameplay.Managers;
//using Gameplay.PowerUps;

//namespace Gameplay.Slot
//{
//    public class SlotMachineController : MonoBehaviour
//    {
//        [SerializeField] private ReelController[] reels;  // Assign 3 reels
//        [SerializeField] private List<PowerupConfig> symbolLibrary;  // All icons

//        void Start()
//        {
//            foreach (var reel in reels)
//            {
//                reel.symbolList = symbolLibrary;  // Share library
//                reel.InitializeReel();
//            }
//        }

//        // Call this with your predetermined results!
//        public void Spin(List<PowerupConfig> resultsPerReel)
//        {
//            for (int i = 0; i < reels.Length; i++)
//            {
//                var reel       = reels[i];
//                var reelResult = resultsPerReel[i];
//                var delay = i * 0.12f;
//                var i1 = i;

//                reel.OnPowerupSelected = null;
//                reel.OnPowerupSelected += OnPowerupSelected;

//                DOVirtual.DelayedCall(delay, () => reels[i1].SpinToResult(reelResult));
//            }
//        }

//        private void OnPowerupSelected(PowerupConfig selected)
//        {
//            GameProgressManager.Instance.PlayerSelectedPowerup(selected);
//            foreach (var reel in reels)
//            {
//                reel.OnPowerupSelected = null;
//            }
//            Managers.GameManager.Instance.OnSpinComplete();
//        }

//        private void OnDisable()
//        {
//            DOTween.KillAll();
//        }
//    }

//}
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
                reel.OnPowerupSelected = null;

            DOTween.KillAll();
        }

        // ───────── Spin ─────────
        private void Spin(List<PowerupConfig> resultsPerReel)
        {
            for (int i = 0; i < reels.Length; i++)
            {
                var result = resultsPerReel[i];
                var index = i;

                reels[i].OnPowerupSelected = null;
                reels[i].OnPowerupSelected += OnReelStopped;

                DOVirtual.DelayedCall(i * 0.12f, () => reels[index].SpinToResult(result));
            }
        }

        // ───────── Reel stops ─────────
        private void OnReelStopped(PowerupConfig selected)
        {
            foreach (var reel in reels)
                reel.OnPowerupSelected = null;

            GameEvents.FirePowerupSelected(selected);
        }

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