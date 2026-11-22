using System;

namespace Echoheart.UI
{
    public struct UiDisplayValueV17 : IEquatable<UiDisplayValueV17>
    {
        public int Width,Height,Mode,Quality,Sync,Limit;
        public bool Equals(UiDisplayValueV17 other)=>Width==other.Width&&Height==other.Height&&Mode==other.Mode&&Quality==other.Quality&&Sync==other.Sync&&Limit==other.Limit;
        public override bool Equals(object value)=>value is UiDisplayValueV17 other&&Equals(other);
        public override int GetHashCode()=>Width^Height^Mode^Quality^Sync^Limit;
    }
    /// <summary>Applied vs staged display values and a one-shot keep/revert transaction.</summary>
    public sealed class UiDisplayEditV17
    {
        private readonly UiDisplayTimerV16 _timer=new UiDisplayTimerV16();
        public UiDisplayValueV17 Applied {get;private set;}
        public UiDisplayValueV17 Draft {get;private set;}
        public UiDisplayValueV17 Before {get;private set;}
        public bool Previewing=>_timer.Active;
        public float Remaining=>_timer.Remaining;
        public bool Dirty=>!Draft.Equals(Applied);
        public void Sync(UiDisplayValueV17 actual){if(!Previewing)Applied=Draft=actual;}
        public bool Stage(UiDisplayValueV17 value){if(Previewing)return false;Draft=value;return true;}
        public void Discard(){if(!Previewing)Draft=Applied;}
        public bool Begin(UiDisplayValueV17 actual){if(!Dirty||!_timer.Begin())return false;Before=actual;return true;}
        public bool Keep(UiDisplayValueV17 actual){if(!_timer.Accept())return false;Applied=Draft=actual;return true;}
        public bool Revert(){if(!_timer.Accept())return false;Applied=Draft=Before;return true;}
        public bool Tick(float delta){if(!_timer.Tick(delta))return false;Applied=Draft=Before;return true;}
    }
}
