using System;
using System.Collections;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class TitleVistaReviewV117 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV117 _menu;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private PreferenceStoreV114 _preferences;
        [SerializeField] private TitleVistaV117 _vista;
        [SerializeField] private Transform _camera;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.16 title vista: "+reason);}
        private bool Same(Vector3 position,Quaternion rotation)=>Vector3.Distance(_camera.position,position)<.001f&&Quaternion.Angle(_camera.rotation,rotation)<.001f;
        [ContextMenu("Title review/Run vista checks (resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;UiValuesV16 original=_settings.Values;bool originalEnabled=_vista.enabled;
            try
            {
                Require(originalEnabled,"Enable the title vista before running its check.");
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;_flow.ReturnToTitle();yield return null;
                var values=original;values.ReduceMotion=false;_settings.Set(values);_menu.Open(UiPageV17.Home);
                Vector3 first=_camera.position;
                for(int i=0;i<60;i++)_vista.RefreshVista(1f/30);
                Require(_vista.Moving&&Vector3.Distance(first,_camera.position)>.01f,"Title home did not advance its scenic view.");
                _menu.Open(UiPageV17.Settings);Vector3 held=_camera.position;Quaternion heldRotation=_camera.rotation;
                _vista.RefreshVista(.1f);Require(!_vista.Moving&&Same(held,heldRotation),"Settings did not hold the background still.");
                values.ReduceMotion=true;_settings.Set(values);_vista.RefreshVista(0);
                Require(!_vista.Moving&&Same(_vista.FixedPosition,_vista.FixedRotation),"Reduced UI motion did not restore the fixed title view.");
                values.ReduceMotion=false;_settings.Set(values);_menu.Open(UiPageV17.Home);_vista.RefreshVista(.1f);
                _menu.Ask(UiModalV17.Quit);held=_camera.position;heldRotation=_camera.rotation;_vista.RefreshVista(.1f);
                Require(!_vista.Moving&&Same(held,heldRotation),"Confirmation did not hold the vista.");_menu.CancelModal();
                _vista.enabled=false;Require(Same(_vista.FixedPosition,_vista.FixedRotation),"Disabling the vista did not restore its fixed title pose.");_vista.enabled=true;
                _flow.StartNewGame();yield return null;held=_camera.position;heldRotation=_camera.rotation;_vista.RefreshVista(.1f);
                Require(!_vista.Moving&&Same(held,heldRotation),"Title presentation wrote the gameplay camera.");
                _flow.Pause();held=_camera.position;heldRotation=_camera.rotation;_vista.RefreshVista(.1f);
                Require(!_vista.Moving&&Same(held,heldRotation),"Title presentation wrote the pause camera.");
                Debug.Log("V1.16 title vista checks PASSED. Inspect framing, geometry clearance, reduced motion and standalone focus behavior manually.",this);
            }
            finally{_vista.enabled=originalEnabled;_settings.Set(original);if(_preferences!=null)_preferences.Flush();if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
#endif
    }
}
