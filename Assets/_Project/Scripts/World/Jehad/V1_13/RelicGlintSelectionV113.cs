using System;
using System.Collections.Generic;

namespace Echoheart.World
{
    public struct RelicGlintCandidateV113
    {
        public int Id;
        public float Distance;
        public bool Eligible;
        public RelicGlintCandidateV113(int id,float distance,bool eligible){Id=id;Distance=distance;Eligible=eligible;}
    }
    /// <summary>Bounded nearby selection with stable slots and an enter/exit distance band.</summary>
    public sealed class RelicGlintSelectionV113
    {
        public const int Capacity=6;
        public const float EnterDistance=10,ExitDistance=12;
        private readonly int[] _slots={-1,-1,-1,-1,-1,-1};
        private readonly Dictionary<int,RelicGlintCandidateV113> _nearest=new Dictionary<int,RelicGlintCandidateV113>();
        private readonly List<RelicGlintCandidateV113> _valid=new List<RelicGlintCandidateV113>(16);
        private readonly HashSet<int> _chosen=new HashSet<int>(),_used=new HashSet<int>();
        private static readonly Comparison<RelicGlintCandidateV113> Sort=Compare;
        public int Slot(int index)=>index>=0&&index<Capacity?_slots[index]:-1;
        private bool Assigned(int id){for(int i=0;i<Capacity;i++)if(_slots[i]==id)return true;return false;}
        private static bool ValidDistance(float value)=>value>=0&&!float.IsNaN(value)&&!float.IsInfinity(value);
        public static bool Available(bool playing,bool active,bool triggerEnabled,bool inView,bool blocked,float distance)=>playing&&active&&triggerEnabled&&inView&&!blocked&&ValidDistance(distance)&&distance<=ExitDistance;
        private static int Compare(RelicGlintCandidateV113 a,RelicGlintCandidateV113 b){int order=a.Distance.CompareTo(b.Distance);return order!=0?order:a.Id.CompareTo(b.Id);}
        public void Update(RelicGlintCandidateV113[] candidates)
        {
            _nearest.Clear();_valid.Clear();_chosen.Clear();_used.Clear();
            if(candidates!=null)foreach(RelicGlintCandidateV113 candidate in candidates)
            {
                if(!candidate.Eligible||candidate.Id<0||!ValidDistance(candidate.Distance)||candidate.Distance>(Assigned(candidate.Id)?ExitDistance:EnterDistance))continue;
                if(!_nearest.TryGetValue(candidate.Id,out RelicGlintCandidateV113 prior)||candidate.Distance<prior.Distance)_nearest[candidate.Id]=candidate;
            }
            foreach(RelicGlintCandidateV113 candidate in _nearest.Values)_valid.Add(candidate);
            _valid.Sort(Sort);int count=Math.Min(Capacity,_valid.Count);
            for(int i=0;i<count;i++)_chosen.Add(_valid[i].Id);
            for(int i=0;i<Capacity;i++)if(!_chosen.Contains(_slots[i])||!_used.Add(_slots[i]))_slots[i]=-1;
            for(int i=0;i<count;i++)if(!_used.Contains(_valid[i].Id))
            {
                for(int slot=0;slot<Capacity;slot++)if(_slots[slot]<0){_slots[slot]=_valid[i].Id;_used.Add(_valid[i].Id);break;}
            }
        }
        public static float Alpha(float distance)=>ValidDistance(distance)?Math.Max(0,Math.Min(1,(ExitDistance-distance)/4f)):0;
        public void Clear(){for(int i=0;i<Capacity;i++)_slots[i]=-1;_nearest.Clear();_valid.Clear();_chosen.Clear();_used.Clear();}
    }
}
