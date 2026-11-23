using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class TitleAudioReviewV124 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldAudioV15 _audio;
        [SerializeField] private WorldAudioBankV15 _bank;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private Transform _arenaEntryPoint;
        [SerializeField] private IslandHeartV13 _heart;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("V1.24 title audio: " + reason); }
        private AudioSource Stinger()
        {
            foreach (AudioSource source in _audio.GetComponentsInChildren<AudioSource>(true))
                if (source.name == "Session stingers") return source;
            throw new InvalidOperationException("V1.24 session-stinger source is missing.");
        }
        private void RequireCue(AudioSource source, WorldCueV15 cue)
        {
            Require(_bank.Cue(cue) != null && source.clip == _bank.Cue(cue) && source.isPlaying,
                "Expected session cue is not playing: " + cue);
        }
        private void DefeatPlayer()
        {
            Health health = _flow.Player.Health; health.ResetHealth();
            Require(health.TryApplyDamage(new DamageInfo(health.Max, health.transform.position, Vector3.zero, Team.Enemy, 0, gameObject)),
                "Real player defeat was rejected.");
            Require(_flow.State == GameState.Dead, "Player defeat did not enter the death state.");
        }
        private void ReturnToTitleAndCheck(AudioSource stinger)
        {
            AudioSource[] sources = _audio.GetComponentsInChildren<AudioSource>(true);
            bool[] playing = new bool[sources.Length]; int tracks = 0;
            for (int i = 0; i < sources.Length; i++)
                if (sources[i].name.StartsWith("Music ", StringComparison.Ordinal)) { playing[i] = sources[i].isPlaying; tracks++; }
            AudioClip cue = stinger.clip;
            float music = _audio.MusicVolume, sfx = _audio.SfxVolume;
            _flow.ReturnToTitle();
            Require(_flow.State == GameState.Title && !stinger.isPlaying, "Old session cue continued on title.");
            Require(stinger.clip == cue, "Stopping the cue changed its clip binding.");
            Require(_audio.MusicVolume == music && _audio.SfxVolume == sfx && _audio.VoiceCapacity == 8, "Audio options/pool changed.");
            Require(tracks == 6 && _audio.Region == MusicRegionV15.Meadow, "Title music routing changed.");
            // This checks the transition synchronously; the original crossfade continues on later frames.
            for (int i = 0; i < sources.Length; i++)
                if (sources[i].name.StartsWith("Music ", StringComparison.Ordinal))
                    Require(sources[i].isPlaying == playing[i], "Title cleanup directly stopped/started a music track.");
        }
        [ContextMenu("Audio review/Run title cue cleanup checks (resets run)")]
        private void Run() { if (Application.isPlaying && !_running) StartCoroutine(Check()); }
        private IEnumerator Check()
        {
            _running = true;
            try
            {
                Require(_flow != null && _audio != null && _bank != null && _arena != null && _arenaEntryPoint != null && _heart != null,
                    "Review bindings missing.");
                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return null;
                AudioSource stinger = Stinger();
                DefeatPlayer(); RequireCue(stinger, WorldCueV15.Death); ReturnToTitleAndCheck(stinger);

                _flow.StartNewGame(); yield return null;
                DefeatPlayer(); RequireCue(stinger, WorldCueV15.Death);
                _flow.Retry(); Require(_flow.State == GameState.Playing && !stinger.isPlaying, "Existing retry cleanup failed.");

                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return null;
                Require(_arena.Ready, "Arena navigation is not ready.");
                _flow.Player.Teleport(_arenaEntryPoint.position, _arenaEntryPoint.rotation);
                yield return new WaitForSecondsRealtime(.25f);
                Require(_arena.Boss != null && _arena.Boss.EncounterBegun, "Real Guardian encounter did not start.");
                _flow.Player.SetControlsEnabled(false);
                Health guardian = _arena.Boss.GetComponent<Health>();
                Require(guardian.TryApplyDamage(new DamageInfo(guardian.Max, guardian.transform.position, Vector3.zero, Team.Player, 0, gameObject)),
                    "Real Guardian defeat was rejected.");
                Require(_flow.HeartUnlocked, "Guardian defeat did not unlock the Heart.");
                _flow.Player.Teleport(_heart.transform.position + Vector3.back * .8f, Quaternion.identity);
                Require(_heart.TryInteract() && _flow.State == GameState.Complete, "Heart restoration did not complete the run.");
                RequireCue(stinger, WorldCueV15.Restore); ReturnToTitleAndCheck(stinger);
                Debug.Log("V1.24 title cue cleanup PASSED for real death and Heart completion, with original retry cleanup and synchronous music isolation. Audition the title transitions and crossfades manually.", this);
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
