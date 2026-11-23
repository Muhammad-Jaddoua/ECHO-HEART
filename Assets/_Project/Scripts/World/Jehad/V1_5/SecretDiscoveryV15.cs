using Echoheart.Audio;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    public sealed class SecretDiscoveryV15 : MonoBehaviour, IResettable
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private string _secretName;
        [SerializeField] private float _radius = 5f;
        private bool _discovered;
        private void Update()
        {
            if (_discovered || _flow == null || _flow.State != GameState.Playing || _flow.Player.Player == null) return;
            if (Vector3.Distance(_flow.Player.Player.position, transform.position) > _radius) return;
            _discovered = true;
            if (_feedback != null) _feedback.Discover(_secretName, transform.position + Vector3.up, WorldCueV15.Secret, WorldBurstV15.Secret);
        }
        public void ResetFor(RetryScope scope) { if (scope == RetryScope.NewGame) _discovered = false; }
    }
}
