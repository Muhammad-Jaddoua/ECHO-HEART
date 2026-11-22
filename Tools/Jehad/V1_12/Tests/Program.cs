using System;
using Echoheart.Core;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.01f;
    private static void Main()
    {
        Check(ObjectiveGuidePolicyV112.Select(GameState.Title,RetryScope.MainArea,false,true,true,false)==GuideGoalV112.None,"Title showed a world objective.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.MainArea,false,true,true,false)==GuideGoalV112.Guardian,"Exploration has no Guardian guidance.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.MainArea,false,false,true,false)==GuideGoalV112.None,"Guide targets an unavailable arena.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.BossArena,false,true,true,false)==GuideGoalV112.None,"Entrance guidance distracts during the fight.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.BossArena,true,true,true,false)==GuideGoalV112.Heart,"Defeat did not switch to the Heart.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Paused,RetryScope.BossArena,true,true,true,false)==GuideGoalV112.None,"Pause retained Heart guidance.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Dead,RetryScope.MainArea,false,true,true,false)==GuideGoalV112.None,"Death retained world guidance.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Complete,RetryScope.BossArena,true,true,true,false)==GuideGoalV112.None,"Completion retained Heart guidance.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.MainArea,false,true,false,false)==GuideGoalV112.None,"Hints-off did not hide the guide.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.BossArena,true,true,false,false)==GuideGoalV112.None,"Hints-off retained the Heart target.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.MainArea,false,true,true,true)==GuideGoalV112.None,"Guide covered an interaction prompt.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.BossArena,true,true,true,true)==GuideGoalV112.None,"Guide covered Heart interaction.");
        Check(ObjectiveGuidePolicyV112.Select(GameState.Playing,RetryScope.NewGame,false,true,true,false)==GuideGoalV112.Guardian,"New Game retained the previous target.");
        var inside=ObjectiveGuidePolicyV112.Place(400,40,1600,900,1,false);
        Check(inside.Valid&&!inside.Edge&&Near(inside.X,400)&&Near(inside.Y,40),"Unobstructed target position changed.");
        var right=ObjectiveGuidePolicyV112.Place(5000,0,1600,900,1,false);
        Check(right.Edge&&Near(right.X,676)&&Near(right.Y,0)&&Near(right.Angle,0),"Right-bearing marker is wrong.");
        var up=ObjectiveGuidePolicyV112.Place(0,5000,1600,900,1,false);
        Check(up.Edge&&Near(up.X,0)&&Near(up.Y,78)&&Near(up.Angle,90),"Up-bearing marker is wrong.");
        var behind=ObjectiveGuidePolicyV112.Place(500,0,1600,900,1,true);
        Check(behind.Edge&&behind.X<0&&Near(Math.Abs(behind.Angle),180),"Behind-camera direction was not corrected.");
        var directlyBehind=ObjectiveGuidePolicyV112.Place(0,0,1600,900,1,true);
        Check(directlyBehind.Edge&&directlyBehind.Y<0&&Near(directlyBehind.Angle,-90),"Directly behind target has no stable direction.");
        Check(!ObjectiveGuidePolicyV112.Place(float.NaN,0,1600,900,1,false).Valid&&!ObjectiveGuidePolicyV112.Place(0,float.PositiveInfinity,1600,900,1,false).Valid,"Invalid camera projection produced a visible marker.");
        var extreme=ObjectiveGuidePolicyV112.Place(float.MaxValue,0,1600,900,float.NaN,false);
        Check(extreme.Valid&&Near(extreme.X,676),"Extreme finite projection/invalid scale poisoned placement.");
        foreach(var available in new[]{(1600f,900f),(2133f,900f),(1600f,1200f),(600f,420f),(900f,1600f)})
        foreach(float scale in new[]{.85f,1f,1.15f})
        {
            UiSurfaceV19 surface=UiLayoutV19.Surface(available.Item1,available.Item2);float halfW=100*scale,halfH=32*scale;
            bool contained=true,reserved=true,clearCenter=true,angles=true;
            foreach(var target in new[]{(0f,0f),(100f,0f),(-100f,0f),(400f,40f),(5000f,0f),(-5000f,0f),(0f,5000f),(0f,-5000f),(5000f,5000f),(-5000f,-5000f)})
            foreach(bool isBehind in new[]{false,true})
            {
                GuidePlacementV112 p=ObjectiveGuidePolicyV112.Place(target.Item1,target.Item2,surface.Width,surface.Height,scale,isBehind);
                float left=available.Item1/2+(p.X-halfW)*surface.Scale,rightEdge=available.Item1/2+(p.X+halfW)*surface.Scale;
                float bottom=available.Item2/2+(p.Y-halfH)*surface.Scale,top=available.Item2/2+(p.Y+halfH)*surface.Scale;
                contained&=p.Valid&&left>=-.02f&&rightEdge<=available.Item1+.02f&&bottom>=-.02f&&top<=available.Item2+.02f;
                reserved&=p.Y+halfH<=surface.Height/2-340+.02f&&p.Y-halfH>=-surface.Height/2+340-.02f;
                clearCenter&=p.X+halfW<=-20+.02f||p.X-halfW>=20-.02f||p.Y+halfH<=-20+.02f||p.Y-halfH>=20-.02f;
                angles&=!float.IsNaN(p.Angle)&&p.Angle>=-180&&p.Angle<=180;
            }
            Check(contained,"Marker escaped the available safe-area surface.");
            Check(reserved,"Marker overlaps top HUD or bottom notifications/prompts.");
            Check(clearCenter,"Marker obscures the aiming center.");
            Check(angles,"Pointer angle is invalid.");
        }
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.12 objective visibility and bearing placement with CoreTypes/shared layout (C# 9.0).");
    }
}
