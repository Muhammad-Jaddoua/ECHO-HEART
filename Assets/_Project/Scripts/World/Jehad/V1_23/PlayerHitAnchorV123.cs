using UnityEngine;

namespace Echoheart.World
{
    /// <summary>Opts this scene into local player hurt feedback after a distant recovery.</summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class PlayerHitAnchorV123 : MonoBehaviour
    {
        [SerializeField] private WorldFeedbackV15 _feedback;
        private bool _before, _owns;
        private void OnEnable()
        {
            if (_feedback == null) { Debug.LogError("V1.23 player hurt feedback binding missing.", this); enabled = false; return; }
            _before = _feedback.LocalPlayerHitFeedback;
            _feedback.LocalPlayerHitFeedback = true;
            _owns = true;
        }
        private void OnDisable()
        {
            if (_owns && _feedback != null) _feedback.LocalPlayerHitFeedback = _before;
            _owns = false;
        }
    }
}
