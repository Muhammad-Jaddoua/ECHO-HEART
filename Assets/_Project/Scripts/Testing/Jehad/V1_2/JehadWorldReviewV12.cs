using UnityEngine;
using UnityEngine.InputSystem;
using Echoheart.World;
using Echoheart.Core;

namespace Echoheart.Testing
{
    /// <summary>World-review controls only. The production PlayerMotor and HUD remain integrator-owned.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class JehadWorldReviewV12 : MonoBehaviour
    {
        [SerializeField] private Transform _camera;
        [SerializeField] private float _walkSpeed = 7f;
        [SerializeField] private float _sprintSpeed = 16f;
        [SerializeField] private LayerMask _obstructionMask = 1 << Layers.Environment;
        private CharacterController _controller;
        private HiddenRelicV12[] _relics;
        private Vector3 _spawn;
        private float _vertical;
        private float _yaw;
        private float _pitch = 24f;
        private float _distance = 9f;
        private GUIStyle _text;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _spawn = transform.position;
            _relics = FindObjectsByType<HiddenRelicV12>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (_camera != null) _camera.SetParent(null, true);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * 0.15f;
                    _pitch = Mathf.Clamp(_pitch - delta.y * 0.15f, 8f, 72f);
                }
                _distance = Mathf.Clamp(_distance - mouse.scroll.ReadValue().y * 0.006f, 3f, 22f);
            }
            if (keyboard == null) return;
            if (keyboard.rKey.wasPressedThisFrame || transform.position.y < -20f)
            {
                _controller.enabled = false;
                transform.position = _spawn;
                _controller.enabled = true;
                _vertical = 0f;
            }
            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            Vector3 move = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(input.x, 0f, input.y);
            move = Vector3.ClampMagnitude(move, 1f) * (keyboard.leftShiftKey.isPressed ? _sprintSpeed : _walkSpeed);
            if (_controller.isGrounded)
            {
                _vertical = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame) _vertical = Mathf.Sqrt(2f * 25f * 1.6f);
            }
            _vertical = Mathf.Max(_vertical - 25f * Time.deltaTime, -45f);
            _controller.Move((move + Vector3.up * _vertical) * Time.deltaTime);
            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 12f * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            Vector3 target = transform.position + Vector3.up * 0.8f;
            Vector3 direction = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.back;
            float distance = _distance;
            if (Physics.SphereCast(target, 0.25f, direction, out RaycastHit hit, distance,
                    _obstructionMask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0.4f, hit.distance - 0.15f);
            _camera.position = target + direction * distance;
            _camera.LookAt(target);
        }

        private void OnGUI()
        {
            if (_text == null) _text = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            int found = 0;
            if (_relics != null)
                foreach (var relic in _relics) if (relic != null && relic.Collected) found++;
            GUI.Box(new Rect(15f, 15f, 445f, 68f), GUIContent.none);
            GUI.Label(new Rect(28f, 23f, 420f, 23f), $"Hidden relics  {found} / {_relics?.Length ?? 0}", _text);
            GUI.Label(new Rect(28f, 48f, 420f, 23f), "WASD  •  Shift sprint  •  Space jump  •  MMB orbit  •  R return");
        }
    }
}
