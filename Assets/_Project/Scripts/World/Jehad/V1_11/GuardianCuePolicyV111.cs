using System;

namespace Echoheart.World
{
    public enum GuardianPhaseV111 { Idle, Telegraph, Attack, Recover, Defeated }
    public enum GuardianCueV111 { None, SlamWarning, RangedWarning, Slam, RangedShot }
    public struct GuardianPointV111
    {
        public float X,Z;
        public GuardianPointV111(float x,float z){X=x;Z=z;}
    }
    /// <summary>Presentation edges and geometry only; no combat timers or damage decisions.</summary>
    public sealed class GuardianCuePolicyV111
    {
        private GuardianPhaseV111 _last;
        private bool _slam;
        public GuardianCueV111 Step(GuardianPhaseV111 phase,bool slam,bool playing)
        {
            if(!playing)return GuardianCueV111.None;
            GuardianCueV111 cue=GuardianCueV111.None;
            if(phase==GuardianPhaseV111.Telegraph&&(_last!=phase||_slam!=slam))cue=slam?GuardianCueV111.SlamWarning:GuardianCueV111.RangedWarning;
            else if(phase==GuardianPhaseV111.Attack&&_last!=phase)cue=slam?GuardianCueV111.Slam:GuardianCueV111.RangedShot;
            _last=phase;_slam=slam;return cue;
        }
        public void Reset(){_last=GuardianPhaseV111.Idle;_slam=false;}
        public static float Progress(float value)=>float.IsNaN(value)?0:Math.Max(0,Math.Min(1,value));
        private static float Distance(float value)=>value>0&&!float.IsInfinity(value)?value:0;
        public static int ArcVertices(float progress)=>Math.Max(2,(int)Math.Ceiling(64*Progress(progress))+1);
        public static GuardianPointV111 Ring(float radius,float turns)
        {
            double angle=-Math.PI*.5+Math.PI*2*Progress(turns);radius=Distance(radius);
            return new GuardianPointV111(radius*(float)Math.Cos(angle),radius*(float)Math.Sin(angle));
        }
        public static float AimDistance(float range,float coverDistance)=>Math.Min(Distance(range),float.IsNaN(coverDistance)?Distance(range):Math.Max(0,coverDistance));
        public static float ImpactRadius(float radius,float elapsed)=>Distance(radius)*(.45f+.55f*Progress(elapsed/.35f));
        public static float ImpactAlpha(float elapsed)=>1-Progress(elapsed/.35f);
    }
}
