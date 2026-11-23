using System;
using Echoheart.Core;

namespace Echoheart.UI
{
    public enum GuideGoalV112 { None, Guardian, Heart }
    public struct GuidePlacementV112
    {
        public float X,Y,Angle;
        public bool Edge,Valid;
        public GuidePlacementV112(float x,float y,float angle,bool edge){X=x;Y=y;Angle=angle;Edge=edge;Valid=true;}
    }
    /// <summary>Objective visibility and HUD-space bearing placement. Never chooses a route or changes progression.</summary>
    public static class ObjectiveGuidePolicyV112
    {
        public static GuideGoalV112 Select(GameState state,RetryScope scope,bool heartUnlocked,bool arenaReady,bool hints,bool prompt)
        {
            if(state!=GameState.Playing||!hints||prompt)return GuideGoalV112.None;
            if(heartUnlocked)return GuideGoalV112.Heart;
            return arenaReady&&scope!=RetryScope.BossArena?GuideGoalV112.Guardian:GuideGoalV112.None;
        }
        private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public static GuidePlacementV112 Place(float x,float y,float width,float height,float hudScale,bool behind)
        {
            if(!Finite(x)||!Finite(y))return default;
            width=Finite(width)?Math.Max(1600,width):1600;height=Finite(height)?Math.Max(900,height):900;
            hudScale=float.IsNaN(hudScale)?1:Math.Max(.85f,Math.Min(1.15f,hudScale));
            float halfWidth=100*hudScale,halfHeight=32*hudScale;
            float extentX=width*.5f-24-halfWidth,extentY=height*.5f-340-halfHeight;
            if(behind){x=-x;y=-y;}
            if(Math.Abs(x)<.001f&&Math.Abs(y)<.001f)y=behind?-1:1;
            float angle=(float)(Math.Atan2(y,x)*180/Math.PI);
            bool edge=behind||Math.Abs(x)>extentX||Math.Abs(y)>extentY;
            if(edge)
            {
                float fit=Math.Min(Math.Abs(x)>.001f?extentX/Math.Abs(x):float.MaxValue,Math.Abs(y)>.001f?extentY/Math.Abs(y):float.MaxValue);
                x*=fit;y*=fit;
            }
            else if(Math.Abs(x)<halfWidth+20&&Math.Abs(y)<halfHeight+20)
                y=(y>=0?1:-1)*(halfHeight+28); // preserve a clear center for aiming
            return new GuidePlacementV112(x,y,angle,edge);
        }
    }
}
