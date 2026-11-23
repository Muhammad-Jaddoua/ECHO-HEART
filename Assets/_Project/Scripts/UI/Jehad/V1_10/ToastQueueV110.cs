using System;
using System.Collections.Generic;

namespace Echoheart.UI
{
    public enum ToastKindV110 { Relic, Chest, Secret, Objective }
    public struct ToastNoticeV110
    {
        public string Key,Heading,Message;
        public ToastKindV110 Kind;
        public ToastNoticeV110(string key,ToastKindV110 kind,string heading,string message){Key=key;Kind=kind;Heading=heading;Message=message;}
    }
    /// <summary>Bounded reading queue. Objectives interrupt, preserving interrupted reading time when capacity permits.</summary>
    public sealed class ToastQueueV110
    {
        private sealed class Entry
        {
            public ToastNoticeV110 Notice;
            public float Remaining;
            public long Order;
        }
        private readonly List<Entry> _pending=new List<Entry>();
        private Entry _active;
        private long _order;
        public int Capacity {get;}
        public int Count=>_pending.Count+(_active==null?0:1);
        public int PendingCount=>_pending.Count;
        public bool HasActive=>_active!=null;
        public ToastNoticeV110 Active=>_active!=null?_active.Notice:default;
        public float Remaining=>_active!=null?_active.Remaining:0;
        public float Duration=>DurationFor(Active.Kind);
        public ToastQueueV110(int capacity=32){Capacity=Math.Max(1,Math.Min(32,capacity));}
        private static int Priority(ToastKindV110 kind)=>kind==ToastKindV110.Objective?2:1;
        private static float DurationFor(ToastKindV110 kind)=>kind==ToastKindV110.Objective?4.4f:3.2f;
        public bool Enqueue(ToastNoticeV110 notice)
        {
            if(string.IsNullOrWhiteSpace(notice.Key)||string.IsNullOrWhiteSpace(notice.Message))return false;
            if(_active!=null&&_active.Notice.Key==notice.Key)return false;
            foreach(Entry entry in _pending)if(entry.Notice.Key==notice.Key)return false;
            if(Count>=Capacity)
            {
                int lowest=_active!=null?Priority(_active.Notice.Kind):int.MaxValue,index=-1;
                for(int i=0;i<_pending.Count;i++)if(Priority(_pending[i].Notice.Kind)<lowest||(index<0&&Priority(_pending[i].Notice.Kind)==lowest)){lowest=Priority(_pending[i].Notice.Kind);index=i;}
                if(Priority(notice.Kind)<=lowest)return false;
                if(index<0)_active=null;else _pending.RemoveAt(index);
            }
            var next=new Entry{Notice=notice,Remaining=DurationFor(notice.Kind),Order=_order++};
            if(_active==null)_active=next;
            else if(Priority(notice.Kind)>Priority(_active.Notice.Kind)){_pending.Add(_active);_active=next;}
            else _pending.Add(next);
            return true;
        }
        public void Tick(float delta,bool playing)
        {
            if(!playing||_active==null||delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
            if(delta<_active.Remaining){_active.Remaining-=delta;return;}
            // A long frame must not consume reading time for messages the player has never seen.
            _active=null;ActivateNext();
        }
        public bool Remove(string key)
        {
            int before=Count;_pending.RemoveAll(entry=>entry.Notice.Key==key);
            if(_active!=null&&_active.Notice.Key==key){_active=null;ActivateNext();}
            return Count!=before;
        }
        private void ActivateNext()
        {
            int best=-1;
            for(int i=0;i<_pending.Count;i++)if(best<0||Priority(_pending[i].Notice.Kind)>Priority(_pending[best].Notice.Kind)||(Priority(_pending[i].Notice.Kind)==Priority(_pending[best].Notice.Kind)&&_pending[i].Order<_pending[best].Order))best=i;
            if(best>=0){_active=_pending[best];_pending.RemoveAt(best);}
        }
        public void Clear(){_active=null;_pending.Clear();_order=0;}
    }
}
