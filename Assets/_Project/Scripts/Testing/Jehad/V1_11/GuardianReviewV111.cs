using System;
using System.Collections;
using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class GuardianReviewV111 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private GuardianFeedbackV111 _presentation;
        [SerializeField] private Transform _arenaEntryPoint;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.11 Guardian presentation: "+reason);}
        [ContextMenu("Guardian review/Run presentation checks (resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                Require(_arena.Ready,"Arena navigation is not ready.");
                _flow.Player.Teleport(_arenaEntryPoint.position,_arenaEntryPoint.rotation);yield return new WaitForSecondsRealtime(.25f);
                BossBrain boss=_arena.Boss;Require(boss!=null&&boss.EncounterBegun,"Arena did not create the real Guardian.");
                Require(_presentation.DedicatedVoiceCapacity==2,"Dedicated boss audio pool is not two voices.");
                Require(boss.GetComponent<GuardianFeedbackV15>()==null,"Legacy boss presenter is also active.");
                Require(boss.GetComponents<ActorFeedbackV15>().Length==1,"Boss hit presentation has duplicate owners.");
                Require(boss.GetComponent<BossTelegraphVisual>()==null||!boss.GetComponent<BossTelegraphVisual>().enabled,"Prefab telegraph is duplicating the new warning.");
                Require(_presentation.Muzzle!=null&&_presentation.Muzzle.name=="Muzzle","Authored muzzle binding is missing.");
                _flow.Player.Teleport(boss.transform.position+Vector3.forward*2+Vector3.up*.5f,Quaternion.identity);
                float deadline=Time.unscaledTime+4;
                while((boss.Current!=BossBrain.State.Telegraph||boss.CurrentAttack!=BossBrain.AttackKind.Slam)&&Time.unscaledTime<deadline&&_flow.State==GameState.Playing)yield return null;
                yield return null;
                Require(boss.Current==BossBrain.State.Telegraph&&boss.CurrentAttack==BossBrain.AttackKind.Slam&&_presentation.WarningVisible,"No visible slam warning was reached.");
                Vector3 delta=_presentation.Boundary.GetPosition(0)-boss.transform.position;delta.y=0;
                Require(Mathf.Abs(delta.magnitude-boss.Definition.SlamRadius)<.02f,"Slam boundary differs from the actual configured damage radius.");
                int warnings=_presentation.WarningCueCount;float progress=boss.TelegraphProgress;
                _flow.Pause();yield return new WaitForSecondsRealtime(.2f);
                Require(!_presentation.WarningVisible&&boss.TelegraphProgress==progress,"Paused warning kept animating or stayed visible.");
                _flow.Resume();yield return null;
                Require(_presentation.WarningCueCount==warnings,"Resume replayed the same warning cue.");
                _flow.Pause();_flow.Retry();yield return null;
                Require(boss.EncounterBegun&&boss.GetComponent<Health>().Current==boss.GetComponent<Health>().Max,"Boss retry did not re-arm the original encounter.");
                Require(!_presentation.WarningVisible&&_presentation.DedicatedVoiceCapacity==2,"Retry retained warning geometry or grew the audio pool.");
                _flow.ReturnToTitle();Require(!_presentation.WarningVisible,"Title retained warning geometry.");
                Debug.Log("V1.11 Guardian presentation checks PASSED. Audition both attack types and inspect ranged cover projection in a standalone playtest.",this);
            }
            finally{if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
#endif
    }
}
