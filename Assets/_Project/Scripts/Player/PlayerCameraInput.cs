using Echoheart.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Echoheart.Player
{
    /// <summary>
    /// Feeds Look input into the Cinemachine orbit and owns the cursor lock. Driving the axes ourselves
    /// (instead of CinemachineInputAxisController) keeps sensitivity, invert-Y and "ignore look while paused"
    /// in one readable place and reuses the same PlayerInputReader as the motor.
    /// </summary>
    public class PlayerCameraInput : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private CinemachineOrbitalFollow _orbit;

        [Header("Sensitivity")]
        [Tooltip("Degrees per pixel of mouse movement.")]
        [SerializeField, Min(0f)] private float _mouseDegreesPerPixel = 0.12f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [SerializeField, Min(0f)] private float _stickDegreesPerSecond = 180f;
        [SerializeField] private bool _invertY;

        private bool _isPlaying = true;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (!_isPlaying || !_input.GameplayEnabled) return;

            Vector2 look = _input.Look;
            Vector2 degrees = _input.LookIsGamepad
                ? look * (_stickDegreesPerSecond * Time.deltaTime)
                : look * _mouseDegreesPerPixel;

            InputAxis horizontal = _orbit.HorizontalAxis;
            horizontal.Value = horizontal.ClampValue(horizontal.Value + degrees.x);
            _orbit.HorizontalAxis = horizontal;

            // Sphere orbit: vertical axis is pitch in degrees. Mouse up should tilt the view up (camera goes down).
            float verticalSign = _invertY ? 1f : -1f;
            InputAxis vertical = _orbit.VerticalAxis;
            vertical.Value = vertical.ClampValue(vertical.Value + verticalSign * degrees.y);
            _orbit.VerticalAxis = vertical;
        }

        private void OnStateChanged(GameState state)
        {
            _isPlaying = state == GameState.Playing;
            SetCursorLocked(_isPlaying);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
