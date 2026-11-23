using System;
using System.Collections;
using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.AI;

namespace Echoheart.Testing
{
    /// <summary>Engine acceptance aid, not a replacement for production-player inhale/combat tests.</summary>
    public sealed class PopulationReviewV14 : MonoBehaviour
    {
        [SerializeField] private MainNavigationV14 _navigation;
        [SerializeField] private EnemyPopulationV14 _population;
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private Transform _mainPoint;
        [SerializeField] private Transform _arenaEntry;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;

        [ContextMenu("Population review/Run acceptance checks (resets run)")]
        private void RunChecks()
        {
            if (Application.isPlaying && !_running) StartCoroutine(CheckPopulation());
        }

        private static void Require(bool condition, string reason)
        {
            if (!condition) throw new InvalidOperationException("V1.4 population acceptance: " + reason);
        }

        private void RequireFrozen()
        {
            Require(!_population.Simulating, "Population is simulating outside main-area play.");
            foreach (EnemySpawner spawner in _population.Spawners)
            {
                Require(!spawner.enabled, "A frozen spawner is ticking.");
                if (spawner.Current != null) Require(!spawner.Current.enabled, "A frozen brain is ticking.");
            }
        }

        private IEnumerator CheckPopulation()
        {
            _running = true;
            try
            {
                Require(_navigation.Ready && _population.Ready, "Navigation/population setup failed; inspect earlier Console errors.");
                Require(_navigation.SourceCount > 0, "Navigation collected no environment geometry.");
                _flow.ReturnToTitle();
                RequireFrozen();
                _flow.StartNewGame();
                yield return null;
                Require(_population.Spawners.Length == 6 && EnemySpawner.ActiveCount == 6, "New Game did not create the six capped sources.");
                int[] powers = new int[4];
                Require(NavMesh.SamplePosition(_mainPoint.position, out NavMeshHit start, 2f, NavMesh.AllAreas), "Entry checkpoint is off NavMesh.");
                foreach (EnemySpawner spawner in _population.Spawners)
                {
                    EnemyBrain enemy = spawner.Current;
                    Require(spawner.enabled && spawner.IsEnemyActive && enemy != null && enemy.enabled, "A main-area source is inactive.");
                    Require(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "An enemy is off NavMesh.");
                    var path = new NavMeshPath();
                    Require(NavMesh.CalculatePath(start.position, enemy.transform.position, NavMesh.AllAreas, path) &&
                        path.status == NavMeshPathStatus.PathComplete, "A source is disconnected from the entry.");
                    Require(enemy.GrantedAbility != null, "A source has no granted power.");
                    powers[(int)enemy.GrantedAbility.Type]++;
                }
                Require(powers[(int)AbilityType.Sword] == 2 && powers[(int)AbilityType.Bow] == 2 && powers[(int)AbilityType.Fire] == 2,
                    "Each power must be available at the entry and boss approach.");

                EnemySpawner source = _population.Spawners[0];
                EnemyBrain captured = source.Current;
                _flow.Player.Teleport(captured.transform.position + Vector3.up * 1.1f, Quaternion.identity);
                Require(captured.CanBeInhaled, "The fresh source cannot begin a capture.");
                captured.OnInhaleStart(_flow.Player.Player);
                captured.OnCaptured();
                Require(!captured.gameObject.activeSelf && source.PendingRespawn && !source.IsEnemyActive, "Capture did not park/replenish the source.");
                Require(_flow.Player.CurrentAbility == null, "World population granted an ability; that belongs to the player holder.");
                yield return new WaitForSecondsRealtime(6.8f);
                Require(source.PendingRespawn && !source.IsEnemyActive, "Source respawned while the player was too close.");
                _flow.Player.Teleport(_mainPoint.position, _mainPoint.rotation);
                yield return new WaitForSecondsRealtime(0.7f);
                Require(source.IsEnemyActive && source.Current == captured && captured.gameObject.activeSelf,
                    "Source did not recycle the same enemy after the player moved away.");
                Require(EnemySpawner.ActiveCount <= EnemySpawner.MaxActive, "Replenishment exceeded the cap.");

                Health health = captured.GetComponent<Health>();
                health.TryApplyDamage(new DamageInfo(health.Max, captured.transform.position, Vector3.zero, Team.Player, 0, gameObject));
                Require(source.PendingRespawn && !source.IsEnemyActive && _flow.Player.CurrentAbility == null,
                    "Defeat did not replenish, or incorrectly granted a power.");
                _flow.Pause();
                RequireFrozen();
                Vector3 position = captured.transform.position;
                yield return new WaitForSecondsRealtime(0.3f);
                Require(Vector3.Distance(position, captured.transform.position) < 0.001f, "Enemy moved while paused.");
                _flow.Retry();
                foreach (EnemySpawner spawner in _population.Spawners)
                {
                    Health resetHealth = spawner.Current.GetComponent<Health>();
                    Require(spawner.IsEnemyActive && resetHealth.Current == resetHealth.Max, "Main retry did not reset an owned enemy.");
                }
                Require(EnemySpawner.ActiveCount == 6, "Main retry corrupted the shared active count.");

                _flow.Player.Teleport(_arenaEntry.position, _arenaEntry.rotation);
                yield return new WaitForSecondsRealtime(0.3f);
                Require(_flow.CurrentRetryScope == RetryScope.BossArena, "Actual arena trigger did not begin the boss encounter.");
                RequireFrozen();
                _flow.Pause();
                _flow.Retry();
                Require(_flow.State == GameState.Playing && _flow.CurrentRetryScope == RetryScope.BossArena, "Boss retry lost its checkpoint.");
                RequireFrozen();
                _flow.ReturnToTitle();
                _flow.StartNewGame();
                Require(_population.Simulating && EnemySpawner.ActiveCount == 6, "A fresh run did not re-enable main-area sources.");
                Debug.Log("V1.4 Unity population acceptance checks PASSED. Production inhale/combat, boss wins and performance still need playtesting.", this);
            }
            finally
            {
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
