using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class WorldPolishReviewV15 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private SecretChestV15[] _chests;
        [SerializeField] private WorldAudioBankV15 _bank;
        [SerializeField] private WorldAudioV15 _audio;
        [SerializeField] private WorldParticlesV15 _particles;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException("V1.5 world acceptance: " + reason); }
        [ContextMenu("World polish/Run chest and asset checks (resets run)")]
        private void RunChecks() { if (Application.isPlaying && !_running) StartCoroutine(CheckWorld()); }
        private IEnumerator CheckWorld()
        {
            _running = true;
            try
            {
                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return null;
                Require(_flow.Relics.Total == 16 && _chests.Length == 2, "Expected 16 existing relics and two chests.");
                Require(_audio.VoiceCapacity == 8 && _particles.BurstCapacity == 12, "Presentation pools are unbounded or misconfigured.");
                for (int i = 0; i < 6; i++) Require(_bank.Music(i) != null && _bank.Music(i).length > 20f, "Music loop missing.");
                foreach (WorldCueV15 cue in Enum.GetValues(typeof(WorldCueV15))) Require(_bank.Cue(cue) != null, "Sound effect missing: " + cue);
                foreach (SecretChestV15 chest in _chests)
                {
                    Collider trigger = chest.Reward.GetComponent<Collider>();
                    Require(!chest.Opened && !trigger.enabled && !chest.Reward.Collected, "Closed chest exposed its reward.");
                    _flow.Player.Teleport(chest.Reward.transform.position, Quaternion.identity);
                    yield return new WaitForSecondsRealtime(.2f);
                    Require(!chest.Reward.Collected, "A locked chest reward was collected through its trigger.");
                    _flow.Pause(); Require(!chest.TryOpen(), "Chest opened through Pause."); _flow.Resume();
                    Require(chest.TryOpen() && !chest.TryOpen(), "Open/repeated-open policy failed.");
                    chest.ResetFor(RetryScope.MainArea); chest.ResetFor(RetryScope.BossArena);
                    Require(chest.Opened, "Area retry re-locked a discovered chest.");
                    _flow.Player.Teleport(chest.Reward.transform.position + Vector3.back * .9f, Quaternion.identity);
                    yield return new WaitForSecondsRealtime(.25f);
                    _flow.Relics.Refresh(true); Require(chest.Reward.Collected, "Revealed relic did not collect through physics.");
                    // Exercise both reset orders: the old pickup never owns the chest's collider gate.
                    chest.ResetFor(RetryScope.NewGame); chest.Reward.ResetFor(RetryScope.NewGame);
                    Require(!chest.Opened && !trigger.enabled, "Reward reset after chest bypassed its gate.");
                    chest.Reward.ResetFor(RetryScope.NewGame); chest.ResetFor(RetryScope.NewGame);
                    Require(!chest.Opened && !trigger.enabled, "Chest reset after reward bypassed its gate.");
                }
                _flow.ReturnToTitle(); _flow.StartNewGame(); _flow.Relics.Refresh(true);
                Require(_flow.Relics.Collected == 0 && _flow.Relics.Total == 16, "New Game duplicated or retained relics.");
                Debug.Log("V1.5 Unity chest/asset acceptance PASSED. Listen to every region, inspect particles/telegraphs and test both volume sliders manually.", this);
            }
            finally { if (_flow != null && Application.isPlaying) _flow.ReturnToTitle(); _running = false; }
        }
#endif
    }
}
