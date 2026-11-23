using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class AmbientMuteReviewV119 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private UiSettingsV17 _settings;
        [SerializeField] private PreferenceStoreV114 _preferences;
        [SerializeField] private WorldAudioV15 _audio;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("V1.19 ambience mute: "+reason);}
        private AudioSource Source(string label)
        {
            foreach(AudioSource source in _audio.GetComponentsInChildren<AudioSource>(true))if(source.name==label)return source;
            throw new InvalidOperationException("V1.19 audio source missing: "+label);
        }
        private void Silent()
        {
            foreach(string name in new[]{"Nearest water","Forest air","Alpine air"})
            {AudioSource source=Source(name);Require(source.volume==0&&!source.isPlaying,"Muted ambience is still active: "+name);}
        }
        [ContextMenu("Audio review/Run ambience mute checks (restores settings, resets run)")]
        private void Run(){if(Application.isPlaying&&!_running)StartCoroutine(Check());}
        private IEnumerator Check()
        {
            _running=true;UiValuesV16 original=_settings.Values;
            try
            {
                _flow.ReturnToTitle();_flow.StartNewGame();yield return null;
                var values=original;values.Sfx=1;_settings.Set(values);yield return new WaitForSecondsRealtime(.35f);
                Require(Source("Forest air").volume>0&&Source("Forest air").isPlaying,"Main-area ambience did not start.");
                AudioSource music=Source("Music Meadow");float musicLevel=music.volume;MusicRegionV15 region=_audio.Region;
                values.Sfx=0;_settings.Set(values);Silent();
                Require(music.volume==musicLevel&&_audio.Region==region&&_audio.MusicVolume==original.Music,"Ambience mute changed music.");
                yield return new WaitForSecondsRealtime(.1f);Silent();
                values.Hints=!values.Hints;_settings.Set(values);Silent();
                values.Sfx=1;_settings.Set(values);yield return new WaitForSecondsRealtime(.25f);
                Require(Source("Forest air").volume>0&&Source("Forest air").isPlaying,"Nonzero sound did not restore the existing ambient fade-in.");
                _flow.Pause();values.Sfx=0;_settings.Set(values);Silent();_flow.Resume();yield return null;Silent();
                Require(_audio.VoiceCapacity==8,"World SFX pool changed.");
                Debug.Log("V1.19 ambience mute checks PASSED. Audition water/forest/alpine mute, unmute, pause and music independence manually.",this);
            }
            finally{_settings.Set(original);if(_preferences!=null)_preferences.Flush();if(_flow!=null&&Application.isPlaying)_flow.ReturnToTitle();_running=false;}
        }
#endif
    }
}
