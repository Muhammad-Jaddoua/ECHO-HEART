using System;
using Echoheart.World;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    private static void Main()
    {
        var policy=new GuardianCuePolicyV111();
        Check(policy.Step(GuardianPhaseV111.Idle,false,true)==GuardianCueV111.None,"Idle played an attack cue.");
        foreach(bool slam in new[]{true,false,true,false})
        {
            Check(policy.Step(GuardianPhaseV111.Telegraph,slam,true)==(slam?GuardianCueV111.SlamWarning:GuardianCueV111.RangedWarning),"A new attack did not play its specific warning.");
            bool silent=true;for(int i=0;i<20;i++)silent&=policy.Step(GuardianPhaseV111.Telegraph,slam,true)==GuardianCueV111.None;
            Check(silent,"A warning repeated every frame.");
            Check(policy.Step(GuardianPhaseV111.Attack,slam,false)==GuardianCueV111.None,"Pause emitted an impact cue.");
            Check(policy.Step(GuardianPhaseV111.Telegraph,slam,true)==GuardianCueV111.None,"Resume replayed the interrupted warning.");
            Check(policy.Step(GuardianPhaseV111.Attack,slam,true)==(slam?GuardianCueV111.Slam:GuardianCueV111.RangedShot),"Actual attack entry missed its impact cue.");
            Check(policy.Step(GuardianPhaseV111.Attack,slam,true)==GuardianCueV111.None,"Impact cue repeated.");
            Check(policy.Step(GuardianPhaseV111.Recover,slam,true)==GuardianCueV111.None&&policy.Step(GuardianPhaseV111.Idle,false,true)==GuardianCueV111.None,"Recovery/Idle replayed damage presentation.");
        }
        policy.Step(GuardianPhaseV111.Telegraph,true,true);policy.Reset();
        Check(policy.Step(GuardianPhaseV111.Telegraph,true,true)==GuardianCueV111.SlamWarning,"Retry failed to re-arm the warning edge.");
        policy.Reset();Check(policy.Step(GuardianPhaseV111.Attack,false,true)==GuardianCueV111.RangedShot,"Late binding missed an actual attack.");
        Check(policy.Step(GuardianPhaseV111.Defeated,false,true)==GuardianCueV111.None,"Defeat invented another attack cue.");
        Check(GuardianCuePolicyV111.ArcVertices(0)==2&&GuardianCuePolicyV111.ArcVertices(.5f)==33&&GuardianCuePolicyV111.ArcVertices(1)==65,"Countdown arc does not reflect readiness.");
        foreach(float invalid in new[]{float.NaN,float.NegativeInfinity,-1f})Check(GuardianCuePolicyV111.Progress(invalid)==0&&GuardianCuePolicyV111.ArcVertices(invalid)==2,"Invalid progress poisoned warning geometry.");
        Check(GuardianCuePolicyV111.Progress(float.PositiveInfinity)==1&&GuardianCuePolicyV111.ArcVertices(100)==65,"Arc exceeded its fixed vertex bound.");
        foreach(float radius in new[]{0f,4.5f,9f})
        {
            bool correct=true;
            for(int i=0;i<64;i++){var point=GuardianCuePolicyV111.Ring(radius,i/64f);correct&=Near((float)Math.Sqrt(point.X*point.X+point.Z*point.Z),radius);}
            Check(correct,"Slam boundary radius changed around the ring or with scale.");
            var first=GuardianCuePolicyV111.Ring(radius,0);var last=GuardianCuePolicyV111.Ring(radius,1);
            Check(Near(first.X,last.X)&&Near(first.Z,last.Z),"A full progress arc does not close.");
        }
        var north=GuardianCuePolicyV111.Ring(4.5f,.5f);var east=GuardianCuePolicyV111.Ring(4.5f,.25f);
        Check(Near(north.X,0)&&Near(north.Z,4.5f)&&Near(east.X,4.5f)&&Near(east.Z,0),"Warning ring orientation is inconsistent.");
        Check(GuardianCuePolicyV111.AimDistance(40,float.PositiveInfinity)==40,"Ranged guide retained the old fixed seven-metre length.");
        Check(GuardianCuePolicyV111.AimDistance(40,8)==8&&GuardianCuePolicyV111.AimDistance(40,80)==40,"Cover projection exceeded cover/range.");
        Check(GuardianCuePolicyV111.AimDistance(40,-1)==0&&GuardianCuePolicyV111.AimDistance(0,10)==0,"Invalid cover/range produced negative geometry.");
        Check(GuardianCuePolicyV111.AimDistance(float.NaN,10)==0&&GuardianCuePolicyV111.AimDistance(40,float.NaN)==40,"Invalid aim distances poisoned geometry.");
        Check(Near(GuardianCuePolicyV111.ImpactRadius(4.5f,0),4.5f*.45f)&&Near(GuardianCuePolicyV111.ImpactRadius(4.5f,.35f),4.5f),"Impact accent exceeded or failed to reach the configured radius.");
        Check(GuardianCuePolicyV111.ImpactAlpha(0)==1&&GuardianCuePolicyV111.ImpactAlpha(.35f)==0&&GuardianCuePolicyV111.ImpactAlpha(100)==0,"Impact accent did not expire.");
        Check(GuardianCuePolicyV111.ImpactRadius(float.PositiveInfinity,.2f)==0,"Invalid radius produced unbounded geometry.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.11 Guardian cue lifecycle and geometry (C# 9.0).");
    }
}
