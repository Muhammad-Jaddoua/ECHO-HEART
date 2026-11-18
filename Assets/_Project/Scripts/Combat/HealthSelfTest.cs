#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Reflection;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Combat
{
    /// <summary>
    /// Self-checking test for Health and HealthEventRelay. Runs in edit mode or play mode via the
    /// context menu. Reflection is confined to this class so Health keeps its private serialized
    /// fields and gains no test-only API.
    /// </summary>
    public class HealthSelfTest : MonoBehaviour
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [SerializeField] private bool _runOnStart;

        private int _passed;
        private int _failed;
        private readonly List<GameObject> _temp = new List<GameObject>();

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Health Self Test")]
        public void Run()
        {
            _passed = 0;
            _failed = 0;
            try
            {
                CaseSameIdTwice();
                CaseIdZeroAlwaysApplies();
                CaseSameTeamIgnored();
                CaseNonPositiveIgnored();
                CaseDiedOnceThenRejected();
                CaseInvulnerabilityWindow();
                CaseIdMemoryEviction();
                CaseResetClearsMemory();
                CaseRelayBossDefeatedOnce();
                CaseRelayPlayerDiedOnce();
            }
            finally
            {
                foreach (var go in _temp)
                    if (go != null) DestroyImmediate(go);
                _temp.Clear();
            }
            Debug.Log($"[HealthSelfTest] {(_failed == 0 ? "ALL PASS" : "FAILURES")}: {_passed} passed, {_failed} failed");
        }

        private Health MakeHealth(Team team, int max, float invuln, out GameObject go)
        {
            go = new GameObject("HealthSelfTest_Temp") { hideFlags = HideFlags.HideAndDontSave };
            _temp.Add(go);
            var h = go.AddComponent<Health>();
            Set(h, "_team", team);
            Set(h, "_maxHealth", max);
            Set(h, "_invulnerabilitySeconds", invuln);
            // Awake does not run in edit mode; ResetHealth initialises Current from the new max.
            h.ResetHealth();
            return h;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Any).SetValue(target, value);

        private static DamageInfo Hit(int id, int amount = 1, Team source = Team.Player) =>
            new DamageInfo(amount, Vector3.zero, Vector3.forward, source, id, null);

        private void Check(string name, bool ok)
        {
            if (ok) _passed++; else _failed++;
            Debug.Log($"[HealthSelfTest] {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private void CaseSameIdTwice()
        {
            var h = MakeHealth(Team.Enemy, 5, 0f, out _);
            bool a = h.TryApplyDamage(Hit(1));
            bool b = h.TryApplyDamage(Hit(1));
            Check("same AttackId twice = one hit", a && !b && h.Current == 4);
        }

        private void CaseIdZeroAlwaysApplies()
        {
            var h = MakeHealth(Team.Enemy, 5, 0f, out _);
            bool a = h.TryApplyDamage(Hit(0));
            bool b = h.TryApplyDamage(Hit(0));
            Check("AttackId 0 always applies", a && b && h.Current == 3);
        }

        private void CaseSameTeamIgnored()
        {
            var h = MakeHealth(Team.Enemy, 5, 0f, out _);
            bool a = h.TryApplyDamage(Hit(1, 1, Team.Enemy));
            Check("same-team damage ignored", !a && h.Current == 5);
        }

        private void CaseNonPositiveIgnored()
        {
            var h = MakeHealth(Team.Enemy, 5, 0f, out _);
            bool a = h.TryApplyDamage(Hit(1, 0));
            bool b = h.TryApplyDamage(Hit(2, -3));
            Check("Amount <= 0 ignored", !a && !b && h.Current == 5);
        }

        private void CaseDiedOnceThenRejected()
        {
            var h = MakeHealth(Team.Enemy, 2, 0f, out _);
            int died = 0;
            h.Died += () => died++;
            h.TryApplyDamage(Hit(1));
            h.TryApplyDamage(Hit(2));
            bool after = h.TryApplyDamage(Hit(3));
            Check("Died fires once and later hits rejected", died == 1 && !after && h.IsDead && h.Current == 0);
        }

        private void CaseInvulnerabilityWindow()
        {
            var h = MakeHealth(Team.Player, 5, 0.6f, out _);
            bool first = h.TryApplyDamage(Hit(1, 1, Team.Enemy));
            bool blocked = h.TryApplyDamage(Hit(2, 1, Team.Enemy));
            // Force the window open/closed directly so the check does not depend on elapsed time.
            Set(h, "_invulnerableUntil", 0f);
            bool allowed = h.TryApplyDamage(Hit(3, 1, Team.Enemy));
            Check("invulnerability window blocks then allows", first && !blocked && allowed && h.Current == 3);
        }

        private void CaseIdMemoryEviction()
        {
            var h = MakeHealth(Team.Enemy, 100, 0f, out _);
            for (int id = 1; id <= 33; id++) h.TryApplyDamage(Hit(id));
            bool oldAgain = h.TryApplyDamage(Hit(1));   // evicted by the 33rd id
            bool recentAgain = h.TryApplyDamage(Hit(33)); // still remembered
            Check("33rd distinct id evicts oldest (id 1 can hit again)", oldAgain && !recentAgain && h.Current == 66);
        }

        private void CaseResetClearsMemory()
        {
            var h = MakeHealth(Team.Enemy, 3, 0f, out _);
            h.TryApplyDamage(Hit(7));
            int changed = 0, lastCur = -1;
            h.Changed += (c, m) => { changed++; lastCur = c; };
            h.ResetHealth();
            bool again = h.TryApplyDamage(Hit(7));
            Check("ResetHealth restores HP, clears id memory, raises Changed",
                again && changed == 2 && h.Current == 2 && lastCur == 2 && h.Max == 3);
        }

        private void CaseRelayBossDefeatedOnce()
        {
            var h = MakeHealth(Team.Enemy, 2, 0f, out var go);
            var relay = go.AddComponent<HealthEventRelay>();
            Set(relay, "_mode", HealthEventRelay.Mode.Boss);
            EnableRelay(relay);

            int defeated = 0, bossChanged = 0, playerDied = 0;
            void OnDefeated() => defeated++;
            void OnChanged(int c, int m) => bossChanged++;
            void OnPlayerDied() => playerDied++;
            GameEvents.BossDefeated += OnDefeated;
            GameEvents.BossHealthChanged += OnChanged;
            GameEvents.PlayerDied += OnPlayerDied;
            try
            {
                h.TryApplyDamage(Hit(1));
                h.TryApplyDamage(Hit(2));
                h.TryApplyDamage(Hit(3)); // rejected: already dead
                bool firstRound = defeated == 1 && bossChanged == 2 && playerDied == 0;

                // After a reset the next defeat is allowed again, still only once.
                h.ResetHealth();
                h.TryApplyDamage(Hit(1));
                h.TryApplyDamage(Hit(2));
                Check("relay (Boss) raises BossDefeated once per death",
                    firstRound && defeated == 2 && playerDied == 0);
            }
            finally
            {
                GameEvents.BossDefeated -= OnDefeated;
                GameEvents.BossHealthChanged -= OnChanged;
                GameEvents.PlayerDied -= OnPlayerDied;
            }
        }

        private void CaseRelayPlayerDiedOnce()
        {
            var h = MakeHealth(Team.Player, 1, 0f, out var go);
            var relay = go.AddComponent<HealthEventRelay>();
            EnableRelay(relay); // default mode is Player

            int died = 0, changed = 0, bossDefeated = 0;
            void OnDied() => died++;
            void OnChanged(int c, int m) => changed++;
            void OnBoss() => bossDefeated++;
            GameEvents.PlayerDied += OnDied;
            GameEvents.PlayerHealthChanged += OnChanged;
            GameEvents.BossDefeated += OnBoss;
            try
            {
                h.TryApplyDamage(Hit(1, 1, Team.Enemy));
                h.TryApplyDamage(Hit(2, 1, Team.Enemy));
                Check("relay (Player) raises PlayerDied once, health changes forwarded",
                    died == 1 && changed == 1 && bossDefeated == 0);
            }
            finally
            {
                GameEvents.PlayerDied -= OnDied;
                GameEvents.PlayerHealthChanged -= OnChanged;
                GameEvents.BossDefeated -= OnBoss;
            }
        }

        // OnEnable does not run for freshly added components in edit mode; subscription is idempotent
        // so invoking it manually is safe in play mode too.
        private static void EnableRelay(HealthEventRelay relay) =>
            typeof(HealthEventRelay).GetMethod("OnEnable", Any).Invoke(relay, null);
    }
}
#endif
