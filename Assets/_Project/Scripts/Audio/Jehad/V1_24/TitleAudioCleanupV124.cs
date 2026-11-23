using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Audio
{
    /// <summary>Stops the old death/restoration cue when this scene returns to title.</summary>
    [DefaultExecutionOrder(115)]
    [DisallowMultipleComponent]
    public sealed class TitleAudioCleanupV124 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldAudioV15 _audio;
        private void OnEnable()
        {
            if (_flow == null || _audio == null)
            { Debug.LogError("V1.24 title audio bindings missing.", this); enabled = false; return; }
            GameEvents.StateChanged += StateChanged;
            StateChanged(_flow.State);
        }
        private void OnDisable() => GameEvents.StateChanged -= StateChanged;
        private void StateChanged(GameState state)
        {
            if (state == GameState.Title && _audio != null) _audio.StopSessionStinger();
        }
    }
}
