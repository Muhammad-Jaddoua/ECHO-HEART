using System;

namespace Echoheart.UI
{
    public enum UiDirectionV17 { Up, Down, Left, Right }
    public struct UiNavPointV17
    {
        public float X,Y;
        public bool HorizontalSlider;
        public UiNavPointV17(float x,float y,bool slider=false){X=x;Y=y;HorizontalSlider=slider;}
    }
    /// <summary>Spatial arrow navigation inside the supplied focus scope. Tab order stays explicit.</summary>
    public sealed class UiNavigationV17
    {
        public static int Next(UiNavPointV17[] points,int current,UiDirectionV17 direction)
        {
            if(points==null||current<0||current>=points.Length||points.Length<2)return -1;
            bool horizontal=direction==UiDirectionV17.Left||direction==UiDirectionV17.Right;
            if(horizontal&&points[current].HorizontalSlider)return -1; // native Slider owns left/right
            float dx=direction==UiDirectionV17.Right?1:direction==UiDirectionV17.Left?-1:0;
            float dy=direction==UiDirectionV17.Up?1:direction==UiDirectionV17.Down?-1:0;
            int result=-1;float best=float.MaxValue;
            for(int i=0;i<points.Length;i++)
            {
                if(i==current)continue;float x=points[i].X-points[current].X,y=points[i].Y-points[current].Y;
                float forward=x*dx+y*dy;if(forward<=.001f)continue;
                float side=Math.Abs(x*dy-y*dx);float score=forward+side*3f+side*side/Math.Max(1f,forward)*.5f;
                if(score<best){best=score;result=i;}
            }
            if(result>=0)return result;
            if(!horizontal)return (current+(direction==UiDirectionV17.Up?-1:1)+points.Length)%points.Length;
            // Horizontal wrapping is limited to the same row, never a distant unrelated control.
            float extreme=direction==UiDirectionV17.Right?float.MaxValue:float.MinValue;
            for(int i=0;i<points.Length;i++)if(i!=current&&Math.Abs(points[i].Y-points[current].Y)<1f)
            {
                if(direction==UiDirectionV17.Right?points[i].X<extreme:points[i].X>extreme){extreme=points[i].X;result=i;}
            }
            return result;
        }
    }
}
