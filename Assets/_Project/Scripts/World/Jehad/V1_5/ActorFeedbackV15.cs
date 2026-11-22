using Echoheart.AI;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Reads existing health/removal/state signals. Never changes health, AI, or ability ownership.</summary>
    public sealed class ActorFeedbackV15 : MonoBehaviour
    {
        private WorldFeedbackV15 _feedback;
        private Health _health;
        private EnemyBrain _enemy;
        private EnemyBrain.State _lastState;
        private Renderer[] _renderers;
        private Color[] _baseColors;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private float _flashUntil;
        private bool _flashing;
        public void Bind(WorldFeedbackV15 feedback, Health health, EnemyBrain enemy)
        {
            Unbind(); _feedback = feedback; _health = health; _enemy = enemy;
            _renderers = GetComponentsInChildren<Renderer>(true); _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i].sharedMaterial;
                _baseColors[i] = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
            }
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable() { _lastState = _enemy != null ? _enemy.Current : EnemyBrain.State.Idle; Subscribe(); }
        private void Subscribe()
        {
            if (_health != null) { _health.Damaged -= HandleHit; _health.Damaged += HandleHit; _health.Changed -= HandleHealth; _health.Changed += HandleHealth; }
            if (_enemy != null) { _enemy.Removed -= HandleRemoved; _enemy.Removed += HandleRemoved; }
        }
        private void Unbind()
        {
            if (_health != null) { _health.Damaged -= HandleHit; _health.Changed -= HandleHealth; }
            if (_enemy != null) _enemy.Removed -= HandleRemoved;
        }
        private void OnDisable() { Unbind(); Restore(); }
        private void HandleHealth(int current, int maximum) { if (current == maximum) Restore(); }
        private void HandleHit(DamageInfo hit)
        {
            if (_feedback == null || !_feedback.Playing || (_enemy != null && _enemy.Current == EnemyBrain.State.BeingInhaled)) return;
            Vector3 point = hit.Point.sqrMagnitude > 0.001f ? hit.Point : transform.position + Vector3.up;
            _feedback.Audio.PlayWorld(_health.Team == Team.Player ? WorldCueV15.PlayerHurt : WorldCueV15.Hit, point);
            _feedback.Particles.Burst(WorldBurstV15.Hit, point);
            _flashUntil = Time.time + 0.12f; _flashing = true; Tint(new Color(1f, 0.88f, 0.65f));
        }
        private void HandleRemoved(EnemyBrain enemy)
        {
            if (_feedback == null || !_feedback.Playing) return;
            bool defeated = _health != null && _health.IsDead;
            _feedback.Audio.PlayWorld(defeated ? WorldCueV15.Defeat : WorldCueV15.Capture, transform.position + Vector3.up);
            _feedback.Particles.Burst(defeated ? WorldBurstV15.Hit : WorldBurstV15.Capture, transform.position + Vector3.up);
        }
        private void Update()
        {
            if (_flashing && Time.time >= _flashUntil) Restore();
            if (_enemy == null || _feedback == null) return;
            if (_feedback.Playing && _enemy.enabled && _enemy.Current == EnemyBrain.State.Attack && _lastState != EnemyBrain.State.Attack)
                _feedback.Audio.PlayWorld(WorldCueV15.EnemyWarning, transform.position + Vector3.up);
            _lastState = _enemy.Current;
        }
        private void Tint(Color color)
        {
            if (_renderers == null) return;
            foreach (Renderer renderer in _renderers) if (renderer != null) { renderer.GetPropertyBlock(_block); _block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(_block); }
        }
        private void Restore()
        {
            if (_renderers != null) for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null)
            { _renderers[i].GetPropertyBlock(_block); _block.SetColor("_BaseColor", _baseColors[i]); _renderers[i].SetPropertyBlock(_block); }
            _flashing = false;
        }
    }
}
