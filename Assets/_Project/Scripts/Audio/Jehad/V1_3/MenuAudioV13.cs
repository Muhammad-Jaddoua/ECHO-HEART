using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Audio
{
    /// <summary>Session volume bindings and original synthesized UI cues; no progress is saved to disk.</summary>
    public sealed class MenuAudioV13 : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.8f;
        [SerializeField] private AudioSource[] _musicSources;
        [SerializeField] private AudioSource[] _sfxSources;
        private AudioSource _ui;
        private AudioClip _press;
        private AudioClip _restore;
        private int _lastRelics;

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        private void Awake()
        {
            _ui = gameObject.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.spatialBlend = 0f;
            _ui.ignoreListenerPause = true;
            _press = MakeCue("Echoheart menu", 0.05f, 760f, 1040f);
            _restore = MakeCue("Echoheart restoration", 0.65f, 523.25f, 783.99f);
            SetMusic(_musicVolume);
            SetSfx(_sfxVolume);
        }

        private void OnEnable()
        {
            GameEvents.GameCompleted += PlayRestoration;
            GameEvents.CollectiblesChanged += HandleRelics;
        }

        private void OnDisable()
        {
            GameEvents.GameCompleted -= PlayRestoration;
            GameEvents.CollectiblesChanged -= HandleRelics;
        }

        public void SetMusic(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            if (_musicSources != null) foreach (var source in _musicSources) if (source != null) source.volume = _musicVolume;
        }

        public void SetSfx(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            if (_ui != null) _ui.volume = _sfxVolume;
            if (_sfxSources != null) foreach (var source in _sfxSources) if (source != null) source.volume = _sfxVolume;
        }

        public void PlayPress() { if (_ui != null && _press != null) _ui.PlayOneShot(_press, 0.25f); }
        private void PlayRestoration() { if (_ui != null && _restore != null) _ui.PlayOneShot(_restore, 0.35f); }
        private void HandleRelics(int current, int total)
        {
            if (current > _lastRelics) PlayPress();
            _lastRelics = current;
        }

        private static AudioClip MakeCue(string name, float duration, float a, float b)
        {
            const int rate = 24000;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Sin(Mathf.PI * i / samples.Length) * Mathf.Exp(-t * 5f);
                samples[i] = (Mathf.Sin(2f * Mathf.PI * a * t) + Mathf.Sin(2f * Mathf.PI * b * t) * 0.45f) * envelope * 0.22f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (_press != null) Destroy(_press);
            if (_restore != null) Destroy(_restore);
        }
    }
}
