using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Replenishing source for ONE enemy. The spawner instantiates its enemy once and recycles that instance:
    /// when it is captured or defeated (EnemyBrain.Removed) a respawn timer starts, and when the timer, the
    /// player-distance rule and the shared active cap all allow it, the enemy is reset at its home point.
    ///
    ///   Active --(Removed)--> Pending --(delay elapsed + player far enough + under cap)--> Active
    ///
    /// It is also the reset owner of that enemy: a captured enemy is an inactive object that no retry service
    /// can find, so the spawner (always active) resets it on ResetFor.
    /// </summary>
    public class EnemySpawner : MonoBehaviour, IResettable
    {
        /// <summary>Game-design cap: at most ~6 normal enemies alive in the main area at once.</summary>
        public const int MaxActive = 6;

        private const float RetryInterval = 0.5f;

        // Shared by every spawner so the cap holds across the whole area.
        private static int s_active;

        [SerializeField] private EnemyBrain _enemyPrefab;
        [Tooltip("Where the enemy lives and respawns. Defaults to this transform.")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField, Min(0f)] private float _respawnDelay = 6f;
        [Tooltip("Never (re)spawn while the player is closer than this to the spawn point; re-checked every 0.5 s.")]
        [SerializeField, Min(0f)] private float _minPlayerDistance = 12f;
        [SerializeField] private bool _spawnOnStart = true;
        [Tooltip("Tick for spawners inside the boss arena so the boss retry resets them. Main-area spawners leave it off.")]
        [SerializeField] private bool _resetOnBossArenaRetry;
        [Tooltip("Boss-arena adds may opt out of the shared main-area cap.")]
        [SerializeField] private bool _countsTowardCap = true;

        private readonly Collider[] _playerBuffer = new Collider[1];

        private EnemyBrain _enemy;
        private bool _bound;        // subscribed to _enemy.Removed
        private bool _enemyAlive;   // between a spawn and the matching Removed; guards a double Removed
        private bool _counted;      // this spawner currently contributes to s_active
        private bool _pending;
        private bool _firstSpawn = true;
        private float _timer;

        /// <summary>Enemies currently alive across all spawners that count toward the cap.</summary>
        public static int ActiveCount => s_active;

        public EnemyBrain Current => _enemy;
        public bool IsEnemyActive => _enemyAlive;
        public bool PendingRespawn => _pending;

        private Transform Point => _spawnPoint != null ? _spawnPoint : transform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_active = 0; // domain reload may be disabled

        private void Start()
        {
            if (!EnsureEnemy()) return;

            // A retry may already have spawned us before our Start; do not reset a live enemy.
            if (_enemyAlive || _pending) return;

            // The first spawn goes through the same path as a respawn so the cap is honoured from frame one.
            _pending = true;
            _timer = _spawnOnStart ? 0f : _respawnDelay;
        }

        private void OnEnable()
        {
            if (_enemy == null) return;
            Bind();
            SetCounted(_enemyAlive);
            // Removed is unsubscribed while we are disabled, so a capture/defeat in that window leaves
            // _enemyAlive true with a parked (inactive) enemy: treat it as the missed Removed it is.
            if (_enemyAlive && !_enemy.gameObject.activeSelf) HandleRemoved(_enemy);
            else if (!_enemyAlive && !_pending) StartPending();
        }

        private void OnDisable()
        {
            Unbind();
            SetCounted(false);
        }

        private void OnDestroy()
        {
            Unbind();
            SetCounted(false);
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>One timer step. Separate from Update only so SpawnerSelfTest can drive it in edit mode.</summary>
        internal void Tick(float deltaTime)
        {
            if (!_pending) return;

            _timer -= deltaTime;
            if (_timer > 0f) return;

            if (CanSpawn()) Spawn();
            else _timer = RetryInterval;
        }

        // ---- Spawning ----

        private bool EnsureEnemy()
        {
            if (_enemy == null)
            {
                if (_enemyPrefab == null)
                {
                    Debug.LogError($"[{name}] EnemySpawner has no enemy prefab.", this);
                    enabled = false;
                    return false;
                }

                _enemy = Instantiate(_enemyPrefab, Point.position, Point.rotation, transform.parent);
                _enemy.SetHome(Point.position, Point.rotation);
                // Parked until the first spawn: spawning is always ResetFor, which re-activates at home.
                _enemy.gameObject.SetActive(false);
                _enemyAlive = false;
            }

            Bind();
            return true;
        }

        private bool CanSpawn()
        {
            if (_countsTowardCap && s_active >= MaxActive) return false;

            // The very first spawn ignores the distance rule: the player has not been anywhere yet.
            if (_firstSpawn || _minPlayerDistance <= 0f) return true;

            int near = Physics.OverlapSphereNonAlloc(Point.position, _minPlayerDistance, _playerBuffer,
                Layers.PlayerMask, QueryTriggerInteraction.Ignore);
            return near == 0;
        }

        private void Spawn()
        {
            _pending = false;
            _firstSpawn = false;
            _enemy.SetHome(Point.position, Point.rotation);
            // MainArea always: the brain's own boss-arena flag must not veto a respawn this spawner decided on.
            _enemy.ResetFor(RetryScope.MainArea);
            _enemyAlive = true;
            SetCounted(true);
        }

        private void StartPending()
        {
            _pending = true;
            _timer = _respawnDelay;
        }

        // ---- Enemy removal ----

        private void Bind()
        {
            if (_bound || _enemy == null) return;
            _enemy.Removed += HandleRemoved;
            _bound = true;
        }

        private void Unbind()
        {
            if (!_bound || _enemy == null) return;
            _enemy.Removed -= HandleRemoved;
            _bound = false;
        }

        private void HandleRemoved(EnemyBrain brain)
        {
            // Exactly once per spawn: capture and defeat are already exclusive in the brain, this is the safety net.
            if (!_enemyAlive) return;
            _enemyAlive = false;
            SetCounted(false);
            StartPending();
        }

        private void SetCounted(bool value)
        {
            value &= _countsTowardCap;
            if (value == _counted) return;
            _counted = value;
            s_active += value ? 1 : -1;
            if (s_active < 0) s_active = 0;
        }

        // ---- IResettable ----

        public void ResetFor(RetryScope scope)
        {
            if (scope == RetryScope.BossArena && !_resetOnBossArenaRetry) return;
            if (!EnsureEnemy()) return;

            _pending = false;

            // Our own live enemy is already in the count and is reset in place, so it does not need a free slot.
            int otherActive = s_active - (_counted ? 1 : 0);
            bool capFull = _countsTowardCap && otherActive >= MaxActive;

            if (_spawnOnStart && !capFull)
            {
                _firstSpawn = true; // the player respawns near the start; do not let the distance rule hold the enemy back
                Spawn();
            }
            else
            {
                // Back to the initial state: parked, waiting for a (delayed) spawn that honours the cap.
                _enemy.gameObject.SetActive(false);
                _enemyAlive = false;
                SetCounted(false);
                StartPending();
            }

            // Shots fired before the retry must not hit the player after it.
            Projectile.DestroyAllActive();
        }

        private void OnDrawGizmos()
        {
            Transform point = Point;
            Gizmos.color = _resetOnBossArenaRetry ? new Color(0.7f, 0.3f, 1f) : new Color(1f, 0.5f, 0.1f);
            Gizmos.DrawWireSphere(point.position, 0.4f);

            if (_minPlayerDistance <= 0f) return;
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            const int segments = 48;
            Vector3 previous = point.position + new Vector3(_minPlayerDistance, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = point.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _minPlayerDistance;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
