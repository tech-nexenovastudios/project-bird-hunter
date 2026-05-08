using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay.Input
{
    /// <summary>
    /// Bridges the generated PlayerInputAction asset to gameplay code.
    /// Exposes IsFiring and PointerPosition for polling callers, plus
    /// FireStarted / FireCanceled events for edge-driven listeners.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private static InputHandler _instance;
        public static InputHandler Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindAnyObjectByType<InputHandler>();
                if (_instance != null) return _instance;
                var go = new GameObject(nameof(InputHandler));
                _instance = go.AddComponent<InputHandler>();
                return _instance;
            }
        }

        public event Action FireStarted;
        public event Action FireCanceled;

        public bool IsFiring { get; private set; }

        public Vector2 PointerPosition =>
            Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;

        private PlayerInputAction _actions;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _actions = new PlayerInputAction();
            _actions.Player.Fire.started  += OnFireStarted;
            _actions.Player.Fire.canceled += OnFireCanceled;
        }

        private void OnEnable()  => _actions?.Player.Enable();
        private void OnDisable() => _actions?.Player.Disable();

        private void OnDestroy()
        {
            if (_instance != this) return;
            if (_actions != null)
            {
                _actions.Player.Fire.started  -= OnFireStarted;
                _actions.Player.Fire.canceled -= OnFireCanceled;
                _actions.Dispose();
            }
            _instance = null;
        }

        private void OnFireStarted(InputAction.CallbackContext _)
        {
            IsFiring = true;
            FireStarted?.Invoke();
        }

        private void OnFireCanceled(InputAction.CallbackContext _)
        {
            IsFiring = false;
            FireCanceled?.Invoke();
        }
    }
}
