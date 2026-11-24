using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Player
{
    /// <summary>
    /// The player's single power slot. Equip replaces the held power (the only way to get one is inhaling),
    /// swaps the accessory prop, and raises GameEvents.AbilityChanged so the HUD never references the player.
    /// Left click fires the held power through Combat's AbilityAttackRunner: Bow/Fire aim at whatever is under the
    /// screen centre (the crosshair), Sword slashes along the camera's flat forward.
    /// </summary>
    public class PlayerAbilityHolder : MonoBehaviour, IResettable
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerMotor _motor;
        [SerializeField] private PlayerInhale _inhale;
        [SerializeField] private Health _health;
        [SerializeField] private AbilityAttackRunner _runner;
        [Tooltip("Where swings and projectiles start.")]
        [SerializeField] private Transform _muzzle;
        [Tooltip("Parent for the held power's accessory prop.")]
        [SerializeField] private Transform _accessorySocket;
        [Tooltip("Aim source. Defaults to the main camera.")]
        [SerializeField] private Transform _aimCamera;

        [Header("Aiming")]
        [SerializeField, Min(1f)] private float _maxAimDistance = 60f;
        [Tooltip("What the crosshair ray can stop on (environment and enemies).")]
        [SerializeField] private LayerMask _aimMask = (1 << Layers.Environment) | (1 << Layers.Enemy) | (1 << Layers.Boss) | 1;

        [Header("Feedback hooks")]
        [SerializeField] private UnityEvent _onEquipped;
        [SerializeField] private UnityEvent _onAttack;

        private GameObject _accessoryInstance;

        public AbilityDefinition Current { get; private set; }

        private void Awake()
        {
            if (_aimCamera == null && Camera.main != null) _aimCamera = Camera.main.transform;
        }

        private void Start()
        {
            // Announce the starting state (no power) once, so the HUD is correct before the first capture.
            GameEvents.RaiseAbilityChanged(Current);
        }

        private void Update()
        {
            if (!_input.AttackPressed || Current == null) return;
            if (_health != null && _health.IsDead) return;
            if (_inhale != null && _inhale.IsInhaling) return;

            Vector3 flatForward = CameraFlatForward();
            _motor.FaceDirection(flatForward);

            Vector3 aim = Current.IsRanged ? CrosshairAim() : flatForward;
            if (_runner.TryUse(Current, _muzzle, aim, gameObject)) _onAttack.Invoke();
        }

        /// <summary>Replaces the held power. Passing null clears it (main-area retry, new game).</summary>
        public void Equip(AbilityDefinition ability)
        {
            Current = ability;

            if (_accessoryInstance != null) Destroy(_accessoryInstance);
            _accessoryInstance = null;
            if (ability != null && ability.AccessoryPrefab != null && _accessorySocket != null)
                _accessoryInstance = Instantiate(ability.AccessoryPrefab, _accessorySocket, false);

            GameEvents.RaiseAbilityChanged(ability);
            if (ability != null) _onEquipped.Invoke();
        }

        /// <summary>Drops the held power. Same as Equip(null); exists so inspector events can wire it without an argument.</summary>
        public void ClearAbility() => Equip(null);

        public void ResetFor(RetryScope scope)
        {
            // Boss retry keeps the power chosen for the fight; every other retry starts powerless (game-design.md).
            if (scope != RetryScope.BossArena) Equip(null);
        }

        private Vector3 CameraFlatForward()
        {
            Vector3 forward = _aimCamera != null ? _aimCamera.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
        }

        /// <summary>
        /// Direction from the muzzle to the point under the screen centre, so shots land where the crosshair is
        /// even though the muzzle sits beside the camera ray.
        /// </summary>
        private Vector3 CrosshairAim()
        {
            if (_aimCamera == null) return transform.forward;

            var ray = new Ray(_aimCamera.position, _aimCamera.forward);
            Vector3 point = ray.GetPoint(_maxAimDistance);
            // Start past the player so the ray never stops on the spirit itself.
            float skip = Vector3.Dot(transform.position - _aimCamera.position, _aimCamera.forward);
            ray.origin += _aimCamera.forward * Mathf.Max(0f, skip);
            if (Physics.Raycast(ray, out RaycastHit hit, _maxAimDistance, _aimMask, QueryTriggerInteraction.Ignore))
                point = hit.point;

            Vector3 direction = point - _muzzle.position;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }
    }
}
