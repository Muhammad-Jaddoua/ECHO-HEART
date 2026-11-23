using System;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Near(double a,double b)=>Math.Abs(a-b)<.00001;
    private static bool Same(TitleVistaOffsetV116 a,TitleVistaOffsetV116 b)=>Near(a.Yaw,b.Yaw)&&Near(a.Lift,b.Lift);
    private static void Advance(TitleVistaMotionV116 motion,int steps){for(int i=0;i<steps;i++)motion.Step(.1f,true,false,true);}
    private static void Main()
    {
        var motion=new TitleVistaMotionV116();
        Check(motion.Elapsed==0&&motion.Offset.Yaw==0&&motion.Offset.Lift==0,"Initial view has an offset.");
        motion.Step(0,true,false,true);Check(motion.Elapsed==0,"A zero-time refresh advances the camera.");
        motion.Step(1f/60,true,false,true);Check(motion.Elapsed>0&&motion.Offset.Yaw>0&&motion.Offset.Lift>=0,"Home view does not advance gently.");
        double phase=motion.Elapsed;var held=motion.Offset;
        motion.Step(.1f,false,false,true);Check(motion.Elapsed==phase&&Same(motion.Offset,held),"A settings/information page advances the vista.");
        motion.Step(.1f,true,false,false);Check(motion.Elapsed==phase&&Same(motion.Offset,held),"An unfocused window advances the vista.");
        motion.Step(.1f,false,false,false);Check(motion.Elapsed==phase,"Combined page/focus suppression advances the vista.");
        motion.Step(.1f,false,true,false);Check(motion.Elapsed==0&&motion.Offset.Yaw==0&&motion.Offset.Lift==0,"Reduced motion does not reset to a fixed pose when browsing/unfocused.");
        motion.Step(.1f,true,true,true);Check(motion.Elapsed==0,"Reduced motion permits home animation.");
        motion.Step(.1f,true,false,true);Check(Near(motion.Elapsed,.1)&&Math.Abs(motion.Offset.Yaw)<.03,"Leaving reduced motion resumes with a camera jump.");
        motion.Reset();Check(motion.Elapsed==0&&motion.Offset.Lift==0&&motion.Offset.Yaw==0,"Title reentry/reset retains its previous phase.");
        foreach(float bad in new[]{-1f,0f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            held=motion.Offset;phase=motion.Elapsed;motion.Step(bad,true,false,true);
            Check(motion.Elapsed==phase&&Same(motion.Offset,held),"Invalid frame time poisons the vista.");
        }
        motion.Step(float.MaxValue,true,false,true);Check(Near(motion.Elapsed,.1),"A large frame time bypasses the motion step limit.");
        motion.Reset();motion.Step(30,true,false,true);Check(Near(motion.Elapsed,.1),"Returning from a long suspension skips through the vista.");
        motion.Reset();Advance(motion,225);
        Check(Near(motion.Offset.Yaw,4)&&Near(motion.Offset.Lift,1.2),"First quarter of the authored orbit is wrong.");
        Advance(motion,225);Check(Near(motion.Offset.Yaw,0)&&Near(motion.Offset.Lift,2.4),"Half-cycle exceeds or misses the authored height.");
        Advance(motion,225);Check(Near(motion.Offset.Yaw,-4)&&Near(motion.Offset.Lift,1.2),"Third quarter of the orbit is wrong.");
        Advance(motion,225);Check(Near(motion.Offset.Yaw,0)&&Near(motion.Offset.Lift,0),"The 90-second loop has a seam.");
        var single=new TitleVistaMotionV116();var split=new TitleVistaMotionV116();single.Step(.1f,true,false,true);
        for(int i=0;i<6;i++)split.Step(1f/60,true,false,true);
        Check(Same(single.Offset,split.Offset),"Ordinary frame partitioning changes the scenic path.");
        bool bounded=true,continuous=true,finite=true,wrapped=true;
        motion.Reset();TitleVistaOffsetV116 previous=motion.Offset;
        for(int i=0;i<100000;i++)
        {
            var next=motion.Step(.1f,true,false,true);
            bounded&=Math.Abs(next.Yaw)<=4&&next.Lift>=0&&next.Lift<=2.4f;
            continuous&=Math.Abs(next.Yaw-previous.Yaw)<.0281&&Math.Abs(next.Lift-previous.Lift)<.0085;
            finite&=!float.IsNaN(next.Yaw)&&!float.IsInfinity(next.Yaw)&&!float.IsNaN(next.Lift)&&!float.IsInfinity(next.Lift);
            wrapped&=motion.Elapsed>=0&&motion.Elapsed<90;previous=next;
        }
        Check(bounded,"Long-running title view exceeds its yaw/height envelope.");
        Check(continuous,"A loop boundary or ordinary step creates a discontinuity.");
        Check(finite,"Long-running motion produces nonfinite values.");
        Check(wrapped,"Elapsed phase grows without a periodic bound.");
        held=motion.Offset;phase=motion.Elapsed;
        for(int i=0;i<300;i++)motion.Step(.1f,false,false,true);
        Check(motion.Elapsed==phase&&Same(motion.Offset,held),"An extended menu visit drifts or consumes scenic time.");
        for(int i=0;i<300;i++)motion.Step(.1f,true,false,false);
        Check(motion.Elapsed==phase&&Same(motion.Offset,held),"An extended focus loss drifts or consumes scenic time.");
        motion.Step(.1f,true,false,true);Check(Math.Abs(motion.Offset.Yaw-held.Yaw)<.0281,"Focus restoration jumps instead of resuming the held phase.");
        motion.Step(float.NaN,false,true,false);Check(motion.Elapsed==0&&Same(motion.Offset,new TitleVistaOffsetV116(0,0)),"Reduced motion depends on a usable clock or active page.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.16 title vista motion, bounds, holds, reduced motion and bounded frame steps (C# 9.0).");
    }
}
