using System;
using Echoheart.Core;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string message){_checks++;if(!value)throw new Exception(message);}
    private static void Main()
    {
        foreach(GameState state in Enum.GetValues(typeof(GameState)))
        foreach(UiPageV16 page in Enum.GetValues(typeof(UiPageV16)))
        foreach(UiModalV16 modal in Enum.GetValues(typeof(UiModalV16)))
        {
            var route=new UiPolicyV16();route.SetState(state);
            Check(route.Open(page)==(state!=GameState.Playing),"A hidden-game menu opened during play.");
            bool accepted=route.Confirm(modal);Check(accepted==(state!=GameState.Playing&&modal!=UiModalV16.None),"Modal availability is inconsistent.");
            if(accepted)
            {
                Check(!route.Open(UiPageV16.Credits),"A modal allowed changing the page behind it.");
                Check(!route.Confirm(UiModalV16.Quit),"A repeated click replaced a pending confirmation.");
                var wrong=modal==UiModalV16.Quit?UiModalV16.Retry:UiModalV16.Quit;
                Check(!route.Consume(wrong)&&route.Modal==modal,"A stale callback consumed a different modal.");
                Check(route.Back()==(modal==UiModalV16.Display?UiBackV16.RevertDisplay:UiBackV16.Modal),"Back did not cancel the modal first.");
                Check(route.Page==page&&route.State==state,"Closing a modal changed its underlying page/game state.");
            }
            else if(state==GameState.Playing)Check(route.Back()==UiBackV16.Pause,"Gameplay back must request Pause.");
            else if(page!=UiPageV16.Home)Check(route.Back()==UiBackV16.Page&&route.Page==UiPageV16.Home,"Back skipped the home menu.");
            else Check(route.Back()==(state==GameState.Paused?UiBackV16.Resume:UiBackV16.None),"Home back has the wrong game action.");
            route.SetState(GameState.Dead);Check(route.Page==UiPageV16.Home&&route.Modal==UiModalV16.None,"External death retained stale UI state.");
        }
        var confirm=new UiPolicyV16();confirm.Confirm(UiModalV16.Retry);Check(confirm.Consume(UiModalV16.Retry)&&!confirm.Consume(UiModalV16.Retry),"A destructive command fired twice.");
        foreach(float input in new[]{-100f,-.01f,0f,.65f,1f,50f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        {
            var values=new UiValuesV16{Music=input,Sfx=input,HudScale=input,HighContrast=true,ReduceMotion=true,Hints=true,Crosshair=true}.Sanitized();
            Check(values.Music>=0&&values.Music<=1&&!float.IsNaN(values.Music),"Invalid music volume escaped sanitizing.");
            Check(values.Sfx>=0&&values.Sfx<=1&&!float.IsNaN(values.Sfx),"Invalid SFX volume escaped sanitizing.");
            Check(values.HudScale>=.85f&&values.HudScale<=1.15f,"HUD scale could exceed the validated layout range.");
            Check(values.HighContrast&&values.ReduceMotion&&values.Hints&&values.Crosshair,"Sanitizing lost independent interface options.");
        }
        var defaults=UiValuesV16.Defaults;Check(defaults.Music==.65f&&defaults.Sfx==.8f&&defaults.HudScale==1&&defaults.Hints&&defaults.Crosshair,"Defaults changed unexpectedly.");
        var timer=new UiDisplayTimerV16();Check(timer.Begin()&&!timer.Begin(),"Two display previews could run together.");
        for(int i=0;i<29;i++)Check(!timer.Tick(.5f)&&timer.Active,"Display preview expired early.");
        Check(timer.Tick(.5f)&&!timer.Active&&timer.Remaining==0,"Paused-game display countdown failed to expire.");
        Check(!timer.Tick(10)&&!timer.Accept(),"A display timeout resolved twice.");
        Check(timer.Begin()&&timer.Accept()&&!timer.Tick(100),"Kept display settings reverted later.");
        timer.Begin();Check(!timer.Tick(float.NaN)&&!timer.Tick(float.PositiveInfinity)&&!timer.Tick(-1)&&timer.Remaining==15,"Invalid elapsed time poisoned the display countdown.");
        Check(timer.Tick(100)&&!timer.Active,"Long-frame timeout did not revert.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.6 routing, preferences and display timer compiled with actual CoreTypes (C# 9.0).");
    }
}
