using System;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Menu composition, separated from state/input and runtime display changes.</summary>
    public sealed class MenuScreensV117
    {
        private readonly UiFactoryV17 _ui;
        private readonly FlowMenuV117 _owner;
        private readonly UiSettingsV17 _settings;
        public RectTransform Title,Pause,Death,Complete,Settings,Controls,Credits,Modal;
        public RectTransform ModalCard { get; private set; }
        public CanvasGroup PagesGroup;
        public Button StartButton,ResumeButton,RetryButton,AgainButton,CancelButton,ConfirmButton;
        public Text ModalTitle,ModalBody,DeathBody,CompleteStats,PauseStats;
        private RectTransform[] _tabs;
        private Button[] _tabButtons;
        private Slider _music,_sfx,_hud;
        private Text _musicValue,_sfxValue,_hudValue,_resolution,_mode,_quality,_sync,_limit;
        private Button _limitButton,_contrast,_motion,_hints,_crosshair,_apply,_settingsBack,_defaults,_controlsBack,_creditsBack;
        private RectTransform[] _tabMarks;
        private Text _tabTitle;
        public Text SettingsStatus;
        public int Tab { get; private set; }
        public Button PreferredSettingsFocus => _tabButtons[Tab];
        public Button ControlsBack => _controlsBack;
        public Button CreditsBack => _creditsBack;
        public Selectable[] SettingsFocusOrder()
        {
            var order=new System.Collections.Generic.List<Selectable>();order.AddRange(_tabButtons);
            order.AddRange(_tabs[Tab].GetComponentsInChildren<Selectable>(true));order.Add(_settingsBack);order.Add(_defaults);return order.ToArray();
        }
        public MenuScreensV117(UiFactoryV17 ui,RectTransform pages,RectTransform modalParent,FlowMenuV117 owner,UiSettingsV17 settings)
        {
            _ui=ui;_owner=owner;_settings=settings;PagesGroup=pages.gameObject.AddComponent<CanvasGroup>();
            BuildTitle(pages);BuildPause(pages);BuildDeath(pages);BuildComplete(pages);BuildSettings(pages);BuildControls(pages);BuildCredits(pages);BuildModal(modalParent);
        }
        private RectTransform Card(string name,Transform parent,float width=1100,float height=760) => _ui.Panel(name,parent,0,0,width,height,UiFactoryV17.Forest,new Vector2(.5f,.5f));
        private void Header(Transform p,string kicker,string title)
        { float width=((RectTransform)p).sizeDelta.x;_ui.Label(p,kicker,36,-30,width-72,26,16,UiFactoryV17.Mint,true);_ui.Label(p,title,32,-70,width-64,76,54,UiFactoryV17.Cream,true); }
        private void BuildTitle(Transform parent)
        {
            Title=_ui.Rect("Title",parent,0,0,1320,760,new Vector2(.5f,.5f));
            _ui.Panel("Title menu card",Title,0,0,600,760,UiFactoryV17.Forest);
            _ui.Label(Title,"THE ISLAND IS WAITING",48,-38,500,28,17,UiFactoryV17.Mint,true);
            _ui.Label(Title,"ECHOHEART",42,-94,540,100,60,UiFactoryV17.Cream,true);
            _ui.Panel("Gold accent",Title,48,-210,84,4,UiFactoryV17.Gold);
            _ui.Label(Title,"Borrow a power.\nRestore the Heart.",48,-250,492,104,32,UiFactoryV17.Cream,true);
            _ui.Label(Title,"Wander the valley. Uncover its secrets.\nFace the Guardian and bring the island home.",48,-366,492,80,21,UiFactoryV17.Muted);
            StartButton=_ui.Button(Title,"Begin adventure",48,-474,492,()=>_owner.StartGame(),true,68);
            _ui.Button(Title,"Settings",48,-558,492,()=>_owner.Open(UiPageV17.Settings));
            _ui.Button(Title,"Controls",48,-636,148,()=>_owner.Open(UiPageV17.Controls),false,54);
            _ui.Button(Title,"Credits",220,-636,148,()=>_owner.Open(UiPageV17.Credits),false,54);
            _ui.Button(Title,"Quit",392,-636,148,()=>_owner.Ask(UiModalV17.Quit),false,54);
            var feature=_ui.Panel("Borrowed powers",Title,760,-560,510,140,UiFactoryV17.Forest);
            _ui.Label(feature,"BORROW A POWER",16,-12,478,22,14,UiFactoryV17.Mint,true);
            var kinds=new[]{UiGlyphKindV16.Sword,UiGlyphKindV16.Bow,UiGlyphKindV16.Fire};var colors=new[]{UiFactoryV17.Gold,UiFactoryV17.Mint,UiFactoryV17.Coral};
            for(int i=0;i<3;i++){var rune=_ui.Panel(kinds[i]+" rune",feature,16+i*166,-48,146,74,new Color(.09f,.26f,.22f));_ui.Icon(rune,kinds[i],8,-19,36,colors[i]);_ui.Label(rune,kinds[i].ToString().ToUpperInvariant(),52,-24,86,26,17,colors[i],true,TextAnchor.MiddleLeft);}
        }
        private void BuildPause(Transform parent)
        {
            Pause=Card("Pause",parent,620,742);Header(Pause,"TAKE A BREATH","Paused");
            PauseStats=_ui.Label(Pause,"",38,-170,544,55,22,UiFactoryV17.Muted);
            ResumeButton=_ui.Button(Pause,"Resume adventure",38,-260,544,()=>_owner.Resume(),true);
            _ui.Button(Pause,"Settings",38,-338,544,()=>_owner.Open(UiPageV17.Settings));
            _ui.Button(Pause,"Controls",38,-416,544,()=>_owner.Open(UiPageV17.Controls));
            _ui.Button(Pause,"Retry area",38,-494,544,()=>_owner.Ask(UiModalV17.Retry));
            _ui.Button(Pause,"Return to title",38,-572,544,()=>_owner.Ask(UiModalV17.Title));
            _ui.Label(Pause,"Esc resumes your adventure",38,-676,544,28,18,UiFactoryV17.Muted,false,TextAnchor.MiddleCenter);
        }
        private void BuildDeath(Transform parent)
        {
            Death=Card("Death",parent,660,644);Header(Death,"EVERY JOURNEY TAKES A BREATH","Try again");
            DeathBody=_ui.Label(Death,"",38,-188,584,108,26,UiFactoryV17.Muted);
            RetryButton=_ui.Button(Death,"Retry",38,-354,584,()=>_owner.Retry(),true);
            _ui.Button(Death,"Settings",38,-434,584,()=>_owner.Open(UiPageV17.Settings));
            _ui.Button(Death,"Return to title",38,-514,584,()=>_owner.Ask(UiModalV17.Title));
        }
        private void BuildComplete(Transform parent)
        {
            Complete=Card("Completion",parent,760,742);_ui.Icon(Complete,UiGlyphKindV16.Heart,331,-30,98,UiFactoryV17.Mint);
            _ui.Label(Complete,"THE ISLAND BREATHES AGAIN",40,-154,680,28,17,UiFactoryV17.Mint,true,TextAnchor.MiddleCenter);
            _ui.Label(Complete,"Heart restored",30,-208,700,76,56,UiFactoryV17.Cream,true,TextAnchor.MiddleCenter);
            CompleteStats=_ui.Label(Complete,"",55,-322,650,108,27,UiFactoryV17.Muted,false,TextAnchor.MiddleCenter);
            AgainButton=_ui.Button(Complete,"Explore again",80,-474,600,()=>_owner.StartGame(),true);
            _ui.Button(Complete,"Settings",80,-554,600,()=>_owner.Open(UiPageV17.Settings));
            _ui.Button(Complete,"Return to title",80,-634,600,()=>_owner.Ask(UiModalV17.Title));
        }
        private void BuildSettings(Transform parent)
        {
            Settings=Card("Settings",parent);Header(Settings,"MAKE YOURSELF AT HOME","Settings");
            _tabs=new RectTransform[3];_tabButtons=new Button[3];_tabMarks=new RectTransform[3];string[] names={"Audio","Display","Interface"};
            for(int i=0;i<3;i++){int index=i;_tabButtons[i]=_ui.Button(Settings,names[i],36,-194-i*78,220,()=>SwitchTab(index));_tabMarks[i]=_ui.Panel(names[i]+" active marker",Settings,24,-208-i*78,4,34,UiFactoryV17.Gold);_tabs[i]=_ui.Stretch(names[i]+" settings",Settings);}
            _tabTitle=_ui.Label(Settings,"AUDIO",304,-166,742,26,16,UiFactoryV17.Mint,true);
            _music=_ui.Slider(_tabs[0],"Music",304,-216,742,.65f,0,1,v=>{var p=_settings.Values;p.Music=v;_settings.Set(p);},out _musicValue);
            _sfx=_ui.Slider(_tabs[0],"Sound effects",304,-346,742,.8f,0,1,v=>{var p=_settings.Values;p.Sfx=v;_settings.Set(p);},out _sfxValue);
            _ui.Button(_tabs[0],"Preview sound",304,-480,280,()=>{});
            _ui.Label(_tabs[0],"Music continues quietly while paused.\nSound effects include footsteps, ambience and UI.",304,-568,742,70,21,UiFactoryV17.Muted);
            _resolution=DisplayRow(_tabs[1],"Resolution",-206,()=>_settings.CycleResolution(),()=>_settings.CycleResolution(-1));
            _mode=DisplayRow(_tabs[1],"Display mode",-286,()=>_settings.ToggleFullscreen());
            _quality=DisplayRow(_tabs[1],"Quality",-366,()=>_settings.CycleQuality(),()=>_settings.CycleQuality(-1));
            _sync=DisplayRow(_tabs[1],"VSync",-446,()=>_settings.ToggleVsync());
            _limit=DisplayRow(_tabs[1],"Frame limit",-526,()=>_settings.CycleLimit(),()=>_settings.CycleLimit(-1));_limitButton=_limit.GetComponentInParent<Button>();
            _apply=_ui.Button(_tabs[1],"Apply display",772,-664,274,()=>_owner.PreviewDisplay(),true,58);
            _hud=_ui.Slider(_tabs[2],"Interface scale",304,-206,742,1,.5f,1.15f,v=>{var p=_settings.Values;p.HudScale=v;_settings.Set(p);},out _hudValue);
            _contrast=Toggle(_tabs[2],"High contrast",-324,()=>{var p=_settings.Values;p.HighContrast=!p.HighContrast;_settings.Set(p);});
            _motion=Toggle(_tabs[2],"Reduce UI motion",-394,()=>{var p=_settings.Values;p.ReduceMotion=!p.ReduceMotion;_settings.Set(p);});
            _hints=Toggle(_tabs[2],"Objective and control hints",-464,()=>{var p=_settings.Values;p.Hints=!p.Hints;_settings.Set(p);});
            _crosshair=Toggle(_tabs[2],"Ranged aim guide",-534,()=>{var p=_settings.Values;p.Crosshair=!p.Crosshair;_settings.Set(p);});
            _settingsBack=_ui.Button(Settings,"Back",36,-664,220,()=>_owner.Back(),false,58);
            _defaults=_ui.Button(Settings,"Restore defaults",304,-664,300,()=>_owner.Ask(UiModalV17.Defaults),false,58);
            SettingsStatus=_ui.Label(Settings,_owner.PreferenceStatus,304,-624,742,24,17,UiFactoryV17.Muted);
            SwitchTab(0,false);
        }
        // Rows with a list of values are one button: the left half steps back, the right half (and Enter/gamepad) steps forward.
        private Text DisplayRow(Transform parent,string label,float y,Action action,Action previous=null)
        {
            _ui.Label(parent,label,304,y,370,44,24,UiFactoryV17.Cream,true,TextAnchor.MiddleLeft);
            UiSplitPressV117 split=null;
            Action press=previous==null?action:()=>{if(split.ConsumeLeft())previous();else action();};
            var b=_ui.Button(parent,label+" value",720,y+6,326,press,false,54);_ui.Icon(b.transform,UiGlyphKindV16.Arrow,298,-17,20,UiFactoryV17.Muted);
            if(previous!=null)
            {
                split=b.gameObject.AddComponent<UiSplitPressV117>();
                var icon=_ui.Icon(b.transform,UiGlyphKindV16.Arrow,0,0,20,UiFactoryV17.Muted).rectTransform;
                icon.pivot=new Vector2(.5f,.5f);icon.anchoredPosition=new Vector2(18,-27);icon.localScale=new Vector3(-1,1,1);
            }
            Text value=b.GetComponentInChildren<Text>();value.rectTransform.sizeDelta=new Vector2(260,46);return value;
        }
        private Button Toggle(Transform parent,string label,float y,Action action)
        { var b=_ui.Button(parent,label,304,y,742,action,false,56);b.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;return b; }
        public void SwitchTab(int index,bool notify=true)
        {
            Tab=index;for(int i=0;i<_tabs.Length;i++){_tabs[i].gameObject.SetActive(i==index);_tabMarks[i].gameObject.SetActive(i==index);var c=_tabButtons[i].colors;c.normalColor=i==index?new Color(.17f,.35f,.27f):new Color(.09f,.24f,.21f);_tabButtons[i].colors=c;}
            if(_tabTitle!=null)_tabTitle.text=index==0?"AUDIO":index==1?"DISPLAY":"INTERFACE";
            if(notify)_owner.RefreshNavigation();
        }
        public void RefreshSettings()
        {
            UiValuesV16 p=_settings.Values;_music.SetValueWithoutNotify(p.Music);_sfx.SetValueWithoutNotify(p.Sfx);_hud.SetValueWithoutNotify(p.HudScale);
            _musicValue.text=Mathf.RoundToInt(p.Music*100)+"%";_sfxValue.text=Mathf.RoundToInt(p.Sfx*100)+"%";_hudValue.text=Mathf.RoundToInt(p.HudScale*100)+"%";
            _resolution.text=_settings.DraftResolution.x+" × "+_settings.DraftResolution.y;_mode.text=_settings.ModeName;_quality.text=_settings.QualityName;_sync.text=_settings.SyncName;_limit.text=_settings.DraftVsync?"Controlled by VSync":_settings.LimitName;_limitButton.interactable=!_settings.DraftVsync;
            _apply.interactable=_settings.HasDisplayChanges&&!_settings.Previewing;
            SettingsStatus.text=_settings.HasDisplayChanges?"Unapplied display changes. Choose Apply to preview.":_owner.PreferenceStatus;SettingsStatus.color=_settings.HasDisplayChanges?UiFactoryV17.Gold:UiFactoryV17.Muted;
            SetToggle(_contrast,"High contrast",p.HighContrast);SetToggle(_motion,"Reduce UI motion",p.ReduceMotion);SetToggle(_hints,"Objective and control hints",p.Hints);SetToggle(_crosshair,"Ranged aim guide",p.Crosshair);
        }
        private static void SetToggle(Button button,string label,bool value) => button.GetComponentInChildren<Text>().text=label+"    ·    "+(value?"On":"Off");
        private void BuildControls(Transform parent)
        {
            Controls=Card("Controls",parent);Header(Controls,"A LITTLE GUIDANCE","Controls");
            string[,] rows={{"Move (hold Shift to run)","W A S D  ·  Shift"},{"Jump","Space"},{"Inhale an enemy (copy its power)","Hold right mouse"},{"Use the held power","Left mouse"},{"Orbit camera","Move mouse"},{"Interact with chests / Heart","E"},{"Pause / back","Esc"},{"Menus","Arrows / Tab · Enter / Space"}};
            for(int i=0;i<rows.GetLength(0);i++){float y=-190-i*49;_ui.Label(Controls,rows[i,0],40,y,430,40,23,UiFactoryV17.Cream,false,TextAnchor.MiddleLeft);var key=_ui.Panel("Control key",Controls,512,y,548,40,new Color(.10f,.25f,.21f));_ui.Label(key,rows[i,1],12,0,524,40,21,UiFactoryV17.Mint,true,TextAnchor.MiddleCenter);}
            _ui.Label(Controls,"Gamepad: navigate menus with the stick / D-pad, select with South, back with East.",40,-616,1020,42,19,UiFactoryV17.Muted);
            _controlsBack=_ui.Button(Controls,"Back",40,-678,250,()=>_owner.Back(),false,52);
        }
        private void BuildCredits(Transform parent)
        {
            Credits=Card("Credits",parent);Header(Credits,"MADE WITH CARE","ECHOHEART");
            _ui.Label(Credits,"THE TEAM",40,-194,460,28,17,UiFactoryV17.Mint,true);
            _ui.Label(Credits,"Jehad Alharbi",40,-248,480,34,29,UiFactoryV17.Cream,true);_ui.Label(Credits,"World, audio and interface",40,-288,480,30,21,UiFactoryV17.Muted);
            _ui.Label(Credits,"Muhammad Jaddoua",40,-354,480,34,29,UiFactoryV17.Cream,true);_ui.Label(Credits,"Combat and enemy systems",40,-394,480,30,21,UiFactoryV17.Muted);
            _ui.Label(Credits,"Tariq Alothman",40,-460,480,34,29,UiFactoryV17.Cream,true);_ui.Label(Credits,"Player movement and abilities",40,-500,480,30,21,UiFactoryV17.Muted);
            _ui.Label(Credits,"ART & SOUND",592,-194,450,28,17,UiFactoryV17.Mint,true);
            _ui.Label(Credits,"Kenney\nNature Kit · CC0\n\nKay Lousberg\nForest Nature Pack · CC0\n\nOriginal ECHOHEART music,\nsound design and UI graphics.",592,-248,450,320,23,UiFactoryV17.Muted);
            _ui.Label(Credits,"SWE 402 · KFUPM",40,-588,470,30,20,UiFactoryV17.Gold);
            _ui.Button(Credits,"Asset credits",592,-590,450,()=>Application.OpenURL("https://github.com/Muhammad-Jaddoua/ECHO-HEART/blob/main/docs/ASSET_CREDITS.md"),false,52);
            _creditsBack=_ui.Button(Credits,"Back",40,-678,250,()=>_owner.Back(),false,52);
        }
        private void BuildModal(Transform parent)
        {
            Modal=_ui.Stretch("Confirmation overlay",parent);_ui.Paint(Modal,new Color(.005f,.025f,.02f,.86f),0,true);
            var card=Card("Confirmation card",Modal,660,432);ModalCard=card;ModalTitle=_ui.Label(card,"",32,-34,596,88,34,UiFactoryV17.Cream,true);
            ModalBody=_ui.Label(card,"",32,-148,596,112,23,UiFactoryV17.Muted);
            CancelButton=_ui.Button(card,"Cancel",32,-330,270,()=>_owner.CancelModal());ConfirmButton=_ui.Button(card,"Confirm",326,-330,302,()=>_owner.AcceptModal(),true);
        }
    }
}
