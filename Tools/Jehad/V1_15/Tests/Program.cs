using System;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.00001f;
    private static HealthBarV115 Full(int maximum){var bar=new HealthBarV115();bar.Observe(maximum,maximum);return bar;}
    private static void Main()
    {
        var bar=new HealthBarV115();
        Check(bar.Fraction==1&&bar.Trail==1&&!bar.Low,"Initial presentation state is invalid.");
        bar.Observe(3,5);Check(bar.Current==3&&bar.Maximum==5&&Near(bar.Fraction,.6f)&&Near(bar.Trail,.6f),"First snapshot is treated as damage.");
        bar=Full(5);bar.Observe(4,5);
        Check(Near(bar.Fraction,.8f)&&bar.Trail==1&&bar.Current==4,"Real health did not update immediately.");
        bar.Tick(.09f,false);Check(bar.Trail==1,"Recent loss has no initial hold.");
        bar.Observe(4,5);bar.Tick(.09f,false);bar.Tick(.05f,false);
        Check(Near(bar.Trail,.93f)&&Near(bar.Fraction,.8f),"Duplicate health messages restart the hold or move current health.");
        bar.Tick(2,false);Check(Near(bar.Trail,.8f),"Loss trail fails to settle on current health.");
        bar.Tick(2,false);Check(Near(bar.Trail,.8f),"Settled trail overshoots current health.");
        bar=Full(10);bar.Observe(8,10);bar.Tick(.2f,false);float prior=bar.Trail;bar.Observe(6,10);
        Check(Near(bar.Fraction,.6f)&&Near(bar.Trail,prior),"A second hit erased the visible recent loss.");
        bar.Tick(.18f,false);Check(Near(bar.Trail,prior),"A second accepted hit did not renew the short hold.");
        bar.Tick(.1f,false);Check(bar.Trail<prior&&bar.Trail>=bar.Fraction,"A burst of hits leaves a stuck or undershooting trail.");
        bar.Observe(9,10);Check(Near(bar.Fraction,.9f)&&Near(bar.Trail,.9f),"Healing leaves a misleading damage trail.");
        bar.Observe(8,20);Check(Near(bar.Fraction,.4f)&&Near(bar.Trail,.4f)&&bar.Maximum==20,"Changed maximum health carries a trail across actors/resets.");
        bar.Observe(2,20);bar.Snap();Check(Near(bar.Trail,.1f)&&Near(bar.Fraction,.1f),"Menu/reset snap retains recent loss.");
        bar.Observe(1,20);bar.Tick(.01f,false);Check(Near(bar.Trail,.1f),"A new hit after snapping lost its own hold.");
        bar.Tick(0,true);Check(Near(bar.Trail,.05f),"Reduced motion does not clear animation immediately.");
        bar=Full(5);bar.Observe(1,5);bar.Tick(float.NaN,true);
        Check(Near(bar.Trail,.2f),"Reduced motion depends on a valid elapsed time.");
        bar=Full(5);bar.Observe(2,5);
        foreach(float bad in new[]{-1f,0f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {bar.Tick(bad,false);Check(bar.Trail==1&&Near(bar.Fraction,.4f),"Invalid elapsed time changes the health display.");}
        bar.Tick(float.MaxValue,false);Check(Near(bar.Trail,.4f),"An extreme finite frame time produces invalid trail data.");
        var single=Full(5);var split=Full(5);single.Observe(4,5);split.Observe(4,5);
        single.Tick(.3f,false);for(int i=0;i<3;i++)split.Tick(.1f,false);
        Check(Near(single.Trail,split.Trail)&&Near(single.Trail,.832f),"Hold consumption depends on frame partitioning.");
        bar=Full(4);bar.Observe(1,4);Check(bar.Low,"Quarter-health boundary has no warning.");
        bar.Observe(2,4);Check(!bar.Low,"Healthy player retains low-health warning.");
        bar.Observe(1,5);Check(bar.Low,"Review player's one HP has no low-health warning.");
        bar.Observe(1,3);Check(!bar.Low,"Warning threshold differs from one quarter.");
        bar.Observe(0,3);Check(!bar.Low&&bar.Fraction==0,"Dead player has nonzero current fill or live low-health warning.");
        bar.Observe(536870911,int.MaxValue);Check(bar.Low,"Large health threshold overflows.");
        bar.Observe(536870912,int.MaxValue);Check(!bar.Low,"Float precision changes the exact low-health threshold.");
        bar=Full(int.MaxValue);bar.Observe(int.MaxValue-1000,int.MaxValue);bar.Observe(int.MaxValue-999,int.MaxValue);
        Check(bar.Trail==bar.Fraction,"Small integer healing at a large maximum retains a stale trail.");
        bar.Observe(-9,5);Check(bar.Current==0&&bar.Fraction==0,"Negative health is not clamped.");
        bar.Observe(9,5);Check(bar.Current==5&&bar.Fraction==1&&bar.Trail==1,"Above-maximum health is not clamped.");
        bar.Observe(5,0);Check(bar.Current==0&&bar.Maximum==0&&bar.Fraction==0&&bar.Trail==0&&!bar.Low,"Zero maximum produces invalid presentation.");
        bar.Observe(int.MaxValue,int.MinValue);Check(bar.Current==0&&bar.Maximum==0&&bar.Trail==0,"Negative maximum produces invalid presentation.");
        var boss=Full(120);boss.Observe(59,120);
        Check(Near(boss.Fraction,59f/120)&&boss.Trail==1,"Guardian damage fails immediate display semantics.");
        boss.Tick(.9f,false);Check(Near(boss.Trail,boss.Fraction),"Guardian trail cannot settle within the bounded animation time.");
        boss.Observe(120,120);Check(boss.Trail==1&&boss.Fraction==1,"Retry healing carries a stale Guardian loss trail.");
        var random=new Random(115);bool bounded=true,currentExact=true,settles=true;
        for(int sample=0;sample<500;sample++)
        {
            int maximum=random.Next(1,251);bar=Full(maximum);
            for(int step=0;step<20;step++)
            {
                int current=random.Next(0,maximum+1);bar.Observe(current,maximum);
                currentExact&=bar.Current==current&&Near(bar.Fraction,(float)current/maximum);
                bar.Tick((float)random.NextDouble()*.2f,false);
                bounded&=bar.Fraction>=0&&bar.Trail>=bar.Fraction&&bar.Trail<=1&&!float.IsNaN(bar.Trail);
            }
            bar.Tick(10,false);settles&=Near(bar.Trail,bar.Fraction);
        }
        Check(currentExact,"Mixed damage/healing sequences delay or alter current health.");
        Check(bounded,"Mixed sequences produce invalid or undershooting trails.");
        Check(settles,"Mixed sequences retain loss after settling.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.15 immediate health/loss trail model, reset and reduced-motion behavior (C# 9.0).");
    }
}
