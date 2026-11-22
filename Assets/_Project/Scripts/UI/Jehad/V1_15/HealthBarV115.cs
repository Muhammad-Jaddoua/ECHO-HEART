using System;

namespace Echoheart.UI
{
    /// <summary>Immediate current health with a bounded, presentation-only recent-loss trail.</summary>
    public sealed class HealthBarV115
    {
        public const float HoldSeconds=.18f,TrailRate=1.4f;
        private bool _initialized;
        private float _hold;
        public int Current {get;private set;}
        public int Maximum {get;private set;}
        public float Fraction {get;private set;}=1;
        public float Trail {get;private set;}=1;
        public bool Low=>Current>0&&Maximum>0&&(long)Current*4<=Maximum;
        public void Observe(int current,int maximum)
        {
            maximum=Math.Max(0,maximum);current=Math.Max(0,Math.Min(maximum,current));
            float next=maximum>0?(float)current/maximum:0;
            if(!_initialized||maximum!=Maximum||current>Current)
            {
                Fraction=Trail=next;_hold=0;
            }
            else if(current<Current)
            {
                Trail=Math.Max(Trail,Fraction);Fraction=next;_hold=HoldSeconds;
            }
            Current=current;Maximum=maximum;_initialized=true;
        }
        public void Snap(){Trail=Fraction;_hold=0;}
        public void Tick(float seconds,bool reduceMotion)
        {
            if(reduceMotion){Snap();return;}
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            float held=Math.Min(_hold,seconds);_hold-=held;seconds-=held;
            Trail=Math.Max(Fraction,Trail-seconds*TrailRate);
        }
    }
}
