using UnityEngine;

namespace Gameplay.Player
{
    /// <summary>
    /// Attach to every child GameObject that has a Collider2D.
    /// Forwards all 2D physics events to the parent BaseCannon via ICannonBase.
    /// Non-generic — no bullet type needed here.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CannonPartCollider : MonoBehaviour
    {
        [Tooltip("Which logical section of the cannon this collider represents.")]
        public CannonPartType partType = CannonPartType.Body;

        [Tooltip("Damage multiplier applied when this part is hit.\n" +
                 "1.0 = normal | 2.0 = weak spot | 0.5 = armoured")]
        [Range(0f, 5f)]
        public float damageMultiplier = 1f;

        // Auto-resolved — no manual wiring needed
        [HideInInspector] public ICannonBase ownerCannon;

        private void Awake()
        {
            // GetComponentInParent works with interface via MonoBehaviour cast
            ownerCannon = GetComponentInParent<MonoBehaviour>() as ICannonBase;

            if (ownerCannon == null)
                Debug.LogWarning($"[CannonPartCollider] '{gameObject.name}' " +
                                 "could not find a BaseCannon in parent hierarchy.", this);
        }

        private void OnCollisionEnter2D(Collision2D col) =>
            ownerCannon?.ReceivePartCollisionEnter(this, col);

        private void OnCollisionExit2D(Collision2D col) =>
            ownerCannon?.ReceivePartCollisionExit(this, col);

        private void OnTriggerEnter2D(Collider2D other) =>
            ownerCannon?.ReceivePartTriggerEnter(this, other);

        private void OnTriggerExit2D(Collider2D other) =>
            ownerCannon?.ReceivePartTriggerExit(this, other);
    }
}