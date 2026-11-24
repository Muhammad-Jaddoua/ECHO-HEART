using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Player
{
    /// <summary>
    /// Hold right click to inhale one normal enemy in front of the spirit and copy its power.
    ///
    /// States:
    ///   Ready   --(hold, valid target in range + cone + line of sight)--> Pulling
    ///   Pulling --(pull time reached)--> capture: OnCaptured once, Equip the power --> WaitRelease
    ///   Pulling --(released / target lost / LoS blocked / out of range / player died)--> OnInhaleCancel --> WaitRelease
    ///   WaitRelease --(button released)--> Ready
    ///
    /// WaitRelease stops one long hold from chain-capturing several enemies. Capture goes through a single
    /// state transition, so OnCaptured and Equip can never run twice for one pull.
    /// Follows the enemy-side protocol in contract-changes.md: CanBeInhaled before start, keep refreshing
    /// OnInhaleStart while pulling, always OnInhaleCancel when a pull stops without a capture.
    /// </summary>
    public class PlayerInhale : MonoBehaviour
    {
        private enum State { Ready, Pulling, WaitRelease }

        private const int BufferSize = 16;

        [Header("References")]
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerAbilityHolder _holder;
        [SerializeField] private Health _health;
        [Tooltip("Where enemies are pulled to and where range is measured from.")]
        [SerializeField] private Transform _mouth;
        [Tooltip("Inhale aims along this camera's flat forward. Defaults to the main camera.")]
        [SerializeField] private Transform _aimCamera;

        [Header("Targeting")]
        [SerializeField, Min(0.5f)] private float _range = 5f;
        [Tooltip("Half-angle of the cone in front of the spirit, in degrees.")]
        [SerializeField, Range(5f, 90f)] private float _coneHalfAngle = 45f;
        [Tooltip("Layers that can hold an inhalable. The boss layer is included on purpose: it is skipped because it is not IInhalable.")]
        [SerializeField] private LayerMask _targetMask = (1 << Layers.Enemy) | (1 << Layers.Boss);
        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask _blockerMask = 1 << Layers.Environment;

        [Header("Pull")]
        [SerializeField, Min(0.05f)] private float _pullSeconds = 0.6f;
        [Tooltip("The pull gives up if the target ends up this much farther than the start range.")]
        [SerializeField, Min(1f)] private float _breakRangeMultiplier = 1.3f;
        [Tooltip("How close to the mouth the target is pulled before it disappears.")]
        [SerializeField, Min(0f)] private float _swallowDistance = 0.5f;
        [Tooltip("Seconds between OnInhaleStart refreshes; must stay well under the enemy's 2 s safety timeout.")]
        [SerializeField, Min(0.02f)] private float _refreshInterval = 0.25f;

        [Header("Feedback hooks")]
        [SerializeField] private UnityEvent _onPullStarted;
        [SerializeField] private UnityEvent _onPullCancelled;
        [SerializeField] private UnityEvent _onCaptured;

        private readonly Collider[] _buffer = new Collider[BufferSize];
        private CharacterController _controller;
        private State _state = State.Ready;
        private IInhalable _target;
        private Transform _targetRoot;
        private Vector3 _targetStartPosition;
        private float _pullTimer;
        private float _nextRefreshTime;

        public bool IsInhaling => _state == State.Pulling;
        /// <summary>0..1 progress of the current pull, for feedback (squash, VFX intensity).</summary>
        public float PullProgress => _state == State.Pulling ? Mathf.Clamp01(_pullTimer / _pullSeconds) : 0f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_aimCamera == null && Camera.main != null) _aimCamera = Camera.main.transform;
        }

        private void OnEnable() => GameEvents.PlayerDied += CancelPull;

        private void OnDisable()
        {
            GameEvents.PlayerDied -= CancelPull;
            CancelPull();
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Ready: TickReady(); break;
                case State.Pulling: TickPulling(); break;
                case State.WaitRelease:
                    if (!_input.InhaleHeld) _state = State.Ready;
                    break;
            }
        }

        private void TickReady()
        {
            if (!_input.InhaleHeld || (_health != null && _health.IsDead)) return;

            _motor.FaceDirection(AimForward());
            IInhalable target = FindTarget(out Transform root);
            if (target == null) return; // keep facing the aim; a target walking into the cone is picked up next frame

            target.OnInhaleStart(transform);
            _target = target;
            _targetRoot = root;
            _targetStartPosition = root.position;
            _pullTimer = 0f;
            _nextRefreshTime = Time.time + _refreshInterval;
            _state = State.Pulling;
            _motor.SetMovementLocked(true);
            _onPullStarted.Invoke();
        }

        private void TickPulling()
        {
            if (!_input.InhaleHeld || !TargetStillValid())
            {
                CancelPull();
                return;
            }

            if (Time.time >= _nextRefreshTime)
            {
                _target.OnInhaleStart(transform); // keeps the enemy's safety timeout from firing mid-pull
                _nextRefreshTime = Time.time + _refreshInterval;
            }

            _pullTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pullTimer / _pullSeconds);

            // Ease-in pull toward the mouth: slow struggle first, quick swallow at the end.
            Vector3 mouth = _mouth.position;
            Vector3 toStart = _targetStartPosition - mouth;
            Vector3 end = mouth + toStart.normalized * Mathf.Min(_swallowDistance, toStart.magnitude);
            _targetRoot.position = Vector3.Lerp(_targetStartPosition, end, t * t);

            if (t >= 1f) Capture();
        }

        private void Capture()
        {
            IInhalable target = _target;
            ClearTarget();
            _state = State.WaitRelease; // leave Pulling before calling out, so nothing can re-enter Capture

            AbilityDefinition granted = target.GrantedAbility;
            target.OnCaptured();
            _holder.Equip(granted);
            _onCaptured.Invoke();
        }

        /// <summary>Stops a pull without capturing. Safe to call any time (death, disable, retry).</summary>
        public void CancelPull()
        {
            if (_state != State.Pulling) return;

            IInhalable target = _target;
            ClearTarget();
            _state = State.WaitRelease;
            target?.OnInhaleCancel();
            _onPullCancelled.Invoke();
        }

        private void ClearTarget()
        {
            _target = null;
            _targetRoot = null;
            _motor.SetMovementLocked(false);
        }

        private bool TargetStillValid()
        {
            // Unity-null check: the enemy may have been destroyed or deactivated (killed by someone else).
            if (_targetRoot == null || !_targetRoot.gameObject.activeInHierarchy) return false;
            float breakRange = _range * _breakRangeMultiplier;
            if ((_target.InhaleTarget.position - _mouth.position).sqrMagnitude > breakRange * breakRange) return false;
            return HasLineOfSight(_target.InhaleTarget.position, _targetRoot);
        }

        private IInhalable FindTarget(out Transform root)
        {
            root = null;
            IInhalable best = null;
            float bestScore = float.MaxValue;
            Vector3 origin = _mouth.position;
            Vector3 forward = transform.forward;
            float minDot = Mathf.Cos(_coneHalfAngle * Mathf.Deg2Rad);

            int count = Physics.OverlapSphereNonAlloc(origin, _range, _buffer, _targetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _buffer[i];
                _buffer[i] = null;

                // Colliders without IInhalable (the boss, props) are ignored.
                IInhalable candidate = hit.GetComponentInParent<IInhalable>();
                if (candidate == null || !candidate.CanBeInhaled || !(candidate is Component component)) continue;

                Vector3 toTarget = candidate.InhaleTarget.position - origin;
                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
                float distance = toTarget.magnitude;
                if (distance > _range) continue;
                if (flat.sqrMagnitude > 0.0001f && Vector3.Dot(forward, flat.normalized) < minDot) continue;
                if (!HasLineOfSight(candidate.InhaleTarget.position, component.transform)) continue;

                // Prefer what is closest to the aim line, then what is nearest.
                float angle = flat.sqrMagnitude > 0.0001f ? Vector3.Angle(forward, flat) : 0f;
                float score = angle / _coneHalfAngle + distance / _range;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    root = component.transform;
                }
            }
            return best;
        }

        /// <summary>
        /// Sight is checked from the body centre, not the mouth: the mouth sits in front of the collider and ends up
        /// inside a wall when the spirit presses against it, and a linecast that starts inside a collider misses it.
        /// The CharacterController keeps the body centre out of walls.
        /// </summary>
        private Vector3 SightOrigin => _controller != null ? transform.TransformPoint(_controller.center) : transform.position;

        private bool HasLineOfSight(Vector3 targetPoint, Transform targetRoot)
        {
            if (!Physics.Linecast(SightOrigin, targetPoint, out RaycastHit hit, _blockerMask, QueryTriggerInteraction.Ignore))
                return true;
            // A blocker collider that belongs to the target itself does not count.
            return hit.transform.IsChildOf(targetRoot);
        }

        private Vector3 AimForward()
        {
            Vector3 forward = _aimCamera != null ? _aimCamera.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = _mouth != null ? _mouth : transform;
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(origin.position, _range);
            Vector3 left = Quaternion.Euler(0f, -_coneHalfAngle, 0f) * transform.forward * _range;
            Vector3 right = Quaternion.Euler(0f, _coneHalfAngle, 0f) * transform.forward * _range;
            Gizmos.DrawLine(origin.position, origin.position + left);
            Gizmos.DrawLine(origin.position, origin.position + right);
        }
    }
}
