using UnityEngine;
using Gameplay.Interfaces;
using Gameplay.Player;

namespace Gameplay.PowerUps
{
    // ═════════════════════════════════════════════════════════
    //  LASER EFFECT
    //
    //  Replaces normal bullet firing with a continuous laser beam.
    //  While active: suppresses bullet spawning, raycasts upward,
    //  damages all Bird/Egg tagged objects in the beam path.
    //
    //  Lifecycle:  Cooldown ready → Player fires → Laser ON for
    //              fireDuration → Laser OFF → Cooldown timer →
    //              repeat.
    //
    //  Setup: Attach a LineRenderer to the cannon (or child).
    //         The script finds it automatically at runtime.
    // ═════════════════════════════════════════════════════════

    [System.Serializable]
    public class LaserEffect : IProjectileModifier
    {
        [Header("Timing")]
        [Tooltip("How long the laser fires (seconds).")]
        [Min(0.1f)] public float fireDuration = 5f;

        [Tooltip("Cooldown after laser ends before it can fire again.")]
        [Min(0.1f)] public float cooldown = 20f;

        [Header("Damage")]
        [Tooltip("Damage applied per second to each target in the beam.")]
        [Min(1)] public int damagePerSecond = 20;

        [Header("Beam Settings")]
        [Tooltip("Maximum beam length (world units).")]
        [Min(1f)] public float maxBeamLength = 30f;

        [Tooltip("Beam width for overlap detection.")]
        [Min(0.05f)] public float beamWidth = 0.3f;

        // ── State ────────────────────────────────────────────
        public bool IsActive { get; private set; }
        public int ExtraProjectiles => 0;

        private ICannon cannon;
        private LineRenderer lineRenderer;
        private LaserPhase phase = LaserPhase.Ready;
        private float phaseTimer;

        // Raycast filter — excludes the cannon's own layer
        private ContactFilter2D rayFilter;
        private readonly RaycastHit2D[] rayResults = new RaycastHit2D[4];

        // Pre-allocated buffer for Physics2D overlap to avoid GC
        private static readonly Collider2D[] hitBuffer = new Collider2D[32];

        private enum LaserPhase
        {
            Ready,
            Firing,
            OnCooldown
        }

        // ═══════════════════════════════════════════════════════
        //  IProjectileModifier interface
        // ═══════════════════════════════════════════════════════

        public void Activate()
        {
            IsActive = true;
            phase = LaserPhase.Ready;
            phaseTimer = 0f;
        }

        public void Deactivate()
        {
            IsActive = false;
            phase = LaserPhase.Ready;
            phaseTimer = 0f;
            SetBeamVisible(false);

            if (cannon != null)
            {
                cannon.SuppressBullets = false;
                cannon = null;
            }
            lineRenderer = null;
        }

        public void ModifyBullet(BaseBullet bullet, int cannonAttack) { }
        public float[] GetExtraAngles() => System.Array.Empty<float>();

        // ═══════════════════════════════════════════════════════
        //  CORE TICK
        // ═══════════════════════════════════════════════════════

        public void Tick(ICannon cannon)
        {
            if (!IsActive || cannon == null) return;
            this.cannon = cannon;

            // Auto-find LineRenderer on first tick
            if (lineRenderer == null)
            {
                lineRenderer = cannon.Transform.GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    UnityEngine.Debug.LogWarning("[LaserEffect] No LineRenderer found on cannon. Add one to the cannon prefab.");
                    return;
                }
                lineRenderer.enabled = false;

                // Build a raycast filter that ignores the cannon's own layer.
                // This prevents the beam from hitting the cannon's collider
                // and collapsing to zero length.
                int cannonLayer = cannon.Transform.gameObject.layer;
                rayFilter = new ContactFilter2D();
                rayFilter.SetLayerMask(~(1 << cannonLayer));
                rayFilter.useLayerMask = true;
            }

            switch (phase)
            {
                case LaserPhase.Ready:
                    HandleReady();
                    break;

                case LaserPhase.Firing:
                    HandleFiring();
                    break;

                case LaserPhase.OnCooldown:
                    HandleCooldown();
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  PHASE HANDLERS
        // ═══════════════════════════════════════════════════════

        private void HandleReady()
        {
            if (cannon.IsFiring)
            {
                phase = LaserPhase.Firing;
                phaseTimer = fireDuration;
                cannon.SuppressBullets = true;

                // Set beam positions BEFORE making it visible
                // so it doesn't flash at (0,0,0) for one frame
                UpdateBeam();
                SetBeamVisible(true);
            }
        }

        private void HandleFiring()
        {
            phaseTimer -= Time.deltaTime;

            if (phaseTimer <= 0f)
            {
                phase = LaserPhase.OnCooldown;
                phaseTimer = cooldown;
                cannon.SuppressBullets = false;
                SetBeamVisible(false);
                return;
            }

            UpdateBeam();
            DealDamageAlongBeam();
        }

        private void HandleCooldown()
        {
            phaseTimer -= Time.deltaTime;
            if (phaseTimer <= 0f)
            {
                phase = LaserPhase.Ready;
            }
        }

        // ═══════════════════════════════════════════════════════
        //  BEAM RENDERING
        // ═══════════════════════════════════════════════════════

        private void UpdateBeam()
        {
            if (lineRenderer == null || cannon == null) return;

            Vector3 start = cannon.Transform.position;
            Vector3 end = start + Vector3.up * maxBeamLength;

            // Raycast using the filter that excludes the cannon's layer.
            // Without this, the ray hits the cannon's own collider and
            // the beam collapses to zero length (invisible).
            int hitCount = Physics2D.Raycast(start, Vector2.up, rayFilter, rayResults, maxBeamLength);
            if (hitCount > 0)
                end = rayResults[0].point;

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        // ═══════════════════════════════════════════════════════
        //  DAMAGE
        // ═══════════════════════════════════════════════════════

        private void DealDamageAlongBeam()
        {
            if (cannon == null) return;

            Vector3 start = cannon.Transform.position;
            Vector3 end = start + Vector3.up * maxBeamLength;

            Vector2 bottomLeft = new Vector2(start.x - beamWidth * 0.5f, start.y);
            Vector2 topRight = new Vector2(end.x + beamWidth * 0.5f, end.y);

            int count = Physics2D.OverlapAreaNonAlloc(bottomLeft, topRight, hitBuffer);

            float frameDamage = damagePerSecond * Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                Collider2D col = hitBuffer[i];
                if (col == null) continue;

                if (!col.CompareTag("Bird") && !col.CompareTag("Egg")) continue;

                IDamageable target = col.GetComponent<IDamageable>();
                if (target != null && target.IsAlive)
                {
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(frameDamage));
                    target.TakeDamage(dmg);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════

        private void SetBeamVisible(bool visible)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = visible;
        }
    }
}