using System;
using System.Globalization;

namespace Echoheart.UI
{
    /// <summary>Versioned audio/interface preferences only; display and run state are excluded.</summary>
    public static class PreferenceCodecV114
    {
        private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
        public static string Encode(UiValuesV16 input)
        {
            UiValuesV16 value=input.Sanitized();
            int flags=(value.HighContrast?1:0)|(value.ReduceMotion?2:0)|(value.Hints?4:0)|(value.Crosshair?8:0);
            return "1|"+value.Music.ToString("R",Invariant)+"|"+value.Sfx.ToString("R",Invariant)+"|"+value.HudScale.ToString("R",Invariant)+"|"+flags.ToString(Invariant);
        }
        private static bool Number(string text,out float value)=>float.TryParse(text,NumberStyles.Float,Invariant,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value);
        public static bool TryDecode(string text,out UiValuesV16 value)
        {
            value=UiValuesV16.Defaults;
            if(string.IsNullOrEmpty(text)||text.Length>128)return false;
            string[] parts=text.Split('|');
            if(parts.Length!=5||parts[0]!="1"||!Number(parts[1],out float music)||!Number(parts[2],out float sfx)||!Number(parts[3],out float scale)||!int.TryParse(parts[4],NumberStyles.None,Invariant,out int flags)||flags>15)return false;
            value=new UiValuesV16{Music=music,Sfx=sfx,HudScale=scale,HighContrast=(flags&1)!=0,ReduceMotion=(flags&2)!=0,Hints=(flags&4)!=0,Crosshair=(flags&8)!=0}.Sanitized();
            return true;
        }
    }

    /// <summary>Coalesces slider edits; returning to the last stored values cancels a write.</summary>
    public sealed class PreferenceSaveV114
    {
        public const float Delay=.5f;
        private string _saved;
        private float _deadline;
        public string Pending {get;private set;}
        public PreferenceSaveV114(UiValuesV16 initial){_saved=PreferenceCodecV114.Encode(initial);}
        public void Observe(UiValuesV16 value,float now)
        {
            string payload=PreferenceCodecV114.Encode(value);
            if(payload==_saved){Pending=null;return;}
            if(payload==Pending)return;
            Pending=payload;_deadline=(float.IsNaN(now)||float.IsInfinity(now)?0:now)+Delay;
        }
        public bool Due(float now)=>Pending!=null&&!float.IsNaN(now)&&!float.IsInfinity(now)&&now>=_deadline;
        public void Acknowledge(string payload)
        {
            if(payload==null)return;
            _saved=payload;if(Pending==payload)Pending=null;
        }
    }
}
