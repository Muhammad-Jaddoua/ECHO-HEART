using System;
using System.Collections;
using Echoheart.Audio;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.Testing
{
    /// <summary>Native closeout regression for immediate-pause counts and deferred pickup feedback.</summary>
    public sealed class JehadFinalReviewV127 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV117 _menus;
        [SerializeField] private WorldFeedbackV15 _feedback;
        [SerializeField] private WorldAudioBankV15 _bank;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private AudioSource[] _voices;
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("V1.27 final pickup/pause review: " + reason); }
        private string PauseSummary()
        {
            foreach (Text label in _menus.GetComponentsInChildren<Text>(true))
                if (label.gameObject.activeInHierarchy && !string.IsNullOrEmpty(label.text) &&
                    label.text.StartsWith("Relics  ", StringComparison.Ordinal)) return label.text;
            throw new InvalidOperationException("V1.27 visible pause summary is missing.");
        }
        private void ClearRelicVoices()
        {
            foreach (AudioSource voice in _voices)
                if (voice != null && voice.name.StartsWith("World SFX ", StringComparison.Ordinal) && voice.clip == _bank.Cue(WorldCueV15.Relic))
                { voice.Stop(); voice.clip = null; }
        }
        private int RelicVoiceBindings()
        {
            int count = 0;
            foreach (AudioSource voice in _voices)
                if (voice != null && voice.name.StartsWith("World SFX ", StringComparison.Ordinal) && voice.clip == _bank.Cue(WorldCueV15.Relic)) count++;
            return count;
        }
        [ContextMenu("Final review/Run pickup pause feedback checks (resets run)")]
        private void Run() { if (Application.isPlaying && !_running) StartCoroutine(Check()); }
        private IEnumerator Check()
        {
            _running = true;
            RelicCounterV13 counter = null;
            bool counterWasEnabled = false;
            bool feedbackWasEnabled = _feedback != null && _feedback.enabled;
            Action<int, int> counted = null;
            Action<GameState> stateChanged = null;
            try
            {
                Require(_flow != null && _menus != null && _feedback != null && _bank != null, "Bindings missing.");
                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return new WaitForSecondsRealtime(.35f);
                Require(_flow.State == GameState.Playing && _feedback.enabled, "New Game or the original feedback owner is not ready.");
                Require(_bank.Cue(WorldCueV15.Relic) != null, "Relic sound is missing.");
                counter = _flow.Relics;
                counterWasEnabled = counter != null && counter.enabled;
                Require(counter != null && counter.Relics != null, "Counter has no pickups.");
                counter.Refresh(true); Require(counter.Collected == 0, "New Game retained relics.");
                _voices = _feedback.Audio.GetComponentsInChildren<AudioSource>(true);
                ClearRelicVoices();
                // Keep both ordinary polling/presentation gaps open while the actual trigger collects.
                counter.enabled = false; _feedback.enabled = false;
                HiddenRelicV12 target = null;
                foreach (HiddenRelicV12 relic in counter.Relics)
                {
                    if (relic == null || relic.Collected || !relic.gameObject.activeInHierarchy) continue;
                    Collider trigger = relic.GetComponent<Collider>();
                    if (trigger != null && trigger.enabled) { target = relic; break; }
                }
                Require(target != null, "No available loose relic.");
                _flow.Player.Teleport(target.transform.position, Quaternion.identity);
                float deadline = Time.realtimeSinceStartup + 2f;
                while (!target.Collected && _flow.State == GameState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
                Require(target.Collected && _flow.State == GameState.Playing && counter.Collected == 0,
                    "Real pickup did not leave the expected pre-poll gap.");
                int countEvents = 0, observed = -1, pauseEvents = 0, countAtPause = -1;
                counted = (value, total) => { countEvents++; observed = value; };
                stateChanged = state => { if (state == GameState.Paused) { pauseEvents++; countAtPause = counter.Collected; } };
                GameEvents.CollectiblesChanged += counted; GameEvents.StateChanged += stateChanged;
                _flow.Pause();
                Require(counter.Collected == 1 && observed == 1 && countEvents == 1 && countAtPause == 1,
                    "Pause summary was published with a stale count.");
                Require(Time.timeScale == 0f && _menus.VisibleState == GameState.Paused &&
                    PauseSummary().StartsWith("Relics  1 / " + counter.Total, StringComparison.Ordinal), "Visible pause state/count is wrong.");
                _flow.Pause(); Require(countEvents == 1 && pauseEvents == 1, "Rejected pause republished events.");
                _feedback.enabled = true;
                yield return null; yield return null; // Give paused LateUpdate a chance to consume the pending flag.
                Require(RelicVoiceBindings() == 0, "Pickup feedback was requested while paused.");
                _flow.Resume(); yield return null;
                Require(RelicVoiceBindings() == 1, "Pending pickup sound was lost or duplicated on resume.");
                ClearRelicVoices();
                _flow.Pause(); yield return null; _flow.Resume(); yield return null;
                Require(RelicVoiceBindings() == 0, "Completed pickup feedback replayed on another resume.");
                Require(countEvents == 1 && pauseEvents == 2 && countAtPause == 1 && _feedback.Audio.VoiceCapacity == 8,
                    "Unchanged pause count or original voice pool changed.");
                Debug.Log("V1.27 pickup/pause closeout review PASSED for the actual trigger, current pause summary and deferred single sound request. Audition sound/particles and complete the acceptance matrix separately.", this);
            }
            finally
            {
                if (counted != null) GameEvents.CollectiblesChanged -= counted;
                if (stateChanged != null) GameEvents.StateChanged -= stateChanged;
                if (counter != null) counter.enabled = counterWasEnabled;
                if (_feedback != null) _feedback.enabled = feedbackWasEnabled;
                _voices = null;
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
