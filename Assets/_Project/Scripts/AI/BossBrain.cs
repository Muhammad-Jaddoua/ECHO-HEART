using System.Collections.Generic;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Echoheart.AI
{
    /// <summary>
    /// Corrupted Guardian state machine:
    ///
    ///   Idle --(encounter begun + player found + idle pause over)--> Telegraph (Slam if player close, else Ranged)
    ///   Telegraph --(warning time over; boss stands still)--> Attack
    ///   Attack --(strike pose over)--> Recover (vulnerable, never attacks) --> Idle
    ///   any state --(Health.Died)--> Defeated (BossDefeated raised once, object switched off after a delay)
    ///   PlayerDied --> Idle, no target, until ResetFor
    ///
    /// Damage is accepted in every live state: each power alone must be able to win, so there are no
    /// immunities, phases, stagger or knockback.
    ///
    /// NOT IInhalable on purpose: the inhale only affects colliders whose parents implement IInhalable, so the
    /// boss simply ignores it (design rule: the boss cannot be captured).
    ///
    /// BossBrain is its own bridge to GameEvents (BossEncounterStarted / BossHealthChanged / BossDefeated).
    /// The boss prefab must NOT also carry a HealthEventRelay, or the events would be raised twice.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class BossBrain : MonoBehaviour, IResettable
    {
        public enum State { Idle, Telegraph, Attack, Recover, Defeated }
        public enum AttackKind { None, Slam, Ranged }

        private const float ScanInterval = 0.2f;
        private const float RepathInterval = 0.25f;
        private const float AimHeight = 1f;
        private const int OverlapBufferSize = 16;

        [SerializeField] private BossDefinition _definition;
        [Tooltip("Where the ranged shot spawns. Defaults to a point above the boss.")]
        [SerializeField] private Transform _muzzle;

        [Header("Inspector hooks (telegraph visuals, audio, camera shake). Read TelegraphProgress for a warning bar.")]
        [SerializeField] private UnityEvent _onTelegraph;
        [SerializeField] private UnityEvent _onSlamTelegraph;
        [SerializeField] private UnityEvent _onRangedTelegraph;
        [SerializeField] private UnityEvent _onAttack;
        [SerializeField] private UnityEvent _onRecover;
        [SerializeField] private UnityEvent _onDefeated;

        private readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];
        private readonly HashSet<IDamageable> _slamVictims = new HashSet<IDamageable>();
        private readonly Collider[] _scanBuffer = new Collider[4];

        private Health _health;
        private NavMeshAgent _agent;

        private State _state = State.Idle;
        private AttackKind _attack = AttackKind.None;
        private float _timer;          // time spent in the current state (or idle pause)
        private float _scanTimer;
        private float _repathTimer;
        private float _deactivateTimer;
        private Transform _target;
        private Health _targetHealth;

        private bool _beginRequested;  // BeginEncounter was called
        private bool _encounterRaised; // BossEncounterStarted raised; cleared only by ResetFor
        private bool _defeatRaised;    // BossDefeated raised; cleared only by ResetFor
        private bool _playerDown;      // PlayerDied seen; no attacks until ResetFor
        private bool _warnedOffNavMesh;
        private bool _warnedNoProjectile;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        public State Current => _state;
        public AttackKind CurrentAttack => _attack;
        public bool EncounterBegun => _beginRequested;

        /// <summary>Tuning data (read-only), e.g. for telegraph visuals that need the slam radius.</summary>
        public BossDefinition Definition => _definition;

        /// <summary>0..1 through the current Telegraph (for a warning ring/bar); 0 outside Telegraph.</summary>
        public float TelegraphProgress { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
            TryGetComponent(out _agent);
            _homePosition = transform.position;
            _homeRotation = transform.rotation;

            WarnAboutSetup();

            if (_definition == null)
            {
                Debug.LogError($"[{name}] BossBrain has no BossDefinition.", this);
                enabled = false;
                return;
            }

            if (_agent != null)
            {
                _agent.speed = _definition.MoveSpeed;
                _agent.updateRotation = false; // BossBrain turns the boss itself (also during telegraphs)
            }
            if (_health.Max != _definition.MaxHealth)
                Debug.LogWarning($"[{name}] Health max ({_health.Max}) differs from BossDefinition ({_definition.MaxHealth}); Health on the prefab wins.", this);
        }

        private void OnValidate() => WarnAboutSetup();

        private void WarnAboutSetup()
        {
            if (GetComponent<HealthEventRelay>() != null)
                Debug.LogWarning($"[{name}] Remove the HealthEventRelay: BossBrain raises the boss GameEvents itself, a relay would raise them twice.", this);
            if (gameObject.layer != Layers.Boss)
                Debug.LogWarning($"[{name}] The boss should be on the Boss layer ({Layers.Boss}) so player attacks hit it.", this);
        }

        private void OnEnable()
        {
            // Unsubscribe first so a repeated enable can never double-subscribe.
            _health.Changed -= HandleHealthChanged;
            _health.Died -= HandleDied;
            _health.Changed += HandleHealthChanged;
            _health.Died += HandleDied;
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.PlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Changed -= HandleHealthChanged;
                _health.Died -= HandleDied;
            }
            GameEvents.PlayerDied -= HandlePlayerDied;
        }

        /// <summary>
        /// Arena trigger hook. Safe to call repeatedly: starts the fight and raises BossEncounterStarted
        /// (and the first health value for the boss bar) once until the next ResetFor.
        /// </summary>
        public void BeginEncounter()
        {
            if (_state == State.Defeated) return;
            _beginRequested = true;
            if (_encounterRaised) return;
            _encounterRaised = true;
            GameEvents.RaiseBossEncounterStarted();
            GameEvents.RaiseBossHealthChanged(_health.Current, _health.Max);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>One simulation step. Separate from Update only so BossSelfTest can drive it with fixed deltas.</summary>
        internal void Tick(float deltaTime)
        {
            switch (_state)
            {
                case State.Idle: TickIdle(deltaTime); break;
                case State.Telegraph: TickTelegraph(deltaTime); break;
                case State.Attack: TickAttack(deltaTime); break;
                case State.Recover: TickRecover(deltaTime); break;
                case State.Defeated: TickDefeated(deltaTime); break;
            }
        }

        // ---- States ----

        private void TickIdle(float dt)
        {
            if (!_beginRequested || _playerDown) return;

            if (!TargetStillValid())
            {
                _scanTimer -= dt;
                if (_scanTimer > 0f) return;
                _scanTimer = ScanInterval;
                if (!TryAcquireTarget()) return;
            }

            FaceTarget(dt);
            // Walk in until slam distance; the player can never park far away and only be shot at forever.
            if (FlatDistanceToTarget() > _definition.SlamRange * 0.9f) Chase(dt);
            else StopAgent();

            _timer += dt;
            if (_timer < _definition.IdlePause) return;

            // Explicit rule: close player gets the slam, anyone farther gets the ranged shot.
            // Distance is judged now, so the choice always matches what the player sees.
            AttackKind kind = FlatDistanceToTarget() <= _definition.SlamRange ? AttackKind.Slam : AttackKind.Ranged;
            EnterTelegraph(kind);
        }

        private void TickTelegraph(float dt)
        {
            if (!TargetStillValid())
            {
                EnterIdle(); // target died or left: abort the warning, nothing fires
                return;
            }

            // Stays still (agent stopped) but keeps facing so the warning reads as aimed at the player.
            FaceTarget(dt);
            _timer += dt;
            float duration = TelegraphDuration();
            TelegraphProgress = duration > 0f ? Mathf.Clamp01(_timer / duration) : 1f;
            if (_timer >= duration) EnterAttack();
        }

        private void TickAttack(float dt)
        {
            _timer += dt;
            if (_timer >= _definition.AttackActiveSeconds) EnterRecover();
        }

        private void TickRecover(float dt)
        {
            // Deliberately does nothing: this is the player's safe window (Sword approach and strikes).
            _timer += dt;
            float duration = _attack == AttackKind.Slam ? _definition.SlamRecover : _definition.RangedRecover;
            if (_timer >= duration) EnterIdle();
        }

        private void TickDefeated(float dt)
        {
            if (!gameObject.activeSelf) return;
            _deactivateTimer += dt;
            if (_deactivateTimer >= _definition.DefeatDeactivateDelay) gameObject.SetActive(false);
        }

        // ---- Transitions ----

        private void EnterIdle()
        {
            _state = State.Idle;
            _attack = AttackKind.None;
            _timer = 0f;
            TelegraphProgress = 0f;
            StopAgent();
        }

        private void EnterTelegraph(AttackKind kind)
        {
            _state = State.Telegraph;
            _attack = kind;
            _timer = 0f;
            TelegraphProgress = 0f;
            StopAgent();

            _onTelegraph?.Invoke();
            if (kind == AttackKind.Slam) _onSlamTelegraph?.Invoke();
            else _onRangedTelegraph?.Invoke();
        }

        private void EnterAttack()
        {
            _state = State.Attack;
            _timer = 0f;
            TelegraphProgress = 1f;

            if (_attack == AttackKind.Slam) DoSlam();
            else DoRangedShot();
            _onAttack?.Invoke();
        }

        private void EnterRecover()
        {
            _state = State.Recover;
            _timer = 0f;
            TelegraphProgress = 0f;
            _onRecover?.Invoke();
        }

        // ---- Attacks ----

        private void DoSlam()
        {
            // ONE AttackId per slam plus a victim set: the player (several colliders) is hurt exactly once.
            int attackId = AttackIds.Next();
            _slamVictims.Clear();

            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, _definition.SlamRadius, _overlapBuffer,
                Layers.PlayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                IDamageable victim = _overlapBuffer[i].GetComponentInParent<IDamageable>();
                if (victim == null || !_slamVictims.Add(victim)) continue;

                Vector3 point = SafeClosestPoint(_overlapBuffer[i], center);
                Vector3 direction = point - center;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;

                var info = new DamageInfo(_definition.SlamDamage, point, direction, Team.Enemy, attackId, gameObject);
                victim.TryApplyDamage(info);
            }
        }

        // Collider.ClosestPoint only supports Box/Sphere/Capsule/convex Mesh colliders; on a CharacterController
        // (the likely player body) or a non-convex MeshCollider it logs an error. Fall back to the bounds.
        private static Vector3 SafeClosestPoint(Collider col, Vector3 point)
        {
            bool supported = col is BoxCollider || col is SphereCollider || col is CapsuleCollider ||
                             (col is MeshCollider mesh && mesh.convex);
            return supported ? col.ClosestPoint(point) : col.bounds.ClosestPoint(point);
        }

        private void DoRangedShot()
        {
            if (_definition.ProjectilePrefab == null)
            {
                if (!_warnedNoProjectile)
                {
                    _warnedNoProjectile = true;
                    Debug.LogWarning($"[{name}] BossDefinition has no projectile prefab; the ranged attack does nothing.", this);
                }
                return;
            }

            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.5f;
            Vector3 aimPoint = _target != null ? _target.position + Vector3.up * AimHeight : origin + transform.forward;
            Vector3 direction = (aimPoint - origin).normalized;

            GameObject instance = Instantiate(_definition.ProjectilePrefab, origin, Quaternion.LookRotation(direction));
            if (!instance.TryGetComponent(out Projectile projectile))
            {
                Debug.LogError($"[{name}] Boss projectile prefab '{_definition.ProjectilePrefab.name}' has no Projectile component on its root.", this);
                Destroy(instance);
                return;
            }

            // Environment is always added by Projectile, so cover works against boss shots too.
            projectile.Launch(direction, _definition.ProjectileSpeed, _definition.ProjectileRange,
                _definition.RangedDamage, Team.Enemy, gameObject, Layers.PlayerMask);
        }

        private float TelegraphDuration() =>
            _attack == AttackKind.Slam ? _definition.SlamTelegraph : _definition.RangedTelegraph;

        // ---- Target and movement ----

        private bool TryAcquireTarget()
        {
            // Player layer scan, no GameObject.Find; the collider's Health tells us if it is a live player.
            int count = Physics.OverlapSphereNonAlloc(transform.position, _definition.SightRange,
                _scanBuffer, Layers.PlayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Health h = _scanBuffer[i].GetComponentInParent<Health>();
                if (h != null && h.IsDead) continue;
                _target = h != null ? h.transform : _scanBuffer[i].transform;
                _targetHealth = h;
                return true;
            }
            return false;
        }

        private bool TargetStillValid()
        {
            if (_target == null) return false; // also covers a destroyed player
            if (_targetHealth != null && _targetHealth.IsDead) return false;
            return Vector3.Distance(transform.position, _target.position) <= _definition.SightRange;
        }

        private float FlatDistanceToTarget()
        {
            Vector3 delta = _target.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void FaceTarget(float dt)
        {
            Vector3 flat = _target.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat),
                _definition.TurnSpeed * dt);
        }

        private void Chase(float dt)
        {
            if (!AgentReady()) return; // no agent / off the NavMesh: the boss just turns in place

            _repathTimer -= dt;
            if (_repathTimer > 0f) return;
            _repathTimer = RepathInterval;
            _agent.isStopped = false;
            _agent.SetDestination(_target.position);
        }

        private bool AgentReady()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh) return true;
            if (!_warnedOffNavMesh)
            {
                _warnedOffNavMesh = true; // once only: an unbaked arena must not spam every frame
                Debug.LogWarning($"[{name}] Boss has no NavMeshAgent on a baked NavMesh; it will turn but not walk.", this);
            }
            return false;
        }

        private void StopAgent()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // ---- Health and flow reactions ----

        private void HandleHealthChanged(int current, int max)
        {
            if (_state == State.Defeated) return; // nothing left to show once defeated
            GameEvents.RaiseBossHealthChanged(current, max);
        }

        private void HandleDied()
        {
            if (_defeatRaised) return; // BossDefeated is a one-shot flow trigger (unlocks the Heart)
            _defeatRaised = true;

            _state = State.Defeated;
            _attack = AttackKind.None;
            _timer = 0f;
            _deactivateTimer = 0f;
            TelegraphProgress = 0f;
            _target = null;
            _targetHealth = null;
            StopAgent();

            _onDefeated?.Invoke();
            GameEvents.RaiseBossDefeated();
        }

        private void HandlePlayerDied()
        {
            // A death before the fight began must not latch: nothing would ever clear it if the player then
            // walks into the arena without a retry in between.
            if (!_beginRequested) return;

            // The encounter stays begun; the boss just stands down until the retry resets it.
            _playerDown = true;
            if (_state == State.Defeated) return;
            _target = null;
            _targetHealth = null;
            EnterIdle();
        }

        // ---- IResettable ----

        public void ResetFor(RetryScope scope)
        {
            // Any retry means the player is alive again, so the stand-down latch always clears, even for a
            // main-area retry that otherwise leaves the boss alone.
            _playerDown = false;

            // The boss lives in the arena: only a boss-arena retry or a new game restores it.
            if (scope == RetryScope.MainArea) return;

            gameObject.SetActive(true); // a defeated boss was switched off; OnEnable re-subscribes

            _state = State.Idle; // before ResetHealth so its Changed event is not swallowed as "defeated"
            _attack = AttackKind.None;
            _timer = 0f;
            _scanTimer = 0f;
            _repathTimer = 0f;
            _deactivateTimer = 0f;
            TelegraphProgress = 0f;
            _target = null;
            _targetHealth = null;
            _beginRequested = false;
            _encounterRaised = false;
            _defeatRaised = false;
            _warnedOffNavMesh = false;

            // In-flight boss shots must not hit the respawned player.
            Projectile.DestroyAllActive();

            _health.ResetHealth(); // raises Changed -> BossHealthChanged(max, max)

            if (_agent != null) _agent.enabled = false;
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
            if (_agent != null && NavMesh.SamplePosition(_homePosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _agent.enabled = true;
                _agent.Warp(hit.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null) return;
            Vector3 p = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p, _definition.SightRange);
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireSphere(p, _definition.SlamRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(p, _definition.SlamRadius);
        }
    }
}
