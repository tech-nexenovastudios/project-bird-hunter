using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Snapshot of a single collision or trigger event on a BaseCannon.
    /// Carries which child part was hit and the final damage already
    /// scaled by that part's damageMultiplier.
    /// </summary>
    public class CannonCollisionData
    {
        /// <summary>The BaseCannon that owns this event.</summary>
        public BaseCannon sourceCannon;

        /// <summary>The specific child part that received the hit.</summary>
        public CannonPartCollider hitPart;

        /// <summary>Shorthand for the part type enum.</summary>
        public CannonPartType HitPartType => hitPart != null ? hitPart.partType : CannonPartType.Body;

        /// <summary>The other GameObject involved.</summary>
        public GameObject otherObject;

        /// <summary>World-space contact point (zero for triggers).</summary>
        public Vector2 contactPoint;

        /// <summary>Surface normal at contact (zero for triggers).</summary>
        public Vector2 contactNormal;

        /// <summary>Relative velocity magnitude at impact (zero for triggers).</summary>
        public float impactForce;

        /// <summary>Type of physics event that produced this data.</summary>
        public CollisionEventType eventType;

        /// <summary>Time.time when the event occurred.</summary>
        public float timestamp;

        /// <summary>
        /// Raw damage from the hazard already multiplied by hitPart.damageMultiplier.
        /// Set by BaseCannon.ApplyPartDamageIfHazard — read this in subclass hooks
        /// if you need to know how much damage was actually dealt.
        /// </summary>
        public int FinalDamage { get; private set; }

        internal void SetFinalDamage(int value) => FinalDamage = value;

        public override string ToString() =>
            $"[CannonCollision] source='{sourceCannon?.name}' | part={HitPartType} " +
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

    /// <summary>
    /// Implement on any hazard that deals damage on contact (e.g. EggDamage).
    /// BaseCannon reads DamageAmount and scales it by the hit part's multiplier.
    /// </summary>
    public interface IDamageSource
    {
        int DamageAmount { get; }
    }
}
