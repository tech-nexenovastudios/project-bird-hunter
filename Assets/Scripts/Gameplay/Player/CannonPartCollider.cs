using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Attach to every child GameObject that has a Collider2D (barrel, body, wheel, etc.).
    /// Forwards all 2D physics events to the parent BaseCannon, carrying the part identity
    /// and damage multiplier so BaseCannon can apply correct per-part damage.
    ///
    /// Setup per child part:
    ///   1. Add a Collider2D  (set Is Trigger = true for egg/hazard detection)
    ///   2. Add this component
    ///   3. Set Part Type and Damage Multiplier in the Inspector
    ///   ownerCannon is found automatically via GetComponentInParent — no manual wiring needed.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CannonPartCollider : MonoBehaviour
    {
        [Tooltip("Which logical section of the cannon this collider represents.")]
        public CannonPartType partType = CannonPartType.Body;

        [Tooltip("Damage multiplier applied when this part is hit.\n" +
                 "1.0 = normal damage  |  2.0 = double (weak spot)  |  0.5 = armoured")]
        [Range(0f, 5f)]
        public float damageMultiplier = 1f;

        // Auto-resolved at Awake — assign manually only if the part lives outside the hierarchy
        [HideInInspector] public BaseCannon ownerCannon;

        private void Awake()
        {
            ownerCannon = GetComponentInParent<BaseCannon>();

            if (ownerCannon == null)
                Debug.LogWarning($"[CannonPartCollider] '{gameObject.name}' could not find " +
                                 "a BaseCannon in its parent hierarchy. Events won't be forwarded.", this);
        }

        // ── Relay — every callback passes 'this' so BaseCannon knows which part was hit ──

        private void OnCollisionEnter2D(Collision2D col)   => ownerCannon?.ReceivePartCollisionEnter(this, col);
        private void OnCollisionExit2D(Collision2D col)    => ownerCannon?.ReceivePartCollisionExit(this, col);
        private void OnTriggerEnter2D(Collider2D other)    => ownerCannon?.ReceivePartTriggerEnter(this, other);
        private void OnTriggerExit2D(Collider2D other)     => ownerCannon?.ReceivePartTriggerExit(this, other);
    }

    /// <summary>
    /// Logical sections of a cannon. Extend this enum as your art grows.
    /// Each value maps to a damageMultiplier set on CannonPartCollider.
    /// </summary>
    public enum CannonPartType
    {
        Body,       // main hull  — default multiplier (1.0)
        Barrel,     // gun tube   — suggest 1.5× (exposed weak spot)
        Wheel,      // mobility   — suggest 0.75× (less critical)
        Shield,     // armour     — suggest 0.5×  (protected)
        Base        // platform   — suggest 1.0×
    }
}
