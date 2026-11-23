using Echoheart.Audio;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoheart.World
{
    public enum WorldBurstV15 { Relic, Chest, Secret, Capture, Hit, Dust, Restore }

    [DefaultExecutionOrder(-130)]
    public sealed class WorldParticlesV15 : MonoBehaviour
    {
        [SerializeField] private Material _material;
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldAudioV15 _audio;
        [SerializeField] private Transform _player;
        private readonly ParticleSystem[] _pool = new ParticleSystem[12];
        private ParticleSystem _ambient;
        private int _next;
        public int BurstCapacity => _pool.Length;

        private ParticleSystem Create(string label, int maximum)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.playOnAwake = false; main.loop = false; main.duration = 1.5f;
            main.maxParticles = maximum; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.22f;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }); color.color = gradient;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return ps;
        }
        private void Awake()
        {
            for (int i = 0; i < _pool.Length; i++) _pool[i] = Create("Burst " + i, 64);
            _ambient = Create("Local environment motes", 64);
            var main = _ambient.main; main.loop = true; main.startLifetime = 6f; main.startSpeed = 0.1f;
            var shape = _ambient.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(18f, 5f, 18f);
            var emission = _ambient.emission; emission.enabled = true; emission.rateOverTime = 5f;
        }
        private void OnEnable() { GameEvents.RetryRequested += ClearForRetry; GameEvents.StateChanged += HandleState; }
        private void OnDisable() { GameEvents.RetryRequested -= ClearForRetry; GameEvents.StateChanged -= HandleState; Clear(); }
        private void ClearForRetry(RetryScope scope) => Clear();
        private void HandleState(GameState state) { if (state == GameState.Title || state == GameState.Complete || state == GameState.Dead) Clear(); }
        private void Update()
        {
            if (_ambient == null || _player == null || _audio == null || _flow == null) return;
            bool show = _flow.State == GameState.Playing && _audio.Region != MusicRegionV15.Guardian;
            if (!show) { if (_ambient.isPlaying) _ambient.Stop(false, ParticleSystemStopBehavior.StopEmitting); return; }
            _ambient.transform.position = _player.position + Vector3.up * 2f;
            var main = _ambient.main;
            main.startColor = _audio.Region == MusicRegionV15.Alpine ? new Color(0.85f, 0.94f, 1f, 0.6f) :
                _audio.Region == MusicRegionV15.Amber ? new Color(1f, 0.67f, 0.25f, 0.5f) : new Color(0.70f, 1f, 0.79f, 0.5f);
            main.startSize = _audio.Region == MusicRegionV15.Alpine ? 0.055f : 0.035f;
            if (!_ambient.isPlaying) _ambient.Play();
        }
        public void Burst(WorldBurstV15 kind, Vector3 position)
        {
            if (_flow == null || (_flow.State != GameState.Playing && kind != WorldBurstV15.Restore)) return;
            ParticleSystem ps = _pool[_next]; _next = (_next + 1) % _pool.Length;
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); ps.transform.position = position;
            var main = ps.main; main.useUnscaledTime = kind == WorldBurstV15.Restore;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, kind == WorldBurstV15.Restore ? 1.5f : 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, kind == WorldBurstV15.Restore ? 0.2f : 0.13f);
            main.startSpeed = kind == WorldBurstV15.Dust ? 0.4f : kind == WorldBurstV15.Restore ? 3.5f : 1.8f;
            main.gravityModifier = kind == WorldBurstV15.Dust ? 0f : 0.15f;
            main.startColor = kind == WorldBurstV15.Hit ? new Color(1f, 0.53f, 0.38f) :
                kind == WorldBurstV15.Capture || kind == WorldBurstV15.Restore ? new Color(0.4f, 1f, 0.78f) :
                kind == WorldBurstV15.Dust ? new Color(0.8f, 0.81f, 0.65f, 0.5f) : new Color(1f, 0.83f, 0.34f);
            ps.Play(); ps.Emit(kind == WorldBurstV15.Dust ? 3 : kind == WorldBurstV15.Restore ? 56 : 22);
        }
        public void Clear()
        {
            foreach (ParticleSystem ps in _pool) if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_ambient != null) _ambient.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
