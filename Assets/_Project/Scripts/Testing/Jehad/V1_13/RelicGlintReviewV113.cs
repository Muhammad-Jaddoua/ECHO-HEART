using System;
using System.Collections;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class RelicGlintReviewV113 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private RelicGlintsV113 _glints;
        [SerializeField] private SecretChestV15[] _chests;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.13 relic glints: "+reason);}
        [ContextMenu("Relic review/Run sparkle checks (resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                Require(_glints.PoolCapacity==6&&_flow.Relics.Total==16,"Pool or original relic count changed.");
                foreach(SecretChestV15 chest in _chests)
                {
                    _flow.Player.Teleport(chest.Reward.transform.position+Vector3.back*2+Vector3.up*.3f,Quaternion.identity);yield return null;_glints.RefreshNow();
                    Require(!chest.Opened&&!chest.Reward.GetComponent<Collider>().enabled&&!_glints.HasGlint(chest.Reward),"Closed chest reward acquired a sparkle.");
                }
                HiddenRelicV12 relic=null;
                foreach(HiddenRelicV12 candidate in _flow.Relics.Relics)
                {
                    bool chestReward=false;foreach(SecretChestV15 chest in _chests)chestReward|=chest.Reward==candidate;
                    if(candidate!=null&&!chestReward){relic=candidate;break;}
                }
                Require(relic!=null,"No ordinary relic available for the check.");
                _flow.Player.Teleport(relic.transform.position+Vector3.back*2+Vector3.up*.3f,Quaternion.identity);yield return new WaitForSecondsRealtime(.15f);_glints.RefreshNow();
                Require(_glints.HasGlint(relic)&&_glints.ActiveCount<=6,"Nearby relic has no glint; inspect camera framing or environment cover.");
                _flow.Pause();Require(_glints.ActiveCount==0,"Pause retained sparkle emitters.");
                _flow.Resume();yield return null;_glints.RefreshNow();Require(_glints.HasGlint(relic),"Resume failed to rebuild local sparkles.");
                _flow.Player.Teleport(relic.transform.position,Quaternion.identity);yield return new WaitForSecondsRealtime(.25f);_glints.RefreshNow();
                Require(relic.Collected&&!_glints.HasGlint(relic),"Collected relic retained a sparkle.");
                _flow.Pause();_flow.Retry();yield return null;_glints.RefreshNow();
                Require(relic.Collected&&!_glints.HasGlint(relic)&&_glints.ActiveCount<=6,"Area retry replayed a collected relic or exceeded the pool.");
                _flow.ReturnToTitle();Require(_glints.ActiveCount==0,"Title retained sparkle emitters.");
                _flow.StartNewGame();yield return null;_glints.RefreshNow();Require(!relic.Collected&&_flow.Relics.Total==16,"New Game did not retain the original pickup reset rules.");
                Debug.Log("V1.13 relic sparkle checks PASSED. Inspect cover/view gating, chest opening, distance fade and particle frame cost manually.",this);
            }
            finally{if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
#endif
    }
}
