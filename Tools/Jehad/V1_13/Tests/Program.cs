using System;
using System.Collections.Generic;
using Echoheart.World;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static RelicGlintCandidateV113 C(int id,float distance,bool eligible=true)=>new RelicGlintCandidateV113(id,distance,eligible);
    private static bool Has(RelicGlintSelectionV113 s,int id){for(int i=0;i<6;i++)if(s.Slot(i)==id)return true;return false;}
    private static int Count(RelicGlintSelectionV113 s){int count=0;for(int i=0;i<6;i++)if(s.Slot(i)>=0)count++;return count;}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    private static void Main()
    {
        Check(RelicGlintSelectionV113.Available(true,true,true,true,false,0),"A ready visible relic is unavailable.");
        Check(RelicGlintSelectionV113.Available(true,true,true,true,false,12),"Exit boundary is unavailable.");
        Check(!RelicGlintSelectionV113.Available(false,true,true,true,false,2),"Non-playing flow admits glints.");
        Check(!RelicGlintSelectionV113.Available(true,false,true,true,false,2),"Inactive/collected pickup admits glints.");
        Check(!RelicGlintSelectionV113.Available(true,true,false,true,false,2),"Closed chest collider gate admits glints.");
        Check(!RelicGlintSelectionV113.Available(true,true,true,false,false,2),"Offscreen pickup admits glints.");
        Check(!RelicGlintSelectionV113.Available(true,true,true,true,true,2),"Occluded pickup admits glints.");
        foreach(float distance in new[]{-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity,12.01f})
            Check(!RelicGlintSelectionV113.Available(true,true,true,true,false,distance),"Invalid/out-of-range distance admitted.");
        var s=new RelicGlintSelectionV113();
        Check(Count(s)==0&&s.Slot(-1)==-1&&s.Slot(6)==-1,"Empty/bad-index state is invalid.");
        s.Update(null);Check(Count(s)==0,"Null candidates filled slots.");
        s.Update(new[]{C(-1,1),C(1,-1),C(2,float.NaN),C(3,float.PositiveInfinity),C(4,1,false)});
        Check(Count(s)==0,"Invalid candidates were selected.");
        s.Update(new[]{C(9,9),C(7,7),C(5,5),C(3,3),C(1,1),C(8,8),C(6,6),C(4,4),C(2,2),C(0,0)});
        Check(Count(s)==6,"Pool does not fill its bounded capacity.");
        for(int i=0;i<6;i++)Check(s.Slot(i)==i,"Nearest candidates were not selected in deterministic order.");
        // Reordering and changed distances retain each winning emitter's identity.
        s.Update(new[]{C(5,0),C(4,1),C(3,2),C(2,3),C(1,4),C(0,5)});
        for(int i=0;i<6;i++)Check(s.Slot(i)==i,"A retained relic moved to another effect slot.");
        s.Update(new[]{C(0,5),C(1,4),C(2,3,false),C(3,2),C(4,1),C(5,0),C(6,.5f)});
        Check(!Has(s,2)&&s.Slot(2)==6&&Count(s)==6,"Unavailable relic was not replaced in the freed slot.");
        Check(s.Slot(0)==0&&s.Slot(1)==1&&s.Slot(3)==3&&s.Slot(4)==4&&s.Slot(5)==5,"Replacement restarted unaffected glints.");
        s.Clear();s.Update(new[]{C(8,2),C(4,2),C(1,2),C(7,2),C(3,2),C(6,2),C(2,2)});
        int[] tieOrder={1,2,3,4,6,7};
        for(int i=0;i<6;i++)Check(s.Slot(i)==tieOrder[i],"Tied distances lack deterministic ID ordering.");
        s.Clear();s.Update(new[]{C(4,9),C(4,1),C(2,2),C(2,3),C(5,11),C(6,1,false)});
        Check(Count(s)==2&&s.Slot(0)==4&&s.Slot(1)==2,"Duplicate IDs were not reduced to the nearest eligible entry.");
        s.Update(Array.Empty<RelicGlintCandidateV113>());Check(Count(s)==0,"Empty input retained stale glints.");
        s.Update(new[]{C(1,10),C(2,10.01f)});Check(Has(s,1)&&!Has(s,2),"Enter range boundary is incorrect.");
        s.Update(new[]{C(1,11.9f),C(2,11)});Check(Has(s,1)&&!Has(s,2),"Hysteresis flickers or admits a new distant relic.");
        s.Update(new[]{C(1,12)});Check(Has(s,1),"Existing glint exits before its boundary.");
        s.Update(new[]{C(1,12.01f)});Check(Count(s)==0,"Out-of-range glint persists.");
        s.Update(new[]{C(1,11)});Check(Count(s)==0,"A released glint reenters beyond enter range.");
        s.Update(new[]{C(1,9.99f)});Check(Has(s,1),"Returning within enter range fails.");
        s.Update(new[]{C(1,3,false)});Check(Count(s)==0,"Collected/occluded eligibility failed to remove glint.");
        s.Update(new[]{C(1,3)});s.Clear();Check(Count(s)==0,"Pause/retry clear retains assignments.");
        s.Update(new[]{C(1,11)});Check(Count(s)==0,"Clear retained hidden hysteresis state.");
        s.Update(new[]{C(1,2)});s.Update(null);Check(Count(s)==0,"Null refresh retained previous assignments.");
        foreach(var pair in new[]{(0f,1f),(8f,1f),(9f,.75f),(10f,.5f),(11f,.25f),(12f,0f),(13f,0f),(-1f,0f),(float.NaN,0f),(float.PositiveInfinity,0f)})
            Check(Near(RelicGlintSelectionV113.Alpha(pair.Item1),pair.Item2),"Distance fade is wrong or nonfinite.");
        // Many candidates across changing frames must keep a unique bounded pool.
        bool bounded=true,unique=true,nearest=true;
        var many=new RelicGlintCandidateV113[100];
        for(int frame=0;frame<40;frame++)
        {
            s.Clear();for(int i=0;i<many.Length;i++)many[i]=C(i,((i+frame)%100)/10f);
            s.Update(many);var seen=new HashSet<int>();bounded&=Count(s)==6;
            for(int slot=0;slot<6;slot++){int id=s.Slot(slot);unique&=seen.Add(id);nearest&=id>=0&&((id+frame)%100)<=5;}
        }
        Check(bounded,"Large input exceeds or loses the six-effect budget.");
        Check(unique,"Multiple emitters target the same relic.");
        Check(nearest,"Large input failed nearest-six selection.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.13 glint eligibility, bounded selection, stable slots, hysteresis and fade (C# 9.0).");
    }
}
