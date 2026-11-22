using Echoheart.Audio;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Echoheart.World
{
    public sealed class SecretChestV15 : MonoBehaviour, IResettable
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private HiddenRelicV12 _reward;
        [SerializeField] private Transform _lid;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private string _secretName;
        private readonly ChestStateV15 _policy = new ChestStateV15();
        private Collider _rewardTrigger;
        private Renderer[] _rewardRenderers;
        private float _animation;
        private bool _prompt;
        public bool Opened => _policy.Opened;
        public HiddenRelicV12 Reward => _reward;

        private void Awake()
        {
            if (_reward == null || _lid == null) { Debug.LogError("V1.5 chest reward/lid reference missing.", this); enabled = false; return; }
            _rewardTrigger = _reward.GetComponent<Collider>(); _rewardRenderers = _reward.GetComponentsInChildren<Renderer>(true);
            // These props sit outside the source prefab hierarchy used by the existing navigation builder.
            var obstacle = gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, 0.45f, 0f); obstacle.size = new Vector3(1.65f, 0.9f, 1.1f);
            obstacle.carving = true;
            RevealReward();
        }
        private bool Near => _flow != null && _flow.Player != null && _flow.Player.Player != null &&
            Vector3.Distance(_flow.Player.Player.position, transform.position + Vector3.up) <= 2.8f;
        private void Update()
        {
            _animation = Mathf.MoveTowards(_animation, Opened ? 1f : 0f, Time.deltaTime * 2f);
            if (_lid != null) _lid.localRotation = Quaternion.Euler(Mathf.SmoothStep(0f, 105f, _animation), 0f, 0f);
            bool prompt = !Opened && Near && _flow.State == GameState.Playing;
            if (prompt != _prompt) { _prompt = prompt; GameEvents.RaisePromptChanged(prompt ? "E  Open secret chest" : string.Empty); }
            if (prompt && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryOpen();
        }
        public bool TryOpen()
        {
            if (_flow == null || !_policy.TryOpen(_flow.State, Near)) return false;
            RevealReward();
            if (_prompt) { _prompt = false; GameEvents.RaisePromptChanged(string.Empty); }
            if (_feedback != null) _feedback.Discover(_secretName, transform.position + Vector3.up, WorldCueV15.Chest, WorldBurstV15.Chest);
            return true;
        }
        private void RevealReward()
        {
            bool visible = Opened && _reward != null && !_reward.Collected;
            if (_rewardTrigger != null) _rewardTrigger.enabled = visible;
            if (_rewardRenderers != null) foreach (Renderer renderer in _rewardRenderers) if (renderer != null) renderer.enabled = visible;
        }
        public void ResetFor(RetryScope scope)
        {
            _policy.ResetFor(scope);
            if (scope == RetryScope.NewGame) { _animation = 0f; if (_lid != null) _lid.localRotation = Quaternion.identity; RevealReward(); }
            if (_prompt) GameEvents.RaisePromptChanged(string.Empty);
            _prompt = false;
        }
        private void OnDisable() { if (_prompt) GameEvents.RaisePromptChanged(string.Empty); _prompt = false; }
    }
}
