using System.Collections.Generic;
using Echoheart.AI;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Echoheart.World
{
    /// <summary>One main-world build, before population/flow Start. The boss keeps its own navigation data.</summary>
    [DefaultExecutionOrder(-450)]
    public sealed class MainNavigationV14 : MonoBehaviour
    {
        [SerializeField] private Transform _worldRoot;
        [SerializeField] private EnemyBrain[] _enemyPrefabs;
        [SerializeField] private Transform[] _sourcePoints;
        [SerializeField] private Bounds _bounds = new Bounds(new Vector3(0f, 30f, 0f), new Vector3(240f, 100f, 240f));
        [SerializeField] private Bounds _arenaExclusion = new Bounds(new Vector3(42f, 30f, 102f), new Vector3(34f, 100f, 34f));
        private NavMeshData _data;
        private NavMeshDataInstance _instance;

        public bool Ready { get; private set; }
        public int SourceCount { get; private set; }
        public float BuildMilliseconds { get; private set; }

        private void Start()
        {
            if (_worldRoot == null || _enemyPrefabs == null || _enemyPrefabs.Length != 3 ||
                _sourcePoints == null || _sourcePoints.Length != 6)
            {
                Fail("Assign the main landscape, three enemy prefabs and six source points.");
                return;
            }
            int agentType = -1;
            float radius = 0f, height = 0f;
            foreach (EnemyBrain prefab in _enemyPrefabs)
            {
                if (prefab == null || !prefab.TryGetComponent(out NavMeshAgent agent))
                {
                    Fail("A source enemy prefab is missing its NavMeshAgent.");
                    return;
                }
                if (agentType != -1 && agentType != agent.agentTypeID)
                {
                    Fail("Source enemies must share one navigation agent type.");
                    return;
                }
                agentType = agent.agentTypeID;
                radius = Mathf.Max(radius, agent.radius);
                height = Mathf.Max(height, agent.height);
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentType);
            if (settings.agentTypeID < 0) { Fail("Missing navigation build settings for the source enemies."); return; }
            settings.agentRadius = radius;
            settings.agentHeight = height;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.2f;
            settings.overrideTileSize = true;
            settings.tileSize = 128;
            var sources = new List<NavMeshBuildSource>();
            Physics.SyncTransforms();
            float started = Time.realtimeSinceStartup;
            NavMeshBuilder.CollectSources(_worldRoot, Layers.EnvironmentMask,
                NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            SourceCount = sources.Count;
            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            if (SourceCount == 0 || notWalkable < 0) { Fail("No environment colliders or Not Walkable area found."); return; }
            // Main terrain also extends beneath the arena. Exclude that footprint from THIS build
            // so the Guardian's larger-radius surface remains the only navigation inside its walls.
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.ModifierBox,
                transform = Matrix4x4.TRS(_arenaExclusion.center, Quaternion.identity, Vector3.one),
                size = _arenaExclusion.size,
                area = notWalkable
            });
            _data = NavMeshBuilder.BuildNavMeshData(settings, sources, _bounds, Vector3.zero, Quaternion.identity);
            if (_data == null) { Fail("Main-world navigation did not build."); return; }
            _instance = NavMesh.AddNavMeshData(_data);
            BuildMilliseconds = (Time.realtimeSinceStartup - started) * 1000f;
            if (!_instance.valid) { Fail("Main-world navigation could not be registered."); return; }
            var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };
            var positions = new Vector3[_sourcePoints.Length];
            for (int i = 0; i < _sourcePoints.Length; i++)
            {
                Transform point = _sourcePoints[i];
                if (point == null || _arenaExclusion.Contains(point.position) ||
                    !NavMesh.SamplePosition(point.position, out NavMeshHit hit, 1.5f, filter))
                {
                    Fail("Source point " + i + " has no safe main-world NavMesh position. Population remains absent.");
                    return;
                }
                positions[i] = hit.position;
            }
            // Commit only when ALL sources are valid: never create a partial/unbalanced set.
            for (int i = 0; i < _sourcePoints.Length; i++) _sourcePoints[i].position = positions[i];
            Ready = true;
        }

        private void Fail(string reason) => Debug.LogError("V1.4 navigation: " + reason, this);

        private void OnDestroy()
        {
            if (_instance.valid) _instance.Remove();
            if (_data != null) Destroy(_data);
        }
    }
}
