#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Reflection;
using Echoheart.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echoheart.UI
{
    /// <summary>
    /// Edit-mode self test for BossHealthBar. Awake/OnEnable do not run in edit mode, so they are invoked by
    /// reflection. Test-only, never in a scene.
    /// </summary>
    public class BossHealthBarSelfTest : MonoBehaviour
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [SerializeField] private bool _runOnStart;

        private int _passed;
        private int _failed;

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Boss Health Bar Self Test")]
        public void Run()
        {
            _passed = 0;
            _failed = 0;
            GameObject go = new GameObject("BossHealthBarTest") { hideFlags = HideFlags.HideAndDontSave };
            BossHealthBar bar = null;
            try
            {
                CanvasGroup group = go.AddComponent<CanvasGroup>();
                Image fill = NewImage(go, "Fill");
                Image ghost = NewImage(go, "Ghost");
                bar = go.AddComponent<BossHealthBar>();
                Set(bar, "_group", group);
                Set(bar, "_fill", fill);
                Set(bar, "_ghost", ghost);
                Set(bar, "_hideDelayAfterDefeat", 0f);
                Set(bar, "_label", NewLabel(go));
                Call(bar, "Awake");
                Call(bar, "OnEnable");

                Check("hidden before encounter", !bar.IsVisible && group.alpha == 0f);

                GameEvents.RaiseBossEncounterStarted();
                Check("shown after BossEncounterStarted", bar.IsVisible && group.alpha == 1f);

                GameEvents.RaiseBossHealthChanged(7, 14);
                Check("fill (scale fallback) equals 7/14 = 0.5", Mathf.Approximately(fill.rectTransform.localScale.x, 0.5f)
                    && Mathf.Approximately(bar.FillValue, 0.5f));

                fill.type = Image.Type.Filled;
                GameEvents.RaiseBossHealthChanged(7, 14);
                GameEvents.RaiseBossHealthChanged(7, 14);
                GameEvents.RaiseBossHealthChanged(14, 14);
                GameEvents.RaiseBossHealthChanged(7, 14);
                Check("Filled image: fillAmount equals 0.5", Mathf.Approximately(fill.fillAmount, 0.5f));

                GameEvents.RaiseBossHealthChanged(14, 14);
                Check("ghost never below fill after heal", ghost.rectTransform.localScale.x >= 1f - 0.0001f || ghost.fillAmount >= 1f - 0.0001f);

                GameEvents.RaiseBossDefeated();
                Check("hidden after BossDefeated", !bar.IsVisible && group.alpha == 0f);

                GameEvents.RaiseBossEncounterStarted();
                GameEvents.RaisePlayerDied();
                Check("hidden after PlayerDied", !bar.IsVisible);

                GameEvents.RaiseBossEncounterStarted();
                GameEvents.RaiseRetryRequested(RetryScope.BossArena);
                Check("hidden after retry (BossArena)", !bar.IsVisible && Mathf.Approximately(bar.FillValue, 1f));

                GameEvents.RaiseBossEncounterStarted();
                GameEvents.RaiseStateChanged(GameState.Title);
                Check("hidden after Title state", !bar.IsVisible);

                Call(bar, "OnDisable");
                GameEvents.RaiseBossEncounterStarted();
                Check("no reaction after OnDisable (unsubscribed)", !bar.IsVisible);
            }
            finally
            {
                if (bar != null) Call(bar, "OnDisable");
                DestroyImmediate(go);
            }

            string verdict = _failed == 0 ? "ALL PASS" : "FAILURES";
            Debug.Log($"[BossHealthBarSelfTest] {verdict}: {_passed} passed, {_failed} failed");
        }

        private static Image NewImage(GameObject parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<Image>();
        }

        private static TMP_Text NewLabel(GameObject parent)
        {
            GameObject child = new GameObject("Label", typeof(RectTransform));
            child.hideFlags = HideFlags.HideAndDontSave;
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<TextMeshProUGUI>();
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Any).SetValue(target, value);

        private static void Call(object target, string method) =>
            target.GetType().GetMethod(method, Any).Invoke(target, null);

        private void Check(string name, bool ok)
        {
            if (ok)
            {
                _passed++;
                Debug.Log($"[BossHealthBarSelfTest] PASS {name}");
            }
            else
            {
                _failed++;
                Debug.LogError($"[BossHealthBarSelfTest] FAIL {name}");
            }
        }
    }
}
#endif
