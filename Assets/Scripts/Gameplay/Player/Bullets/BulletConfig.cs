using UnityEngine;

namespace Gameplay.Player
{
    [CreateAssetMenu(fileName = "New BulletConfig", menuName = "BirdHunter/Bullet Config")]
    public class BulletConfig : ScriptableObject
    {
        [Header("Prefabs")]
        public GameObject bulletPrefab;
        public BulletEffect bulletEffect;
        
        [Header("Base Settings")]
        public float bulletSpeed;
        public float damage;
        public float lifetime = 5f;

        [Header("Hit Impulse")]
        public float maxHitImpulseForce = 8f;
        public float minHitImpulseForce = 2f;
        public float maxForceDistance   = 10f;
        public bool  applyHitImpulse    = true;
    }
}