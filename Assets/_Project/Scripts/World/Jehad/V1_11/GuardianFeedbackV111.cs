using Echoheart.AI;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoheart.World
{
    /// <summary>Scene-owned boss presentation. Reads the original BossBrain; never drives its simulation.</summary>
    [DefaultExecutionOrder(90)]
    public sealed class GuardianFeedbackV111 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private WorldAudioBankV15 _bank;
        [SerializeField] private Material _warningMaterial;
        [SerializeField] private string _muzzlePath="Muzzle";
        private readonly GuardianCuePolicyV111 _policy=new GuardianCuePolicyV111();
        private BossBrain _boss;
        private Transform _muzzle,_root;
        private BossTelegraphVisual _legacy;
        private bool _restoreLegacy;
        private LineRenderer _boundary,_progress;
        private AudioSource _warning,_impact;
        private float _impactAge=.35f;
        private Vector3 _impactOrigin;
        private float _impactRadius;
        public bool WarningVisible=>_boundary!=null&&_boundary.enabled;
        public LineRenderer Boundary=>_boundary;
        public Transform Muzzle=>_muzzle;
        public int WarningCueCount {get;private set;}
        public int DedicatedVoiceCapacity=>_root!=null?_root.GetComponentsInChildren<AudioSource>(true).Length:0;
        private bool Playing=>_flow!=null&&_flow.State==GameState.Playing;
        private void Awake()
        {
            if(_flow==null||_arena==null||_feedback==null||_bank==null||_warningMaterial==null){Debug.LogError("V1.11 Guardian presentation bindings missing.",this);enabled=false;return;}
            _root=new GameObject("Guardian presentation").transform;_root.SetParent(transform,false);
            _boundary=Line("Attack boundary",.08f);_progress=Line("Attack readiness",.14f);
            _warning=Voice("Guardian warning",32);_impact=Voice("Guardian impact",48);
        }
        private LineRenderer Line(string name,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(_root,false);var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=_warningMaterial;line.useWorldSpace=true;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.numCornerVertices=3;line.startWidth=line.endWidth=width;line.enabled=false;return line;
        }
        private AudioSource Voice(string name,int priority)
        {
            var go=new GameObject(name);go.transform.SetParent(_root,false);var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=false;source.spatialBlend=1;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=8;source.maxDistance=80;source.priority=priority;return source;
        }
        private void OnEnable()
        {
            GameEvents.RetryRequested+=Retrying;GameEvents.StateChanged+=StateChanged;GameEvents.BossDefeated+=Defeated;
            if(_legacy!=null)_legacy.enabled=false;
        }
        private void OnDisable()
        {
            GameEvents.RetryRequested-=Retrying;GameEvents.StateChanged-=StateChanged;GameEvents.BossDefeated-=Defeated;Clear();
            if(_legacy!=null)_legacy.enabled=_restoreLegacy;
        }
        private bool BindBoss()
        {
            BossBrain next=_arena!=null?_arena.Boss:null;if(next==null)return false;if(next==_boss)return true;
            if(_legacy!=null)_legacy.enabled=_restoreLegacy;
            _boss=next;_policy.Reset();_muzzle=_boss.transform.Find(_muzzlePath);
            _legacy=_boss.GetComponent<BossTelegraphVisual>();if(_legacy!=null){_restoreLegacy=_legacy.enabled;_legacy.enabled=false;}
            var actor=_boss.GetComponent<ActorFeedbackV15>();if(actor==null)actor=_boss.gameObject.AddComponent<ActorFeedbackV15>();
            actor.Bind(_feedback,_boss.GetComponent<Health>(),null);return true;
        }
        private void Retrying(RetryScope scope)=>Clear();
        private void StateChanged(GameState state)
        {
            if(state==GameState.Paused){Hide();if(_warning!=null)_warning.Pause();if(_impact!=null)_impact.Pause();}
            else if(state==GameState.Playing){if(_warning!=null)_warning.UnPause();if(_impact!=null)_impact.UnPause();}
            else Clear();
        }
        private void Clear(){_policy.Reset();_impactAge=.35f;Hide();if(_warning!=null)_warning.Stop();if(_impact!=null)_impact.Stop();}
        private void Hide(){if(_boundary!=null)_boundary.enabled=false;if(_progress!=null)_progress.enabled=false;}
        private Vector3 ShotOrigin=>_muzzle!=null?_muzzle.position:_boss.transform.position+Vector3.up*1.5f;
        private void Play(AudioSource source,WorldCueV15 cue,Vector3 position)
        {
            AudioClip clip=_bank.Cue(cue);if(source==null||clip==null||!Playing)return;
            source.Stop();source.transform.position=position;source.clip=clip;source.volume=_feedback.Audio.SfxVolume*.8f;source.Play();
        }
        private void Defeated()
        {
            if(!BindBoss()||!Playing)return;Clear();
            Play(_impact,WorldCueV15.Defeat,_boss.transform.position);_feedback.Particles.Burst(WorldBurstV15.Capture,_boss.transform.position+Vector3.up);
        }
        private static GuardianPhaseV111 Phase(BossBrain.State state)
        {
            switch(state){case BossBrain.State.Telegraph:return GuardianPhaseV111.Telegraph;case BossBrain.State.Attack:return GuardianPhaseV111.Attack;case BossBrain.State.Recover:return GuardianPhaseV111.Recover;case BossBrain.State.Defeated:return GuardianPhaseV111.Defeated;default:return GuardianPhaseV111.Idle;}
        }
        private void LateUpdate()
        {
            if(_root==null)return;
            _warning.volume=_impact.volume=_feedback.Audio.SfxVolume*.8f;
            if(!BindBoss()||_boss.Definition==null||!Playing){Hide();return;}
            if(!_boss.isActiveAndEnabled){Clear();return;}
            bool slam=_boss.CurrentAttack==BossBrain.AttackKind.Slam;
            GuardianCueV111 cue=_policy.Step(Phase(_boss.Current),slam,true);
            if(cue==GuardianCueV111.SlamWarning||cue==GuardianCueV111.RangedWarning)
            {
                WarningCueCount++;_impactAge=.35f;Play(_warning,slam?WorldCueV15.SlamWarning:WorldCueV15.RangedWarning,_boss.transform.position);
            }
            else if(cue==GuardianCueV111.Slam||cue==GuardianCueV111.RangedShot)
            {
                _warning.Stop();Vector3 origin=slam?_boss.transform.position:ShotOrigin;
                Play(_impact,slam?WorldCueV15.Slam:WorldCueV15.RangedShot,origin);_feedback.Particles.Burst(WorldBurstV15.Hit,slam?origin+Vector3.up*.5f:origin);
                if(slam){_impactAge=0;_impactOrigin=_boss.transform.position+Vector3.up*.09f;_impactRadius=_boss.Definition.SlamRadius;}
            }
            Hide();
            if(_boss.Current==BossBrain.State.Telegraph)
            {
                float progress=GuardianCuePolicyV111.Progress(_boss.TelegraphProgress);
                if(slam)DrawSlam(progress);else DrawRanged(progress);
            }
            else if(_impactAge<.35f)
            {
                Ring(_progress,_impactOrigin,GuardianCuePolicyV111.ImpactRadius(_impactRadius,_impactAge),1,true);
                Color color=new Color(1,.68f,.25f,GuardianCuePolicyV111.ImpactAlpha(_impactAge));_progress.startColor=_progress.endColor=color;
                _impactAge+=Time.deltaTime;
            }
        }
        private void DrawSlam(float progress)
        {
            Vector3 center=_boss.transform.position+Vector3.up*.09f;float radius=_boss.Definition.SlamRadius;
            Ring(_boundary,center,radius,1,true);_boundary.startColor=_boundary.endColor=new Color(.98f,.45f,.22f,.85f);
            if(progress>.001f){Ring(_progress,center+Vector3.up*.01f,radius,progress,false);_progress.startColor=_progress.endColor=new Color(1,.82f,.36f);}
        }
        private static void Ring(LineRenderer line,Vector3 center,float radius,float fill,bool closed)
        {
            line.loop=closed;int count=closed?64:GuardianCuePolicyV111.ArcVertices(fill);line.positionCount=count;line.enabled=true;
            for(int i=0;i<count;i++){GuardianPointV111 point=GuardianCuePolicyV111.Ring(radius,closed?i/64f:fill*i/(count-1));line.SetPosition(i,center+new Vector3(point.X,0,point.Z));}
        }
        private void DrawRanged(float progress)
        {
            if(_flow.Player.Player==null)return;
            Vector3 origin=ShotOrigin,direction=(_flow.Player.Player.position+Vector3.up-origin).normalized;
            float range=_boss.Definition.ProjectileRange;
            bool covered=Physics.Raycast(origin,direction,out RaycastHit hit,range,Layers.EnvironmentMask,QueryTriggerInteraction.Ignore);
            float distance=GuardianCuePolicyV111.AimDistance(range,covered?hit.distance:float.PositiveInfinity);
            Vector3 start=origin,end=origin+direction*distance;start.y=end.y=_boss.transform.position.y+.09f;
            _boundary.loop=false;_boundary.positionCount=2;_boundary.SetPosition(0,start);_boundary.SetPosition(1,end);_boundary.enabled=true;_boundary.startColor=_boundary.endColor=new Color(.77f,.36f,1,.8f);
            _progress.loop=false;_progress.positionCount=2;_progress.SetPosition(0,start);_progress.SetPosition(1,Vector3.Lerp(start,end,progress));_progress.enabled=progress>.001f;_progress.startColor=_progress.endColor=new Color(1,.82f,.36f);
        }
    }
}
