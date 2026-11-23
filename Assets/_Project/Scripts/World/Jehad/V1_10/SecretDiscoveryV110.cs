using Echoheart.Audio;
using Echoheart.Core;
using UnityEngine;
using Echoheart.UI;

namespace Echoheart.World
{
    public sealed class SecretDiscoveryV110 : MonoBehaviour, IResettable
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private string _secretName;
        [SerializeField] private float _radius = 5f;
        [SerializeField] private WorldToastV110 _toast;
        private bool _discovered;
        private void Update()
        {
            if (_discovered || _flow == null || _flow.State != GameState.Playing || _flow.Player.Player == null) return;
            if (Vector3.Distance(_flow.Player.Player.position, transform.position) > _radius) return;
            _discovered = true;
            if (_feedback != null) _feedback.Discover(_secretName, transform.position + Vector3.up, WorldCueV15.Secret, WorldBurstV15.Secret);
            if (_toast != null) _toast.Show(new ToastNoticeV110("secret:" + _secretName, ToastKindV110.Secret, "SECRET DISCOVERED", _secretName));
        }
        public void ResetFor(RetryScope scope) { if (scope == RetryScope.NewGame) _discovered = false; }
    }
}
