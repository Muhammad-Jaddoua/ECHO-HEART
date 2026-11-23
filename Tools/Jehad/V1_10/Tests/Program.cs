using System;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.005f;
    private static ToastNoticeV110 Notice(string key,ToastKindV110 kind=ToastKindV110.Relic)=>new ToastNoticeV110(key,kind,"Heading","Message "+key);
    private static void Main()
    {
        var queue=new ToastQueueV110();Check(!queue.HasActive&&queue.Count==0&&queue.Remaining==0,"Empty queue has visible content.");
        Check(!queue.Enqueue(Notice(""))&&!queue.Enqueue(new ToastNoticeV110("bad",ToastKindV110.Secret,"Heading","  ")),"Blank notices were accepted.");
        Check(queue.Enqueue(Notice("chest",ToastKindV110.Chest))&&queue.Enqueue(Notice("relic"))&&queue.Enqueue(Notice("secret",ToastKindV110.Secret)),"Discovery burst was rejected.");
        Check(queue.Count==3&&queue.PendingCount==2&&queue.Active.Key=="chest","Discovery burst overwrote its first message.");
        Check(!queue.Enqueue(Notice("chest"))&&!queue.Enqueue(Notice("relic"))&&queue.Count==3,"Duplicate active/pending keys filled the queue.");
        queue.Tick(1,true);float saved=queue.Remaining;
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,0})queue.Tick(invalid,true);
        queue.Tick(100,false);Check(Near(queue.Remaining,saved)&&queue.Active.Key=="chest","Paused/invalid time consumed reading time.");
        Check(queue.Enqueue(Notice("boss",ToastKindV110.Objective))&&queue.Active.Key=="boss"&&queue.Count==4,"Boss objective did not interrupt the discovery queue.");
        Check(Near(queue.Remaining,4.4f),"Objective reading duration is wrong.");
        queue.Tick(4.4f,true);Check(queue.Active.Key=="chest"&&Near(queue.Remaining,saved),"Interrupted reading time/order was lost.");
        queue.Tick(100,true);Check(queue.Active.Key=="relic"&&Near(queue.Remaining,3.2f),"Long frame consumed a newly activated notice before it could be read.");
        queue.Tick(3.2f,true);Check(queue.Active.Key=="secret","Equal-priority FIFO order changed.");
        queue.Tick(3.2f,true);Check(!queue.HasActive&&queue.Count==0,"Last message did not expire.");
        Check(queue.Enqueue(Notice("chest")),"A completed key was permanently blacklisted.");queue.Clear();
        Check(!queue.HasActive&&queue.PendingCount==0&&queue.Remaining==0,"Retry/title clear retained active or pending content.");
        // One authored run: 16 relics, 2 chests, 2 secret locations and 2 boss notices.
        for(int i=0;i<16;i++)Check(queue.Enqueue(Notice("relic:"+i)),"Authored relic batch exceeded the queue.");
        for(int i=0;i<2;i++)Check(queue.Enqueue(Notice("chest:"+i,ToastKindV110.Chest))&&queue.Enqueue(Notice("secret:"+i,ToastKindV110.Secret)),"Authored chest/secret batch exceeded the queue.");
        Check(queue.Enqueue(Notice("boss:start",ToastKindV110.Objective))&&queue.Enqueue(Notice("boss:end",ToastKindV110.Objective))&&queue.Count==22,"Authored run lost notifications.");
        Check(queue.Active.Key=="boss:start","Second objective interrupted an earlier objective.");
        queue.Tick(100,true);Check(queue.Active.Key=="boss:end","Queued objectives were not served before discoveries.");
        queue.Tick(100,true);
        for(int i=0;i<16;i++){Check(queue.Active.Key=="relic:"+i,"Relic batch was reordered or lost after objective interruption.");queue.Tick(100,true);}
        for(int i=0;i<2;i++)
        {
            Check(queue.Active.Key=="chest:"+i,"Chest order changed.");queue.Tick(100,true);
            Check(queue.Active.Key=="secret:"+i,"Secret order changed.");queue.Tick(100,true);
        }
        Check(queue.Count==0,"Authored run left stale notifications.");
        var bounded=new ToastQueueV110(3);bounded.Enqueue(Notice("visible"));bounded.Enqueue(Notice("queued1"));bounded.Enqueue(Notice("queued2"));
        Check(!bounded.Enqueue(Notice("overflow"))&&bounded.Count==3&&bounded.Active.Key=="visible","Overflow grew the queue or replaced a visible peer.");
        Check(!bounded.Enqueue(Notice("queued1"))&&bounded.Count==3,"Duplicate at capacity evicted another message.");
        Check(bounded.Enqueue(Notice("urgent",ToastKindV110.Objective))&&bounded.Count==3&&bounded.Active.Key=="urgent","Objective could not make bounded room.");
        bounded.Tick(100,true);Check(bounded.Active.Key=="visible","Urgent overflow discarded the visible interrupted message instead of a queued peer.");
        var single=new ToastQueueV110(1);single.Enqueue(Notice("old"));Check(single.Enqueue(Notice("urgent",ToastKindV110.Objective))&&single.Count==1&&single.Active.Key=="urgent","Capacity-one queue rejected an urgent objective.");
        Check(new ToastQueueV110(0).Capacity==1&&new ToastQueueV110(999).Capacity==32,"Queue capacity is not bounded.");
        var superseded=new ToastQueueV110();superseded.Enqueue(Notice("relic"));superseded.Tick(1,true);
        superseded.Enqueue(Notice("boss:start",ToastKindV110.Objective));superseded.Enqueue(Notice("secret",ToastKindV110.Secret));
        Check(superseded.Remove("boss:start")&&superseded.Active.Key=="relic"&&Near(superseded.Remaining,2.2f),"Dismissing an outdated objective lost the interrupted discovery.");
        superseded.Enqueue(Notice("boss:defeated",ToastKindV110.Objective));
        Check(superseded.Active.Key=="boss:defeated"&&superseded.Count==3,"Defeat did not replace the outdated boss-start prompt.");
        Check(superseded.Remove("secret")&&superseded.Active.Key=="boss:defeated"&&superseded.Count==2,"Dismissing a queued item changed the visible objective.");
        Check(!superseded.Remove("missing")&&superseded.Count==2,"Dismissing a missing key changed the queue.");
        // The new 820x104 card uses the unchanged V1.9 safe-area surface and prompt gap.
        foreach(var available in new[]{(1600f,900f),(2133f,900f),(1000f,600f),(600f,700f),(200f,140f)})
        foreach(float scale in new[]{.85f,1f,1.15f})
        {
            UiSurfaceV19 surface=UiLayoutV19.Surface(available.Item1,available.Item2);
            float left=surface.Width/2-410*scale,bottom=UiLayoutV19.ToastBottom(scale);
            Check(left>=0&&left+820*scale<=surface.Width&&bottom+104*scale<=surface.Height,"Larger notification escaped the logical surface.");
            Check(Near(bottom-96-66*scale,22),"Notification card overlaps the interaction prompt.");
            Check(surface.Width*surface.Scale<=available.Item1+.01f&&surface.Height*surface.Scale<=available.Item2+.01f,"Notification surface escaped the actual available area.");
        }
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.10 notification queue and shared layout model (C# 9.0).");
    }
}
