using DG.Tweening;
using UnityEngine;

namespace Gameplay.Player
{
    // ─────────────────────────────────────────
    // 1. SINGLE SHOT
    // ─────────────────────────────────────────

    // ─────────────────────────────────────────
    // 2. RAPID FIRE — alternates between gun tips
    // ─────────────────────────────────────────
    public class RapidFireCannon : BaseCannon<StraightBullet, StraightBehaviour>
    {
        private int _tipIndex;

        // Rapid fire overrides Shoot() to alternate tips instead of firing all at once
        protected override void Shoot()
        {
            if (gunTips == null || gunTips.Length == 0) return;
            var tip = gunTips[_tipIndex % gunTips.Length];
            if (tip != null) Fire(tip.position, tip.up);
            _tipIndex++;
            PlayFireParticles();
        }

        private void PlayFireParticles()
        {
            if (fireParticles == null) return;
            foreach (var p in fireParticles)
                if (p != null) p.Play();
        }

        protected override void Fire(Vector2 position, Vector2 direction)
        {
            SpawnBullet(position, direction);
        }
    }

    // ─────────────────────────────────────────
    // 3. SHOTGUN — spread pellets per tip
    // ─────────────────────────────────────────
    public class ShotgunCannon : BaseCannon<SpreadBullet, SpreadBehaviour>
    {
        [Header("Shotgun Config")]
        [SerializeField] private int pelletCount = 5;
        [SerializeField] private float spreadAngle = 45f;

        protected override void Fire(Vector2 position, Vector2 direction)
        {
            int count = Mathf.Clamp(CannonStats.currentNumberOfMaxBulletInShot, 3, 9);
            for (int i = 0; i < count; i++)
            {
                float angle = -spreadAngle / 2f + (spreadAngle / (count - 1)) * i;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * direction;
                SpawnBullet(position, dir);
            }
        }
    }

    // ─────────────────────────────────────────
    // 4. DOUBLE BULLET — 2 bullets ±15°
    // ─────────────────────────────────────────
    public class DoubleCannon : BaseCannon<StraightBullet, StraightBehaviour>
    {
        [SerializeField] private float splitAngle = 15f;

        protected override void Fire(Vector2 position, Vector2 direction)
        {
            SpawnBullet(position, Quaternion.Euler(0, 0, -splitAngle) * direction);
            SpawnBullet(position, Quaternion.Euler(0, 0,  splitAngle) * direction);
        }
    }

    // ─────────────────────────────────────────
    // 5. TRIPLE BULLET — 3 bullets -30°, 0°, +30°
    // ─────────────────────────────────────────
    public class TripleCannon : BaseCannon<StraightBullet, StraightBehaviour>
    {
        [SerializeField] private float spreadAngle = 30f;

        protected override void Fire(Vector2 position, Vector2 direction)
        {
            SpawnBullet(position, Quaternion.Euler(0, 0, -spreadAngle) * direction);
            SpawnBullet(position, direction);
            SpawnBullet(position, Quaternion.Euler(0, 0,  spreadAngle) * direction);
        }
    }

    // ─────────────────────────────────────────
    // 6. BIG BARTHA — explosive + screen shake
    // ─────────────────────────────────────────
    public class BigBarthaCannon : BaseCannon<ExplosiveBullet, ExplosiveBehaviour>
    {
        protected override void Fire(Vector2 position, Vector2 direction)
        {
            SpawnBullet(position, direction);
            Camera.main.transform.DOShakePosition(0.15f, 0.2f, 8);
        }
    }

    // ─────────────────────────────────────────
    // 7. LUCKY CANNON — homing + crit chance
    // ─────────────────────────────────────────
    public class LuckyCannon : BaseCannon<HomingBullet, HomingBehaviour>
    {
        [SerializeField] [Range(0f, 1f)] private float critChance = 0.25f;

        protected override void Fire(Vector2 position, Vector2 direction)
        {
            var bullet = SpawnBullet(position, direction);
            if (bullet != null && Random.value < critChance)
                bullet.currentDamage *= 2f;
        }
    }
}
