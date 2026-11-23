using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoheart.Testing
{
    public sealed class UiReviewHarnessV16 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV16 _menu;
        [SerializeField] private UiSettingsV16 _settings;
        [SerializeField] private MenuAudioV13 _volumes;
        [SerializeField] private WorldAudioV15 _worldAudio;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException("V1.6 UI acceptance: "+reason);}
        [ContextMenu("UI review/Run navigation and settings checks (resets run)")]
        private void RunChecks(){if(Application.isPlaying&&!_running)StartCoroutine(CheckUi());}
        private IEnumerator CheckUi()
        {
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                _flow.ReturnToTitle();yield return null;
                Require(_menu.VisibleState==GameState.Title&&Time.timeScale==0,"Title did not freeze gameplay.");CheckBounds();
                foreach(UiPageV16 page in new[]{UiPageV16.Settings,UiPageV16.Controls,UiPageV16.Credits})
                {
                    _menu.Open(page);yield return null;
                    Require(_menu.Page==page&&_menu.FocusableCount>0,"Subpage missing keyboard focus.");CheckBounds();
                    Require(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null,"Subpage has no selected control.");
                    _menu.Back();yield return null;Require(_menu.Page==UiPageV16.Home&&_flow.State==GameState.Title,"Back left the wrong origin state.");
                }
                UiValuesV16 prefs=original;prefs.Music=0;prefs.Sfx=0;prefs.HudScale=1.15f;prefs.HighContrast=true;prefs.ReduceMotion=true;
                _settings.Set(prefs);yield return null;
                Require(_volumes.MusicVolume==0&&_volumes.SfxVolume==0&&_worldAudio.MusicVolume==0&&_worldAudio.SfxVolume==0,"Settings do not reach real audio volumes.");
                _menu.StartGame();Require(_flow.State==GameState.Playing,"Begin adventure did not start.");
                JehadFlowExplorerV13 explorer=_flow.Player.Player.GetComponent<JehadFlowExplorerV13>();
                if(explorer!=null)Require(!explorer.enabled,"Menu submit leaked into movement in the same frame.");
                yield return null;yield return null;if(explorer!=null)Require(explorer.enabled,"The input shield left movement disabled.");
                foreach(Button button in _menu.GetComponentsInChildren<Button>(false))ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                Require(EventSystem.current.currentSelectedGameObject==null,"Hovering the HUD stole gameplay's Space/Submit input.");
                _flow.Pause();_menu.Open(UiPageV16.Settings);yield return null;
                Require(_flow.State==GameState.Paused&&Time.timeScale==0,"Opening settings resumed gameplay.");
                _menu.Ask(UiModalV16.Defaults);yield return null;
                Require(_menu.ConfirmationOpen&&_menu.FocusableCount==2,"Modal focus is not isolated.");
                _menu.CancelModal();Require(!_menu.ConfirmationOpen&&_menu.Page==UiPageV16.Settings,"Cancel did not restore settings.");
                _menu.Back();yield return null;_menu.Ask(UiModalV16.Retry);yield return null;_menu.AcceptModal();
                Require(_flow.State==GameState.Playing&&!_menu.ConfirmationOpen,"Confirmed retry did not return to play.");
                _menu.AcceptModal();Require(_flow.State==GameState.Playing,"Repeated modal callback changed state.");
                _flow.Pause();yield return null;CheckBounds();
                Debug.Log("V1.6 Unity UI navigation/settings acceptance PASSED. Test display keep/revert in a standalone build and manually inspect all aspect ratios.",this);
            }
            finally{_settings.Set(original);if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
        private void CheckBounds()
        {
            Canvas.ForceUpdateCanvases();Rect safe=Screen.safeArea;safe.xMin-=2;safe.yMin-=2;safe.xMax+=2;safe.yMax+=2;
            foreach(Button button in _menu.GetComponentsInChildren<Button>(false))
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(Vector3 corner in corners)Require(safe.Contains(new Vector2(corner.x,corner.y)),"Button outside safe area: "+button.name);
            }
        }
#endif
    }
}
