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
    public sealed class UiReviewHarnessV18 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV18 _menu;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private MenuAudioV13 _volumes;
        [SerializeField] private WorldAudioV15 _worldAudio;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException("V1.8 UI acceptance: "+reason);}
        [ContextMenu("UI review/Run navigation and settings checks (resets run)")]
        private void RunChecks(){if(Application.isPlaying&&!_running)StartCoroutine(CheckUi());}
        private IEnumerator CheckUi()
        {
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                _flow.ReturnToTitle();yield return null;
                Require(_menu.VisibleState==GameState.Title&&Time.timeScale==0,"Title did not freeze gameplay.");CheckBounds();
                Button origin=FindButton("Settings");EventSystem.current.SetSelectedGameObject(origin.gameObject);
                _menu.Open(UiPageV17.Settings);yield return null;_menu.Back();yield return null;
                Require(EventSystem.current.currentSelectedGameObject==origin.gameObject,"Back did not restore the title button that opened Settings.");
                EventSystem.current.SetSelectedGameObject(null);_menu.RecoverFocus();
                Require(EventSystem.current.currentSelectedGameObject==origin.gameObject,"External deselection lost the remembered title focus.");
                foreach(UiPageV17 page in new[]{UiPageV17.Settings,UiPageV17.Controls,UiPageV17.Credits})
                {
                    _menu.Open(page);yield return null;
                    Require(_menu.Page==page&&_menu.FocusableCount>0,"Subpage missing keyboard focus.");CheckBounds();
                    Require(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null,"Subpage has no selected control.");
                    if(page==UiPageV17.Controls||page==UiPageV17.Credits)Require(EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text=="Back","Information page did not focus its Back control.");
                    if(page==UiPageV17.Credits)
                    {
                        EventSystem.current.SetSelectedGameObject(FindButton("Asset credits").gameObject);
                        _menu.Back();yield return null;_menu.Open(UiPageV17.Credits);yield return null;
                        Require(EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Text>().text=="Back","Re-entering Credits restored its external link rather than Back.");
                    }
                    _menu.Back();yield return null;Require(_menu.Page==UiPageV17.Home&&_flow.State==GameState.Title,"Back left the wrong origin state.");
                }
                _menu.Open(UiPageV17.Settings);yield return null;
                Require(!_settings.HasDisplayChanges,"Opening settings staged a display change.");
                _menu.PreviewDisplay();Require(!_menu.ConfirmationOpen&&!_settings.Previewing,"Unchanged settings started a native display preview.");
                _settings.CycleLimit();Require(_settings.HasDisplayChanges,"A display edit did not become dirty.");
                GameObject editingFocus=EventSystem.current.currentSelectedGameObject;
                _menu.Back();yield return null;
                Require(_menu.ConfirmationOpen&&_menu.Page==UiPageV17.Settings,"Back silently discarded a staged display edit.");
                CheckModalArrows();_menu.CancelModal();
                Require(_settings.HasDisplayChanges&&EventSystem.current.currentSelectedGameObject==editingFocus,"Keep editing lost the draft or the previous focus.");
                yield return null;_menu.Back();yield return null;_menu.AcceptModal();
                Require(!_settings.HasDisplayChanges&&!_menu.ConfirmationOpen&&_menu.Page==UiPageV17.Home,"Discard did not clear only the staged display edit and return home.");
                UiValuesV16 prefs=original;prefs.Music=0;prefs.Sfx=0;prefs.HudScale=1.15f;prefs.HighContrast=true;prefs.ReduceMotion=true;
                _settings.Set(prefs);yield return null;
                Require(_volumes.MusicVolume==0&&_volumes.SfxVolume==0&&_worldAudio.MusicVolume==0&&_worldAudio.SfxVolume==0,"Settings do not reach real audio volumes.");
                _menu.StartGame();Require(_flow.State==GameState.Playing,"Begin adventure did not start.");
                JehadFlowExplorerV13 explorer=_flow.Player.Player.GetComponent<JehadFlowExplorerV13>();
                if(explorer!=null)Require(!explorer.enabled,"Menu submit leaked into movement in the same frame.");
                yield return null;yield return null;if(explorer!=null)Require(explorer.enabled,"The input shield left movement disabled.");
                foreach(Button button in _menu.GetComponentsInChildren<Button>(false))ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                Require(EventSystem.current.currentSelectedGameObject==null,"Hovering the HUD stole gameplay's Space/Submit input.");
                _flow.Pause();_menu.Open(UiPageV17.Settings);yield return null;
                Require(_flow.State==GameState.Paused&&Time.timeScale==0,"Opening settings resumed gameplay.");
                _menu.Ask(UiModalV17.Defaults);yield return null;
                Require(_menu.ConfirmationOpen&&_menu.FocusableCount==2,"Modal focus is not isolated.");
                GameObject cancelFocus=EventSystem.current.currentSelectedGameObject;EventSystem.current.SetSelectedGameObject(null);_menu.RecoverFocus();
                Require(EventSystem.current.currentSelectedGameObject==cancelFocus,"Focus recovery escaped the confirmation or skipped Cancel.");
                CheckModalArrows();
                _menu.CancelModal();Require(!_menu.ConfirmationOpen&&_menu.Page==UiPageV17.Settings,"Cancel did not restore settings.");
                _menu.Back();yield return null;_menu.Ask(UiModalV17.Retry);yield return null;_menu.AcceptModal();
                Require(_flow.State==GameState.Playing&&!_menu.ConfirmationOpen,"Confirmed retry did not return to play.");
                _menu.AcceptModal();Require(_flow.State==GameState.Playing,"Repeated modal callback changed state.");
                _flow.Pause();yield return null;CheckBounds();
                Debug.Log("V1.8 Unity UI navigation/settings acceptance PASSED. Test display keep/revert in a standalone build and manually inspect all aspect ratios.",this);
            }
            finally{_settings.DiscardDisplay();_settings.Set(original);if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
        private Button FindButton(string caption)
        {
            foreach(Button button in _menu.GetComponentsInChildren<Button>(false))
            {
                Text text=button.GetComponentInChildren<Text>();if(text!=null&&text.text==caption&&button.IsInteractable())return button;
            }
            throw new InvalidOperationException("Missing active UI button: "+caption);
        }
        private void CheckModalArrows()
        {
            Selectable cancel=EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            Require(cancel!=null&&cancel.navigation.selectOnRight!=null,"Modal has no right-arrow route to its action.");
            Selectable action=cancel.navigation.selectOnRight;
            Require(action.navigation.selectOnLeft==cancel,"Modal left arrow does not return to Cancel.");
            Require(cancel.navigation.selectOnLeft==action&&action.navigation.selectOnRight==cancel,"Modal row does not wrap within its isolated scope.");
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
