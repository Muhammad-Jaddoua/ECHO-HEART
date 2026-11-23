using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.UI
{
    /// <summary>Scenic title presentation only. Never writes the camera outside Title.</summary>
    [DefaultExecutionOrder(150)]
    public sealed class TitleVistaV117 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV117 _menu;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private Transform _camera;
        [SerializeField] private Vector3 _focusPoint;
        private readonly TitleVistaMotionV116 _motion=new TitleVistaMotionV116();
        private Vector3 _fixedPosition;
        private Quaternion _fixedRotation;
        private bool _captured;
        private bool Focused=>Application.isEditor||Application.isFocused;
        public bool Moving=>isActiveAndEnabled&&_captured&&_flow.State==GameState.Title&&_menu.VisibleState==GameState.Title&&_menu.Page==UiPageV17.Home&&!_menu.ConfirmationOpen&&!_settings.Values.ReduceMotion&&Focused;
        public Vector3 FixedPosition=>_fixedPosition;
        public Quaternion FixedRotation=>_fixedRotation;
        private void Awake()
        {
            if(_flow==null||_menu==null||_settings==null||_camera==null){Debug.LogError("V1.16 title vista bindings missing.",this);enabled=false;}
        }
        private void OnEnable(){GameEvents.StateChanged+=StateChanged;if(_flow!=null&&_flow.State==GameState.Title)Capture();}
        private void OnDisable()
        {
            GameEvents.StateChanged-=StateChanged;
            if(_captured&&_flow!=null&&_flow.State==GameState.Title&&_camera!=null)_camera.SetPositionAndRotation(_fixedPosition,_fixedRotation);
            _captured=false;_motion.Reset();
        }
        private void Capture()
        {
            if(_camera==null)return;
            _fixedPosition=_camera.position;_fixedRotation=_camera.rotation;_captured=true;_motion.Reset();
        }
        private void StateChanged(GameState state)
        {
            if(state==GameState.Title)Capture();else{_captured=false;_motion.Reset();}
        }
        private void LateUpdate()=>RefreshVista(Time.unscaledDeltaTime);
        public void RefreshVista(float seconds)
        {
            if(!isActiveAndEnabled||_camera==null)return;
            if(_flow.State!=GameState.Title){_captured=false;_motion.Reset();return;}
            if(!_captured)Capture();
            bool reduced=_settings.Values.ReduceMotion;
            TitleVistaOffsetV116 offset=_motion.Step(seconds,Moving,reduced,Focused);
            if(!reduced&&!Moving)return;
            Vector3 position=_fixedPosition;Quaternion rotation=_fixedRotation;
            if(!reduced&&(offset.Yaw!=0||offset.Lift!=0))
            {
                position=_focusPoint+Quaternion.AngleAxis(offset.Yaw,Vector3.up)*(_fixedPosition-_focusPoint)+Vector3.up*offset.Lift;
                Vector3 direction=_focusPoint-position;if(direction.sqrMagnitude>.001f)rotation=Quaternion.LookRotation(direction,Vector3.up);
            }
            if(_camera.position!=position||_camera.rotation!=rotation)_camera.SetPositionAndRotation(position,rotation);
        }
    }
}
