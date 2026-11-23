using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class GuardianMusicReviewV118 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private Transform _arenaEntryPoint;
        [SerializeField] private WorldAudioV15 _audio;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.18 Guardian music: "+reason);}
        private AudioSource GuardianTrack()
        {
            foreach(AudioSource source in _audio.GetComponentsInChildren<AudioSource>(true))if(source.name=="Music Guardian")return source;
            return null;
        }
        [ContextMenu("Audio review/Run Guardian resolution checks (resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                Require(_arena.Ready,"Arena navigation is not ready.");
                _flow.Player.Teleport(_arenaEntryPoint.position,_arenaEntryPoint.rotation);yield return new WaitForSecondsRealtime(.25f);
                Require(_arena.Boss!=null&&_arena.Boss.EncounterBegun&&_audio.Region==MusicRegionV15.Guardian,"Real encounter did not select Guardian music.");
                AudioSource guardian=GuardianTrack();Require(guardian!=null,"Guardian music source is missing.");float before=guardian.volume;
                Health health=_arena.Boss.GetComponent<Health>();
                Require(health.TryApplyDamage(new DamageInfo(health.Max,health.transform.position,Vector3.zero,Team.Player,0,gameObject)),"Real Guardian defeat was rejected.");
                Vector3 p=_flow.Player.Player.position;MusicRegionV15 expected=EnvironmentMusicV15.Select(p.x,p.y,p.z,false);
                Require(_flow.HeartUnlocked&&_audio.Region==expected&&expected!=MusicRegionV15.Guardian,"Defeat did not release the Guardian music override.");
                _audio.EndGuardianMusic();Require(_audio.Region==expected,"Repeated resolution changed the regional target.");
                yield return new WaitForSecondsRealtime(1.25f);
                if(_audio.MusicVolume>0&&before>.001f)Require(guardian.volume<before,"Guardian track did not fade toward the new region.");
                Require(_audio.VoiceCapacity==8,"World SFX pool changed.");
                _flow.Pause();yield return new WaitForSecondsRealtime(.2f);Require(_audio.Region==expected,"Pause restored defeated-boss music.");
                _flow.Resume();yield return null;Require(_audio.Region==expected,"Resume restored defeated-boss music.");
                _flow.Pause();_flow.Retry();yield return null;
                Require(!_flow.HeartUnlocked&&_arena.Boss.EncounterBegun&&_audio.Region==MusicRegionV15.Guardian,"Boss retry failed to restore Guardian music.");
                _flow.ReturnToTitle();Require(_audio.Region==MusicRegionV15.Meadow,"Title retained encounter music.");
                _flow.StartNewGame();yield return null;Require(_audio.Region!=MusicRegionV15.Guardian,"New Game retained encounter music.");
                Debug.Log("V1.18 Guardian music checks PASSED. Audition the defeat tail, regional crossfade, ambience, mute and retry transitions manually.",this);
            }
            finally{if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
#endif
    }
}
