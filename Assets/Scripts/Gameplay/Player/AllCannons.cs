// using DG.Tweening;
// using UnityEngine;
//
// namespace Gameplay.Player
// {
//     // ── 1. RAPID FIRE — alternates gun tips ──────────────────────────────────
//     public class RapidFireCannon : BaseCannon<StraightBullet>
//     {
//         private int _tipIndex;
//
//         protected override void Shoot()
//         {
//             if (gunTips == null || gunTips.Length == 0) return;
//             var tip = gunTips[_tipIndex % gunTips.Length];
//             if (tip != null) Fire(tip.position, tip.up);
//             _tipIndex++;
//             PlayFireParticles();
//         }
//
//         protected override void Fire(Vector2 position, Vector2 direction)
//             => SpawnBullet(position, direction);
//     }
//
//     // ── 2. SHOTGUN — spread pellets per shot ─────────────────────────────────
//     public class ShotgunCannon : BaseCannon<StraightBullet>
//     {
//         [Header("Shotgun Config")]
//         [SerializeField] private float spreadAngle = 45f;
//
//         protected override void Fire(Vector2 position, Vector2 direction)
//         {
//             int count = Mathf.Clamp(CannonStats.currentNumberOfMaxBulletInShot, 3, 9);
//             for (int i = 0; i < count; i++)
//             {
//                 float   angle = -spreadAngle / 2f + (spreadAngle / (count - 1)) * i;
//                 Vector2 dir   = Quaternion.Euler(0, 0, angle) * direction;
//                 SpawnBullet(position, dir);
//             }
//         }
//     }
//
//     // ── 3. DOUBLE — 2 bullets ±angle ─────────────────────────────────────────
//     public class DoubleCannon : BaseCannon<StraightBullet>
//     {
//         [SerializeField] private float splitAngle = 15f;
//
//         protected override void Fire(Vector2 position, Vector2 direction)
//         {
//             SpawnBullet(position, Quaternion.Euler(0, 0, -splitAngle) * direction);
//             SpawnBullet(position, Quaternion.Euler(0, 0,  splitAngle) * direction);
//         }
//     }
//
//     // ── 4. TRIPLE — 3 bullets -30°, 0°, +30° ────────────────────────────────
//     public class TripleCannon : BaseCannon<StraightBullet>
//     {
//         [SerializeField] private float spreadAngle = 30f;
//
//         protected override void Fire(Vector2 position, Vector2 direction)
//         {
//             SpawnBullet(position, Quaternion.Euler(0, 0, -spreadAngle) * direction);
//             SpawnBullet(position, direction);
//             SpawnBullet(position, Quaternion.Euler(0, 0,  spreadAngle) * direction);
//         }
//     }
//
//     // ── 5. BIG BARTHA — explosive + screen shake ─────────────────────────────
//     public class BigBarthaCannon : BaseCannon<ExplosiveBullet>
//     {
//         protected override void Fire(Vector2 position, Vector2 direction)
//         {
//             SpawnBullet(position, direction);
//             Camera.main.transform.DOShakePosition(0.15f, 0.2f, 8);
//         }
//     }
//
//     // ── 6. LUCKY CANNON — homing bullet + crit chance ────────────────────────
//     public class LuckyCannon : BaseCannon<HomingBullet>
//     {
//         [SerializeField, Range(0f, 1f)] private float critChance = 0.25f;
//
//         protected override void Fire(Vector2 position, Vector2 direction)
//         {
//             var bullet = SpawnBullet(position, direction);
//             if (bullet != null && Random.value < critChance)
//                 bullet.SetDamageMultiplier(2f);   // uses new clean API, no public field mutation
//         }
//     }
// }
