using Gameplay.Eggs;
using UnityEngine;

namespace Gameplay.Birds
{
    [CreateAssetMenu(menuName = "BirdHunter/Bird Config")]
    public class BirdConfig : ScriptableObject
    {
        [Header("Identity")]
        public string birdId;
        
        [Header("Prefabs")]
        public GameObject birdPrefab;

        [Header("Egg")]
        public EggTierConfig eggTier;
        [Header("Egg Lay Timing")]
        [Tooltip("Fastest possible lay")] public float layIntervalMin = 1.0f;
        [Tooltip("Slowest possible lay")] public float layIntervalMax = 3.5f;

        [Header("Movement")]
        public BirdMovementType movementType;
        public float moveSpeed = 2f;
        public float chaseSpeed = 4f;
        public Vector3[] zigZagPoints;
        public bool useCameraBoundary = true;

        [Header("Flight Physics")]
        public float gravity = -9f;
        public float flapForce = 5f;
        public float heightTolerance = 0.3f;
        public float damping = 0.985f;

        [Header("Lifetime & Health")]
        public float lifetime = 10f;
        public int baseHp = 50;

        [Header("Spawn Weights")]
        [Range(0f, 1f)]
        public float spawnWeight = 0.5f;
        public float difficultyWeight = 1f;
    }
}
