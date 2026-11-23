using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.World;
using UnityEngine;

namespace Echoheart.Testing
{
    public sealed class PlayerHurtReviewV123 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private WorldAudioBankV15 _bank;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private AudioSource[] _voices;
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("V1.23 player hurt feedback: " + reason); }

        private void ClearVoiceClips()
        {
            // Review-only isolation: the normal audio owner still selects and plays each voice.
            foreach (AudioSource voice in _voices)
                if (voice != null && voice.name.StartsWith("World SFX ", StringComparison.Ordinal))
                { voice.Stop(); voice.clip = null; }
        }
        private void RequireHurtAt(Vector3 point)
        {
            AudioClip cue = _bank.Cue(WorldCueV15.PlayerHurt);
            Require(cue != null, "Player-hurt clip is missing.");
            foreach (AudioSource voice in _voices)
                if (voice != null && voice.name.StartsWith("World SFX ", StringComparison.Ordinal) &&
                    voice.clip == cue && (voice.transform.position - point).sqrMagnitude < .0001f) return;
            throw new InvalidOperationException("V1.23 player hurt voice was not positioned at " + point);
        }

        [ContextMenu("Player hurt/Run recovery feedback checks (resets run)")]
        private void Run() { if (Application.isPlaying && !_running) StartCoroutine(Check()); }
        private IEnumerator Check()
        {
            _running = true;
            try
            {
                Require(_flow != null && _feedback != null && _bank != null && _feedback.LocalPlayerHitFeedback,
                    "Scene bindings or the optional local-feedback gate are missing.");
                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return new WaitForSecondsRealtime(.4f);
                _flow.Player.SetControlsEnabled(false);
                Health health = _flow.Player.Health;
                Transform player = _flow.Player.Player;
                Require(health != null && player != null && health.Max > 1, "Review needs a living multi-HP player.");
                _voices = _feedback.Audio.GetComponentsInChildren<AudioSource>(true);

                health.ResetHealth(); ClearVoiceClips();
                Vector3 nearby = player.position + Vector3.up + Vector3.right * .25f;
                Require(health.TryApplyDamage(new DamageInfo(1, nearby, Vector3.zero, Team.Enemy, AttackIds.Next(), gameObject)), "Nearby hit rejected.");
                RequireHurtAt(nearby);

                health.ResetHealth(); ClearVoiceClips();
                Vector3 oldPoint = player.position + Vector3.down * 30f;
                Require(health.TryApplyDamage(new DamageInfo(1, oldPoint, Vector3.zero, Team.Enemy, AttackIds.Next(), gameObject)), "Distant hit rejected.");
                RequireHurtAt(player.position + Vector3.up);

                health.ResetHealth(); ClearVoiceClips();
                Vector3 belowWorld = player.position; belowWorld.y = -20f;
                _flow.Player.Teleport(belowWorld, Quaternion.identity);
                yield return null; // The real SessionFlowV13.Update must perform recovery and one fall hit.
                Require(player.position.y >= -18f && health.Current == health.Max - 1, "Actual fall recovery/damage did not run as expected.");
                RequireHurtAt(player.position + Vector3.up);
                Require(_feedback.Audio.VoiceCapacity == 8, "World voice pool capacity changed.");
                Debug.Log("V1.23 player hurt request positions PASSED: nearby impact, distant impact and real fall recovery. Manually audition sound and inspect hit particles; source positions do not prove audible playback.", this);
            }
            finally
            {
                _voices = null;
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
