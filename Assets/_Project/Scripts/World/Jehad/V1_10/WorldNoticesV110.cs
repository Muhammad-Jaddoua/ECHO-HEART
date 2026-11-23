using System.Text;
using Echoheart.Core;
using Echoheart.UI;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Presentation-only observer. Pickups/chests keep their existing reward, sound and particle owners.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class WorldNoticesV110 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private RelicCounterV13 _relics;
        [SerializeField] private WorldToastV110 _toast;
        [SerializeField] private SecretChestV15[] _chests;
        private bool[] _collected,_opened;
        private bool _resetting;
        private void OnEnable()
        {
            GameEvents.RetryRequested+=Retrying;GameEvents.StateChanged+=StateChanged;
            GameEvents.BossEncounterStarted+=BossStarted;GameEvents.BossDefeated+=BossDefeated;
        }
        private void OnDisable()
        {
            GameEvents.RetryRequested-=Retrying;GameEvents.StateChanged-=StateChanged;
            GameEvents.BossEncounterStarted-=BossStarted;GameEvents.BossDefeated-=BossDefeated;
        }
        private void Start()
        {
            if(_flow==null||_relics==null||_toast==null||_chests==null){Debug.LogError("V1.10 notification observers are missing bindings.",this);enabled=false;return;}
            Capture();
        }
        private void Retrying(RetryScope scope){_resetting=true;}
        private void StateChanged(GameState state)
        {
            // Flow publishes Playing only after its synchronous retry/reset transaction is complete.
            if(state==GameState.Playing&&_resetting){Capture();_resetting=false;}
        }
        private void Capture()
        {
            if(_relics==null||_relics.Relics==null||_chests==null)return;
            _collected=new bool[_relics.Total];for(int i=0;i<_collected.Length;i++)_collected[i]=_relics.Relics[i]!=null&&_relics.Relics[i].Collected;
            _opened=new bool[_chests.Length];for(int i=0;i<_opened.Length;i++)_opened[i]=_chests[i]!=null&&_chests[i].Opened;
        }
        private void LateUpdate()
        {
            if(_flow==null||_flow.State!=GameState.Playing||_resetting||_collected==null)return;
            // Opening is reported before its relic, including when both occur in the same frame.
            for(int i=0;i<_opened.Length;i++)
            {
                bool opened=_chests[i]!=null&&_chests[i].Opened;
                if(opened&&!_opened[i])_toast.Show(new ToastNoticeV110("chest:"+i,ToastKindV110.Chest,"CHEST OPENED","A hidden relic waits inside."));
                _opened[i]=opened;
            }
            for(int i=0;i<_collected.Length;i++)
            {
                HiddenRelicV12 relic=_relics.Relics[i];bool collected=relic!=null&&relic.Collected;
                if(collected&&!_collected[i])_toast.Show(new ToastNoticeV110("relic:"+i,ToastKindV110.Relic,"RELIC FOUND",Friendly(relic.DisplayName)));
                _collected[i]=collected;
            }
        }
        private void BossStarted(){if(_toast!=null)_toast.Show(new ToastNoticeV110("boss:start",ToastKindV110.Objective,"THE CORRUPTED GUARDIAN","Use any power to defeat the Guardian."));}
        private void BossDefeated()
        {
            if(_toast==null)return;
            _toast.Dismiss("boss:start");_toast.Show(new ToastNoticeV110("boss:defeated",ToastKindV110.Objective,"GUARDIAN DEFEATED","Find the Island Heart and press E to restore it."));
        }
        private static string Friendly(string value)
        {
            if(string.IsNullOrEmpty(value))return "Hidden relic";
            var result=new StringBuilder();for(int i=0;i<value.Length;i++){if(i>0&&char.IsUpper(value[i])&&!char.IsWhiteSpace(value[i-1]))result.Append(' ');result.Append(value[i]);}return result.ToString();
        }
    }
}
