using Gameplay.Interfaces;
using UnityEngine;

namespace Gameplay.Birds
{
    // ═══════════════════════════════════════════════════════════════════════
    // LASER BIRD
    // Fires a laser beam straight downward at the cannon's X position.
    // ═══════════════════════════════════════════════════════════════════════
    public class LaserBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            // Spawn laser beam directly below this bird
            var laser = Instantiate(
                Config.projectilePrefab,
                transform.position,
                Quaternion.identity);

            // The laser prefab is responsible for travelling downward and
            // dealing damage on collision with the cannon layer.
            Debug.Log($"[LaserBird] Fired laser at {transform.position}");
        }

        protected override void OnMovementTick()
        {
            // Slow horizontal patrol
            transform.Translate(Vector3.left * (Config.moveSpeed * Time.deltaTime));
            if (transform.position.x < ScreenBounds.minX)
            {
                var pos   = transform.position;
                pos.x     = ScreenBounds.maxX;
                transform.position = pos;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BOMB BIRD
    // Drops a bomb that explodes on ground impact — uniform AoE damage within
    // 12.5% of horizontal screen width (driven by aoeScreenFraction in config).
    // ═══════════════════════════════════════════════════════════════════════
    public class BombBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            var bomb = Instantiate(
                Config.projectilePrefab,
                transform.position,
                Quaternion.identity);

            // Pass AoE data to bomb via a component (BombProjectile expected on prefab).
            if (bomb.TryGetComponent<BombProjectile>(out var bp))
            {
                float screenWidth = (ScreenBounds.maxX - ScreenBounds.minX);
                bp.Init(screenWidth * Config.aoeScreenFraction, transform.position);
            }

            Debug.Log($"[BombBird] Dropped bomb, AoE radius = {Config.aoeScreenFraction * 100f:F0}% screen");
        }

        protected override void OnMovementTick()
        {
            // Random horizontal drift — updated lazily via a direction
            transform.Translate(Vector3.left * (Config.moveSpeed * 0.5f * Time.deltaTime));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GUN BIRD
    // Fires bullets at ±45° from vertical; moves randomly in environment.
    // Bullets reflect off left/right walls.
    // ═══════════════════════════════════════════════════════════════════════
    public class GunBird : AttackingBird
    {
        private Vector2 _velocity;

        protected override void OnInit()
        {
            // Start with a random horizontal direction
            float angle = Random.Range(-60f, 60f);
            _velocity   = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * Config.moveSpeed;
        }

        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            // Two bullets: +45° and -45° from straight down
            SpawnBullet(new Vector2( 1f, -1f).normalized);
            SpawnBullet(new Vector2(-1f, -1f).normalized);
        }

        private void SpawnBullet(Vector2 dir)
        {
            var go = Instantiate(Config.projectilePrefab, transform.position, Quaternion.identity);
            if (go.TryGetComponent<ReflectingBullet>(out var rb))
                rb.Init(dir, Config.dotDamagePerSecond);
        }

        protected override void OnMovementTick()
        {
            // Bounce off left/right screen edges
            var pos = transform.position;
            pos    += (Vector3)_velocity * Time.deltaTime;

            if (pos.x < ScreenBounds.minX || pos.x > ScreenBounds.maxX)
                _velocity.x = -_velocity.x;

            pos.x               = Mathf.Clamp(pos.x, ScreenBounds.minX, ScreenBounds.maxX);
            transform.position  = pos;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ARROW BIRD
    // Fires an arrow directly at the cannon's current position each interval.
    // ═══════════════════════════════════════════════════════════════════════
    public class ArrowBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            // Find the cannon (tagged "Cannon" in the scene)
            var cannonGO = GameObject.FindWithTag("Cannon");
            if (cannonGO == null) return;

            Vector2 dir = (cannonGO.transform.position - transform.position).normalized;
            float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var arrow = Instantiate(
                Config.projectilePrefab,
                transform.position,
                Quaternion.Euler(0f, 0f, angle));

            if (arrow.TryGetComponent<ArrowProjectile>(out var ap))
                ap.Init(dir);

            Debug.Log($"[ArrowBird] Arrow fired toward {cannonGO.transform.position}");
        }

        protected override void OnMovementTick()
        {
            // Hovers in place with a small vertical bob
            transform.position += Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.5f * Time.deltaTime);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SUICIDE BIRD
    // Acts like a generic egg-dropping bird until HP ≤ 50%, then dives
    // straight at the cannon.
    // ═══════════════════════════════════════════════════════════════════════
    public class SuicideBird : AttackingBird
    {
        private bool    _diving;
        private Vector2 _diveDir;

        protected override void OnAttackTick()
        {
            // Pre-dive: nothing special (eggs handled by SpawnController if
            // this bird is also registered as an egg-layer). Post-dive: just dive.
        }

        protected override void OnHpThresholdReached()
        {
            _diving = true;
            var cannonGO = GameObject.FindWithTag("Cannon");
            if (cannonGO != null)
                _diveDir = (cannonGO.transform.position - transform.position).normalized;
            else
                _diveDir = Vector2.down;

            Debug.Log("[SuicideBird] HP ≤ 50% — diving at cannon!");
        }

        protected override void OnMovementTick()
        {
            if (!_diving) return;

            // High-speed dive toward cannon
            transform.position +=
                (Vector3)_diveDir * (Config.moveSpeed * 3f * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_diving) return;
            if (!other.CompareTag("Cannon")) return;

            // Deal impact damage to cannon
            if (other.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(Mathf.RoundToInt(MaxHp * 0.5f));

            ForceKill();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // JUGGERNAUT BIRD
    // Folds wings and spins on its own axis; zig-zags at 45° from top to bottom.
    // ═══════════════════════════════════════════════════════════════════════
    public class JuggernautBird : AttackingBird
    {
        private Vector2 _direction = new Vector2(1f, -1f).normalized; // 45° initially

        protected override void OnInit()
        {
            // Start from a random top-edge X position moving diagonally
            bool  goRight = Random.value > 0.5f;
            float startX  = goRight ? ScreenBounds.minX : ScreenBounds.maxX;
            float startY  = ScreenBounds.maxY;
            transform.position = new Vector3(startX, startY, 0f);
            _direction         = new Vector2(goRight ? 1f : -1f, -1f).normalized;
        }

        protected override void OnAttackTick()
        {
            // No projectile — the bird itself is the hazard (contact damage handled in OnTriggerEnter2D)
        }

        protected override void OnMovementTick()
        {
            // Spin on own axis
            transform.Rotate(0f, 0f, 720f * Time.deltaTime);

            // Zig-zag movement
            transform.position +=
                (Vector3)_direction * (Config.moveSpeed * Time.deltaTime);

            // Bounce off left/right walls
            if (transform.position.x < ScreenBounds.minX || transform.position.x > ScreenBounds.maxX)
            {
                _direction.x = -_direction.x;
                var clamped  = transform.position;
                clamped.x    = Mathf.Clamp(clamped.x, ScreenBounds.minX, ScreenBounds.maxX);
                transform.position = clamped;
            }

            // Despawn when off the bottom of the screen
            if (transform.position.y < ScreenBounds.minY)
                ForceKill();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var damageable))
                damageable.TakeDamage(Config.baseHp / 5);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ICE BIRD
    // Releases ice eggs; cannon is frozen for statusDuration seconds on hit.
    // ═══════════════════════════════════════════════════════════════════════
    public class IceBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            var iceEgg = Instantiate(Config.projectilePrefab, transform.position, Quaternion.identity);
            if (iceEgg.TryGetComponent<StatusProjectile>(out var sp))
                sp.Init(StatusEffect.Freeze, Config.statusDuration, 0f);
        }

        protected override void OnMovementTick()
        {
            transform.Translate(Vector3.left * (Config.moveSpeed * Time.deltaTime));
            if (transform.position.x < ScreenBounds.minX)
            {
                var p = transform.position;
                p.x = ScreenBounds.maxX;
                transform.position = p;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VENOM BIRD
    // Throws venom packets that spread over 20% of ground length.
    // While the cannon is in the venom zone it takes continuous damage.
    // ═══════════════════════════════════════════════════════════════════════
    public class VenomBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            var packet = Instantiate(Config.projectilePrefab, transform.position, Quaternion.identity);

            float groundWidth     = (ScreenBounds.maxX - ScreenBounds.minX) * Config.aoeScreenFraction;
            float groundY         = ScreenBounds.minY + 0.5f; // approximate ground level
            float spawnX          = Random.Range(ScreenBounds.minX, ScreenBounds.maxX);

            packet.transform.position = new Vector3(spawnX, transform.position.y, 0f);

            if (packet.TryGetComponent<GroundHazard>(out var gh))
                gh.Init(groundWidth, groundY, Config.dotDamagePerSecond, Config.statusDuration);
        }

        protected override void OnMovementTick()
        {
            transform.Translate(Vector3.right * (Config.moveSpeed * 0.5f * Time.deltaTime));
            if (transform.position.x > ScreenBounds.maxX)
            {
                var p = transform.position;
                p.x = ScreenBounds.minX;
                transform.position = p;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LIGHTNING BIRD
    // Strikes a random screen X position with a lightning bolt straight down.
    // ═══════════════════════════════════════════════════════════════════════
    public class LightningBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            float strikeX  = Random.Range(ScreenBounds.minX, ScreenBounds.maxX);
            var   strikePos = new Vector3(strikeX, transform.position.y, 0f);

            var bolt = Instantiate(Config.projectilePrefab, strikePos, Quaternion.identity);
            if (bolt.TryGetComponent<LightningBolt>(out var lb))
                lb.Init(Config.dotDamagePerSecond);

            Debug.Log($"[LightningBird] Strike at x={strikeX:F1}");
        }

        protected override void OnMovementTick()
        {
            // Erratic horizontal movement
            float noise = Mathf.PerlinNoise(Time.time * 0.7f, 0f) * 2f - 1f; // -1..1
            transform.Translate(new Vector3(noise * Config.moveSpeed * Time.deltaTime, 0f, 0f));
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, ScreenBounds.minX, ScreenBounds.maxX);
            transform.position = p;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ICE FROST BIRD
    // Deposits an ice sheet covering 20% of ground length — slows cannon movement.
    // ═══════════════════════════════════════════════════════════════════════
    public class IceFrostBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            float zoneWidth = (ScreenBounds.maxX - ScreenBounds.minX) * Config.aoeScreenFraction;
            float groundY   = ScreenBounds.minY + 0.5f;
            float centerX   = Random.Range(
                ScreenBounds.minX + zoneWidth * 0.5f,
                ScreenBounds.maxX - zoneWidth * 0.5f);

            var frost = Instantiate(
                Config.projectilePrefab,
                new Vector3(centerX, groundY, 0f),
                Quaternion.identity);

            if (frost.TryGetComponent<GroundHazard>(out var gh))
                gh.Init(zoneWidth, groundY, 0f, Config.statusDuration, StatusEffect.Slow);
        }

        protected override void OnMovementTick()
        {
            transform.Translate(Vector3.left * (Config.moveSpeed * Time.deltaTime));
            if (transform.position.x < ScreenBounds.minX)
            {
                var p = transform.position;
                p.x = ScreenBounds.maxX;
                transform.position = p;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SOUL THROWER BIRD
    // Releases multiple souls that fall on random winding paths.
    // ═══════════════════════════════════════════════════════════════════════
    public class SoulThrowerBird : AttackingBird
    {
        private static readonly int SoulCount = 5;

        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            for (int i = 0; i < SoulCount; i++)
            {
                var offset = new Vector3(Random.Range(-1f, 1f), 0f, 0f);
                var soul   = Instantiate(Config.projectilePrefab,
                                         transform.position + offset,
                                         Quaternion.identity);

                if (soul.TryGetComponent<SoulProjectile>(out var sp))
                    sp.Init(Config.dotDamagePerSecond);
            }
        }

        protected override void OnMovementTick()
        {
            // Slow drift left
            transform.Translate(Vector3.left * (Config.moveSpeed * 0.4f * Time.deltaTime));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SPINNING BIRD
    // Rotates on its axis and dives toward the ground at 45° at high speed.
    // ═══════════════════════════════════════════════════════════════════════
    public class SpinningBird : AttackingBird
    {
        private Vector2 _diveDir;

        protected override void OnInit()
        {
            bool goRight = Random.value > 0.5f;
            _diveDir     = new Vector2(goRight ? 1f : -1f, -1f).normalized;
        }

        protected override void OnAttackTick()
        {
            // Pure movement hazard — no projectile needed
        }

        protected override void OnMovementTick()
        {
            // Spin and dive
            transform.Rotate(0f, 0f, 900f * Time.deltaTime);
            transform.position += (Vector3)_diveDir * (Config.moveSpeed * 2.5f * Time.deltaTime);

            if (transform.position.y < ScreenBounds.minY)
                ForceKill();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(Config.baseHp / 4);
            ForceKill();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FIRE THROWER BIRD
    // Drops fire eggs; cannon burns for statusDuration seconds on hit (DoT).
    // ═══════════════════════════════════════════════════════════════════════
    public class FireThrowerBird : AttackingBird
    {
        protected override void OnAttackTick()
        {
            if (Config.projectilePrefab == null) return;

            var egg = Instantiate(Config.projectilePrefab, transform.position, Quaternion.identity);
            if (egg.TryGetComponent<StatusProjectile>(out var sp))
                sp.Init(StatusEffect.Burn, Config.statusDuration, Config.dotDamagePerSecond);
        }

        protected override void OnMovementTick()
        {
            transform.Translate(Vector3.left * (Config.moveSpeed * Time.deltaTime));
            if (transform.position.x < ScreenBounds.minX)
            {
                var p = transform.position;
                p.x = ScreenBounds.maxX;
                transform.position = p;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BEE HURDLE BIRD
    // Spawns a 3×3 grid of bees that fly together at 30° from vertical,
    // top to bottom, dealing damage on contact with the cannon.
    // ═══════════════════════════════════════════════════════════════════════
    public class BeeHurdleBird : AttackingBird
    {
        // Individual bee wrapper (lives inside the formation)
        private class Bee
        {
            public GameObject go;
            public bool       alive = true;
        }

        private Bee[]   _bees;
        private Vector2 _groupVelocity;
        private bool    _formed;

        protected override void OnInit()
        {
            // Build 3×3 formation around this transform
            int cols = 3, rows = 3;
            _bees = new Bee[cols * rows];

            float spacing = 0.6f;
            int   idx     = 0;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                Vector3 localOffset = new Vector3(
                    (c - 1) * spacing,
                    (r - 1) * spacing,
                    0f);

                _bees[idx] = new Bee();

                if (Config.prefab != null)
                {
                    _bees[idx].go = Instantiate(
                        Config.prefab,
                        transform.position + localOffset,
                        Quaternion.identity,
                        transform);       // child of formation root
                }
                idx++;
            }

            // Descent direction: 30° from vertical
            float angle    = Config.beeDescentAngle * Mathf.Deg2Rad;
            bool  goRight  = Random.value > 0.5f;
            _groupVelocity = new Vector2(
                Mathf.Sin(angle) * (goRight ? 1f : -1f),
                -Mathf.Cos(angle)) * Config.beeGroupSpeed;

            _formed = true;
        }

        protected override void OnAttackTick()
        {
            // No extra projectile — bees do contact damage
        }

        protected override void OnMovementTick()
        {
            if (!_formed) return;

            // Move entire formation
            transform.position +=
                (Vector3)_groupVelocity * Time.deltaTime;

            // Exit when below screen
            if (transform.position.y < ScreenBounds.minY)
                ForceKill();
        }

        // Each individual bee GameObject should have a BeeContactDamager component
        // that calls the cannon's TakeDamage on collision.
    }

    // ═══════════════════════════════════════════════════════════════════════
    // STUB PROJECTILE / HAZARD COMPONENTS
    // These are intentionally minimal — replace with full implementations
    // once the corresponding prefabs and cannon-status system are ready.
    // ═══════════════════════════════════════════════════════════════════════

    public enum StatusEffect { None, Freeze, Burn, Slow }

    /// <summary>A projectile that applies a status effect on cannon contact.</summary>
    public class StatusProjectile : MonoBehaviour
    {
        private StatusEffect _effect;
        private float        _duration;
        private float        _dps;

        public void Init(StatusEffect effect, float duration, float dps)
        {
            _effect   = effect;
            _duration = duration;
            _dps      = dps;
            Destroy(gameObject, 5f); // auto-cleanup
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            // TODO: other.GetComponent<CannonStatusController>()?.ApplyStatus(_effect, _duration, _dps);
            Debug.Log($"[StatusProjectile] Applied {_effect} to cannon for {_duration}s");
            Destroy(gameObject);
        }

        private void Update() => transform.Translate(Vector3.down * (8f * Time.deltaTime));
    }

    /// <summary>A ground zone that persists for a duration, applying a status to the cannon while inside.</summary>
    public class GroundHazard : MonoBehaviour
    {
        private float        _width;
        private float        _dps;
        private StatusEffect _effect;

        public void Init(float width, float groundY, float dps, float duration,
                         StatusEffect effect = StatusEffect.None)
        {
            _width  = width;
            _dps    = dps;
            _effect = effect;
            transform.localScale = new Vector3(width, 0.4f, 1f);
            Destroy(gameObject, duration);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (_dps > 0f && other.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(Mathf.RoundToInt(_dps * Time.deltaTime));
            // TODO: apply slow status via CannonStatusController
        }
    }

    /// <summary>Bomb payload — AoE damage on landing.</summary>
    public class BombProjectile : MonoBehaviour
    {
        private float   _radius;
        private Vector3 _origin;

        public void Init(float radius, Vector3 origin)
        {
            _radius = radius;
            _origin = origin;
            Destroy(gameObject, 6f);
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!col.gameObject.CompareTag("Ground")) return;

            // Overlap circle for AoE
            var hits = Physics2D.OverlapCircleAll(transform.position, _radius);
            foreach (var h in hits)
            {
                if (h.CompareTag("Cannon") && h.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(30); // TODO: drive from config
            }
            Destroy(gameObject);
        }
    }

    /// <summary>Bullet that reflects off left/right screen edges.</summary>
    public class ReflectingBullet : MonoBehaviour
    {
        private Vector2 _dir;
        private float   _damage;

        public void Init(Vector2 direction, float damage)
        {
            _dir    = direction;
            _damage = damage;
            Destroy(gameObject, 5f);
        }

        private void Update()
        {
            transform.position += (Vector3)_dir * (10f * Time.deltaTime);
            float x = transform.position.x;
            if (x < ScreenBounds.minX || x > ScreenBounds.maxX)
                _dir.x = -_dir.x;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(Mathf.RoundToInt(_damage));
            Destroy(gameObject);
        }
    }

    /// <summary>Arrow projectile — travels in a fixed direction.</summary>
    public class ArrowProjectile : MonoBehaviour
    {
        private Vector2 _dir;
        public void Init(Vector2 dir) { _dir = dir; Destroy(gameObject, 4f); }
        private void Update() => transform.position += (Vector3)_dir * (12f * Time.deltaTime);
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var d)) d.TakeDamage(20);
            Destroy(gameObject);
        }
    }

    /// <summary>Lightning bolt — instant-damage column at spawn position.</summary>
    public class LightningBolt : MonoBehaviour
    {
        private float _damage;
        public void Init(float damage) { _damage = damage; Destroy(gameObject, 0.5f); }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(Mathf.RoundToInt(_damage));
        }
    }

    /// <summary>Soul projectile — wanders on random path downward.</summary>
    public class SoulProjectile : MonoBehaviour
    {
        private float _damage;
        private float _noiseOffset;
        public void Init(float damage) { _damage = damage; _noiseOffset = Random.Range(0f, 100f); Destroy(gameObject, 6f); }
        private void Update()
        {
            float xDrift = (Mathf.PerlinNoise(_noiseOffset, Time.time * 0.8f) * 2f - 1f) * 2f;
            transform.position += new Vector3(xDrift, -3f, 0f) * Time.deltaTime;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Cannon")) return;
            if (other.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(Mathf.RoundToInt(_damage));
            Destroy(gameObject);
        }
    }
}