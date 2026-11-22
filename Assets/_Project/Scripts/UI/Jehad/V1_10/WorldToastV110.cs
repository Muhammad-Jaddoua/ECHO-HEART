using Echoheart.Core;
using Echoheart.World;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    [DefaultExecutionOrder(-120)]
    public sealed class WorldToastV110 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private UiSettingsV17 _settings;
        private readonly ToastQueueV110 _queue=new ToastQueueV110();
        private RectTransform _safe,_surface,_back,_progress;
        private Text _heading,_message,_count;
        private UiGlyphV16 _icon;
        private UiRoundedV16 _paint;
        private CanvasGroup _group;
        private Rect _lastSafe;
        private Vector2 _lastSize;
        private int _width,_height;
        private float _hudScale=-1;
        public RectTransform Backdrop=>_back;
        public int Count=>_queue.Count;
        public string CurrentKey=>_queue.HasActive?_queue.Active.Key:string.Empty;
        public float Remaining=>_queue.Remaining;
        private bool Playing=>_flow!=null&&_flow.State==GameState.Playing;
        private void Awake()
        {
            if(_flow==null||_settings==null){Debug.LogError("V1.10 notifications require flow/settings.",this);enabled=false;return;}
            var root=new GameObject("World discoveries",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=95;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=new UiFactoryV17(null);_safe=ui.Stretch("Discovery safe area",root.transform);_surface=ui.Rect("Discovery layout surface",_safe,0,0,1600,900,new Vector2(.5f,.5f));
            _back=ui.Panel("Discovery backdrop",_surface,0,194,820,104,UiFactoryV17.Forest,new Vector2(.5f,0));_paint=_back.GetComponent<UiRoundedV16>();
            _group=_back.gameObject.AddComponent<CanvasGroup>();_group.alpha=0;_group.blocksRaycasts=false;_group.interactable=false;
            _icon=ui.Icon(_back,UiGlyphKindV16.Relic,18,-30,42,UiFactoryV17.Gold);
            _heading=ui.Label(_back,"",80,-10,620,22,15,UiFactoryV17.Gold,true);
            _message=ui.Label(_back,"",80,-36,714,56,22,UiFactoryV17.Cream);
            _count=ui.Label(_back,"",704,-10,90,22,14,UiFactoryV17.Muted,false,TextAnchor.UpperRight);
            _progress=ui.Rect("Reading time",_back,14,-97,792,3);ui.Paint(_progress,UiFactoryV17.Mint,1);
            RefreshLayout();
        }
        private void OnEnable(){GameEvents.RetryRequested+=Retrying;GameEvents.StateChanged+=StateChanged;}
        private void OnDisable(){GameEvents.RetryRequested-=Retrying;GameEvents.StateChanged-=StateChanged;Clear();}
        private void Retrying(RetryScope scope)=>Clear();
        private void StateChanged(GameState state){if(state==GameState.Title||state==GameState.Dead||state==GameState.Complete)Clear();else Render();}
        public bool Show(ToastNoticeV110 notice)
        {
            if(!Playing||!enabled)return false;
            bool accepted=_queue.Enqueue(notice);Render();return accepted;
        }
        public void Clear(){_queue.Clear();if(_group!=null)_group.alpha=0;}
        public void Dismiss(string key){_queue.Remove(key);Render();}
        public void RefreshLayout()
        {
            if(_safe==null)return;
            _width=Mathf.Max(1,Screen.width);_height=Mathf.Max(1,Screen.height);_lastSafe=Screen.safeArea;
            UiSafeRectV19 safe=UiLayoutV19.SafeArea(_width,_height,new UiSafeRectV19(_lastSafe.x,_lastSafe.y,_lastSafe.width,_lastSafe.height));
            _safe.anchorMin=new Vector2(safe.X/_width,safe.Y/_height);_safe.anchorMax=new Vector2((safe.X+safe.Width)/_width,(safe.Y+safe.Height)/_height);_safe.offsetMin=_safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();_lastSize=_safe.rect.size;
            UiSurfaceV19 surface=UiLayoutV19.Surface(_lastSize.x,_lastSize.y);_surface.sizeDelta=new Vector2(surface.Width,surface.Height);_surface.localScale=Vector3.one*surface.Scale;
            _hudScale=_settings.Values.HudScale;_back.anchoredPosition=new Vector2(0,UiLayoutV19.ToastBottom(_hudScale));_back.localScale=Vector3.one*_hudScale;
        }
        private void Update()
        {
            if(_group==null)return;
            if(_width!=Screen.width||_height!=Screen.height||_lastSafe!=Screen.safeArea||_lastSize!=_safe.rect.size||_hudScale!=_settings.Values.HudScale)RefreshLayout();
            _queue.Tick(Time.unscaledDeltaTime,Playing);Render();
        }
        private void Render()
        {
            if(_group==null)return;
            bool visible=Playing&&_queue.HasActive;_group.alpha=visible?(_settings.Values.ReduceMotion?1:Mathf.Clamp01(_queue.Remaining/.35f)):0;
            if(!visible)return;
            ToastNoticeV110 notice=_queue.Active;_heading.text=notice.Heading;_message.text=notice.Message;_count.text=_queue.PendingCount>0?_queue.PendingCount+" more":"";
            _icon.Kind=notice.Kind==ToastKindV110.Objective?UiGlyphKindV16.Heart:notice.Kind==ToastKindV110.Secret?UiGlyphKindV16.Check:UiGlyphKindV16.Relic;
            _icon.color=_heading.color=notice.Kind==ToastKindV110.Objective?UiFactoryV17.Mint:UiFactoryV17.Gold;
            _progress.gameObject.SetActive(!_settings.Values.ReduceMotion);_progress.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,792*Mathf.Clamp01(_queue.Remaining/_queue.Duration));
            Color color=UiFactoryV17.Forest;if(_settings.Values.HighContrast)color.a=1;_paint.color=color;
        }
    }
}
