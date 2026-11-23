using System;

namespace Echoheart.UI
{
    public readonly struct TitleVistaOffsetV116
    {
        public readonly float Yaw,Lift;
        public TitleVistaOffsetV116(float yaw,float lift){Yaw=yaw;Lift=lift;}
    }

    /// <summary>Bounded menu-only orbit offsets. Holds its phase while a menu or focus interrupts it.</summary>
    public sealed class TitleVistaMotionV116
    {
        public const float Period=90f,YawLimit=4f,LiftLimit=2.4f,MaxStep=.1f;
        private double _elapsed;
        public double Elapsed=>_elapsed;
        public TitleVistaOffsetV116 Offset
        {
            get
            {
                double phase=_elapsed/Period*Math.PI*2;
                return new TitleVistaOffsetV116((float)(YawLimit*Math.Sin(phase)),(float)(LiftLimit*.5*(1-Math.Cos(phase))));
            }
        }
        public TitleVistaOffsetV116 Step(float seconds,bool home,bool reduceMotion,bool focused)
        {
            if(reduceMotion){Reset();return Offset;}
            if(home&&focused&&seconds>0&&!float.IsNaN(seconds)&&!float.IsInfinity(seconds))_elapsed=(_elapsed+Math.Min(seconds,MaxStep))%Period;
            return Offset;
        }
        public void Reset()=>_elapsed=0;
    }
}
