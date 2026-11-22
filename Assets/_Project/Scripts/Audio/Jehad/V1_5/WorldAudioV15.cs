using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Audio
{
    /// <summary>Six region music loops, spatial water, ambient air, and eight reusable world SFX voices.</summary>
    [DefaultExecutionOrder(-140)]
    public sealed class WorldAudioV15 : MonoBehaviour
    {
        [SerializeField] private WorldAudioBankV15 _bank;
        [SerializeField] private MenuAudioV13 _volumes;
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private Transform _player;
        [SerializeField] private Vector3[] _waterLine;
        private readonly EnvironmentMusicV15 _mix = new EnvironmentMusicV15();
        private readonly AudioSource[] _music = new AudioSource[6];
        private readonly AudioSource[] _voices = new AudioSource[8];
        private AudioSource _stinger, _water, _forest, _alpine;
        private GameState _state = GameState.Title;
        private bool _arena;
        private bool _resetting;
        private int _nextVoice;
        public MusicRegionV15 Region => _mix.Region;
        public int VoiceCapacity => _voices.Length;
        public float SfxVolume => _volumes != null ? _volumes.SfxVolume : 0.8f;
        public float MusicVolume => _volumes != null ? _volumes.MusicVolume : 0.65f;

        private AudioSource Source(string label, bool spatial, bool loop)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = spatial ? 1f : 0f;
            source.dopplerLevel = 0f; source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f; source.maxDistance = 32f; source.volume = 0f;
            return source;
        }

        private void Awake()
        {
            if (_bank == null) { Debug.LogError("V1.5 audio bank is missing.", this); enabled = false; return; }
            for (int i = 0; i < _music.Length; i++) { _music[i] = Source("Music " + (MusicRegionV15)i, false, true); _music[i].clip = _bank.Music(i); }
            for (int i = 0; i < _voices.Length; i++) _voices[i] = Source("World SFX " + i, true, false);
            _stinger = Source("Session stingers", false, false);
            _water = Source("Nearest water", true, true); _water.clip = _bank.Water;
            _forest = Source("Forest air", false, true); _forest.clip = _bank.Forest;
            _alpine = Source("Alpine air", false, true); _alpine.clip = _bank.Alpine;
        }

        private void OnEnable()
        {
            GameEvents.StateChanged += HandleState;
            GameEvents.RetryRequested += HandleRetry;
            GameEvents.BossEncounterStarted += HandleArena;
            GameEvents.GameCompleted += HandleComplete;
        }
        private void OnDisable()
        {
            GameEvents.StateChanged -= HandleState;
            GameEvents.RetryRequested -= HandleRetry;
            GameEvents.BossEncounterStarted -= HandleArena;
            GameEvents.GameCompleted -= HandleComplete;
            foreach (AudioSource source in _music) if (source != null) source.Stop();
            StopVoices();
            foreach (AudioSource source in new[] { _water, _forest, _alpine, _stinger }) if (source != null) source.Stop();
        }

        private void HandleState(GameState state)
        {
            GameState previous = _state; _state = state;
            if (state == GameState.Title || state == GameState.Complete) _arena = false;
            if (state == GameState.Paused) { foreach (AudioSource source in _voices) if (source != null) source.Pause(); }
            else if (state == GameState.Playing && previous == GameState.Paused) { foreach (AudioSource source in _voices) if (source != null) source.UnPause(); }
            else if (state != GameState.Playing) StopVoices();
            if (state == GameState.Dead && previous != state) PlayStinger(WorldCueV15.Death);
            _mix.Request(ChooseRegion(), 0f, _resetting || state == GameState.Title || state == GameState.Complete);
            _resetting = false;
        }
        private void HandleRetry(RetryScope scope)
        {
            _resetting = true;
            if (scope != RetryScope.BossArena) _arena = false;
            StopVoices(); if (_stinger != null) _stinger.Stop();
        }
        private void HandleArena() { _arena = true; _mix.Request(MusicRegionV15.Guardian, 0f, true); StopVoices(); }
        private void HandleComplete() => PlayStinger(WorldCueV15.Restore);
        public void EndGuardianMusic()
        {
            // Optional scene hook; leave existing one-shot feedback and crossfade weights intact.
            if (!_arena) return;
            _arena = false;
            _mix.Request(ChooseRegion(), 0f, true);
        }
        private MusicRegionV15 ChooseRegion()
        {
            if (_state == GameState.Title || _state == GameState.Complete || _player == null) return MusicRegionV15.Meadow;
            Vector3 p = _player.position; return EnvironmentMusicV15.Select(p.x, p.y, p.z, _arena);
        }
        private void Update()
        {
            if (_bank == null) return;
            if (_state == GameState.Playing || _state == GameState.Title || _state == GameState.Complete)
                _mix.Request(ChooseRegion(), Time.unscaledDeltaTime, _arena);
            _mix.Advance(Time.unscaledDeltaTime);
            float duck = _state == GameState.Paused || _state == GameState.Dead ? 0.32f : _state == GameState.Complete ? 0.5f : 1f;
            for (int i = 0; i < _music.Length; i++)
            {
                AudioSource source = _music[i]; float weight = _mix.Weight(i);
                if (source == null || source.clip == null) continue;
                source.volume = MusicVolume * 0.7f * Mathf.Sqrt(weight) * duck;
                if (weight > 0.001f && !source.isPlaying)
                {
                    source.time = i == (int)MusicRegionV15.Guardian ? 0f : (float)(AudioSettings.dspTime % source.clip.length);
                    source.Play();
                }
                else if (weight <= 0.001f && source.isPlaying) source.Stop();
            }
            foreach (AudioSource source in _voices) if (source != null) source.volume = SfxVolume * 0.7f;
            if (_stinger != null) _stinger.volume = SfxVolume * 0.7f;
            bool ambient = _state == GameState.Playing && !_arena;
            if (_player != null) _water.transform.position = NearestWater(_player.position);
            Loop(_water, ambient ? SfxVolume * 0.5f : 0f);
            Loop(_forest, ambient ? SfxVolume * 0.24f * (1f - _mix.Weight((int)MusicRegionV15.Alpine)) : 0f);
            Loop(_alpine, ambient ? SfxVolume * 0.45f * _mix.Weight((int)MusicRegionV15.Alpine) : 0f);
        }
        private static void Loop(AudioSource source, float volume)
        {
            if (source == null || source.clip == null) return;
            source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime * 0.5f);
            if (volume > 0.001f && !source.isPlaying) source.Play();
            else if (source.volume <= 0.001f && source.isPlaying) source.Stop();
        }
        private Vector3 NearestWater(Vector3 p)
        {
            Vector3 lake = p - new Vector3(52f, 0f, -3f); lake.y = 0f;
            float radius = Mathf.Sqrt(lake.x * lake.x / 900f + lake.z * lake.z / 1296f);
            Vector3 best = new Vector3(52f, 1.55f, -3f) + lake / Mathf.Max(1f, radius);
            float distance = (best - p).sqrMagnitude;
            if (_waterLine != null) for (int i = 1; i < _waterLine.Length; i++)
            {
                Vector3 a = _waterLine[i - 1], delta = _waterLine[i] - a;
                Vector3 point = a + delta * Mathf.Clamp01(Vector3.Dot(p - a, delta) / Mathf.Max(0.001f, delta.sqrMagnitude));
                float d = (point - p).sqrMagnitude; if (d < distance) { distance = d; best = point; }
            }
            return best;
        }
        public void PlayWorld(WorldCueV15 cue, Vector3 position)
        {
            if (_state != GameState.Playing || _resetting || _bank == null) return;
            AudioClip clip = _bank.Cue(cue); if (clip == null) return;
            AudioSource source = _voices[_nextVoice]; _nextVoice = (_nextVoice + 1) % _voices.Length;
            source.Stop(); source.transform.position = position; source.clip = clip; source.volume = SfxVolume * 0.7f; source.Play();
        }
        private void PlayStinger(WorldCueV15 cue)
        {
            if (_stinger == null || _bank == null || _bank.Cue(cue) == null) return;
            _stinger.Stop(); _stinger.clip = _bank.Cue(cue); _stinger.volume = SfxVolume * 0.7f; _stinger.Play();
        }
        private void StopVoices() { foreach (AudioSource source in _voices) if (source != null) source.Stop(); }
    }
}
