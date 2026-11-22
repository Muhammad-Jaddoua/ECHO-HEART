using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Observes motion after the motor; requests existing cues without moving the player.</summary>
    [DefaultExecutionOrder(125)]
    [DisallowMultipleComponent]
    public sealed class AirborneFeedbackV121 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldFeedbackV15 _feedback;
        private readonly MovementCuePolicyV121 _policy = new MovementCuePolicyV121();
        private Transform _player;
        private CharacterController _controller;
        private Vector3 _lastPosition;
        private bool _hasPosition, _owns, _before;
        private float _movementAfter;
        public int JumpRequests { get; private set; }
        public int LandingRequests { get; private set; }
        public bool OwnsAirborneCues => _owns && _feedback != null && _feedback.ExternalAirborneCues;

        private void OnEnable()
        {
            if (_flow == null || _flow.Player == null || _feedback == null || _feedback.Audio == null || _feedback.Particles == null)
            { Debug.LogError("V1.21 airborne feedback bindings missing.", this); enabled = false; return; }
            _before = _feedback.ExternalAirborneCues;
            _feedback.ExternalAirborneCues = true;
            _owns = true;
            GameEvents.RetryRequested += Retrying;
            GameEvents.StateChanged += StateChanged;
            ResetMotion();
            _movementAfter = Time.time + .3f;
        }

        private void OnDisable()
        {
            GameEvents.RetryRequested -= Retrying;
            GameEvents.StateChanged -= StateChanged;
            if (_owns && _feedback != null) _feedback.ExternalAirborneCues = _before;
            _owns = false;
            ResetMotion();
        }

        private void ResetMotion() { _policy.Reset(); _hasPosition = false; }
        private void Retrying(RetryScope scope) { ResetMotion(); _movementAfter = Time.time + .3f; }
        private void StateChanged(GameState state)
        {
            if (state == GameState.Title || state == GameState.Dead || state == GameState.Complete) ResetMotion();
        }

        private void LateUpdate()
        {
            Transform current = _flow.Player.Player;
            if (_player != current)
            {
                _player = current;
                _controller = current != null ? current.GetComponent<CharacterController>() : null;
                ResetMotion();
            }
            if (_player == null) { ResetMotion(); return; }
            Vector3 position = _player.position;
            bool discontinuity = !_hasPosition || (position - _lastPosition).sqrMagnitude >= 64f;
            _lastPosition = position;
            _hasPosition = true;
            if (_controller == null || !_controller.enabled || !_controller.gameObject.activeInHierarchy || Time.time < _movementAfter)
            { _policy.Reset(); return; }
            MovementCueV121 cue = _policy.Step(_controller.isGrounded, _controller.velocity.y, Time.deltaTime,
                _flow.State == GameState.Playing, discontinuity);
            if (cue == MovementCueV121.Jump)
            {
                JumpRequests++;
                _feedback.Audio.PlayWorld(WorldCueV15.Jump, position);
            }
            else if (cue == MovementCueV121.Land)
            {
                LandingRequests++;
                _feedback.Audio.PlayWorld(WorldCueV15.Land, position);
                _feedback.Particles.Burst(WorldBurstV15.Dust, position - Vector3.up * .7f);
            }
        }
    }
}
