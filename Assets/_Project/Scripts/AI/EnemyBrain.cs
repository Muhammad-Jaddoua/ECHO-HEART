using System;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Echoheart.AI
{
    /// <summary>
    /// Normal-enemy state machine, shared by all three variants:
    ///
    ///   Idle --(player in sight range)--> Chase --(in attack range + line of sight)--> Attack
    ///   Attack: Windup (telegraph) -> Execute -> Recovery -> Chase (or Idle if the player is gone)
    ///   Chase --(player beyond lose-sight range)--> Idle
    ///   any live state --(OnInhaleStart)--> BeingInhaled --(OnInhaleCancel)--> Chase/Idle
    ///   any live state --(Health.Died)--> Defeated
    ///
    /// Capture and defeat share ONE "_resolved" flag: whichever happens first wins and the other is
    /// rejected, so the enemy is removed once, never grants a power on defeat, and never double-notifies
    /// its spawner.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(NavMeshAgent))]
    public class EnemyBrain : MonoBehaviour, IInhalable, IResettable
    {
        public enum State { Idle, Chase, Attack, BeingInhaled, Defeated }

        private enum AttackPhase { Windup, Recovery }

        private const float RepathInterval = 0.25f;
        private const float ScanInterval = 0.2f;
        private const float EyeHeight = 1f;

        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private EnemyAttackBehaviour _attack;
        [Tooltip("Optional. Leave empty to find the player by scanning the Player layer within sight range.")]
        [SerializeField] private Transform _target;
        [Tooltip("Point the inhale aims at (chest height). Defaults to this transform.")]
        [SerializeField] private Transform _inhaleAnchor;
        [Tooltip("Seconds a defeated enemy stays visible (for the death feedback) before it is switched off.")]
        [SerializeField, Min(0f)] private float _despawnDelay = 0.6f;
        [Tooltip("Normally the boss arena retry leaves main-area enemies alone. Tick for enemies that live in the arena.")]
        [SerializeField] private bool _resetOnBossArenaRetry;

        [Tooltip("Safety net: if BeingInhaled lasts this long without a cancel or capture (e.g. the player died mid-inhale), the enemy recovers as if the inhale was cancelled. Any repeated OnInhaleStart refreshes it.")]
        [SerializeField, Min(0.1f)] private float _inhaleTimeout = 2f;

        [Header("Inspector hooks (telegraph, struggle, death feedback, audio)")]
        [SerializeField] private UnityEvent _onAttackWindup;
        [SerializeField] private UnityEvent _onAttackExecuted;
        [SerializeField] private UnityEvent _onInhaleStruggle;
        [SerializeField] private UnityEvent _onDefeated;

        private readonly Collider[] _scanBuffer = new Collider[4];

        private Health _health;
        private NavMeshAgent _agent;
        private Collider[] _colliders;
        private bool[] _colliderInitiallyEnabled;

        private State _state = State.Idle;
        private AttackPhase _attackPhase;
        private float _phaseEndsAt;
        private float _nextRepathAt;
        private float _nextScanAt;
        private float _despawnAt;
        private float _inhaleDeadline;
        private Transform _currentTarget;
        private Health _currentTargetHealth;

        private bool _resolved;          // captured OR defeated; set once, cleared only by ResetFor
        private bool _pendingDefeat;     // lethal hit landed mid-inhale; resolved if the pull is cancelled
        private bool _warnedOffNavMesh;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        /// <summary>Raised exactly once when this enemy is captured or defeated. The spawner listens to replenish.</summary>
        public event Action<EnemyBrain> Removed;

        public State Current => _state;
        public EnemyDefinition Definition => _definition;

        // IInhalable
        public bool CanBeInhaled =>
            isActiveAndEnabled && !_resolved && _state != State.Defeated && _state != State.BeingInhaled;
        public AbilityDefinition GrantedAbility => _definition != null ? _definition.GrantedAbility : null;
        public Transform InhaleTarget => _inhaleAnchor != null ? _inhaleAnchor : transform;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _agent = GetComponent<NavMeshAgent>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _colliderInitiallyEnabled = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++) _colliderInitiallyEnabled[i] = _colliders[i].enabled;

            _homePosition = transform.position;
            _homeRotation = transform.rotation;

            if (_definition == null)
            {
                Debug.LogError($"[{name}] EnemyBrain has no EnemyDefinition.", this);
                enabled = false;
                return;
            }

            _agent.speed = _definition.MoveSpeed;
            if (_health.Max != _definition.MaxHealth)
                Debug.LogWarning($"[{name}] Health max ({_health.Max}) differs from EnemyDefinition ({_definition.MaxHealth}); Health on the prefab wins.", this);
        }

        private void OnEnable()
        {
            _health.Damaged += HandleDamaged;
            _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= HandleDamaged;
            _health.Died -= HandleDied;
        }

        /// <summary>Spawner hook: remember where this enemy lives so ResetFor returns it there.</summary>
        public void SetHome(Vector3 position, Quaternion rotation)
        {
            _homePosition = position;
            _homeRotation = rotation;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Idle: TickIdle(); break;
                case State.Chase: TickChase(); break;
                case State.Attack: TickAttack(); break;
                case State.BeingInhaled: TickBeingInhaled(); break; // the inhale moves us; we only watch for a stalled pull
                case State.Defeated: TickDefeated(); break;
            }
        }

        // ---- States ----

        private void TickIdle()
        {
            if (Time.time < _nextScanAt) return;
            _nextScanAt = Time.time + ScanInterval;

            if (TryAcquireTarget()) EnterChase();
        }

        private void TickChase()
        {
            if (!TargetStillValid())
            {
                LoseTarget();
                return;
            }

            float distance = FlatDistanceToTarget();
            if (distance <= _definition.AttackRange && HasLineOfSight())
            {
                EnterAttack();
                return;
            }

            if (Time.time >= _nextRepathAt && AgentReady())
            {
                _nextRepathAt = Time.time + RepathInterval;
                _agent.SetDestination(_currentTarget.position);
            }
        }

        private void TickAttack()
        {
            if (!TargetStillValid())
            {
                LoseTarget();
                return;
            }

            if (_attackPhase == AttackPhase.Windup)
            {
                FaceTarget();
                if (Time.time < _phaseEndsAt) return;

                _attack.Execute(_definition, _currentTarget);
                _onAttackExecuted?.Invoke();
                _attackPhase = AttackPhase.Recovery;
                _phaseEndsAt = Time.time + _definition.AttackCooldown;
            }
            else if (Time.time >= _phaseEndsAt)
            {
                EnterChase(); // Chase re-checks range, so a player still in reach is attacked again
            }
        }

        private void TickBeingInhaled()
        {
            // If the player dies or is stunned mid-inhale, nobody calls OnInhaleCancel and the enemy would be
            // stuck frozen forever. A live inhale keeps calling OnInhaleStart, which refreshes the deadline.
            if (Time.time >= _inhaleDeadline) OnInhaleCancel();
        }

        private void TickDefeated()
        {
            if (_resolved && _despawnAt > 0f && Time.time >= _despawnAt)
            {
                _despawnAt = 0f;
                gameObject.SetActive(false);
            }
        }

        private void EnterChase()
        {
            _state = State.Chase;
            _nextRepathAt = 0f;
            if (!_agent.enabled) EnableAgentNearby(); // e.g. recovering from an inhale
            if (AgentReady()) _agent.isStopped = false;
        }

        private void EnterAttack()
        {
            if (_attack == null)
            {
                Debug.LogError($"[{name}] No EnemyAttackBehaviour assigned.", this);
                enabled = false;
                return;
            }

            _state = State.Attack;
            _attackPhase = AttackPhase.Windup;
            _phaseEndsAt = Time.time + _definition.AttackWindup;
            StopAgent();
            _onAttackWindup?.Invoke();
        }

        private void LoseTarget()
        {
            _currentTarget = null;
            _currentTargetHealth = null;
            StopAgent();
            _state = State.Idle;
        }

        // ---- Target and senses ----

        private bool TryAcquireTarget()
        {
            Transform candidate = _target;
            if (candidate == null)
            {
                int count = Physics.OverlapSphereNonAlloc(transform.position, _definition.SightRange,
                    _scanBuffer, Layers.PlayerMask, QueryTriggerInteraction.Ignore);
                if (count == 0) return false;
                candidate = _scanBuffer[0].transform;
            }

            if (Vector3.Distance(transform.position, candidate.position) > _definition.SightRange) return false;

            Health targetHealth = candidate.GetComponentInParent<Health>();
            if (targetHealth != null && targetHealth.IsDead) return false;

            _currentTarget = candidate;
            _currentTargetHealth = targetHealth;
            return true;
        }

        private bool TargetStillValid()
        {
            if (_currentTarget == null) return false;
            if (_currentTargetHealth != null && _currentTargetHealth.IsDead) return false;
            return Vector3.Distance(transform.position, _currentTarget.position) <= _definition.LoseSightRange;
        }

        private float FlatDistanceToTarget()
        {
            Vector3 delta = _currentTarget.position - transform.position;
            delta.y = 0f;
            return delta.magnitude;
        }

        private bool HasLineOfSight()
        {
            Vector3 from = transform.position + Vector3.up * EyeHeight;
            Vector3 to = _currentTarget.position + Vector3.up * EyeHeight;
            return !Physics.Linecast(from, to, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore);
        }

        private void FaceTarget()
        {
            Vector3 flat = _currentTarget.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), 540f * Time.deltaTime);
        }

        // ---- NavMeshAgent safety ----

        private bool AgentReady()
        {
            if (_agent.enabled && _agent.isOnNavMesh) return true;
            if (!_warnedOffNavMesh)
            {
                _warnedOffNavMesh = true; // once only: an unbaked scene must not spam every frame
                Debug.LogWarning($"[{name}] Enemy is not on a baked NavMesh; it cannot move. Bake the NavMesh or move the spawn point.", this);
            }
            return false;
        }

        private void StopAgent()
        {
            // Silent when disabled (inhaled/defeated): there is nothing to stop and that is not a setup error.
            if (!_agent.enabled) return;
            if (!AgentReady()) return;
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        /// <summary>
        /// Re-enables the agent on the NavMesh near the current position (the inhale may have dragged us
        /// off it), falling back to home. The transform is moved BEFORE enabling so the agent never tries to
        /// register at an invalid spot. If neither spot is on a NavMesh the agent stays off (AgentReady warns once).
        /// </summary>
        private void EnableAgentNearby()
        {
            Vector3 position;
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                position = hit.position;
            else if (NavMesh.SamplePosition(_homePosition, out hit, 2f, NavMesh.AllAreas))
                position = hit.position;
            else
            {
                transform.position = _homePosition;
                return;
            }

            transform.position = position;
            _agent.enabled = true;
            _agent.Warp(position);
        }

        // ---- Health reactions ----

        private void HandleDamaged(DamageInfo info)
        {
            // Health cannot be told to reject hits, so we undo them here. Defeated needs nothing:
            // Health already ignores hits once dead. A lethal hit mid-inhale is handled in HandleDied.
            if (_state == State.BeingInhaled && !_health.IsDead)
                _health.Heal(info.Amount);
        }

        private void HandleDied()
        {
            if (_resolved) return;

            if (_state == State.BeingInhaled)
            {
                // The pull is already in progress and capture wins if it finishes; only if the
                // player cancels do we owe the enemy its defeat.
                _pendingDefeat = true;
                return;
            }

            Defeat();
        }

        private void Defeat()
        {
            if (!Resolve()) return;
            _state = State.Defeated;
            _attack?.Cancel(); // a dead beetle must not land a swing that is still open
            StopAgent();
            _agent.enabled = false;
            SetCollidersEnabled(false);
            _despawnAt = Time.time + Mathf.Max(0.01f, _despawnDelay);
            _onDefeated?.Invoke(); // feedback only: defeating never grants a power
            Removed?.Invoke(this);
        }

        /// <returns>True only for the first capture/defeat; every later attempt is rejected.</returns>
        private bool Resolve()
        {
            if (_resolved) return false;
            _resolved = true;
            return true;
        }

        // ---- IInhalable ----

        public void OnInhaleStart(Transform inhaler)
        {
            if (_state == State.BeingInhaled && !_resolved)
            {
                _inhaleDeadline = Time.time + _inhaleTimeout; // inhale still active: keep it alive
                return;
            }
            if (!CanBeInhaled) return;

            _state = State.BeingInhaled;
            _inhaleDeadline = Time.time + _inhaleTimeout;
            _attack?.Cancel();
            StopAgent();
            // The inhale moves the transform directly; an enabled agent would fight it and snap back to the NavMesh.
            _agent.enabled = false;
            _onInhaleStruggle?.Invoke();
        }

        public void OnInhaleCancel()
        {
            if (_resolved || _state != State.BeingInhaled) return; // never resurrect a captured/defeated enemy

            if (_pendingDefeat)
            {
                _pendingDefeat = false;
                Defeat();
                return;
            }

            if (_currentTarget != null)
            {
                EnterChase(); // re-seats the agent on the NavMesh
            }
            else
            {
                _state = State.Idle;
                EnableAgentNearby();
            }
        }

        public void OnCaptured()
        {
            if (!Resolve())
            {
                Debug.LogWarning($"[{name}] Capture rejected: enemy was already captured or defeated.", this);
                return;
            }

            _state = State.Defeated;
            _pendingDefeat = false;
            _attack?.Cancel();
            StopAgent();
            Removed?.Invoke(this);
            gameObject.SetActive(false);
        }

        // ---- IResettable ----

        public void ResetFor(RetryScope scope)
        {
            if (scope == RetryScope.BossArena && !_resetOnBossArenaRetry) return;

            gameObject.SetActive(true); // captured/despawned enemies are inactive; OnEnable re-subscribes

            _resolved = false;
            _pendingDefeat = false;
            _despawnAt = 0f;
            _currentTarget = null;
            _currentTargetHealth = null;
            _warnedOffNavMesh = false;

            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = _colliderInitiallyEnabled[i];
            _health.ResetHealth();

            _attack?.Cancel();
            _agent.enabled = false;
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
            EnableAgentNearby();
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;

            _state = State.Idle;
        }

        private void SetCollidersEnabled(bool value)
        {
            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = value;
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null) return;
            Vector3 p = transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p, _definition.SightRange);
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireSphere(p, _definition.LoseSightRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(p, _definition.AttackRange);
        }
    }
}
