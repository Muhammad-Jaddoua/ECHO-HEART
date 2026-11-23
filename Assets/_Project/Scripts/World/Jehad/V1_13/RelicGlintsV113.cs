using Echoheart.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoheart.World
{
    /// <summary>Presentation-only, local collectible sparkles. Reads the existing pickup collider gate.</summary>
    [DefaultExecutionOrder(130)]
    public sealed class RelicGlintsV113 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private RelicCounterV13 _counter;
        [SerializeField] private Camera _camera;
        [SerializeField] private Material _material;
        private readonly RelicGlintSelectionV113 _selection=new RelicGlintSelectionV113();
        private readonly ParticleSystem[] _pool=new ParticleSystem[RelicGlintSelectionV113.Capacity];
        private readonly ParticleSystemRenderer[] _renderers=new ParticleSystemRenderer[RelicGlintSelectionV113.Capacity];
        private readonly int[] _bound={-1,-1,-1,-1,-1,-1};
        private HiddenRelicV12[] _pickups;
        private Collider[] _triggers;
        private RelicGlintCandidateV113[] _candidates;
        private float _nextRefresh;
        public int PoolCapacity=>_pool.Length;
        public int ActiveCount {get;private set;}
        private bool Running=>isActiveAndEnabled&&_flow!=null&&_flow.State==GameState.Playing&&_flow.Player.Player!=null&&_camera!=null&&_camera.isActiveAndEnabled;
        private void Awake()
        {
            if(_flow==null||_counter==null||_camera==null||_material==null){Debug.LogError("V1.13 relic glint bindings missing.",this);enabled=false;return;}
            _pickups=_counter.Relics;if(_pickups==null){Debug.LogError("V1.13 relic counter is not initialized.",this);enabled=false;return;}
            _triggers=new Collider[_pickups.Length];_candidates=new RelicGlintCandidateV113[_pickups.Length];
            for(int i=0;i<_pickups.Length;i++)if(_pickups[i]!=null)_triggers[i]=_pickups[i].GetComponent<Collider>();
            for(int i=0;i<_pool.Length;i++)
            {
                var go=new GameObject("Relic sparkle "+i);go.transform.SetParent(transform,false);var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.playOnAwake=false;main.loop=true;main.duration=1.2f;main.maxParticles=8;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.useUnscaledTime=false;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.2f);main.startSize=new ParticleSystem.MinMaxCurve(.03f,.08f);main.startSpeed=.04f;main.gravityModifier=0;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.32f;
                var emission=ps.emission;emission.enabled=true;emission.rateOverTime=3;
                var lifetime=ps.colorOverLifetime;lifetime.enabled=true;var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.25f),new GradientAlphaKey(0,1)});lifetime.color=gradient;
                var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=_material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
                _pool[i]=ps;_renderers[i]=renderer;
            }
        }
        private void OnEnable(){GameEvents.StateChanged+=StateChanged;GameEvents.RetryRequested+=Retrying;_nextRefresh=0;}
        private void OnDisable(){GameEvents.StateChanged-=StateChanged;GameEvents.RetryRequested-=Retrying;Clear();}
        private void StateChanged(GameState state){if(state!=GameState.Playing)Clear();else _nextRefresh=0;}
        private void Retrying(RetryScope scope)=>Clear();
        private void StopSlot(int index)
        {
            if(_bound[index]<0)return;
            if(_pool[index]!=null)_pool[index].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(_renderers[index]!=null)_renderers[index].enabled=false;_bound[index]=-1;
        }
        private void Clear(){_selection.Clear();for(int i=0;i<_pool.Length;i++)StopSlot(i);ActiveCount=0;_nextRefresh=0;}
        private bool PickupReady(int index)=>index>=0&&index<_pickups.Length&&_pickups[index]!=null&&!_pickups[index].Collected&&_pickups[index].gameObject.activeInHierarchy&&_triggers[index]!=null&&_triggers[index].enabled;
        public void RefreshNow()
        {
            if(_pickups==null)return;if(!Running){Clear();return;}
            Vector3 player=_flow.Player.Player.position,eye=player+Vector3.up;
            for(int i=0;i<_pickups.Length;i++)
            {
                bool ready=PickupReady(i);float distance=ready?Vector3.Distance(player,_pickups[i].transform.position):float.PositiveInfinity;bool inView=false,blocked=true;
                if(ready&&distance<=RelicGlintSelectionV113.ExitDistance)
                {
                    Vector3 view=_camera.WorldToViewportPoint(_pickups[i].transform.position);inView=view.z>0&&view.x>=-.08f&&view.x<=1.08f&&view.y>=-.08f&&view.y<=1.08f;
                    if(inView)blocked=Physics.Linecast(eye,_pickups[i].transform.position,Layers.EnvironmentMask,QueryTriggerInteraction.Ignore);
                }
                _candidates[i]=new RelicGlintCandidateV113(i,distance,RelicGlintSelectionV113.Available(true,ready,ready,inView,blocked,distance));
            }
            _selection.Update(_candidates);_nextRefresh=Time.unscaledTime+.12f;UpdateSlots();
        }
        private void LateUpdate()
        {
            if(_pickups==null)return;if(!Running){Clear();return;}
            if(Time.unscaledTime>=_nextRefresh)RefreshNow();UpdateSlots();
        }
        private void UpdateSlots()
        {
            ActiveCount=0;
            for(int i=0;i<_pool.Length;i++)
            {
                int id=_selection.Slot(i);if(!PickupReady(id)){StopSlot(i);continue;}
                float alpha=RelicGlintSelectionV113.Alpha(Vector3.Distance(_flow.Player.Player.position,_pickups[id].transform.position));
                if(alpha<=0){StopSlot(i);continue;}
                bool changed=_bound[i]!=id;if(changed)StopSlot(i);
                _pool[i].transform.position=_pickups[id].transform.position+Vector3.up*.1f;var main=_pool[i].main;main.startColor=new Color(1,.83f,.34f,alpha*.8f);
                _renderers[i].enabled=true;if(changed){_bound[i]=id;_pool[i].Play(false);}ActiveCount++;
            }
        }
        public bool HasGlint(HiddenRelicV12 relic)
        {
            if(_pickups==null||relic==null)return false;
            for(int i=0;i<_bound.Length;i++)if(_bound[i]>=0&&_pickups[_bound[i]]==relic)return true;return false;
        }
    }
}
