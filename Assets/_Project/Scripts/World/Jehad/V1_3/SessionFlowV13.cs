using System;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Scene-owned game state, ordered retry transactions, and completion authority.</summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public sealed class SessionFlowV13 : MonoBehaviour
    {
        [SerializeField] private PlayerFlowBindingV13 _player;
        [SerializeField] private RetryServiceV13 _retry;
        [SerializeField] private RelicCounterV13 _relics;
        [SerializeField] private ArenaEncounterV13 _arena;
        [SerializeField] private Transform _mainRetryPoint;
        [SerializeField] private Transform _bossRetryPoint;
        [SerializeField] private Transform _camera;
        [SerializeField] private bool _lockCursorWhilePlaying;
        [SerializeField] private Vector3 _titlePosition = new Vector3(101f, 25f, -60f);
        [SerializeField] private Vector3 _titleLookAt = new Vector3(35f, 10f, 8f);
        private readonly SessionPolicyV13 _policy = new SessionPolicyV13();
        private bool _transitioning;
        private bool _ready;
        private CursorLockMode _originalCursorLock;
        private bool _originalCursorVisible;

        public GameState State => _policy.State;
        public RetryScope CurrentRetryScope => _policy.RetryScope == RetryScope.BossArena ? RetryScope.BossArena : RetryScope.MainArea;
        public bool HeartUnlocked => _policy.HeartUnlocked;
        public float ElapsedSeconds { get; private set; }
        public PlayerFlowBindingV13 Player => _player;
        public RelicCounterV13 Relics => _relics;

        private void Awake()
        {
            _originalCursorLock = Cursor.lockState;
            _originalCursorVisible = Cursor.visible;
        }

        private void OnEnable()
        {
            GameEvents.PlayerDied += HandlePlayerDied;
            GameEvents.BossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            GameEvents.PlayerDied -= HandlePlayerDied;
            GameEvents.BossDefeated -= HandleBossDefeated;
        }

        private void Start()
        {
            if (_player == null || _retry == null || _mainRetryPoint == null || _bossRetryPoint == null)
            {
                Debug.LogError("V1.3 flow is missing its player, retry service or retry points.", this);
                enabled = false;
                return;
            }
            _ready = true;
            PublishState();
        }

        private void Update()
        {
            if (State != GameState.Playing || _transitioning) return;
            ElapsedSeconds += Time.deltaTime;
            if (_player.Player != null && _player.Player.position.y < -18f)
            {
                Vector3 fallPoint = _player.Player.position;
                Transform safe = CurrentRetryScope == RetryScope.BossArena ? _bossRetryPoint : _mainRetryPoint;
                _player.Teleport(safe.position, safe.rotation);
                if (_player.Health != null)
                    _player.Health.TryApplyDamage(new DamageInfo(1, fallPoint, Vector3.zero, Team.Enemy, 0, gameObject));
            }
        }

        public void StartNewGame()
        {
            if (!_ready || _transitioning || !_policy.StartNewGame()) return;
            ElapsedSeconds = 0f;
            ResetSession(RetryScope.NewGame);
        }

        public void Pause()
        {
            if (_ready && !_transitioning && _policy.Pause()) PublishState();
        }

        public void Resume()
        {
            if (_ready && !_transitioning && _policy.Resume()) PublishState();
        }

        public void Retry()
        {
            if (!_ready || _transitioning || !_policy.Retry()) return;
            ResetSession(CurrentRetryScope);
        }

        public void ReturnToTitle()
        {
            if (_ready && !_transitioning && _policy.ReturnToTitle())
            {
                GameEvents.RaisePromptChanged(string.Empty);
                PublishState();
            }
        }

        public bool EnterArena()
        {
            return _ready && !_transitioning && _policy.EnterArena();
        }

        public bool CompleteFromHeart()
        {
            if (!_ready || _transitioning || !_policy.Complete()) return false;
            _transitioning = true;
            try
            {
                GameEvents.RaisePromptChanged(string.Empty);
                if (_relics != null) _relics.Refresh(true);
                // Commit before notifications; a listener cannot start a new run midway through completion.
                PublishState();
                GameEvents.RaiseGameCompleted();
            }
            finally { _transitioning = false; }
            return true;
        }

        private void HandlePlayerDied()
        {
            if (_ready && !_transitioning && _policy.Die())
            {
                GameEvents.RaisePromptChanged(string.Empty);
                PublishState();
            }
        }

        private void HandleBossDefeated()
        {
            if (!_transitioning) _policy.DefeatBoss();
        }

        private void ResetSession(RetryScope scope)
        {
            _transitioning = true;
            _player.SetControlsEnabled(false);
            Time.timeScale = 0f;
            AbilityDefinition retained = SessionPolicyV13.ClearsAbility(scope) ? null : _player.CurrentAbility;
            try
            {
                GameEvents.RaiseRetryRequested(scope);
                GameEvents.RaisePromptChanged(string.Empty);
                _retry.ResetWorld(scope);
                _player.RestoreHealth();
                _player.ApplyAbility(retained);
                Transform point = scope == RetryScope.BossArena ? _bossRetryPoint : _mainRetryPoint;
                _player.Teleport(point.position, point.rotation);
                if (_relics != null) _relics.Refresh(true);
                if (_arena != null) _arena.AfterReset(scope);
            }
            catch (Exception error)
            {
                Debug.LogException(error, this);
                _policy.ReturnToTitle();
            }
            finally
            {
                _transitioning = false;
                PublishState();
            }
        }

        private void PublishState()
        {
            bool playing = State == GameState.Playing;
            Time.timeScale = playing ? 1f : 0f;
            _player.SetControlsEnabled(playing);
            Cursor.lockState = playing && _lockCursorWhilePlaying ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing || !_lockCursorWhilePlaying;
            if (State == GameState.Title && _camera != null)
            {
                _camera.position = _titlePosition;
                _camera.rotation = Quaternion.LookRotation(_titleLookAt - _titlePosition);
            }
            GameEvents.RaiseStateChanged(State);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isEditor) Pause();
        }

        private void OnDestroy()
        {
            if (!_ready) return;
            Time.timeScale = 1f;
            Cursor.lockState = _originalCursorLock;
            Cursor.visible = _originalCursorVisible;
        }
    }
}
