using System;
using System.Globalization;
using Echoheart.UI;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value,string reason){_checks++;if(!value)throw new Exception(reason);}
    private static bool Same(UiValuesV16 a,UiValuesV16 b)=>a.Music==b.Music&&a.Sfx==b.Sfx&&a.HudScale==b.HudScale&&a.HighContrast==b.HighContrast&&a.ReduceMotion==b.ReduceMotion&&a.Hints==b.Hints&&a.Crosshair==b.Crosshair;
    private static UiValuesV16 Values(float music){var v=UiValuesV16.Defaults;v.Music=music;return v;}
    private static void Main()
    {
        UiValuesV16 defaults=UiValuesV16.Defaults;
        string initial=PreferenceCodecV114.Encode(defaults);
        Check(initial=="1|0.65|0.8|1|12","Default record schema changed.");
        Check(PreferenceCodecV114.TryDecode(initial,out var decoded)&&Same(decoded,defaults),"Defaults fail round trip.");
        for(int flags=0;flags<16;flags++)
        {
            var v=new UiValuesV16{Music=.12345678f,Sfx=.9876543f,HudScale=1.1234567f,HighContrast=(flags&1)!=0,ReduceMotion=(flags&2)!=0,Hints=(flags&4)!=0,Crosshair=(flags&8)!=0};
            string record=PreferenceCodecV114.Encode(v);
            Check(PreferenceCodecV114.TryDecode(record,out decoded)&&Same(v,decoded),"Flags/float precision fail round trip.");
            Check(PreferenceCodecV114.Encode(decoded)==record,"Canonical record changes after decoding.");
        }
        var originalCulture=CultureInfo.CurrentCulture;
        try
        {
            foreach(string name in new[]{"en-US","fr-FR","de-DE","ar-SA"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(name);
                Check(PreferenceCodecV114.Encode(defaults)==initial,"Record depends on current locale.");
                Check(PreferenceCodecV114.TryDecode("1|0.25|0.75|1.1|3",out decoded)&&decoded.Music==.25f&&decoded.Sfx==.75f&&decoded.HudScale==1.1f&&decoded.HighContrast&&decoded.ReduceMotion&&!decoded.Hints&&!decoded.Crosshair,"Invariant record cannot load in another locale.");
            }
        }
        finally{CultureInfo.CurrentCulture=originalCulture;}
        foreach(string bad in new[]{null,"","1","2|0.2|0.3|1|12","01|0.2|0.3|1|12","1|0.2|0.3|1","1|0.2|0.3|1|12|extra","1||0.3|1|12","1|0,2|0.3|1|12","1|NaN|0.3|1|12","1|0.2|Infinity|1|12","1|0.2|0.3|-Infinity|12","1|1e999|0.3|1|12","1|0.2|0.3|1|-1","1|0.2|0.3|1|16","1|0.2|0.3|1|+3","1|0.2|0.3|1|1.0","1|0.2|0.3|1|2147483648",new string('x',129)})
            Check(!PreferenceCodecV114.TryDecode(bad,out decoded)&&Same(decoded,defaults),"Malformed/unsupported record did not fail safely to defaults.");
        Check(PreferenceCodecV114.TryDecode("1|-2|5|9|0",out decoded)&&decoded.Music==0&&decoded.Sfx==1&&decoded.HudScale==1.15f,"Finite edited values were not clamped by the actual settings model.");
        Check(PreferenceCodecV114.TryDecode("1|0|0|0|15",out decoded)&&decoded.HudScale==.5f&&decoded.Music==0&&decoded.Sfx==0,"Mute/minimum HUD values are not preserved.");
        var nonfinite=defaults;nonfinite.Music=float.NaN;nonfinite.Sfx=float.PositiveInfinity;nonfinite.HudScale=float.NegativeInfinity;
        Check(PreferenceCodecV114.Encode(nonfinite)==initial,"Encoding bypassed actual settings sanitization.");
        var save=new PreferenceSaveV114(defaults);
        Check(save.Pending==null&&!save.Due(100),"Initialization schedules a needless write.");
        save.Observe(defaults,1);Check(save.Pending==null,"Unchanged/display-only notifications schedule a write.");
        save.Observe(Values(.2f),2);string first=save.Pending;
        Check(first==PreferenceCodecV114.Encode(Values(.2f))&&!save.Due(2.49f)&&save.Due(2.5f),"Debounce boundary is wrong.");
        save.Observe(Values(.2f),2.4f);Check(save.Due(2.5f),"Duplicate change restarted the pending timer.");
        save.Observe(Values(.3f),2.45f);Check(!save.Due(2.5f)&&save.Due(2.95f)&&save.Pending==PreferenceCodecV114.Encode(Values(.3f)),"Slider edits are not coalesced to the latest value.");
        save.Observe(defaults,2.6f);Check(save.Pending==null&&!save.Due(9),"Returning to stored values did not cancel disk work.");
        save.Observe(Values(.4f),10);first=save.Pending;
        Check(save.Due(11)&&save.Pending==first,"Pending payload disappeared before successful acknowledgement.");
        save.Acknowledge(first);Check(save.Pending==null&&!save.Due(12),"Successful write did not clear dirty state.");
        save.Observe(Values(.4f),13);Check(save.Pending==null,"A stored value causes repeated writes.");
        save.Observe(Values(.5f),14);first=save.Pending;save.Observe(Values(.6f),14.1f);save.Acknowledge(first);
        Check(save.Pending==PreferenceCodecV114.Encode(Values(.6f)),"Stale acknowledgement lost a newer edit.");
        save.Acknowledge(null);Check(save.Pending!=null,"Invalid acknowledgement lost dirty state.");
        save.Observe(Values(.5f),15);Check(save.Pending==null,"Returning to the acknowledged value failed to cancel newer work.");
        save.Observe(Values(.7f),float.NaN);
        Check(!save.Due(float.NaN)&&!save.Due(float.PositiveInfinity)&&save.Due(.5f),"Invalid time poisoned debounce scheduling.");
        save.Acknowledge(save.Pending);save.Observe(defaults,20);
        Check(save.Pending==initial,"Restore Defaults did not stage the normal default record.");
        save.Acknowledge(save.Pending);Check(save.Pending==null,"Default preferences did not become the stored baseline.");
        Console.WriteLine($"PASS: {_checks} assertions; actual V1.14 preference codec and save scheduling with actual UiValuesV16/CoreTypes (C# 9.0).");
    }
}
