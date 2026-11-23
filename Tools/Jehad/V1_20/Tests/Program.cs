using System;
using Echoheart.Core;
using Echoheart.World;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Visible(bool open,bool collected,bool gated,float progress)=>ChestRevealPolicyV120.Visible(open,collected,gated,progress);
    private static void Main()
    {
        float[] progress={-1,0,.2f,.94f,.94999f,.95f,1,2,float.NaN,float.PositiveInfinity,float.NegativeInfinity};
        bool legacy=true,closed=true,collected=true;
        foreach(bool open in new[]{false,true})foreach(bool taken in new[]{false,true})foreach(float value in progress)
            legacy&=Visible(open,taken,false,value)==(open&&!taken);
        foreach(bool gate in new[]{false,true})foreach(float value in progress)
        {closed&=!Visible(false,false,gate,value);collected&=!Visible(true,true,gate,value);}
        Check(legacy,"Opt-out changes prior scene behavior for a progress value.");
        Check(closed,"Presentation gate unlocks a closed chest.");
        Check(collected,"Presentation gate resurrects a collected reward.");
        foreach(float value in new[]{-1f,0f,.2f,.94f,.94999f})Check(!Visible(true,false,true,value),"Partly open lid exposes a reward.");
        foreach(float value in new[]{.95f,1f,2f})Check(Visible(true,false,true,value),"Cleared lid fails to reveal a reward.");
        foreach(float value in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})Check(!Visible(true,false,true,value),"Invalid animation progress exposes a gated reward.");
        var chest=new ChestStateV15();
        Check(!Visible(chest.Opened,false,true,1),"Closed original policy admits a reward.");
        Check(!chest.TryOpen(GameState.Paused,true)&&!chest.Opened,"Pause bypasses the original open guard.");
        Check(!chest.TryOpen(GameState.Playing,false)&&!chest.Opened,"Distance bypasses the original open guard.");
        Check(chest.TryOpen(GameState.Playing,true)&&chest.Opened,"Accepted opening fails.");
        Check(!Visible(chest.Opened,false,true,0)&&Visible(chest.Opened,false,false,0),"Lid opt-in does not distinguish opening from legacy reveal.");
        Check(!chest.TryOpen(GameState.Playing,true),"Repeated open bypasses the original once-only guard.");
        Check(!Visible(chest.Opened,false,true,.5f)&&Visible(chest.Opened,false,true,1),"Opening-to-revealed journey is incorrect.");
        chest.ResetFor(RetryScope.MainArea);
        Check(chest.Opened&&Visible(chest.Opened,false,true,1),"Main-area retry re-locks a revealed chest.");
        chest.ResetFor(RetryScope.BossArena);
        Check(chest.Opened&&Visible(chest.Opened,false,true,1),"Boss-area retry re-locks a revealed chest.");
        Check(!Visible(chest.Opened,true,true,1),"Collected state loses priority after area retries.");
        chest.ResetFor(RetryScope.NewGame);
        Check(!chest.Opened&&!Visible(chest.Opened,false,true,1),"New Game retains a reveal even before animation reset.");
        Check(!Visible(chest.Opened,false,true,0)&&!Visible(chest.Opened,true,true,0),"Pickup reset ordering bypasses the closed gate.");
        Check(chest.TryOpen(GameState.Playing,true)&&!Visible(chest.Opened,false,true,.2f),"A second run retains old lid readiness.");
        Check(Visible(chest.Opened,false,false,.2f),"Disabling the optional gate does not restore legacy availability.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.20 lid reveal policy with original chest/reset policy and CoreTypes (C# 9.0).");
    }
}
