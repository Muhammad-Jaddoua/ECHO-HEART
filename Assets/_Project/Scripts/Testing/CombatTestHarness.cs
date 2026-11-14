using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoheart.Testing
{
    /// <summary>
    /// Sandbox stand-in for the real ability holder, inhale and retry service. Lets the combat slice be played
    /// before those land: 1/2/3 equip Sword/Bow/Fire, LMB or F attacks, C fakes a capture (calls the same
    /// IInhalable protocol the real inhale will), B starts the boss, R/T/N run ResetFor. Never in the shipped scene.
    /// </summary>
    public class CombatTestHarness : MonoBehaviour
    {
        private const float CaptureRange = 6f;

        [SerializeField] private AbilityDefinition _sword;
        [SerializeField] private AbilityDefinition _bow;
        [SerializeField] private AbilityDefinition _fire;
        [SerializeField] private AbilityAttackRunner _runner;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Health _playerHealth;
        [SerializeField] private BossBrain _boss;
        [SerializeField] private Transform _respawnPoint;

        private CharacterController _controller;
        private Health _bossHealth;

        public AbilityDefinition Held { get; private set; }

        private void Awake()
        {
            TryGetComponent(out _controller);
            if (_boss != null) _bossHealth = _boss.GetComponent<Health>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) Equip(_sword);
            if (keyboard.digit2Key.wasPressedThisFrame) Equip(_bow);
            if (keyboard.digit3Key.wasPressedThisFrame) Equip(_fire);

            Mouse mouse = Mouse.current;
            bool attackHeld = keyboard.fKey.isPressed || (mouse != null && mouse.leftButton.isPressed);
            if (attackHeld) Attack();

            if (keyboard.bKey.wasPressedThisFrame) BeginBoss();
            if (keyboard.rKey.wasPressedThisFrame) Retry(RetryScope.BossArena);
            if (keyboard.tKey.wasPressedThisFrame) Retry(RetryScope.MainArea);
            if (keyboard.nKey.wasPressedThisFrame) Retry(RetryScope.NewGame);
            if (keyboard.cKey.wasPressedThisFrame) SimulateCapture();
        }

        public void Equip(AbilityDefinition ability)
        {
            Held = ability;
            GameEvents.RaiseAbilityChanged(ability);
        }

        /// <summary>Uses the held power the way the real holder will: muzzle origin, flat-ish aim along the player's facing.</summary>
        public bool Attack()
        {
            if (Held == null || _runner == null || (_playerHealth != null && _playerHealth.IsDead)) return false;
            return _runner.TryUse(Held, _muzzle != null ? _muzzle : transform, AimDirection(), gameObject);
        }

        public void BeginBoss()
        {
            if (_boss != null) _boss.BeginEncounter();
        }

        /// <summary>
        /// Stand-in for Tariq's inhale: the same OnInhaleStart -> OnCaptured order the real one must follow,
        /// then the granted power is equipped through the same path as the keys.
        /// </summary>
        public bool SimulateCapture()
        {
            EnemyBrain best = null;
            float bestDistance = CaptureRange;
            foreach (EnemyBrain enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if (!enemy.CanBeInhaled) continue;
                Vector3 delta = enemy.transform.position - transform.position;
                delta.y = 0f;
                if (delta.magnitude > bestDistance || Vector3.Dot(transform.forward, delta.normalized) < 0.3f) continue;
                bestDistance = delta.magnitude;
                best = enemy;
            }
            if (best == null) return false;

            AbilityDefinition granted = best.GrantedAbility;
            best.OnInhaleStart(transform);
            best.OnCaptured();
            if (granted != null) Equip(granted);
            return true;
        }

        /// <summary>What the real RetryService will do: reset every IResettable, refill and re-seat the player.</summary>
        public void Retry(RetryScope scope)
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IResettable resettable) resettable.ResetFor(scope);

            if (_playerHealth != null) _playerHealth.ResetHealth();
            if (_respawnPoint == null) return;
            if (_controller != null) _controller.enabled = false; // CharacterController overrides direct transform moves
            transform.SetPositionAndRotation(_respawnPoint.position, _respawnPoint.rotation);
            if (_controller != null) _controller.enabled = true;
        }

        private Vector3 AimDirection()
        {
            // Level aim: shots and slashes stay at enemy height whatever the camera pitch is.
            Vector3 aim = transform.forward;
            if (Camera.main != null) aim = Camera.main.transform.forward;
            aim.y = 0f;
            return aim.sqrMagnitude > 0.01f ? aim.normalized : transform.forward;
        }

        private void OnGUI()
        {
            string held = Held != null ? Held.DisplayName : "none";
            string hp = _playerHealth != null ? $"{_playerHealth.Current}/{_playerHealth.Max}" : "-";
            string boss = _boss != null && _bossHealth != null
                ? $"{_boss.Current} {_bossHealth.Current}/{_bossHealth.Max} {(_boss.Current == BossBrain.State.Telegraph ? _boss.CurrentAttack.ToString() : string.Empty)}"
                : "-";
            GUI.Box(new Rect(10, 10, 330, 190),
                $"Power: {held}\nPlayer HP: {hp}{(_playerHealth != null && _playerHealth.IsDead ? " (DEAD, press R/T/N)" : string.Empty)}\n" +
                $"Boss: {boss}\nActive enemies: {EnemySpawner.ActiveCount}/{EnemySpawner.MaxActive}\n\n" +
                "1/2/3 Sword/Bow/Fire   LMB or F attack\nC capture nearest   B begin boss\nR retry boss arena   T retry main\nN new game   WASD/Space move");
        }
    }
}
