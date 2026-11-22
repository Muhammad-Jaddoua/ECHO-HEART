using System;
using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Echoheart.World
{
    /// <summary>Scene integration only. Existing spawners own capture/defeat/respawn and the six-enemy cap.</summary>
    [DefaultExecutionOrder(-350)]
    public sealed class EnemyPopulationV14 : MonoBehaviour
    {
        [Serializable]
        private sealed class Source
        {
            [SerializeField] private EnemySpawner _prefab;
            [SerializeField] private Transform _point;
            public EnemySpawner Prefab => _prefab;
            public Transform Point => _point;
        }

        [SerializeField] private MainNavigationV14 _navigation;
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private Source[] _sources;
        private EnemySpawner[] _spawners = Array.Empty<EnemySpawner>();
        private GameState _state = GameState.Title;
        private bool _insideArena;
        private bool _resetting;

        public bool Ready { get; private set; }
        public bool Simulating => Ready && _state == GameState.Playing && !_insideArena && !_resetting;
        public EnemySpawner[] Spawners => _spawners;

        private void OnEnable()
        {
            GameEvents.StateChanged += HandleState;
            GameEvents.RetryRequested += HandleRetry;
            GameEvents.BossEncounterStarted += HandleArena;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= HandleState;
            GameEvents.RetryRequested -= HandleRetry;
            GameEvents.BossEncounterStarted -= HandleArena;
            Synchronize(false);
        }

        private void Start()
        {
            if (_navigation == null || !_navigation.Ready || _flow == null || _sources == null || _sources.Length != 6)
            {
                Debug.LogError("V1.4 population needs ready navigation, flow and six sources. No spawners were created.", this);
                return;
            }
            foreach (Source source in _sources)
            {
                if (source == null || source.Point == null || source.Prefab == null || source.Prefab.gameObject.activeSelf)
                {
                    Debug.LogError("V1.4 source prefabs must be assigned and inactive before instantiation.", this);
                    return;
                }
            }
            _spawners = new EnemySpawner[_sources.Length];
            for (int i = 0; i < _sources.Length; i++)
            {
                Source source = _sources[i];
                EnemySpawner spawner = Instantiate(source.Prefab, source.Point.position, source.Point.rotation, transform);
                spawner.name = source.Point.name + " source";
                spawner.enabled = false;
                spawner.gameObject.SetActive(true); // remains discoverable by RetryService while its timer is disabled
                _spawners[i] = spawner;
            }
            Ready = true;
            _state = _flow.State;
            Synchronize(Simulating);
        }

        private void HandleState(GameState state)
        {
            _state = state;
            _resetting = false;
            Synchronize(Simulating);
        }

        private void HandleRetry(RetryScope scope)
        {
            _resetting = true;
            if (scope != RetryScope.BossArena) _insideArena = false;
            Synchronize(false);
        }

        private void HandleArena()
        {
            _insideArena = true;
            Synchronize(false);
            Projectile.DestroyAllActive(); // no main-area shot may cross into the one-on-one boss encounter
        }

        // A delayed respawn can replace the brain's enabled state in the spawner's Update.
        // Reconcile after that tick as well as synchronously on state changes.
        private void LateUpdate() => Synchronize(Simulating);

        private void Synchronize(bool simulate)
        {
            foreach (EnemySpawner spawner in _spawners)
            {
                if (spawner == null) continue;
                if (spawner.enabled != simulate) spawner.enabled = simulate;
                EnemyBrain enemy = spawner.Current;
                if (enemy == null) continue;
                if (!simulate && enemy.enabled)
                {
                    if (enemy.Current == EnemyBrain.State.BeingInhaled) enemy.OnInhaleCancel();
                    EnemyAttackBehaviour attack = enemy.GetComponent<EnemyAttackBehaviour>();
                    if (attack != null) attack.Cancel();
                }
                NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
                bool resuming = simulate && !enemy.enabled;
                if (enemy.enabled != simulate) enemy.enabled = simulate;
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    if (!simulate) agent.isStopped = true;
                    else if (resuming)
                        agent.isStopped = enemy.Current == EnemyBrain.State.Attack || enemy.Current == EnemyBrain.State.Idle;
                    // During normal play the brain alone decides when to stop for an attack.
                }
            }
        }
    }
}
