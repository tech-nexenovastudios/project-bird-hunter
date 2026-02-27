using UnityEngine;

namespace Gameplay.Levels
{
    [CreateAssetMenu(menuName = "BirdHunter/Level Profile")]
    public class LevelProfile : ScriptableObject
    {
        public int chapter;
        public int globalLevel;           // 1–600

        public int targetScore;
        public float minDuration = 45f;
        public float maxDuration = 90f;

        public int pressureMax;
        public int pressureAvg;
        public int maxE4;
        public int maxE3;
        public int maxE2;

        public float spawnIntervalMin;
        public float spawnIntervalMax;

        public int expectedDps;
        public float hpMultiplier = 1f;   // from spreadsheet
    }

}