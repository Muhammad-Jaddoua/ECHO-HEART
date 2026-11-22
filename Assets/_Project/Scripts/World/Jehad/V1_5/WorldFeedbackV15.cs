using System.Collections.Generic;
using Echoheart.AI;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.UI;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Scene-owned presentation coordinator; uses existing contracts without awarding items or damage.</summary>
    public sealed class WorldFeedbackV15 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldAudioV15 _audio;
        [SerializeField] private WorldParticlesV15 _particles;
        [SerializeField] private WorldToastV15 _toast;
        [SerializeField] private RelicCounterV13 _relics;
        [SerializeField] private EnemyPopulationV14 _population;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private Material _warningMaterial;
        [SerializeField] private Transform _heart;
        private readonly HashSet<Health> _bound = new HashSet<Health>();
        private bool[] _collected;
        private CharacterController _controller;
        private Vector3 _lastPlayer;
        private float _walked, _nextStep, _movementAfter;
        private bool _grounded, _refresh;
        private BossBrain _presentedBoss;
        public bool Playing => _flow != null && _flow.State == GameState.Playing;
        public WorldAudioV15 Audio => _audio;
        public WorldParticlesV15 Particles => _particles;
        public bool ExternalAirborneCues { get; set; }

        private void OnEnable()
        {
            GameEvents.RetryRequested += HandleRetry;
            GameEvents.AbilityChanged += HandlePower;
            GameEvents.BossDefeated += HandleBossDefeated;
            GameEvents.GameCompleted += HandleComplete;
        }
        private void OnDisable()
        {
            GameEvents.RetryRequested -= HandleRetry;
            GameEvents.AbilityChanged -= HandlePower;
            GameEvents.BossDefeated -= HandleBossDefeated;
            GameEvents.GameCompleted -= HandleComplete;
        }
        private void Start()
        {
            if (_flow == null || _audio == null || _particles == null || _relics == null)
            { Debug.LogError("V1.5 feedback references are missing.", this); enabled = false; return; }
            _collected = new bool[_relics.Total];
            if (_flow.Player.Player != null) _controller = _flow.Player.Player.GetComponent<CharacterController>();
            Bind(_flow.Player.Health, null); _refresh = true;
        }
        private void Bind(Health health, EnemyBrain enemy)
        {
            if (health == null || !_bound.Add(health)) return;
            ActorFeedbackV15 presenter = health.gameObject.AddComponent<ActorFeedbackV15>(); presenter.Bind(this, health, enemy);
        }
        private void HandleRetry(RetryScope scope) { _refresh = true; _walked = 0f; _movementAfter = Time.time + 0.3f; }
        private void HandlePower(AbilityDefinition ability)
        {
            if (Playing && ability != null && !_refresh) _audio.PlayWorld(WorldCueV15.Power, _flow.Player.Player.position + Vector3.up);
        }
        private void HandleBossDefeated()
        {
            if (_arena != null && _arena.Boss != null)
            { _audio.PlayWorld(WorldCueV15.Defeat, _arena.Boss.transform.position); _particles.Burst(WorldBurstV15.Capture, _arena.Boss.transform.position + Vector3.up); }
        }
        private void HandleComplete() { if (_heart != null) _particles.Burst(WorldBurstV15.Restore, _heart.position); }
        private void LateUpdate()
        {
            if (_collected == null) return;
            foreach (EnemySpawner source in _population.Spawners)
                if (source != null && source.Current != null) Bind(source.Current.GetComponent<Health>(), source.Current);
            if (_arena != null && _arena.Boss != null && _presentedBoss != _arena.Boss)
            {
                _presentedBoss = _arena.Boss; Bind(_presentedBoss.GetComponent<Health>(), null);
                var presenter = _presentedBoss.gameObject.AddComponent<GuardianFeedbackV15>(); presenter.Bind(_presentedBoss, this, _warningMaterial);
            }
            for (int i = 0; i < _collected.Length; i++)
            {
                HiddenRelicV12 relic = _relics.Relics[i]; if (relic == null) continue;
                if (!_refresh && Playing && relic.Collected && !_collected[i])
                {
                    _audio.PlayWorld(WorldCueV15.Relic, relic.transform.position);
                    _particles.Burst(WorldBurstV15.Relic, relic.transform.position);
                    if (_toast != null) _toast.Show("Relic found  ·  " + Friendly(relic.DisplayName));
                }
                _collected[i] = relic.Collected;
            }
            if (_flow.Player.Player != null)
            {
                Vector3 position = _flow.Player.Player.position;
                float distance = new Vector2(position.x - _lastPlayer.x, position.z - _lastPlayer.z).magnitude;
                bool grounded = _controller != null && _controller.isGrounded;
                if (Playing && !_refresh && Time.time >= _movementAfter && distance < 8f)
                {
                    if (!ExternalAirborneCues)
                    {
                        if (!grounded && _grounded) _audio.PlayWorld(WorldCueV15.Jump, position);
                        if (grounded && !_grounded) { _audio.PlayWorld(WorldCueV15.Land, position); _particles.Burst(WorldBurstV15.Dust, position - Vector3.up * 0.7f); }
                    }
                    if (grounded)
                    {
                        _walked += distance;
                        if (_walked >= 1.7f && Time.time >= _nextStep)
                        {
                            WorldCueV15 cue = _audio.Region == MusicRegionV15.Lake && position.y < 3.2f ? WorldCueV15.StepWater :
                                _audio.Region == MusicRegionV15.Alpine ? WorldCueV15.StepStone : WorldCueV15.StepGrass;
                            _audio.PlayWorld(cue, position); _particles.Burst(WorldBurstV15.Dust, position - Vector3.up * 0.7f);
                            _walked = 0f; _nextStep = Time.time + 0.24f;
                        }
                    }
                }
                _lastPlayer = position; _grounded = grounded;
            }
            _refresh = false;
        }
        public void Discover(string name, Vector3 position, WorldCueV15 sound, WorldBurstV15 burst)
        {
            if (!Playing) return;
            _audio.PlayWorld(sound, position); _particles.Burst(burst, position);
            if (_toast != null) _toast.Show("Secret discovered  ·  " + name);
        }
        private static string Friendly(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Hidden relic";
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++) { if (i > 0 && char.IsUpper(value[i])) result.Append(' '); result.Append(value[i]); }
            return result.ToString();
        }
        // Bind to actual successful SwordAttack._onSwing / ProjectileAttack._onFired hooks once the player exists.
        public void SwordUsed() => AbilityUsed(AbilityType.Sword);
        public void RangedUsed() { if (_flow.Player.CurrentAbility != null) AbilityUsed(_flow.Player.CurrentAbility.Type); }
        private void AbilityUsed(AbilityType type)
        {
            if (!Playing || _flow.Player.Player == null) return;
            Vector3 position = _flow.Player.Player.position + Vector3.up;
            _audio.PlayWorld(type == AbilityType.Sword ? WorldCueV15.Sword : type == AbilityType.Bow ? WorldCueV15.Bow : WorldCueV15.Fire, position);
            _particles.Burst(type == AbilityType.Fire ? WorldBurstV15.Hit : WorldBurstV15.Capture, position + _flow.Player.Player.forward);
        }
    }
}
