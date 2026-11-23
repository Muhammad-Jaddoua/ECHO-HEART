using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Read-only objective bearing/distance. Hidden secrets and pickups are never targets.</summary>
    [DefaultExecutionOrder(120)]
    public sealed class ObjectiveGuideV112 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _guardianEntry;
        [SerializeField] private Transform _heart;
        private RectTransform _safe,_surface,_marker;
        private UiRoundedV16 _paint;
        private UiGlyphV16 _icon;
        private UiGlyphKindV16 _shownKind=UiGlyphKindV16.None;
        private Text _caption,_distance;
        private Rect _lastSafe;
        private Vector2 _lastSize;
        private int _width,_height,_lastMetres=-1;
        private bool _promptVisible;
        public GuideGoalV112 CurrentGoal {get;private set;}
        public bool Visible=>_marker!=null&&_marker.gameObject.activeInHierarchy;
        public RectTransform Marker=>_marker;
        private void Awake()
        {
            if(_flow==null||_arena==null||_settings==null||_camera==null||_guardianEntry==null||_heart==null){Debug.LogError("V1.12 objective guide bindings missing.",this);enabled=false;return;}
            var go=new GameObject("Objective bearing",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=97;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=new UiFactoryV17(null);_safe=ui.Stretch("Objective safe area",go.transform);_surface=ui.Rect("Objective layout surface",_safe,0,0,1600,900,new Vector2(.5f,.5f));
            _marker=ui.Panel("Objective marker",_surface,0,0,200,64,UiFactoryV17.Forest,new Vector2(.5f,.5f));_paint=_marker.GetComponent<UiRoundedV16>();
            var group=_marker.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            _icon=ui.Icon(_marker,UiGlyphKindV16.Arrow,14,-18,28,UiFactoryV17.Gold);_icon.rectTransform.pivot=new Vector2(.5f,.5f);_icon.rectTransform.anchoredPosition=new Vector2(28,-32);
            _caption=ui.Label(_marker,"",54,-8,134,20,14,UiFactoryV17.Gold,true);_distance=ui.Label(_marker,"",54,-29,134,28,22,UiFactoryV17.Cream,true);
            RefreshLayout();Hide();
        }
        private void OnEnable(){GameEvents.PromptChanged+=Prompt;}
        private void OnDisable(){GameEvents.PromptChanged-=Prompt;Hide();}
        private void Prompt(string value){_promptVisible=!string.IsNullOrWhiteSpace(value);}
        private void Hide(){CurrentGoal=GuideGoalV112.None;if(_marker!=null)_marker.gameObject.SetActive(false);}
        private void RefreshLayout()
        {
            _width=Mathf.Max(1,Screen.width);_height=Mathf.Max(1,Screen.height);_lastSafe=Screen.safeArea;
            UiSafeRectV19 safe=UiLayoutV19.SafeArea(_width,_height,new UiSafeRectV19(_lastSafe.x,_lastSafe.y,_lastSafe.width,_lastSafe.height));
            _safe.anchorMin=new Vector2(safe.X/_width,safe.Y/_height);_safe.anchorMax=new Vector2((safe.X+safe.Width)/_width,(safe.Y+safe.Height)/_height);_safe.offsetMin=_safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();_lastSize=_safe.rect.size;UiSurfaceV19 surface=UiLayoutV19.Surface(_lastSize.x,_lastSize.y);
            _surface.sizeDelta=new Vector2(surface.Width,surface.Height);_surface.localScale=Vector3.one*surface.Scale;
        }
        private void LateUpdate()=>RefreshGuide();
        public void RefreshGuide()
        {
            if(_safe==null)return;
            if(_width!=Screen.width||_height!=Screen.height||_lastSafe!=Screen.safeArea||_lastSize!=_safe.rect.size)RefreshLayout();
            GuideGoalV112 goal=ObjectiveGuidePolicyV112.Select(_flow.State,_flow.CurrentRetryScope,_flow.HeartUnlocked,_arena.Ready,_settings.Values.Hints,_promptVisible);
            if(goal==GuideGoalV112.None||_flow.Player.Player==null||!_camera.isActiveAndEnabled){Hide();return;}
            Transform target=goal==GuideGoalV112.Heart?_heart:_guardianEntry;
            Vector3 screen=_camera.WorldToScreenPoint(target.position+Vector3.up*1.5f);
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(_surface,new Vector2(screen.x,screen.y),null,out Vector2 local)){Hide();return;}
            float scale=_settings.Values.HudScale;GuidePlacementV112 placement=ObjectiveGuidePolicyV112.Place(local.x,local.y,_surface.sizeDelta.x,_surface.sizeDelta.y,scale,screen.z<=0);
            if(!placement.Valid){Hide();return;}
            _marker.gameObject.SetActive(true);CurrentGoal=goal;_marker.localScale=Vector3.one*scale;_marker.anchoredPosition=new Vector2(placement.X,placement.Y);
            UiGlyphKindV16 kind=placement.Edge?UiGlyphKindV16.Arrow:goal==GuideGoalV112.Heart?UiGlyphKindV16.Heart:UiGlyphKindV16.Sword;
            if(kind!=_shownKind){_shownKind=kind;_icon.Kind=kind;}
            _icon.rectTransform.localEulerAngles=new Vector3(0,0,placement.Edge?placement.Angle:0);
            _caption.text=goal==GuideGoalV112.Heart?"ISLAND HEART":"GUARDIAN";
            _icon.color=_caption.color=goal==GuideGoalV112.Heart?UiFactoryV17.Mint:UiFactoryV17.Gold;
            int metres=Mathf.CeilToInt(Vector3.Distance(_flow.Player.Player.position,target.position));if(metres!=_lastMetres){_lastMetres=metres;_distance.text=metres+" m";}
            Color color=UiFactoryV17.Forest;if(_settings.Values.HighContrast)color.a=1;_paint.color=color;
        }
    }
}
