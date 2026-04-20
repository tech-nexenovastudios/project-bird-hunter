using UnityEngine;
using BirdHunter.Inventory.Stats;
using Gameplay.Interfaces;
using Gameplay.Player;

namespace Gameplay.PowerUps
{
    // ═════════════════════════════════════════════════════════
    //  SHADOW CANNON
    //
    //  Greyscale clone of the player's cannon. Follows the
    //  original with a smooth delay (Ball Blast style).
    //  Fires the same bullets at x% of original damage.
    //
    //  NOT parented to the cannon — moves independently
    //  by lerping toward the cannon's position each frame.
    // ═════════════════════════════════════════════════════════

    public class ShadowCannon : MonoBehaviour
    {
        private ICannon sourceCannon;
        private BaseCannon sourceCannonMb;
        private float damagePercent;
        private float fireTimer;

        [Header("Follow Settings")]
        private float followSpeed = 5f;
        private Vector3 offset;

        private Transform[] gunTips;
        private GameObject bulletPrefab;

        public void Init(ICannon cannon, float damagePercent, Vector3 offset, float followSpeed)
        {
            sourceCannon = cannon;
            sourceCannonMb = cannon as BaseCannon;
            this.damagePercent = damagePercent;
            this.offset = offset;
            this.followSpeed = followSpeed;

            if (sourceCannonMb == null) return;

            bulletPrefab = sourceCannonMb.BulletPrefab;
            gunTips = FindGunTips();
            ApplyGreyscale();
        }

        private void Update()
        {
            if (sourceCannon == null || sourceCannonMb == null) return;
            if (!sourceCannon.IsAlive) return;

            FollowCannon();
            HandleFiring();
        }

        // ═══════════════════════════════════════════════════════
        //  FOLLOW — smooth lerp toward cannon + offset
        // ═══════════════════════════════════════════════════════

        private void FollowCannon()
        {
            Vector3 targetPos = sourceCannon.Transform.position + offset;

            // Only follow on X axis — keep same Y (ground level)
            targetPos.y = transform.position.y;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                followSpeed * Time.deltaTime
            );
        }

        // ═══════════════════════════════════════════════════════
        //  FIRING — mirrors source cannon's fire rate
        // ═══════════════════════════════════════════════════════

        private void HandleFiring()
        {
            if (sourceCannonMb.Stats == null || bulletPrefab == null) return;
            if (!sourceCannon.IsFiring) return;

            fireTimer += Time.deltaTime;
            float interval = 1f / sourceCannonMb.Stats.Get(StatType.FireRate);

            if (fireTimer >= interval)
            {
                Shoot();
                fireTimer = 0f;
            }
        }

        private void Shoot()
        {
            if (gunTips != null && gunTips.Length > 0)
            {
                for (int i = 0; i < gunTips.Length; i++)
                    if (gunTips[i] != null)
                        SpawnBullet(gunTips[i].position, gunTips[i].rotation);
            }
            else
            {
                SpawnBullet(transform.position, transform.rotation);
            }
        }

        private void SpawnBullet(Vector3 position, Quaternion rotation)
        {
            GameObject go = Instantiate(bulletPrefab, position, rotation);
            BaseBullet bullet = go.GetComponent<BaseBullet>();
            if (bullet == null) return;

            bullet.bulletSpeed = sourceCannonMb.Stats.Get(StatType.BulletSpeed);
            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(sourceCannon.CurrentAttack * damagePercent / 100f));

            if (bullet is BouncingBullet bouncing)
                bouncing.bounceCount = sourceCannonMb.Stats.GetInt(StatType.BulletBounce);

            Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = (Vector2)(rotation * Vector2.up) * bullet.bulletSpeed;
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private Transform[] FindGunTips()
        {
            Transform[] all = GetComponentsInChildren<Transform>();
            var found = new System.Collections.Generic.List<Transform>();

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == transform) continue;
                string lower = all[i].name.ToLower();
                if (lower.Contains("guntip") || lower.Contains("tip") ||
                    lower.Contains("muzzle") || lower.Contains("firepoint"))
                    found.Add(all[i]);
            }

            return found.Count > 0 ? found.ToArray() : null;
        }

        private void ApplyGreyscale()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            Color shadowColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = shadowColor;
        }
    }
}