using System;
using System.Collections;
using Echoheart.Core;
using Echoheart.UI;
using Echoheart.World;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.Testing
{
    public sealed class PauseRelicReviewV126 : MonoBehaviour
    {
        [SerializeField] private SessionFlowV13 _flow;
        [SerializeField] private FlowMenuV117 _menus;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool _running;
        private static void Require(bool value, string reason)
        { if (!value) throw new InvalidOperationException("V1.26 pause relic summary: " + reason); }
        private string PauseSummary()
        {
            foreach (Text label in _menus.GetComponentsInChildren<Text>(true))
                if (label.gameObject.activeInHierarchy && !string.IsNullOrEmpty(label.text) &&
                    label.text.StartsWith("Relics  ", StringComparison.Ordinal)) return label.text;
            throw new InvalidOperationException("V1.26 visible pause summary is missing.");
        }
        [ContextMenu("Pause review/Run immediate pickup summary checks (resets run)")]
        private void Run() { if (Application.isPlaying && !_running) StartCoroutine(Check()); }
        private IEnumerator Check()
        {
            _running = true;
            RelicCounterV13 counter = null;
            bool counterWasEnabled = false;
            Action<int, int> counted = null;
            Action<GameState> stateChanged = null;
            try
            {
                Require(_flow != null && _menus != null, "Review bindings missing.");
                _flow.ReturnToTitle(); _flow.StartNewGame(); yield return null;
                Require(_flow.State == GameState.Playing, "New Game did not start.");
                counter = _flow.Relics;
                counterWasEnabled = counter != null && counter.enabled;
                Require(counter != null && counter.Relics != null, "Counter has no pickup bindings.");
                counter.Refresh(true);
                Require(counter.Collected == 0, "New Game did not clear relics.");
                // Hold the ordinary polling gap open; public Refresh still uses the original counter.
                counter.enabled = false;
                HiddenRelicV12 target = null;
                foreach (HiddenRelicV12 relic in counter.Relics)
                {
                    if (relic == null || relic.Collected || !relic.gameObject.activeInHierarchy) continue;
                    Collider trigger = relic.GetComponent<Collider>();
                    if (trigger != null && trigger.enabled) { target = relic; break; }
                }
                Require(target != null, "No available loose relic was found.");
                _flow.Player.Teleport(target.transform.position, Quaternion.identity);
                float deadline = Time.realtimeSinceStartup + 2f;
                while (!target.Collected && _flow.State == GameState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
                Require(target.Collected && _flow.State == GameState.Playing, "Real trigger pickup did not complete safely.");
                Require(counter.Collected == 0, "Fixture did not retain the stale pre-poll count.");

                int countEvents = 0, observed = -1, pauseEvents = 0, countAtPause = -1;
                counted = (value, total) => { countEvents++; observed = value; };
                stateChanged = state => { if (state == GameState.Paused) { pauseEvents++; countAtPause = counter.Collected; } };
                GameEvents.CollectiblesChanged += counted;
                GameEvents.StateChanged += stateChanged;
                _flow.Pause();
                Require(counter.Collected == 1 && observed == 1 && countEvents == 1 && countAtPause == 1,
                    "Pause was published before the latest collectible count.");
                Require(Time.timeScale == 0f && _menus.VisibleState == GameState.Paused && pauseEvents == 1,
                    "Pause state/menu timing changed.");
                Require(PauseSummary().StartsWith("Relics  1 / " + counter.Total, StringComparison.Ordinal),
                    "Visible pause summary retained the stale count.");
                _flow.Pause();
                Require(countEvents == 1 && pauseEvents == 1, "Rejected repeated pause republished a count or state.");
                _flow.Resume(); _flow.Pause();
                Require(counter.Collected == 1 && countEvents == 1 && pauseEvents == 2 && countAtPause == 1,
                    "Unchanged-count pause forced an extra collectible event.");
                Debug.Log("V1.26 immediate-pickup pause summary PASSED using the real trigger and counter with polling temporarily held. Inspect ordinary pickup-to-pause timing and pause/resume manually.", this);
            }
            finally
            {
                if (counted != null) GameEvents.CollectiblesChanged -= counted;
                if (stateChanged != null) GameEvents.StateChanged -= stateChanged;
                if (counter != null) counter.enabled = counterWasEnabled;
                if (_flow != null && Application.isPlaying) _flow.ReturnToTitle();
                _running = false;
            }
        }
#endif
    }
}
