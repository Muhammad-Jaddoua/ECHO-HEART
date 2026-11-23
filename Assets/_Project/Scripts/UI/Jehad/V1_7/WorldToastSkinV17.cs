using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Styles the existing discovery presenter without changing its event/timer ownership.</summary>
    public sealed class WorldToastSkinV17 : MonoBehaviour
    {
        [SerializeField] private WorldToastV15 _toast;
        [SerializeField] private UiSettingsV17 _settings;
        private CanvasGroup _textGroup,_backGroup;
        private RectTransform _text,_back;
        private void Start()
        {
            if(_toast==null||_settings==null)return;Text label=_toast.GetComponentInChildren<Text>(true);if(label==null)return;
            _text=label.rectTransform;_textGroup=label.GetComponent<CanvasGroup>();label.fontSize=24;label.color=UiFactoryV17.Cream;
            _text.anchorMin=_text.anchorMax=_text.pivot=new Vector2(.5f,0);_text.anchoredPosition=new Vector2(0,194);_text.sizeDelta=new Vector2(820,76);
            CanvasScaler scaler=label.GetComponentInParent<Canvas>().GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=new UiFactoryV17(null);_back=ui.Panel("Discovery backdrop",_text.parent,0,194,820,76,UiFactoryV17.Forest,new Vector2(.5f,0));_back.SetSiblingIndex(_text.GetSiblingIndex());
            _backGroup=_back.gameObject.AddComponent<CanvasGroup>();_backGroup.blocksRaycasts=false;_backGroup.interactable=false;
            ui.Icon(_back,UiGlyphKindV16.Relic,18,-25,26,UiFactoryV17.Gold);
        }
        private void LateUpdate()
        {
            if(_textGroup==null||_backGroup==null)return;
            if(_settings.Values.ReduceMotion&&_textGroup.alpha>0)_textGroup.alpha=1f;
            _backGroup.alpha=_textGroup.alpha;_text.localScale=_back.localScale=Vector3.one*_settings.Values.HudScale;
            Color color=UiFactoryV17.Forest;if(_settings.Values.HighContrast)color.a=1f;_back.GetComponent<UiRoundedV16>().color=color;
        }
    }
}
