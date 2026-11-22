using System;
using System.Collections;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class ObjectiveGuideReviewV112 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private ObjectiveGuideV112 _guide;
        [SerializeField] private IslandHeartV13 _heart;
        [SerializeField] private Transform _arenaEntry;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.12 objective guide: "+reason);}
        [ContextMenu("Objective review/Run guide checks (resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                Require(_arena.Ready,"Arena is not ready.");var prefs=original;prefs.Hints=true;_settings.Set(prefs);_guide.RefreshGuide();
                Require(_guide.Visible&&_guide.CurrentGoal==GuideGoalV112.Guardian,"Exploration did not show Guardian guidance.");CheckBounds();
                prefs.Hints=false;_settings.Set(prefs);_guide.RefreshGuide();Require(!_guide.Visible,"Hints-off retained the guide.");
                prefs.Hints=true;_settings.Set(prefs);_flow.Pause();_guide.RefreshGuide();Require(!_guide.Visible,"Pause retained the guide.");
                _flow.Resume();GameEvents.RaisePromptChanged("E  Guide suppression test");_guide.RefreshGuide();Require(!_guide.Visible,"Guide covered an interaction prompt.");
                GameEvents.RaisePromptChanged(string.Empty);_guide.RefreshGuide();Require(_guide.Visible,"Guide did not return after the prompt cleared.");
                _flow.Player.Teleport(_arenaEntry.position,_arenaEntry.rotation);yield return new WaitForSecondsRealtime(.25f);_guide.RefreshGuide();
                Require(_flow.CurrentRetryScope==RetryScope.BossArena&&!_guide.Visible,"Guide remained visible during the boss fight.");
                _flow.Pause();_flow.Retry();yield return null;_guide.RefreshGuide();Require(!_guide.Visible,"Boss retry showed the entrance guide.");
                // Presentation-flow test only; this does not validate player attacks or boss balance.
                Health health=_arena.Boss.GetComponent<Health>();health.TryApplyDamage(new DamageInfo(health.Max,health.transform.position,Vector3.zero,Team.Player,0,gameObject));
                _guide.RefreshGuide();Require(_flow.HeartUnlocked&&_guide.CurrentGoal==GuideGoalV112.Heart&&_guide.Visible,"Defeat did not switch to Heart guidance.");CheckBounds();
                _flow.Player.Teleport(_heart.transform.position,Quaternion.identity);Require(_heart.TryInteract(),"Heart did not complete through its real interaction.");
                _guide.RefreshGuide();Require(!_guide.Visible,"Completion retained the guide.");
                _flow.StartNewGame();yield return null;_guide.RefreshGuide();Require(_guide.CurrentGoal==GuideGoalV112.Guardian,"New Game retained the Heart target.");
                Debug.Log("V1.12 objective guide checks PASSED. Inspect camera bearings, behind-camera arrows and all aspect ratios manually.",this);
            }
            finally{_settings.Set(original);GameEvents.RaisePromptChanged(string.Empty);if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_guide.RefreshGuide();_running=false;}
        }
        private void CheckBounds()
        {
            Canvas.ForceUpdateCanvases();Rect safe=Screen.safeArea;safe.xMin-=2;safe.yMin-=2;safe.xMax+=2;safe.yMax+=2;var corners=new Vector3[4];_guide.Marker.GetWorldCorners(corners);
            foreach(Vector3 corner in corners)Require(safe.Contains(new Vector2(corner.x,corner.y)),"Marker escaped the safe area.");
        }
#endif
    }
}
