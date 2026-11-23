using System;
using System.Collections;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    /// <summary>Editor/development acceptance actions, on an EditorOnly scene object. Never a gameplay shortcut.</summary>
    public sealed class FlowReviewHarnessV114 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private IslandHeartV13 _heart;
        [SerializeField] private FlowMenuV114 _menus;
        [SerializeField] private Transform _arenaEntryPoint;
        [SerializeField] private Transform _heartApproach;
        [SerializeField] private AbilityDefinition _sword;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;

        [ContextMenu("Flow review/1 - Defeat player")]
        private void DefeatPlayer()
        {
            if (!Application.isPlaying || _flow.State != GameState.Playing) return;
            Health health = _flow.Player.Health;
            health.TryApplyDamage(new DamageInfo(health.Max, health.transform.position, Vector3.zero, Team.Enemy, 0, gameObject));
        }

        [ContextMenu("Flow review/2 - Move to arena entry")]
        private void MoveToArena()
        {
            if (Application.isPlaying && _flow.State == GameState.Playing)
                _flow.Player.Teleport(_arenaEntryPoint.position, _arenaEntryPoint.rotation);
        }

        [ContextMenu("Flow review/3 - Defeat Guardian for flow test")]
        private void DefeatGuardian()
        {
            if (!Application.isPlaying || _flow.State != GameState.Playing || _arena.Boss == null) return;
            Health health = _arena.Boss.GetComponent<Health>();
            health.TryApplyDamage(new DamageInfo(health.Max, health.transform.position, Vector3.zero, Team.Player, 0, gameObject));
        }

        [ContextMenu("Flow review/4 - Move to Heart")]
        private void MoveToHeart()
        {
            if (Application.isPlaying && _flow.State == GameState.Playing)
                _flow.Player.Teleport(_heartApproach.position, _heartApproach.rotation);
        }

        [ContextMenu("Flow review/Run acceptance checks (resets run)")]
        private void RunChecks()
        {
            if (!Application.isPlaying || _running) return;
            StartCoroutine(CheckFlow());
        }

        private static void Require(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException("V1.9 flow acceptance: " + message);
        }

        private IEnumerator CheckFlow()
        {
            _running = true;
            int completionEvents = 0;
            Action completed = () => completionEvents++;
            GameEvents.GameCompleted += completed;
            try
            {
                _flow.ReturnToTitle();
                _flow.StartNewGame();
                yield return null;
                Require(_flow.State == GameState.Playing && Time.timeScale == 1f, "New Game did not unfreeze gameplay.");
                Require(_flow.Player.Health.Current == _flow.Player.Health.Max, "New Game did not restore health.");
                Require(_flow.Relics.Collected == 0, "New Game retained old relics.");
                Require(!_heart.TryInteract(), "Heart accepted completion before the boss.");
                _flow.Pause();
                Require(Time.timeScale == 0f && _menus.VisibleState == GameState.Paused, "Pause did not show/freeze correctly.");
                _flow.Resume();
                Require(Time.timeScale == 1f, "Resume left time frozen.");

                HiddenRelicV12 relic = _flow.Relics.Relics[0];
                _flow.Player.Teleport(relic.transform.position, Quaternion.identity);
                yield return new WaitForSecondsRealtime(0.35f);
                _flow.Relics.Refresh(true);
                Require(relic.Collected && _flow.Relics.Collected == 1, "Trigger pickup did not collect exactly once.");
                _flow.Player.ApplyAbility(_sword);
                DefeatPlayer();
                Require(_flow.State == GameState.Dead && Time.timeScale == 0f, "Player death did not show Death.");
                _flow.Retry();
                Require(_flow.State == GameState.Playing && _flow.CurrentRetryScope == RetryScope.MainArea, "Main retry used the wrong checkpoint.");
                Require(_flow.Player.CurrentAbility == null, "Main retry kept a power.");
                Require(_flow.Player.Health.Current == _flow.Player.Health.Max && _flow.Relics.Collected == 1, "Main retry health/relic policy failed.");

                _flow.ReturnToTitle();
                _flow.StartNewGame();
                Require(!relic.Collected && _flow.Relics.Collected == 0, "Inactive relic was not reset on New Game.");
                Require(_arena.Ready, "Runtime arena NavMesh is not ready; inspect the Console.");
                _flow.Player.ApplyAbility(_sword);
                MoveToArena();
                yield return new WaitForSecondsRealtime(0.25f);
                Require(_flow.CurrentRetryScope == RetryScope.BossArena && _arena.Boss != null && _arena.Boss.EncounterBegun, "Arena entry did not start the existing BossBrain.");
                DefeatPlayer();
                Require(_flow.State == GameState.Dead, "Boss-area death was not reported.");
                _flow.Retry();
                Require(_flow.Player.CurrentAbility == _sword, "Boss retry lost the selected power.");
                Require(_arena.Boss.EncounterBegun, "Boss retry failed to restart the encounter.");
                Health bossHealth = _arena.Boss.GetComponent<Health>();
                Require(bossHealth.Current == bossHealth.Max, "Boss retry did not restore Guardian health.");
                Require(!_flow.HeartUnlocked, "Boss retry left the Heart unlocked.");

                DefeatGuardian();
                Require(_flow.HeartUnlocked, "Actual boss death did not unlock the Heart.");
                MoveToHeart();
                Require(_heart.TryInteract(), "Nearby Heart interaction did not complete.");
                Require(!_heart.TryInteract() && completionEvents == 1, "Completion fired more than once.");
                Require(_flow.State == GameState.Complete && _menus.VisibleState == GameState.Complete && Time.timeScale == 0f, "Completion screen/state is inconsistent.");
                _flow.StartNewGame();
                Require(!_flow.HeartUnlocked && !_heart.Restored && _flow.Relics.Collected == 0, "Play Again did not clear run progress.");
                Debug.Log("V1.9 Unity flow acceptance checks PASSED. Combat balance and production-player integration need their own playtest.", this);
            }
            finally
            {
                GameEvents.GameCompleted -= completed;
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
