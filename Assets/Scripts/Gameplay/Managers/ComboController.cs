using UnityEngine;

namespace Gameplay.Managers
{
    /// <summary>
    /// Combo system — disabled for now, kept as stub for future reactivation.
    /// </summary>
    public class ComboController : MonoBehaviour
    {
        public static ComboController Instance { get; private set; }

        public int   ComboCount      => 0;
        public float ComboMultiplier => 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
    }
}