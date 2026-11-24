using UnityEngine;

namespace Echoheart.Player
{
    /// <summary>
    /// Third-person movement on a CharacterController: camera-relative walking with acceleration,
    /// turning to face the move direction, gravity, and a jump with coyote time and input buffering
    /// so jumps at ledges feel fair. Other systems stop movement with <see cref="SetMovementLocked"/>
    /// and reposition the player with <see cref="Teleport"/> (retry, fall respawn).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader _input;
        [Tooltip("Movement is relative to this. Defaults to the main camera.")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Walking")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6f;
        [Tooltip("Move speed multiplier while Sprint (Left Shift) is held.")]
        [SerializeField, Min(1f)] private float _sprintMultiplier = 1.6f;
        [SerializeField, Min(0f)] private float _acceleration = 40f;
        [SerializeField, Min(0f)] private float _deceleration = 50f;
        [Tooltip("Fraction of ground acceleration available in the air.")]
        [SerializeField, Range(0f, 1f)] private float _airControl = 0.6f;
        [SerializeField, Min(0f)] private float _turnSpeedDegrees = 720f;

        [Header("Jumping")]
        [SerializeField, Min(0f)] private float _jumpHeight = 1.6f;
        [SerializeField] private float _gravity = -30f;
        [Tooltip("Extra gravity multiplier while falling, for a snappier arc.")]
        [SerializeField, Min(1f)] private float _fallGravityMultiplier = 1.6f;
        [SerializeField] private float _maxFallSpeed = -40f;
        [Tooltip("Seconds after walking off a ledge during which a jump is still allowed.")]
        [SerializeField, Min(0f)] private float _coyoteSeconds = 0.12f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _lastGroundedAt = float.NegativeInfinity;
        private bool _jumpedSinceGrounded;
        private bool _movementLocked;

        public bool IsGrounded { get; private set; }
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;
        /// <summary>0..1 of max walk speed; drives walk animation.</summary>
        public float NormalizedSpeed => _moveSpeed > 0f ? _horizontalVelocity.magnitude / _moveSpeed : 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateGrounded();
            UpdateHorizontal(dt);
            UpdateVertical(dt);

            CollisionFlags flags = _controller.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);

            // Hitting a ceiling ends the jump instead of hovering against it.
            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f) _verticalVelocity = 0f;
        }

        /// <summary>Stops walking/jumping input (inhale pull, death, menus). Gravity still applies.</summary>
        public void SetMovementLocked(bool locked)
        {
            _movementLocked = locked;
            if (locked) _horizontalVelocity = Vector3.zero;
        }

        /// <summary>Turns the spirit instantly to a flat world direction (attacking or inhaling toward the camera aim).</summary>
        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>Moves the player instantly, clearing momentum. CharacterController must be off to set position.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            ResetMotion();
        }

        /// <summary>Clears momentum without moving. For flow code that repositions the player itself (respawn hooks).</summary>
        public void ResetMotion()
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _jumpedSinceGrounded = false;
        }

        private void UpdateGrounded()
        {
            // isGrounded reflects the previous Move; only treat it as grounded when not rising from a jump.
            IsGrounded = _controller.isGrounded && _verticalVelocity <= 0f;
            if (IsGrounded)
            {
                _lastGroundedAt = Time.time;
                _jumpedSinceGrounded = false;
            }
        }

        private void UpdateHorizontal(float dt)
        {
            float speed = _input.SprintHeld ? _moveSpeed * _sprintMultiplier : _moveSpeed;
            Vector3 desired = _movementLocked ? Vector3.zero : CameraRelative(_input.Move) * speed;

            float rate = desired.sqrMagnitude > 0.0001f ? _acceleration : _deceleration;
            if (!IsGrounded) rate *= _airControl;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desired, rate * dt);

            if (desired.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeedDegrees * dt);
            }
        }

        private void UpdateVertical(float dt)
        {
            bool canJump = !_movementLocked && !_jumpedSinceGrounded && Time.time - _lastGroundedAt <= _coyoteSeconds;
            if (canJump && _input.ConsumeJump())
            {
                _verticalVelocity = Mathf.Sqrt(2f * _jumpHeight * -_gravity);
                _jumpedSinceGrounded = true;
                IsGrounded = false;
                return;
            }

            if (IsGrounded)
            {
                // Small constant push keeps the controller snapped to slopes and steps.
                _verticalVelocity = -2f;
                return;
            }

            float multiplier = _verticalVelocity < 0f ? _fallGravityMultiplier : 1f;
            _verticalVelocity = Mathf.Max(_maxFallSpeed, _verticalVelocity + _gravity * multiplier * dt);
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            Transform basis = _cameraTransform != null ? _cameraTransform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(basis.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(basis.right, Vector3.up).normalized;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
