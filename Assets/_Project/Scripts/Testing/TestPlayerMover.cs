using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoheart.Testing
{
    /// <summary>
    /// Throwaway capsule controller so the world and combat sandboxes can be walked before the real
    /// PlayerMotor lands. WASD relative to the main camera, Space to jump. Not used in the shipped scene.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TestPlayerMover : MonoBehaviour
    {
        [SerializeField] private float _speed = 6f;
        [SerializeField] private float _jumpHeight = 1.4f;
        [SerializeField] private float _gravity = -25f;

        private CharacterController _controller;
        private float _verticalVelocity;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));

            var cam = Camera.main != null ? Camera.main.transform : transform;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            var move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f) * _speed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame)
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            _verticalVelocity += _gravity * Time.deltaTime;

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);

            _controller.Move((move + Vector3.up * _verticalVelocity) * Time.deltaTime);
        }
    }
}
