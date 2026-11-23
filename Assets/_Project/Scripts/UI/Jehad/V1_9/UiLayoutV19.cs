using System;

namespace Echoheart.UI
{
    public struct UiSafeRectV19
    {
        public float X,Y,Width,Height;
        public UiSafeRectV19(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
    }
    public struct UiSurfaceV19
    {
        public float Width,Height,Scale;
        public UiSurfaceV19(float width,float height,float scale){Width=width;Height=height;Scale=scale;}
    }
    /// <summary>Engine-independent safe-area clipping and uniform UI fitting in canvas units.</summary>
    public static class UiLayoutV19
    {
        public const float ReferenceWidth=1600f,ReferenceHeight=900f;
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        private static float Positive(float value,float fallback)=>Finite(value)&&value>0f?value:fallback;
        public static UiSafeRectV19 SafeArea(float width,float height,UiSafeRectV19 requested)
        {
            width=Positive(width,1);height=Positive(height,1);
            var full=new UiSafeRectV19(0,0,width,height);
            if(!Finite(requested.X)||!Finite(requested.Y)||!Finite(requested.Width)||!Finite(requested.Height)||requested.Width<=0||requested.Height<=0)return full;
            float left=Math.Max(0,Math.Min(width,requested.X)),bottom=Math.Max(0,Math.Min(height,requested.Y));
            float right=Math.Max(0,Math.Min(width,requested.X+requested.Width)),top=Math.Max(0,Math.Min(height,requested.Y+requested.Height));
            return right>left&&top>bottom?new UiSafeRectV19(left,bottom,right-left,top-bottom):full;
        }
        public static float CardScale(float width,float height,float cardWidth,float cardHeight)
        {
            width=Positive(width,1);height=Positive(height,1);cardWidth=Positive(cardWidth,1);cardHeight=Positive(cardHeight,1);
            float margin=Math.Min(24f,Math.Min(width,height)*.25f);
            return Math.Min(1f,Math.Min((width-2*margin)/cardWidth,(height-2*margin)/cardHeight));
        }
        public static UiSurfaceV19 Surface(float width,float height)
        {
            width=Positive(width,1);height=Positive(height,1);
            float logicalWidth=Math.Max(ReferenceWidth,width),logicalHeight=Math.Max(ReferenceHeight,height);
            return new UiSurfaceV19(logicalWidth,logicalHeight,Math.Min(1f,Math.Min(width/logicalWidth,height/logicalHeight)));
        }
        public static float ToastBottom(float hudScale)
        {
            hudScale=Math.Max(.85f,Math.Min(1.15f,Positive(hudScale,1)));
            return 96f+66f*hudScale+22f;
        }
    }
}
