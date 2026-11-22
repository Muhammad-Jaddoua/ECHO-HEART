using System.Collections.Generic;
using Echoheart.AI;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Echoheart.World
{
    /// <summary>Builds arena-only navigation and activates the existing Guardian on entry.</summary>
    [DefaultExecutionOrder(-400)]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ArenaEncounterV13 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private BossBrain _bossPrefab;
        [SerializeField] private Transform _arenaRoot;
        [SerializeField] private Transform _bossPoint;
        [SerializeField] private GameObject _gate;
        [SerializeField] private Bounds _navigationBounds = new Bounds(new Vector3(42f, 10f, 102f), new Vector3(32f, 16f, 32f));
        private BossBrain _boss;
        private NavMeshData _data;
        private NavMeshDataInstance _instance;
        private bool _ready;
        private bool _entered;
        private Collider _volume;

        public BossBrain Boss => _boss;
        public bool Ready => _ready;

        private void Awake() => _volume = GetComponent<Collider>();

        private void OnEnable()
        {
            GameEvents.StateChanged += HandleState;
            GameEvents.BossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= HandleState;
            GameEvents.BossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            if (_gate != null) _gate.SetActive(false);
            if (_arenaRoot == null || _bossPrefab == null || _bossPoint == null || _flow == null)
            {
                Debug.LogError("V1.3 arena is missing its prefab, bounds, spawn point or flow reference.", this);
                return;
            }
            NavMeshAgent prefabAgent = _bossPrefab.GetComponent<NavMeshAgent>();
            if (prefabAgent == null)
            {
                Debug.LogError("The Guardian prefab has no NavMeshAgent.", this);
                return;
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(prefabAgent.agentTypeID);
            if (settings.agentTypeID < 0)
            {
                Debug.LogError("No navigation build settings exist for the Guardian's agent type.", this);
                return;
            }
            settings.agentRadius = prefabAgent.radius;
            settings.agentHeight = prefabAgent.height;
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(_arenaRoot, Layers.EnvironmentMask,
                NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            _data = NavMeshBuilder.BuildNavMeshData(settings, sources, _navigationBounds, Vector3.zero, Quaternion.identity);
            if (_data == null)
            {
                Debug.LogError("Arena navigation failed to build; the encounter cannot begin.", this);
                return;
            }
            _instance = NavMesh.AddNavMeshData(_data);
            _ready = _instance.valid && NavMesh.SamplePosition(_bossPoint.position, out _, 2f, NavMesh.AllAreas);
            if (!_ready) Debug.LogError("The Guardian spawn is outside the built arena NavMesh.", this);
        }

        private void OnTriggerEnter(Collider other) => TryEnter(other);
        private void OnTriggerStay(Collider other) => TryEnter(other);

        private void TryEnter(Collider other)
        {
            if (!_ready || _entered || other.gameObject.layer != Layers.Player || _flow.State != GameState.Playing) return;
            if (_flow.Player == null || _flow.Player.Player == null || !_volume.bounds.Contains(_flow.Player.Player.position)) return;
            if (!_flow.EnterArena()) return;
            _entered = true;
            EnsureBoss();
            HandleState(GameState.Playing);
        }

        private void EnsureBoss()
        {
            if (_boss == null) _boss = Instantiate(_bossPrefab, _bossPoint.position, _bossPoint.rotation);
            _boss.gameObject.SetActive(true);
        }

        private void HandleState(GameState state)
        {
            if (_boss == null) return;
            bool simulate = state == GameState.Playing && _entered;
            _boss.enabled = simulate;
            if (simulate && !_flow.HeartUnlocked && !_boss.GetComponent<Echoheart.Combat.Health>().IsDead)
                _boss.BeginEncounter(); // idempotent on resume, re-armed by BossBrain.ResetFor on retry
            if (_gate != null) _gate.SetActive(_entered && !_flow.HeartUnlocked && state != GameState.Title);
        }

        private void HandleBossDefeated()
        {
            if (_gate != null) _gate.SetActive(false);
        }

        public void AfterReset(RetryScope scope)
        {
            if (scope == RetryScope.NewGame || scope == RetryScope.MainArea)
            {
                _entered = false;
                if (_boss != null) _boss.gameObject.SetActive(false);
                if (_gate != null) _gate.SetActive(false);
            }
            else
            {
                _entered = true;
                EnsureBoss();
                if (_gate != null) _gate.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (_instance.valid) _instance.Remove();
            if (_data != null) Destroy(_data);
            if (_boss != null) Destroy(_boss.gameObject);
        }
    }
}
