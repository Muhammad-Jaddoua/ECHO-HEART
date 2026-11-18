#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Reflection;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Self-checking test for EnemySpawner: respawn once per removal, cap, retry reset, boss-arena scope.
    /// Runs in EDIT mode like CombatSelfTest: Awake/OnEnable/Update do not run there, so they are invoked
    /// by hand and the spawner timer is driven through its internal Tick(). Test-only, never in a scene.
    /// </summary>
    public class SpawnerSelfTest : MonoBehaviour
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const BindingFlags StaticAny = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        [SerializeField] private bool _runOnStart;

        private int _passed;
        private int _failed;
        private int _slot;
        private readonly List<GameObject> _temp = new List<GameObject>();
        private readonly List<Object> _tempAssets = new List<Object>();

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Spawner Self Test")]
        public void Run()
        {
            _passed = 0;
            _failed = 0;
            SetActiveCount(0);
            try
            {
                CaseRespawnsAfterRemoved();
                CaseDuplicateRemovedDoesNotDoubleSpawn();
                CaseCapBlocksRespawn();
                CasePlayerDistanceBlocksRespawn();
                CaseResetRestoresEnemyAndCount();
                CaseBossArenaScopeIgnoredWhenFlagFalse();
                CaseBossArenaScopeAppliesWhenFlagTrue();
                CaseResetRespectsCapWhenSpawnOnStart();
                CaseMissedRemovedWhileDisabledIsRecovered();
            }
            finally
            {
                foreach (var go in _temp)
                    if (go != null) DestroyImmediate(go);
                foreach (var asset in _tempAssets)
                    if (asset != null) DestroyImmediate(asset);
                _temp.Clear();
                _tempAssets.Clear();
                SetActiveCount(0); // OnDestroy does not run in edit mode
            }
            Debug.Log($"[SpawnerSelfTest] {(_failed == 0 ? "ALL PASS" : "FAILURES")}: {_passed} passed, {_failed} failed");
        }

        // ---- helpers ----

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Any).SetValue(target, value);

        private static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Any).Invoke(target, args);

        private static void SetActiveCount(int value) =>
            typeof(EnemySpawner).GetField("s_active", StaticAny).SetValue(null, value);

        private void Check(string name, bool ok)
        {
            if (ok) _passed++; else _failed++;
            Debug.Log($"[SpawnerSelfTest] {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private Vector3 NextSpot() => new Vector3(7000f + 100f * _slot++, 0f, 7000f);

        private GameObject NewGo(string name, Vector3 position)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.position = position;
            _temp.Add(go);
            return go;
        }

        private sealed class Rig
        {
            public EnemySpawner Spawner;
            public EnemyBrain Brain;
            public Health Health;
            public Vector3 Spot;
        }

        /// <summary>A spawner owning a ready enemy; the first Tick spawns it (first spawn ignores distance).</summary>
        private Rig MakeSpawner(bool resetOnBossArena = false)
        {
            SetActiveCount(0); // every case starts from an empty cap
            Vector3 spot = NextSpot();

            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.hideFlags = HideFlags.HideAndDontSave;
            _tempAssets.Add(def);
            Set(def, "_maxHealth", 3);

            var enemyGo = NewGo("SpawnerSelfTest_Enemy", spot);
            enemyGo.layer = Layers.Enemy;
            enemyGo.SetActive(false); // a NavMeshAgent that wakes up off the NavMesh logs an error
            var brain = enemyGo.AddComponent<EnemyBrain>();
            var health = enemyGo.GetComponent<Health>();
            enemyGo.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            Set(health, "_maxHealth", 3);
            health.ResetHealth();
            Set(brain, "_definition", def);
            enemyGo.SetActive(true);
            Call(brain, "Awake");
            Call(brain, "OnEnable");

            var spawner = NewGo("SpawnerSelfTest_Spawner", spot).AddComponent<EnemySpawner>();
            Set(spawner, "_enemy", brain); // stands in for the Instantiate the spawner does from its prefab
            Set(spawner, "_resetOnBossArenaRetry", resetOnBossArena);
            Call(spawner, "EnsureEnemy"); // binds Removed
            Set(spawner, "_pending", true);
            Set(spawner, "_timer", 0f);
            spawner.Tick(0.01f); // first spawn

            return new Rig { Spawner = spawner, Brain = brain, Health = health, Spot = spot };
        }

        private static DamageInfo Hit(int amount) =>
            new DamageInfo(amount, Vector3.zero, Vector3.forward, Team.Player, AttackIds.Next(), null);

        private static void Defeat(Rig r) => r.Health.TryApplyDamage(Hit(3));

        // ---- cases ----

        private void CaseRespawnsAfterRemoved()
        {
            Rig r = MakeSpawner();
            bool spawned = r.Spawner.IsEnemyActive && EnemySpawner.ActiveCount == 1;

            Defeat(r);
            bool removed = !r.Spawner.IsEnemyActive && r.Spawner.PendingRespawn && EnemySpawner.ActiveCount == 0;

            r.Spawner.Tick(5f);
            bool waiting = !r.Spawner.IsEnemyActive;
            r.Spawner.Tick(1.5f); // 6.5 s total, past the 6 s delay

            Check("spawner respawns its enemy after the delay, restored to full HP at full count",
                spawned && removed && waiting && r.Spawner.IsEnemyActive && !r.Spawner.PendingRespawn &&
                r.Health.Current == 3 && r.Brain.Current == EnemyBrain.State.Idle && EnemySpawner.ActiveCount == 1);
        }

        private void CaseDuplicateRemovedDoesNotDoubleSpawn()
        {
            Rig r = MakeSpawner();
            Defeat(r);
            r.Spawner.Tick(3f);
            Call(r.Spawner, "HandleRemoved", r.Brain); // a second Removed must not restart the timer or count twice
            bool countStill = EnemySpawner.ActiveCount == 0;
            r.Spawner.Tick(3.1f);
            Check("duplicate Removed neither restarts the timer nor double-counts",
                countStill && r.Spawner.IsEnemyActive && EnemySpawner.ActiveCount == 1);
        }

        private void CaseCapBlocksRespawn()
        {
            SetActiveCount(0);
            Rig r = MakeSpawner();
            Defeat(r);
            SetActiveCount(EnemySpawner.MaxActive); // other spawners fill the cap
            r.Spawner.Tick(10f);
            bool blocked = !r.Spawner.IsEnemyActive && r.Spawner.PendingRespawn;

            SetActiveCount(EnemySpawner.MaxActive - 1);
            r.Spawner.Tick(0.6f); // retry interval is 0.5 s
            Check("respawn waits while the shared cap is full, then proceeds",
                blocked && r.Spawner.IsEnemyActive && EnemySpawner.ActiveCount == EnemySpawner.MaxActive);
            SetActiveCount(0);
        }

        private void CasePlayerDistanceBlocksRespawn()
        {
            Rig r = MakeSpawner();
            Defeat(r);

            var player = NewGo("SpawnerSelfTest_Player", r.Spot + Vector3.forward * 3f);
            player.layer = Layers.Player;
            player.AddComponent<SphereCollider>();
            Physics.SyncTransforms();

            r.Spawner.Tick(10f);
            bool blocked = !r.Spawner.IsEnemyActive;

            player.transform.position = r.Spot + Vector3.forward * 500f;
            Physics.SyncTransforms();
            r.Spawner.Tick(0.6f);
            Check("respawn waits while the player is inside the minimum distance",
                blocked && r.Spawner.IsEnemyActive);
        }

        private void CaseResetRestoresEnemyAndCount()
        {
            Rig r = MakeSpawner();
            Defeat(r);
            r.Brain.transform.position += Vector3.right * 4f;

            r.Spawner.ResetFor(RetryScope.MainArea);
            bool restored = r.Spawner.IsEnemyActive && !r.Spawner.PendingRespawn && r.Health.Current == 3 &&
                            r.Brain.Current == EnemyBrain.State.Idle &&
                            Vector3.Distance(r.Brain.transform.position, r.Spot) < 0.01f;
            bool countOne = EnemySpawner.ActiveCount == 1;

            r.Spawner.ResetFor(RetryScope.NewGame); // resetting a live enemy must not count it twice
            Check("ResetFor restores the enemy at home and keeps the active count at 1",
                restored && countOne && EnemySpawner.ActiveCount == 1);
        }

        private void CaseBossArenaScopeIgnoredWhenFlagFalse()
        {
            Rig r = MakeSpawner(resetOnBossArena: false);
            Defeat(r);
            r.Spawner.ResetFor(RetryScope.BossArena);
            Check("BossArena scope is ignored when the flag is off",
                !r.Spawner.IsEnemyActive && r.Spawner.PendingRespawn && EnemySpawner.ActiveCount == 0);
        }

        private void CaseBossArenaScopeAppliesWhenFlagTrue()
        {
            Rig r = MakeSpawner(resetOnBossArena: true);
            Defeat(r);
            r.Spawner.ResetFor(RetryScope.BossArena);
            Check("BossArena scope resets the enemy when the flag is on",
                r.Spawner.IsEnemyActive && !r.Spawner.PendingRespawn && r.Health.Current == 3 &&
                EnemySpawner.ActiveCount == 1);
        }

        private void CaseResetRespectsCapWhenSpawnOnStart()
        {
            Rig r = MakeSpawner(); // _spawnOnStart defaults to true
            Defeat(r);
            SetActiveCount(EnemySpawner.MaxActive); // other spawners fill the cap

            r.Spawner.ResetFor(RetryScope.MainArea);
            bool blocked = !r.Spawner.IsEnemyActive && r.Spawner.PendingRespawn &&
                           EnemySpawner.ActiveCount == EnemySpawner.MaxActive;

            SetActiveCount(0);
            r.Spawner.Tick(7f); // past the respawn delay once the cap frees up
            Check("ResetFor with spawn-on-start falls back to the delayed respawn when the cap is full",
                blocked && r.Spawner.IsEnemyActive && EnemySpawner.ActiveCount == 1);
        }

        private void CaseMissedRemovedWhileDisabledIsRecovered()
        {
            Rig r = MakeSpawner();
            Call(r.Spawner, "OnDisable");          // Removed unsubscribed, count released
            r.Brain.gameObject.SetActive(false);   // enemy captured/defeated while we were disabled
            Call(r.Spawner, "OnEnable");
            Check("OnEnable recovers a Removed missed while disabled (pending respawn, not stuck alive)",
                !r.Spawner.IsEnemyActive && r.Spawner.PendingRespawn && EnemySpawner.ActiveCount == 0);
        }
    }
}
#endif
