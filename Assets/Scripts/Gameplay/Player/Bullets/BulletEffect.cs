using UnityEngine;

namespace Gameplay.Player
{
    [CreateAssetMenu(fileName = "New Bullet Effect", menuName = "BirdHunter/Bullet")]
    public class BulletEffect : ScriptableObject
    {
        public GameObject particleSystem;
        public float duration;
    }
}