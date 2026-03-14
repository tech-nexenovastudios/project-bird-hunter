using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Snapshot of a single collision or trigger event on a BaseCannon.
    /// Non-generic — collision data never needs to know the bullet type.
    /// </summary>
    public class CannonCollisionData
    {
        /// The BaseCannon that owns this event (non-generic base reference)
        public ICannonBase sourceCannon;

        /// The specific child part that received the hit
        public CannonPartCollider hitPart;

        /// Shorthand for part type enum
        public CannonPartType HitPartType => hitPart != null ? hitPart.partType : CannonPartType.Body;

        /// The other GameObject involved
        public GameObject otherObject;

        /// World-space contact point (zero for triggers)
        public Vector2 contactPoint;

        /// Surface normal at contact (zero for triggers)
        public Vector2 contactNormal;

        /// Relative velocity magnitude at impact (zero for triggers)
        public float impactForce;

        /// Type of physics event that produced this data
        public CollisionEventType eventType;

        /// Time.time when the event occurred
        public float timestamp;

        /// Raw damage already scaled by hitPart.damageMultiplier
        public int FinalDamage { get; private set; }
        internal void SetFinalDamage(int value) => FinalDamage = value;

        public override string ToString() =>
            $"[CannonCollision] source='{sourceCannon?.CannonName}' | part={HitPartType} " +
            $"(x{hitPart?.damageMultiplier:F1}) | dmg={FinalDamage} | {eventType} | " +
            $"other='{otherObject?.name}' | t={timestamp:F2}s";
    }

    public enum CollisionEventType
    {
        CollisionEnter,
        CollisionExit,
        TriggerEnter,
        TriggerExit
    }
    public enum CannonPartType
    {
        Body,    // main hull     — 1.0×
        Barrel,  // gun tube      — 1.5×
        Wheel,   // mobility      — 0.75×
        Shield,  // armour        — 0.5×
        Base     // platform      — 1.0×
    }
}
