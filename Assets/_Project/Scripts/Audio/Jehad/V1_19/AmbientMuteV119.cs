using Echoheart.UI;
using UnityEngine;

namespace Echoheart.Audio
{
    /// <summary>Opts this scene into immediate ambience silence when the SFX setting reaches zero.</summary>
    [DefaultExecutionOrder(115)]
    [DisallowMultipleComponent]
    public sealed class AmbientMuteV119 : MonoBehaviour
    {
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private WorldAudioV15 _audio;
        private void Awake()
        {
            if(_settings==null||_audio==null){Debug.LogError("V1.19 ambience mute bindings missing.",this);enabled=false;}
        }
        private void OnEnable(){if(_settings!=null)_settings.Changed+=Changed;Changed();}
        private void OnDisable(){if(_settings!=null)_settings.Changed-=Changed;}
        private void Changed(){if(_audio!=null)_audio.SilenceMutedAmbience();}
    }
}
