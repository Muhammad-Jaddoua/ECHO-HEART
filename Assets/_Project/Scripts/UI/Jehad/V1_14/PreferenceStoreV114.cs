using System;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.UI
{
    /// <summary>Persists only the existing audio/interface values through their original settings owner.</summary>
    [DefaultExecutionOrder(-210)]
    public sealed class PreferenceStoreV114 : MonoBehaviour
    {
        public const string StorageKey="Echoheart.Jehad.AudioInterface.v1";
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private SessionFlowV13 _flow;
        private PreferenceSaveV114 _save;
        private string _observed;
        private bool _failed;
        public event Action StatusChanged;
        public string Status=>_failed?"Settings could not save. Changes apply this session.":_save!=null&&_save.Pending!=null?"Saving audio/interface... Display is session-only.":"Audio/interface save automatically. Display is session-only.";

        private void Awake()
        {
            if(_settings==null||_flow==null){Debug.LogError("V1.14 preference settings/flow binding missing.",this);enabled=false;return;}
            try
            {
                if(PreferenceCodecV114.TryDecode(PlayerPrefs.GetString(StorageKey,string.Empty),out UiValuesV16 saved))_settings.Set(saved);
            }
            catch(Exception){Debug.LogWarning("ECHOHEART could not read audio/interface preferences; using current session values.",this);}
            _observed=PreferenceCodecV114.Encode(_settings.Values);_save=new PreferenceSaveV114(_settings.Values);
        }
        private void OnEnable(){GameEvents.StateChanged+=StateChanged;if(_settings!=null){_settings.Changed+=Changed;Changed();}}
        private void OnDisable(){GameEvents.StateChanged-=StateChanged;if(_settings!=null)_settings.Changed-=Changed;Flush();}
        private void StateChanged(GameState state)=>Flush();
        private void Changed()
        {
            if(_save==null)return;
            string value=PreferenceCodecV114.Encode(_settings.Values);if(value==_observed)return;
            _observed=value;_save.Observe(_settings.Values,Time.unscaledTime);_failed=false;StatusChanged?.Invoke();
        }
        private void Update(){if(!_failed&&_flow!=null&&_flow.State!=GameState.Playing&&_save!=null&&_save.Due(Time.unscaledTime))Flush();}
        private void OnApplicationFocus(bool focused){if(!focused)Flush();}
        private void OnApplicationPause(bool paused){if(paused)Flush();}
        private void OnApplicationQuit()=>Flush();
        public bool Flush()
        {
            if(_save==null||_save.Pending==null)return true;
            string payload=_save.Pending;
            bool hadPrevious=false,captured=false;string previous=string.Empty;
            try
            {
                hadPrevious=PlayerPrefs.HasKey(StorageKey);previous=PlayerPrefs.GetString(StorageKey,string.Empty);captured=true;
                PlayerPrefs.SetString(StorageKey,payload);PlayerPrefs.Save();
            }
            catch(Exception)
            {
                // Unity also saves its in-memory preferences on quit; undo a failed staged write.
                if(captured)try{if(hadPrevious)PlayerPrefs.SetString(StorageKey,previous);else PlayerPrefs.DeleteKey(StorageKey);}catch(Exception){}
                if(!_failed)Debug.LogWarning("ECHOHEART could not save audio/interface preferences. Changes still apply this session.",this);
                _failed=true;StatusChanged?.Invoke();return false;
            }
            _save.Acknowledge(payload);_failed=false;StatusChanged?.Invoke();return true;
        }
    }
}
