using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Fits the existing discovery presenter to the same safe-area surface as the HUD.</summary>
    public sealed class WorldToastSkinV19 : MonoBehaviour
    {
        [SerializeField] private WorldToastV15 _toast;
        [SerializeField] private UiSettingsV17 _settings;
        private CanvasGroup _textGroup,_backGroup;
        private RectTransform _safe,_surface,_text,_back;
        private UiRoundedV16 _backPaint;
        private Rect _lastSafe;
        private Vector2 _lastSize;
        private int _width,_height;
        private float _hudScale=-1;
        public RectTransform Backdrop => _back;
        private void Start()
        {
            if(_toast==null||_settings==null)return;
            Text label=_toast.GetComponentInChildren<Text>(true);if(label==null)return;
            Canvas canvas=label.GetComponentInParent<Canvas>();CanvasScaler scaler=canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=new UiFactoryV17(null);_safe=ui.Stretch("Discovery safe area",canvas.transform);
            _surface=ui.Rect("Discovery layout surface",_safe,0,0,1600,900,new Vector2(.5f,.5f));
            _text=label.rectTransform;_text.SetParent(_surface,false);_textGroup=label.GetComponent<CanvasGroup>();label.fontSize=24;label.color=UiFactoryV17.Cream;
            _text.anchorMin=_text.anchorMax=_text.pivot=new Vector2(.5f,0);_text.sizeDelta=new Vector2(820,76);
            _back=ui.Panel("Discovery backdrop",_surface,0,194,820,76,UiFactoryV17.Forest,new Vector2(.5f,0));_back.SetAsFirstSibling();
            _backPaint=_back.GetComponent<UiRoundedV16>();_backGroup=_back.gameObject.AddComponent<CanvasGroup>();_backGroup.blocksRaycasts=false;_backGroup.interactable=false;
            ui.Icon(_back,UiGlyphKindV16.Relic,18,-25,26,UiFactoryV17.Gold);RefreshLayout();
        }
        public void RefreshLayout()
        {
            if(_safe==null)return;
            _width=Mathf.Max(1,Screen.width);_height=Mathf.Max(1,Screen.height);_lastSafe=Screen.safeArea;
            UiSafeRectV19 safe=UiLayoutV19.SafeArea(_width,_height,new UiSafeRectV19(_lastSafe.x,_lastSafe.y,_lastSafe.width,_lastSafe.height));
            _safe.anchorMin=new Vector2(safe.X/_width,safe.Y/_height);_safe.anchorMax=new Vector2((safe.X+safe.Width)/_width,(safe.Y+safe.Height)/_height);_safe.offsetMin=_safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();_lastSize=_safe.rect.size;
            UiSurfaceV19 surface=UiLayoutV19.Surface(_lastSize.x,_lastSize.y);
            _surface.sizeDelta=new Vector2(surface.Width,surface.Height);_surface.localScale=Vector3.one*surface.Scale;
            _hudScale=_settings.Values.HudScale;Vector2 position=new Vector2(0,UiLayoutV19.ToastBottom(_hudScale));
            _text.anchoredPosition=_back.anchoredPosition=position;_text.localScale=_back.localScale=Vector3.one*_hudScale;
        }
        private void LateUpdate()
        {
            if(_textGroup==null||_backGroup==null)return;
            if(_width!=Screen.width||_height!=Screen.height||_lastSafe!=Screen.safeArea||_lastSize!=_safe.rect.size||_hudScale!=_settings.Values.HudScale)RefreshLayout();
            if(_settings.Values.ReduceMotion&&_textGroup.alpha>0)_textGroup.alpha=1f;
            _backGroup.alpha=_textGroup.alpha;
            Color color=UiFactoryV17.Forest;if(_settings.Values.HighContrast)color.a=1f;_backPaint.color=color;
        }
    }
}
