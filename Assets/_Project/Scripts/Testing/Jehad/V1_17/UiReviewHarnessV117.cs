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
    public sealed class UiReviewHarnessV117 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV117 _menu;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private MenuAudioV13 _volumes;
        [SerializeField] private WorldAudioV15 _worldAudio;
        [SerializeField] private WorldToastV110 _toast;
        [SerializeField] private PreferenceStoreV114 _preferences;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        [ContextMenu("UI review/Run open title layout checks (resets run)")]
        private void RunTitleLayout(){if(Application.isPlaying&&!_running)StartCoroutine(CheckTitleLayout());}
        private IEnumerator CheckTitleLayout()
        {
            _running=true;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;_flow.ReturnToTitle();yield return null;
                RectTransform title=HudRect("Title"),panel=HudRect("Title menu card"),powers=HudRect("Borrowed powers");
                var shade=HudRect("Menu overlay").GetComponent<UiRoundedV16>();
                Require(title.GetComponent<Graphic>()==null,"The full title frame still paints over the island.");
                Require(panel.parent==title&&panel.rect.width==600&&panel.rect.height==760,"Left title panel geometry changed.");
                Require(powers.parent==title&&powers.anchoredPosition==new Vector2(760,-560)&&powers.rect.height==140,"Power strip is not condensed into its lower-right space.");
                Button[] buttons=title.GetComponentsInChildren<Button>(false);Require(buttons.Length==5,"Title gained or lost an interactive control.");
                foreach(Button button in buttons)
                {
                    var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                    foreach(Vector3 corner in corners){Vector3 local=panel.InverseTransformPoint(corner);Require(panel.rect.Contains(new Vector2(local.x,local.y)),"Title button escaped its left panel: "+button.name);}
                }
                Require(Mathf.Abs(shade.color.a-.16f)<.001f,"Title home does not use the lighter backdrop shade.");CheckBounds();
                _menu.Open(UiPageV17.Settings);Require(Mathf.Abs(shade.color.a-.48f)<.001f,"Settings lost its normal backdrop shade.");CheckBounds();
                _menu.Open(UiPageV17.Home);_menu.Ask(UiModalV17.Quit);
                Require(Mathf.Abs(shade.color.a-.48f)<.001f&&_menu.ConfirmationOpen,"Confirmation did not restore its normal shade.");_menu.CancelModal();
                Require(Mathf.Abs(shade.color.a-.16f)<.001f,"Cancel did not return to the open title composition.");
                Debug.Log("V1.17 open title layout checks PASSED. Inspect text fit, contrast, scenic composition, focus and safe-area variants manually.",this);
            }
            finally{if(_menu!=null&&_menu.ConfirmationOpen)_menu.CancelModal();if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
        [ContextMenu("UI review/Run vitality HUD checks (resets run)")]
        private void RunVitalityChecks(){if(Application.isPlaying&&!_running)StartCoroutine(CheckVitality());}
        private RectTransform HudRect(string label)
        {
            foreach(RectTransform rect in _menu.GetComponentsInChildren<RectTransform>(true))if(rect.name==label)return rect;
            throw new InvalidOperationException("V1.15 HUD element missing: "+label);
        }
        private bool HasLabel(string label)
        {
            foreach(Text text in _menu.GetComponentsInChildren<Text>(true))if(text.text==label)return true;return false;
        }
        private IEnumerator CheckVitality()
        {
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                var values=original;values.ReduceMotion=false;_settings.Set(values);
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                var health=_flow.Player.Health;Require(health!=null&&health.Max>=4,"Vitality check requires the review player's four-or-more maximum HP.");
                int target=Math.Max(1,health.Max/4);
                Require(health.TryApplyDamage(new DamageInfo(health.Current-target,health.transform.position,Vector3.zero,Team.Enemy,0,gameObject)),"Test damage was rejected.");
                RectTransform fill=HudRect("Health fill"),trail=HudRect("Health recent loss");float expected=206f*health.Current/health.Max;
                Require(Mathf.Abs(fill.rect.width-expected)<.02f&&trail.rect.width>fill.rect.width,"Current health did not update immediately with a separate loss trail.");
                Require(HasLabel("LOW VITALITY"),"Low health has no text warning.");
                _flow.Pause();Require(Mathf.Abs(trail.rect.width-fill.rect.width)<.02f,"Pause retained a pending loss animation.");
                _flow.Resume();Require(Mathf.Abs(trail.rect.width-fill.rect.width)<.02f,"Resume replayed an old damage trail.");
                _flow.Pause();_flow.Retry();yield return null;
                Require(Mathf.Abs(fill.rect.width-206)<.02f&&Mathf.Abs(trail.rect.width-206)<.02f&&!HasLabel("LOW VITALITY"),"Retry did not restore the full-health presentation.");
                values.ReduceMotion=true;_settings.Set(values);
                Require(health.TryApplyDamage(new DamageInfo(health.Current-target,health.transform.position,Vector3.zero,Team.Enemy,0,gameObject)),"Reduced-motion test damage was rejected.");
                Require(Mathf.Abs(fill.rect.width-206f*health.Current/health.Max)<.02f&&Mathf.Abs(trail.rect.width-fill.rect.width)<.02f,"Reduced UI motion retained a trailing animation.");
                Debug.Log("V1.15 vitality HUD checks PASSED. Inspect Guardian damage, trail contrast, label fit and frame cost manually.",this);
            }
            finally{_settings.Set(original);if(_preferences!=null)_preferences.Flush();if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
        [ContextMenu("UI review/Run preference storage check (restores settings)")]
        private void CheckPreferenceStorage()
        {
            if(!Application.isPlaying||_running)return;
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                Require(_preferences!=null,"Preference store binding missing.");
                var sample=original;sample.Music=original.Music==.25f?.35f:.25f;sample.Hints=!original.Hints;
                _settings.Set(sample);
                Require(_preferences.Flush()&&PlayerPrefs.GetString(PreferenceStoreV114.StorageKey)==PreferenceCodecV114.Encode(sample),"Native preference write/read failed.");
                _settings.Set(UiValuesV16.Defaults);
                Require(_preferences.Flush()&&PlayerPrefs.GetString(PreferenceStoreV114.StorageKey)==PreferenceCodecV114.Encode(UiValuesV16.Defaults),"Default preferences did not reach storage.");
                Debug.Log("V1.14 preference storage check PASSED. Relaunch a standalone build to check startup restoration separately.",this);
            }
            finally{_settings.Set(original);if(_preferences!=null)_preferences.Flush();_running=false;}
        }
        private static void Require(bool valid,string reason){if(!valid)throw new InvalidOperationException("V1.10 UI acceptance: "+reason);}
        [ContextMenu("World feedback/Run notification checks (resets run)")]
        private void RunNotifications(){if(Application.isPlaying&&!_running)StartCoroutine(CheckNotifications());}
        private IEnumerator CheckNotifications()
        {
            _running=true;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;_toast.Clear();
                Require(_toast.Show(new ToastNoticeV110("test:secret",ToastKindV110.Secret,"SECRET DISCOVERED","Notification test")),"First notification did not display.");
                Require(_toast.Show(new ToastNoticeV110("test:relic",ToastKindV110.Relic,"RELIC FOUND","Notification test"))&&_toast.Count==2&&_toast.CurrentKey=="test:secret","A second notification overwrote the first.");
                _flow.Pause();float remaining=_toast.Remaining;yield return new WaitForSecondsRealtime(.2f);
                Require(_toast.Remaining==remaining&&_toast.Count==2,"Pause consumed notification reading time.");
                _flow.Resume();
                Require(_toast.Show(new ToastNoticeV110("test:objective",ToastKindV110.Objective,"OBJECTIVE","Notification test"))&&_toast.CurrentKey=="test:objective"&&_toast.Count==3,"Objective did not interrupt while retaining discoveries.");
                CheckBounds();_flow.Pause();_flow.Retry();
                Require(_toast.Count==0,"Area retry retained queued notifications.");
                _toast.Show(new ToastNoticeV110("test:title",ToastKindV110.Relic,"RELIC FOUND","Notification test"));_flow.ReturnToTitle();
                Require(_toast.Count==0,"Returning to Title retained a notification.");
                Debug.Log("V1.10 Unity notification queue checks PASSED. Collect real relics, open both chests and complete the boss/Heart path separately.",this);
            }
            finally{if(_toast!=null)_toast.Clear();if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
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
                Debug.Log("V1.10 Unity UI navigation/settings acceptance PASSED. Test display keep/revert in a standalone build and manually inspect all aspect ratios.",this);
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
            _menu.RefreshLayout();if(_toast!=null)_toast.RefreshLayout();
            Canvas.ForceUpdateCanvases();Rect safe=Screen.safeArea;safe.xMin-=2;safe.yMin-=2;safe.xMax+=2;safe.yMax+=2;
            foreach(Button button in _menu.GetComponentsInChildren<Button>(false))
            {
                var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);
                foreach(Vector3 corner in corners)Require(safe.Contains(new Vector2(corner.x,corner.y)),"Button outside safe area: "+button.name);
            }
            if(_toast!=null&&_toast.Backdrop!=null)
            {
                var corners=new Vector3[4];_toast.Backdrop.GetWorldCorners(corners);
                foreach(Vector3 corner in corners)Require(safe.Contains(new Vector2(corner.x,corner.y)),"Discovery toast outside safe area.");
            }
        }
#endif
    }
}
