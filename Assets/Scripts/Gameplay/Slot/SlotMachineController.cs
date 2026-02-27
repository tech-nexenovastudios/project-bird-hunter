using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Gameplay.PowerUps;

namespace Gameplay.Slot
{
    public class SlotMachineController : MonoBehaviour
    {
        [SerializeField] private ReelController[] reels;  // Assign 3 reels
        [SerializeField] private List<Sprite> symbolLibrary;  // All icons

        void Start()
        {
            foreach (var reel in reels)
            {
                reel.symbolSprites = symbolLibrary;  // Share library
                reel.InitializeReel();
            }
        }

        // Call this with your predetermined results!
        public void Spin(List<PowerupConfig> resultsPerReel)
        {
            for (int i = 0; i < reels.Length; i++)
            {
                var reelResult = resultsPerReel[i];
                var delay = i * 0.12f;
                var i1 = i;
                DOVirtual.DelayedCall(delay, () => reels[i1].SpinToResult(reelResult));
            }
        }

        // Example usage from your economy/RNG
        [ContextMenu("Test Spin")]
        public void TestSpin()
        {
            var results = new List<Sprite>
            {
                symbolLibrary[0],  // Reel 0
                symbolLibrary[2],  // Reel 1  
                symbolLibrary[8]   // Reel 2 (win!)
            };
            //Spin(results);
        }
    }

}