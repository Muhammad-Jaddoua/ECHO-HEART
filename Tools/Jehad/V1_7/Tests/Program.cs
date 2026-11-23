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
        foreach(UiPageV17 page in Enum.GetValues(typeof(UiPageV17)))
        foreach(UiModalV17 modal in Enum.GetValues(typeof(UiModalV17)))
        {
            var route=new UiPolicyV17();route.SetState(state);
            Check(route.Open(page)==(state!=GameState.Playing),"A hidden-game menu opened during play.");
            bool accepted=route.Confirm(modal);Check(accepted==(state!=GameState.Playing&&modal!=UiModalV17.None),"Modal availability is inconsistent.");
            if(accepted)
            {
                Check(!route.Open(UiPageV17.Credits),"A modal allowed changing the page behind it.");
                Check(!route.Confirm(UiModalV17.Quit),"A repeated click replaced a pending confirmation.");
                var wrong=modal==UiModalV17.Quit?UiModalV17.Retry:UiModalV17.Quit;
                Check(!route.Consume(wrong)&&route.Modal==modal,"A stale callback consumed a different modal.");
                Check(route.Back()==(modal==UiModalV17.Display?UiBackV17.RevertDisplay:UiBackV17.Modal),"Back did not cancel the modal first.");
                Check(route.Page==page&&route.State==state,"Closing a modal changed its underlying page/game state.");
            }
            else if(state==GameState.Playing)Check(route.Back()==UiBackV17.Pause,"Gameplay back must request Pause.");
            else if(page!=UiPageV17.Home)Check(route.Back()==UiBackV17.Page&&route.Page==UiPageV17.Home,"Back skipped the home menu.");
            else Check(route.Back()==(state==GameState.Paused?UiBackV17.Resume:UiBackV17.None),"Home back has the wrong game action.");
            route.SetState(GameState.Dead);Check(route.Page==UiPageV17.Home&&route.Modal==UiModalV17.None,"External death retained stale UI state.");
        }
        var confirm=new UiPolicyV17();confirm.Confirm(UiModalV17.Retry);Check(confirm.Consume(UiModalV17.Retry)&&!confirm.Consume(UiModalV17.Retry),"A destructive command fired twice.");
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
        Navigation();DisplayEdits();
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.7 routing, spatial navigation and display transaction; reused V1.6 preferences/timer compiled with actual CoreTypes (C# 9.0).");
    }

    private static void Navigation()
    {
        var row=new[]{new UiNavPointV17(0,0),new UiNavPointV17(300,0)};
        Check(UiNavigationV17.Next(row,0,UiDirectionV17.Right)==1,"Cancel cannot reach Confirm with Right.");
        Check(UiNavigationV17.Next(row,1,UiDirectionV17.Left)==0,"Confirm cannot reach Cancel with Left.");
        Check(UiNavigationV17.Next(row,0,UiDirectionV17.Left)==1&&UiNavigationV17.Next(row,1,UiDirectionV17.Right)==0,"Modal row did not wrap.");
        var grid=new[]{new UiNavPointV17(0,100),new UiNavPointV17(300,100),new UiNavPointV17(0,0),new UiNavPointV17(300,0)};
        Check(UiNavigationV17.Next(grid,0,UiDirectionV17.Down)==2,"Down skipped its aligned column.");
        Check(UiNavigationV17.Next(grid,2,UiDirectionV17.Up)==0,"Up skipped its aligned column.");
        Check(UiNavigationV17.Next(grid,0,UiDirectionV17.Right)==1,"Right skipped its aligned row.");
        Check(UiNavigationV17.Next(grid,3,UiDirectionV17.Left)==2,"Left skipped its aligned row.");
        var controls=new[]{new UiNavPointV17(0,160),new UiNavPointV17(0,80,true),new UiNavPointV17(0,0)};
        Check(UiNavigationV17.Next(controls,1,UiDirectionV17.Right)==-1&&UiNavigationV17.Next(controls,1,UiDirectionV17.Left)==-1,"Slider arrows escaped instead of adjusting its value.");
        Check(UiNavigationV17.Next(controls,1,UiDirectionV17.Up)==0&&UiNavigationV17.Next(controls,1,UiDirectionV17.Down)==2,"Slider trapped vertical navigation.");
        Check(UiNavigationV17.Next(controls,0,UiDirectionV17.Up)==2&&UiNavigationV17.Next(controls,2,UiDirectionV17.Down)==0,"Vertical menu did not wrap.");
        Check(UiNavigationV17.Next(controls,0,UiDirectionV17.Right)==-1,"Single column wrapped horizontally to unrelated rows.");
        foreach(UiDirectionV17 d in Enum.GetValues(typeof(UiDirectionV17)))
        {
            Check(UiNavigationV17.Next(null,0,d)==-1&&UiNavigationV17.Next(Array.Empty<UiNavPointV17>(),0,d)==-1,"Empty focus scope has a target.");
            Check(UiNavigationV17.Next(new[]{row[0]},0,d)==-1,"Single remaining button navigated outside its scope.");
            Check(UiNavigationV17.Next(row,-1,d)==-1&&UiNavigationV17.Next(row,2,d)==-1,"Invalid selection index had a target.");
            for(int i=0;i<grid.Length;i++)for(int scale=1;scale<=4;scale++)
            {
                var moved=Array.ConvertAll(grid,p=>new UiNavPointV17(p.X*scale+37,p.Y*scale-64));
                Check(UiNavigationV17.Next(grid,i,d)==UiNavigationV17.Next(moved,i,d),"Navigation changed under canvas scale/translation.");
            }
        }
        var title=new[]{new UiNavPointV17(300,-500),new UiNavPointV17(300,-580),new UiNavPointV17(172,-660),new UiNavPointV17(428,-660),new UiNavPointV17(1210,-700)};
        Check(UiNavigationV17.Next(title,2,UiDirectionV17.Right)==3&&UiNavigationV17.Next(title,3,UiDirectionV17.Left)==2,"Title Controls/Credits row is not reachable horizontally.");
        // Reaching a neighbor must not depend on child/sibling enumeration order.
        var reordered=new[]{grid[3],grid[2],grid[1],grid[0]};
        Check(UiNavigationV17.Next(reordered,3,UiDirectionV17.Down)==1,"Down depends on hierarchy order rather than position.");
    }
    private static void DisplayEdits()
    {
        for(int mode=0;mode<4;mode++)foreach(int sync in new[]{0,1,2,4})
        {
            var actual=new UiDisplayValueV17{Width=1920,Height=1080,Mode=mode,Quality=2,Sync=sync,Limit=75};
            var edit=new UiDisplayEditV17();edit.Sync(actual);
            Check(!edit.Dirty&&!edit.Begin(actual)&&!edit.Previewing,"Unchanged settings started a native display operation.");
            Check(!edit.Keep(actual)&&!edit.Revert()&&!edit.Tick(100),"Inactive preview resolved.");
            var draft=actual;draft.Limit=120;
            Check(edit.Stage(draft)&&edit.Dirty&&edit.Applied.Equals(actual),"Staging mutated accepted native settings.");
            Check(edit.Draft.Mode==mode&&edit.Draft.Sync==sync,"An unrelated frame-cap edit collapsed the exact mode/VSync count.");
            edit.Discard();Check(!edit.Dirty&&edit.Draft.Equals(actual),"Discard did not restore the accepted draft.");
            edit.Stage(draft);
            var external=actual;external.Width=1600;external.Height=900;
            Check(edit.Begin(external)&&edit.Before.Equals(external)&&edit.Previewing&&edit.Remaining==15,"Revert snapshot was not captured at preview start.");
            Check(!edit.Begin(actual)&&edit.Before.Equals(external),"Double Apply replaced the rollback snapshot.");
            Check(!edit.Stage(actual)&&edit.Draft.Equals(draft),"Draft could change behind the preview dialog.");
            edit.Sync(actual);edit.Discard();Check(edit.Before.Equals(external)&&edit.Draft.Equals(draft),"Refresh/discard destroyed an active preview.");
            Check(!edit.Tick(float.NaN)&&!edit.Tick(float.PositiveInfinity)&&!edit.Tick(-1)&&edit.Remaining==15,"Invalid delta corrupted the display countdown.");
            Check(!edit.Tick(14.9f)&&edit.Previewing,"Display preview expired early.");
            Check(edit.Tick(.2f)&&!edit.Previewing&&!edit.Dirty&&edit.Applied.Equals(external)&&edit.Draft.Equals(external),"Timeout failed to restore the actual pre-preview native state.");
            Check(!edit.Tick(100)&&!edit.Revert()&&!edit.Keep(draft),"Timeout resolved twice.");
            edit.Stage(draft);Check(edit.Begin(external)&&edit.Revert()&&!edit.Revert()&&edit.Draft.Equals(external),"Cancel/focus-loss rollback was not one-shot.");
            edit.Stage(draft);Check(edit.Begin(external),"Could not preview after reverting.");
            var accepted=draft;accepted.Width=1280;accepted.Height=720;
            Check(edit.Keep(accepted)&&!edit.Previewing&&!edit.Dirty&&edit.Applied.Equals(accepted),"Keep failed to read the actual platform-selected size.");
            Check(!edit.Tick(100)&&!edit.Revert()&&!edit.Keep(actual)&&edit.Draft.Equals(accepted),"Accepted values reverted or executed twice.");
            foreach(int field in new[]{0,1,2,3,4,5})
            {
                var changed=actual;
                switch(field){case 0:changed.Width++;break;case 1:changed.Height++;break;case 2:changed.Mode++;break;case 3:changed.Quality++;break;case 4:changed.Sync++;break;case 5:changed.Limit++;break;}
                edit.Sync(actual);edit.Stage(changed);Check(edit.Dirty&&!changed.Equals(actual),"Dirty comparison ignored a native display field.");
                edit.Stage(actual);Check(!edit.Dirty,"Returning all options to applied values stayed dirty.");
            }
        }
    }
}
