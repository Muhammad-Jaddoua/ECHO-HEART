using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoheart.Player
{
    /// <summary>
    /// The single place that reads the "Player" action map. Motor, camera, inhale and attacks read from here,
    /// so remapping a control never touches gameplay code and input can be switched off in one spot
    /// (pause, death, completion).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _actions;
        [Tooltip("Seconds a Jump press is remembered, so pressing just before landing still jumps.")]
        [SerializeField, Min(0f)] private float _jumpBufferSeconds = 0.12f;

        private InputActionMap _map;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _attack;
        private InputAction _inhale;
        private InputAction _interact;
        private InputAction _pause;
        private float _jumpPressedAt = float.NegativeInfinity;

        public Vector2 Move => _move.ReadValue<Vector2>();
        public Vector2 Look => _look.ReadValue<Vector2>();
        /// <summary>True when the current Look value comes from a stick (scale by time) rather than a mouse delta.</summary>
        public bool LookIsGamepad => _look.activeControl?.device is Gamepad;
        public bool AttackPressed => _attack.WasPressedThisFrame();
        public bool InhaleHeld => _inhale.IsPressed();
        public bool InteractPressed => _interact.WasPressedThisFrame();
        public bool PausePressed => _pause.WasPressedThisFrame();
        public bool GameplayEnabled => _map.enabled;

        private void Awake()
        {
            _map = _actions.FindActionMap("Player", throwIfNotFound: true);
            _move = _map.FindAction("Move", true);
            _look = _map.FindAction("Look", true);
            _jump = _map.FindAction("Jump", true);
            _attack = _map.FindAction("Attack", true);
            _inhale = _map.FindAction("Inhale", true);
            _interact = _map.FindAction("Interact", true);
            _pause = _map.FindAction("Pause", true);
        }

        private void OnEnable()
        {
            _jump.performed += OnJump;
            _map.Enable();
        }

        private void OnDisable()
        {
            _jump.performed -= OnJump;
            _map.Disable();
        }

        /// <summary>Turns gameplay controls on/off (e.g. while paused or dead). Pause stays readable.</summary>
        public void SetGameplayEnabled(bool enabled)
        {
            if (enabled) _map.Enable();
            else
            {
                _map.Disable();
                _pause.Enable();
                _jumpPressedAt = float.NegativeInfinity;
            }
        }

        /// <summary>Returns true once per buffered Jump press, then forgets it.</summary>
        public bool ConsumeJump()
        {
            if (Time.time - _jumpPressedAt > _jumpBufferSeconds) return false;
            _jumpPressedAt = float.NegativeInfinity;
            return true;
        }

        private void OnJump(InputAction.CallbackContext _) => _jumpPressedAt = Time.time;
    }
}
