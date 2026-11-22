using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>Shared visual language for the menu, HUD, settings, prompts and discovery toast.</summary>
    public sealed class UiFactoryV17
    {
        public static readonly Color Forest = new Color(.025f,.09f,.08f,.96f);
        public static readonly Color Card = new Color(.055f,.17f,.15f,.96f);
        public static readonly Color Cream = new Color(.96f,.94f,.86f);
        public static readonly Color Mint = new Color(.50f,.89f,.73f);
        public static readonly Color Gold = new Color(.93f,.78f,.45f);
        public static readonly Color Muted = new Color(.63f,.77f,.71f);
        public static readonly Color Coral = new Color(.96f,.52f,.43f);
        private readonly Font _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly Action _press;
        private readonly List<Graphic> _panels = new List<Graphic>();
        public UiFactoryV17(Action press) => _press = press;
        public RectTransform Rect(string name, Transform parent, float x, float y, float w, float h, Vector2? anchor = null)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);
            Vector2 a=anchor??new Vector2(0,1);r.anchorMin=r.anchorMax=r.pivot=a;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public RectTransform Stretch(string name,Transform parent)
        {
            RectTransform r=Rect(name,parent,0,0,0,0,Vector2.zero);r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        public UiRoundedV16 Paint(RectTransform rect,Color color,float radius=16f,bool raycast=false,float border=0f)
        {
            var panel=rect.gameObject.AddComponent<UiRoundedV16>();panel.color=color;panel.Radius=radius;panel.Border=border;panel.raycastTarget=raycast;
            if(border==0f&&color.a>.7f&&color.r<.2f)_panels.Add(panel);return panel;
        }
        public RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color? color=null,Vector2? anchor=null)
        {var r=Rect(name,parent,x,y,w,h,anchor);Paint(r,color??Card);return r;}
        public Text Label(Transform parent,string value,float x,float y,float w,float h,int size=24,Color? color=null,bool bold=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            var r=Rect(value.Length>32?"Text":value,parent,x,y,w,h);var label=r.gameObject.AddComponent<Text>();label.font=_font;label.text=value;label.fontSize=size;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.color=color??Cream;
            label.alignment=align;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
        }
        public UiGlyphV16 Icon(Transform parent,UiGlyphKindV16 kind,float x,float y,float size,Color? color=null)
        {var r=Rect(kind+" icon",parent,x,y,size,size);var glyph=r.gameObject.AddComponent<UiGlyphV16>();glyph.Kind=kind;glyph.color=color??Mint;glyph.raycastTarget=false;return glyph;}
        public Button Button(Transform parent,string caption,float x,float y,float w,Action action,bool primary=false,float h=62f)
        {
            var r=Rect(caption,parent,x,y,w,h);var graphic=Paint(r,Color.white,13f,true);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=graphic;
            ColorBlock colors=b.colors;colors.normalColor=primary?Mint:new Color(.09f,.24f,.21f);colors.highlightedColor=primary?Gold:new Color(.14f,.33f,.28f);colors.selectedColor=primary?Gold:new Color(.14f,.33f,.28f);colors.pressedColor=primary?new Color(.70f,.69f,.43f):new Color(.06f,.19f,.17f);colors.disabledColor=new Color(.10f,.18f,.16f,.55f);colors.fadeDuration=.08f;b.colors=colors;
            Label(r,caption,18,-4,w-36,h-8,24,primary?Forest:Cream,true,TextAnchor.MiddleCenter);
            var frame=Stretch("Focus outline",r);frame.offsetMin=new Vector2(-4,-4);frame.offsetMax=new Vector2(4,4);Paint(frame,Mint,17f,false,2f);
            r.gameObject.AddComponent<UiFocusV16>().Bind(frame.gameObject);
            b.onClick.AddListener(()=>{_press?.Invoke();action?.Invoke();});return b;
        }
        public Slider Slider(Transform parent,string caption,float x,float y,float w,float value,float low,float high,Action<float> changed,out Text percent)
        {
            Label(parent,caption,x,y,w-100,34,24,Cream,true);percent=Label(parent,"",x+w-96,y,96,34,22,Mint,false,TextAnchor.UpperRight);
            var rect=Rect(caption+" slider",parent,x,y-44,w,34);Paint(rect,new Color(.04f,.12f,.10f,.001f),4,true);
            var track=Rect("Track",rect,0,-13,w,8);Paint(track,new Color(.19f,.34f,.28f),4);
            var fill=Stretch("Fill",track);Paint(fill,Mint,4);
            var handles=Stretch("Handle area",rect);handles.offsetMin=new Vector2(10,0);handles.offsetMax=new Vector2(-10,0);
            var handle=Rect("Handle",handles,0,0,22,28,new Vector2(0,.5f));var knob=Paint(handle,Cream,8,true);
            var slider=rect.gameObject.AddComponent<Slider>();slider.targetGraphic=knob;slider.fillRect=fill;slider.handleRect=handle;slider.minValue=low;slider.maxValue=high;slider.SetValueWithoutNotify(value);slider.onValueChanged.AddListener(v=>changed(v));
            var focus=Stretch("Slider focus",rect);focus.offsetMin=new Vector2(-5,-5);focus.offsetMax=new Vector2(5,5);Paint(focus,Mint,8,false,2);rect.gameObject.AddComponent<UiFocusV16>().Bind(focus.gameObject);return slider;
        }
        public void Contrast(bool high)
        {
            foreach(Graphic panel in _panels)if(panel!=null){Color c=panel.color;c.a=high?1f:.96f;panel.color=c;}
        }
        public static void Navigation(IList<Selectable> items)
        {
            var active=new List<Selectable>();foreach(Selectable item in items)if(item!=null&&item.gameObject.activeInHierarchy&&item.IsInteractable())active.Add(item);
            Canvas.ForceUpdateCanvases();var points=new UiNavPointV17[active.Count];
            for(int i=0;i<active.Count;i++){var corners=new Vector3[4];((RectTransform)active[i].transform).GetWorldCorners(corners);Vector3 center=(corners[0]+corners[2])*.5f;points[i]=new UiNavPointV17(center.x,center.y,active[i] is Slider);}
            for(int i=0;i<active.Count;i++)
            {
                int up=UiNavigationV17.Next(points,i,UiDirectionV17.Up),down=UiNavigationV17.Next(points,i,UiDirectionV17.Down),left=UiNavigationV17.Next(points,i,UiDirectionV17.Left),right=UiNavigationV17.Next(points,i,UiDirectionV17.Right);
                active[i].navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=up>=0?active[up]:null,selectOnDown=down>=0?active[down]:null,selectOnLeft=left>=0?active[left]:null,selectOnRight=right>=0?active[right]:null};
            }
        }
    }
}
