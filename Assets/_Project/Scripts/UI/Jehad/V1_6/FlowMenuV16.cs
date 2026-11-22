using System;
using System.Collections.Generic;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echoheart.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class FlowMenuV16 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private MenuAudioV13 _audio;
        [SerializeField] private UiSettingsV16 _settings;
        private readonly UiPolicyV16 _route=new UiPolicyV16();
        private readonly List<Selectable> _focus=new List<Selectable>();
        private UiFactoryV16 _ui;
        private UiHudV16 _hud;
        private MenuScreensV16 _views;
        private RectTransform _safe,_overlay,_pages;
        private CanvasGroup _fade;
        private Action _pending;
        private GameObject _previousFocus;
        private bool _bossVisible,_bossDefeated;
        private int _backFrame=-1;
        private int _releaseInputAt=-1;
        private Rect _lastSafe;
        private int _width,_height;
        public GameState VisibleState => _route.State;
        public UiPageV16 Page => _route.Page;
        public bool ConfirmationOpen => _route.Modal!=UiModalV16.None;
        public UiSettingsV16 Settings => _settings;
        public int FocusableCount => _focus.Count;

        private void Awake()
        {
            if(_flow==null||_settings==null){Debug.LogError("V1.6 UI needs flow and settings bindings.",this);enabled=false;return;}
            if(EventSystem.current==null)
            {var system=new GameObject("Menu Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));system.transform.SetParent(transform,false);}
            else if(EventSystem.current.GetComponent<InputSystemUIInputModule>()==null)
                Debug.LogError("V1.6 menus require an InputSystemUIInputModule on the EventSystem.",this);
            _ui=new UiFactoryV16(PlayPress);
            var go=new GameObject("ECHOHEART Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            _safe=_ui.Stretch("Safe interface",go.transform);_safe.gameObject.AddComponent<UiCancelV16>().Bind(this);
            _hud=new UiHudV16(_ui,_safe,()=>_flow.Pause());
            _overlay=_ui.Stretch("Menu overlay",_safe);_ui.Paint(_overlay,new Color(.004f,.025f,.02f,.48f),0,true);
            _fade=_overlay.gameObject.AddComponent<CanvasGroup>();_pages=_ui.Stretch("Menu pages",_overlay);
            _views=new MenuScreensV16(_ui,_pages,_overlay,this,_settings);
            _views.RefreshSettings();SafeArea();ShowState(_flow.State);
        }
        private void OnEnable()
        {
            GameEvents.StateChanged+=ShowState;GameEvents.PlayerHealthChanged+=Health;GameEvents.AbilityChanged+=Power;
            GameEvents.CollectiblesChanged+=Relics;GameEvents.BossEncounterStarted+=BossStarted;GameEvents.BossHealthChanged+=BossHealth;
            GameEvents.BossDefeated+=BossDefeated;GameEvents.PromptChanged+=Prompt;GameEvents.RetryRequested+=Retrying;
            if(_settings!=null){_settings.Changed+=PreferencesChanged;_settings.DisplayReverted+=DisplayReverted;}
        }
        private void OnDisable()
        {
            GameEvents.StateChanged-=ShowState;GameEvents.PlayerHealthChanged-=Health;GameEvents.AbilityChanged-=Power;
            GameEvents.CollectiblesChanged-=Relics;GameEvents.BossEncounterStarted-=BossStarted;GameEvents.BossHealthChanged-=BossHealth;
            GameEvents.BossDefeated-=BossDefeated;GameEvents.PromptChanged-=Prompt;GameEvents.RetryRequested-=Retrying;
            if(_settings!=null){_settings.Changed-=PreferencesChanged;_settings.DisplayReverted-=DisplayReverted;_settings.RevertDisplay();}
            if(_releaseInputAt>=0&&_flow!=null&&_flow.State==GameState.Playing)_flow.Player.SetControlsEnabled(true);
            _releaseInputAt=-1;
        }
        private void Start(){if(_views!=null)ShowState(_flow.State);}
        private void Update()
        {
            if(_views==null)return;
            if(_releaseInputAt>=0&&Time.frameCount>=_releaseInputAt){_releaseInputAt=-1;if(_flow.State==GameState.Playing)_flow.Player.SetControlsEnabled(true);}
            if(Screen.width!=_width||Screen.height!=_height||Screen.safeArea!=_lastSafe)SafeArea();
            _fade.alpha=_settings.Values.ReduceMotion?1f:Mathf.MoveTowards(_fade.alpha,1f,Time.unscaledDeltaTime*6f);
            _hud.Tick(_settings.Values);
            if(_route.Modal==UiModalV16.Display)_views.ModalBody.text="Reverting in "+Mathf.CeilToInt(_settings.PreviewSeconds)+" seconds.\nChoose Revert if the image is difficult to see.";
            Keyboard keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)Back();
            if(Gamepad.current!=null&&Gamepad.current.startButton.wasPressedThisFrame&&(_flow.State==GameState.Playing||_flow.State==GameState.Paused))Back();
            if(keyboard!=null&&keyboard.tabKey.wasPressedThisFrame&&_flow.State!=GameState.Playing)Tab(keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed?-1:1);
        }
        private void SafeArea()
        {
            _width=Mathf.Max(1,Screen.width);_height=Mathf.Max(1,Screen.height);_lastSafe=Screen.safeArea;
            _safe.anchorMin=new Vector2(_lastSafe.xMin/_width,_lastSafe.yMin/_height);_safe.anchorMax=new Vector2(_lastSafe.xMax/_width,_lastSafe.yMax/_height);_safe.offsetMin=_safe.offsetMax=Vector2.zero;
        }
        private void Health(int value,int maximum){if(_hud!=null)_hud.Health(value,maximum);}
        private void Power(AbilityDefinition value){if(_hud!=null){_hud.Power(value);_hud.Apply(_settings.Values);}}
        private void Relics(int value,int total){if(_hud!=null)_hud.Relics(value,total);}
        private void Prompt(string value){if(_hud!=null)_hud.Prompt(value);}
        private void BossHealth(int value,int maximum){if(_hud!=null)_hud.BossHealth(value,maximum);}
        private void BossStarted(){_bossVisible=true;_bossDefeated=false;Objective();}
        private void BossDefeated(){_bossVisible=false;_bossDefeated=true;Objective();}
        private void Retrying(RetryScope scope){_bossVisible=_bossDefeated=false;if(_hud!=null)_hud.Prompt(string.Empty);Objective();}
        private void Objective(){if(_hud==null)return;_hud.Boss(_bossVisible);_hud.Objective(_bossDefeated||_flow.HeartUnlocked,_bossVisible);}
        private static string Clock(float seconds)=>Mathf.FloorToInt(seconds/60f).ToString("00")+":"+Mathf.FloorToInt(seconds%60f).ToString("00");
        private void ShowState(GameState state)
        {
            if(_views==null)return;
            if(_settings.Previewing)_settings.RevertDisplay();
            _pending=null;_route.SetState(state);
            // The submit/click that closes a menu must not also jump/attack in the world that frame.
            _releaseInputAt=state==GameState.Playing?Time.frameCount+1:-1;
            if(state==GameState.Playing)_flow.Player.SetControlsEnabled(false);
            if(state==GameState.Title)_bossVisible=_bossDefeated=false;
            _views.PauseStats.text="Relics  "+_flow.Relics.Collected+" / "+_flow.Relics.Total+"       Time  "+Clock(_flow.ElapsedSeconds);
            _views.DeathBody.text=_flow.CurrentRetryScope==RetryScope.BossArena?"Return to the Guardian with full health\nand your selected power.\nYour discovered relics stay with you.":"Return to the clearing with full health\nand an empty power slot.\nYour discovered relics stay with you.";
            _views.CompleteStats.text="RELICS FOUND     "+_flow.Relics.Collected+" / "+_flow.Relics.Total+"\n\nEXPLORATION TIME     "+Clock(_flow.ElapsedSeconds);
            _hud.State(state,_settings.Values);Objective();Render(true);
        }
        private RectTransform ActivePage()
        {
            if(_route.Page==UiPageV16.Settings)return _views.Settings;
            if(_route.Page==UiPageV16.Controls)return _views.Controls;
            if(_route.Page==UiPageV16.Credits)return _views.Credits;
            return _route.State==GameState.Title?_views.Title:_route.State==GameState.Paused?_views.Pause:_route.State==GameState.Dead?_views.Death:_views.Complete;
        }
        private void Render(bool defaultFocus=false)
        {
            if(_views==null)return;bool menus=_route.State!=GameState.Playing;
            RectTransform active=ActivePage();foreach(RectTransform page in new[]{_views.Title,_views.Pause,_views.Death,_views.Complete,_views.Settings,_views.Controls,_views.Credits})page.gameObject.SetActive(menus&&page==active);
            _overlay.gameObject.SetActive(menus);_views.Modal.gameObject.SetActive(ConfirmationOpen);
            _views.PagesGroup.interactable=!ConfirmationOpen;_views.PagesGroup.blocksRaycasts=!ConfirmationOpen;
            _ui.Contrast(_settings.Values.HighContrast);RefreshNavigation();
            if(!menus){if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);return;}
            if(defaultFocus||ConfirmationOpen||!ValidFocus())
            {
                Selectable selected=ConfirmationOpen?_views.CancelButton:_route.Page!=UiPageV16.Home?(_focus.Count>0?_focus[0]:null):_route.State==GameState.Title?_views.StartButton:_route.State==GameState.Paused?_views.ResumeButton:_route.State==GameState.Dead?_views.RetryButton:_views.AgainButton;
                Select(selected!=null?selected.gameObject:null);
            }
        }
        private bool ValidFocus()
        {
            if(EventSystem.current==null||EventSystem.current.currentSelectedGameObject==null)return false;
            Selectable selected=EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();return selected!=null&&_focus.Contains(selected);
        }
        private static void Select(GameObject value){if(EventSystem.current!=null){EventSystem.current.SetSelectedGameObject(null);EventSystem.current.SetSelectedGameObject(value);}}
        public void RefreshNavigation()
        {
            if(_views==null)return;_focus.Clear();Transform root=ConfirmationOpen?_views.Modal:ActivePage();
            foreach(Selectable selectable in root.GetComponentsInChildren<Selectable>(true))if(selectable.gameObject.activeInHierarchy&&selectable.IsInteractable())_focus.Add(selectable);
            UiFactoryV16.Navigation(_focus);
        }
        private void Tab(int direction)
        {
            RefreshNavigation();if(_focus.Count==0)return;int index=-1;
            if(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null)index=_focus.FindIndex(s=>s.gameObject==EventSystem.current.currentSelectedGameObject);
            if(index<0&&direction<0)index=0;
            Select(_focus[(index+direction+_focus.Count)%_focus.Count].gameObject);
        }
        public void Open(UiPageV16 page)
        {
            if(!_route.Open(page))return;
            if(page==UiPageV16.Settings){_settings.ReadDraft();_views.SettingsStatus.text="Changes last for this session.";_views.RefreshSettings();}
            if(!_settings.Values.ReduceMotion)_fade.alpha=.45f;Render(true);
        }
        public void Back()
        {
            if(_views==null||_backFrame==Time.frameCount)return;_backFrame=Time.frameCount;
            UiBackV16 action=_route.Back();_pending=null;
            if(action==UiBackV16.Pause)_flow.Pause();else if(action==UiBackV16.Resume)_flow.Resume();
            else if(action==UiBackV16.RevertDisplay){_settings.RevertDisplay();Render(true);}
            else if(action==UiBackV16.Page||action==UiBackV16.Modal)Render(true);
            else if(_route.State==GameState.Title)Ask(UiModalV16.Quit);
        }
        public void Ask(UiModalV16 kind)
        {
            if(!_route.Confirm(kind))return;
            _previousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            string title="",body="";Action action=null;
            if(kind==UiModalV16.Retry){title="Retry this area?";body=_flow.CurrentRetryScope==RetryScope.BossArena?"Restart the Guardian encounter with full health and your selected power. Your relics stay collected.":"Return to the entry clearing with full health and no power. Your relics stay collected.";action=()=>_flow.Retry();}
            if(kind==UiModalV16.Title){title="Return to title?";body="There is no Continue option. Starting a new adventure resets this run's relics and discoveries.";action=()=>_flow.ReturnToTitle();}
            if(kind==UiModalV16.Quit){title="Leave the island?";body="Quit ECHOHEART and close this adventure.";action=()=>Application.Quit();}
            if(kind==UiModalV16.Defaults){title="Restore settings?";body="Restore the default audio and interface options. Display defaults are staged until you choose Apply.";action=()=>_settings.RestoreDefaults();}
            _pending=action;_views.ModalTitle.text=title;_views.ModalBody.text=body;ModalLabels("Cancel","Confirm");Render();
        }
        public void PreviewDisplay()
        {
            if(!_route.Confirm(UiModalV16.Display))return;
            _previousFocus=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
            _views.ModalTitle.text="Keep these display settings?";_views.ModalBody.text="Reverting in 15 seconds.";ModalLabels("Revert","Keep settings");_pending=()=>_settings.KeepDisplay();Render();
            if(!_settings.PreviewDisplay()){_route.Consume(UiModalV16.Display);_pending=null;Render(true);}
        }
        private void ModalLabels(string cancel,string confirm){_views.CancelButton.GetComponentInChildren<Text>().text=cancel;_views.ConfirmButton.GetComponentInChildren<Text>().text=confirm;}
        public void CancelModal()
        {
            UiModalV16 kind=_route.Modal;if(!_route.Consume(kind))return;_pending=null;
            if(kind==UiModalV16.Display)_settings.RevertDisplay();Render();if(_previousFocus!=null&&_previousFocus.activeInHierarchy)Select(_previousFocus);
        }
        public void AcceptModal()
        {
            UiModalV16 kind=_route.Modal;if(!_route.Consume(kind))return;
            Action action=_pending;_pending=null;Render();action?.Invoke();
        }
        private void DisplayReverted()
        {
            if(_views==null)return;
            if(_route.Modal==UiModalV16.Display){_route.Consume(UiModalV16.Display);_pending=null;Render(true);}
            _views.SettingsStatus.text="Previous display settings restored.";
        }
        private void PreferencesChanged()
        {
            if(_views==null)return;_views.RefreshSettings();_ui.Contrast(_settings.Values.HighContrast);_hud.Apply(_settings.Values);RefreshNavigation();
            if(_route.State!=GameState.Playing&&!ValidFocus()&&_focus.Count>0)Select(_focus[0].gameObject);
        }
        public void PlayPress(){if(_audio!=null)_audio.PlayPress();}
        public void StartGame()=>_flow.StartNewGame();
        public void Resume()=>_flow.Resume();
        public void Retry()=>_flow.Retry();
    }
}
