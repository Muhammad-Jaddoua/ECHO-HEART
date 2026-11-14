#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Reflection;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Self-checking test for BossBrain: once-only events, reset behaviour, slam hits once per slam, attack
    /// choice by distance, not inhalable, PlayerDied stand-down. Runs in EDIT mode: Awake/OnEnable do not run
    /// there, so the test calls them through reflection and drives the FSM with BossBrain.Tick(fixed delta).
    /// Test-only, never in a scene.
    /// </summary>
    public class BossSelfTest : MonoBehaviour
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private const float Dt = 0.1f;

        [SerializeField] private bool _runOnStart;

        private int _passed;
        private int _failed;
        private int _slot;
        private int _encounterStarted;
        private int _bossDefeated;
        private int _bossHealthChanged;
        private readonly List<GameObject> _temp = new List<GameObject>();
        private readonly List<BossBrain> _bosses = new List<BossBrain>();
        private readonly List<Object> _tempAssets = new List<Object>();

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Boss Self Test")]
        public void Run()
        {
            _passed = 0;
            _failed = 0;
            GameEvents.BossEncounterStarted += OnEncounterStarted;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.BossHealthChanged += OnBossHealthChanged;
            try
            {
                CaseNotInhalable();
                CaseBeginEncounterOnce();
                CaseDefeatRaisedOnceAndHealthEvents();
                CaseResetBossArenaRearms();
                CaseResetMainAreaIgnored();
                CaseSlamHitsOncePerSlam();
                CaseChoosesSlamNearRangedFar();
                CaseRangedShotHurtsPlayer();
                CasePlayerDiedReturnsToIdle();
                CaseNoAttackBeforeEncounter();
                CasePlayerDiedBeforeEncounterDoesNotLatch();
                CasePlayerDiedThenMainAreaResetUnlatches();
                CaseResetBossArenaDestroysLiveShots();
                CaseTelegraphVisualRingAndTint();
            }
            finally
            {
                GameEvents.BossEncounterStarted -= OnEncounterStarted;
                GameEvents.BossDefeated -= OnBossDefeated;
                GameEvents.BossHealthChanged -= OnBossHealthChanged;
                // Edit mode never calls OnDisable, so unsubscribe the static PlayerDied handlers by hand.
                foreach (var b in _bosses)
                    if (b != null) Call(b, "OnDisable");
                _bosses.Clear();
                foreach (var go in _temp)
                    if (go != null) DestroyImmediate(go);
                foreach (var asset in _tempAssets)
                    if (asset != null) DestroyImmediate(asset);
                foreach (var p in Resources.FindObjectsOfTypeAll<Projectile>())
                    // Hidden-flag filter: FindObjectsOfTypeAll also returns prefab assets, which must never be destroyed.
                    if (p != null && p.gameObject.hideFlags == HideFlags.HideAndDontSave) DestroyImmediate(p.gameObject);
                _temp.Clear();
                _tempAssets.Clear();
            }
            Debug.Log($"[BossSelfTest] {(_failed == 0 ? "ALL PASS" : "FAILURES")}: {_passed} passed, {_failed} failed");
        }

        // ---- helpers ----

        private void OnEncounterStarted() => _encounterStarted++;
        private void OnBossDefeated() => _bossDefeated++;
        private void OnBossHealthChanged(int c, int m) => _bossHealthChanged++;

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Any).SetValue(target, value);

        private static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Any).Invoke(target, args);

        private void Check(string name, bool ok)
        {
            if (ok) _passed++; else _failed++;
            Debug.Log($"[BossSelfTest] {(ok ? "PASS" : "FAIL")}: {name}");
        }

        private void ResetCounters()
        {
            _encounterStarted = 0;
            _bossDefeated = 0;
            _bossHealthChanged = 0;
        }

        // Each case gets its own far-away spot so cases cannot see each other's colliders.
        private Vector3 NextSpot() => new Vector3(5000f + 100f * _slot++, 0f, 7000f);

        private GameObject NewGo(string name, Vector3 position)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.position = position;
            _temp.Add(go);
            return go;
        }

        private BossDefinition MakeDefinition(GameObject projectilePrefab = null)
        {
            var def = ScriptableObject.CreateInstance<BossDefinition>();
            def.hideFlags = HideFlags.HideAndDontSave;
            _tempAssets.Add(def);
            Set(def, "_maxHealth", 3);
            Set(def, "_sightRange", 40f);
            Set(def, "_slamRange", 4f);
            Set(def, "_slamRadius", 5f);
            Set(def, "_slamDamage", 2);
            Set(def, "_slamTelegraph", 1f);
            Set(def, "_slamRecover", 1.5f);
            Set(def, "_rangedTelegraph", 0.9f);
            Set(def, "_rangedRecover", 1.2f);
            Set(def, "_rangedDamage", 1);
            Set(def, "_projectileSpeed", 20f);
            Set(def, "_projectileRange", 60f);
            Set(def, "_projectilePrefab", projectilePrefab);
            Set(def, "_idlePause", 0.6f);
            Set(def, "_attackActiveSeconds", 0.2f);
            return def;
        }

        private BossBrain MakeBoss(Vector3 position, GameObject projectilePrefab = null)
        {
            var go = NewGo("BossSelfTest_Boss", position);
            go.layer = Layers.Boss;
            var brain = go.AddComponent<BossBrain>(); // RequireComponent adds Health
            var health = go.GetComponent<Health>();
            Set(health, "_maxHealth", 3);
            health.ResetHealth(); // Awake does not run in edit mode
            Set(brain, "_definition", MakeDefinition(projectilePrefab));
            Call(brain, "Awake");
            Call(brain, "OnEnable");
            _bosses.Add(brain);
            return brain;
        }

        private Health MakePlayer(Vector3 position, int maxHealth = 10)
        {
            var go = NewGo("BossSelfTest_Player", position);
            go.layer = Layers.Player;
            var health = go.AddComponent<Health>();
            Set(health, "_team", Team.Player);
            Set(health, "_maxHealth", maxHealth);
            health.ResetHealth();
            AddBody(go);
            var child = new GameObject("SecondCollider") { hideFlags = HideFlags.HideAndDontSave, layer = Layers.Player };
            child.transform.SetParent(go.transform, false);
            AddBody(child); // two colliders: catches double hits
            return health;
        }

        // Body-sized box (feet at the pivot) so shots aimed at chest height actually hit it.
        private static void AddBody(GameObject go)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = Vector3.up;
            box.size = new Vector3(1f, 2f, 1f);
        }

        private static DamageInfo Hit(int amount) =>
            new DamageInfo(amount, Vector3.zero, Vector3.forward, Team.Player, AttackIds.Next(), null);

        private static void Tick(BossBrain boss, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++) Call(boss, "Tick", Dt);
        }

        /// <summary>Ticks until the boss reaches the state or the timeout (simulated seconds) passes.</summary>
        private static bool TickUntil(BossBrain boss, BossBrain.State state, float timeout)
        {
            for (float t = 0f; t < timeout; t += Dt)
            {
                if (boss.Current == state) return true;
                Call(boss, "Tick", Dt);
            }
            return boss.Current == state;
        }

        // ---- cases ----

        private void CaseNotInhalable()
        {
            Check("boss is not IInhalable (inhale ignores it)", !typeof(IInhalable).IsAssignableFrom(typeof(BossBrain)));
        }

        private void CaseBeginEncounterOnce()
        {
            ResetCounters();
            BossBrain boss = MakeBoss(NextSpot());
            boss.BeginEncounter();
            boss.BeginEncounter();
            boss.BeginEncounter();
            Check("BeginEncounter x3 raises BossEncounterStarted once", _encounterStarted == 1);
        }

        private void CaseDefeatRaisedOnceAndHealthEvents()
        {
            ResetCounters();
            BossBrain boss = MakeBoss(NextSpot());
            var health = boss.GetComponent<Health>();
            boss.BeginEncounter();
            int before = _bossHealthChanged;
            health.TryApplyDamage(Hit(1));
            Check("damage raises BossHealthChanged", _bossHealthChanged == before + 1);

            health.TryApplyDamage(Hit(5)); // lethal
            health.TryApplyDamage(Hit(5)); // ignored: already dead
            Check("killing the boss raises BossDefeated exactly once",
                _bossDefeated == 1 && boss.Current == BossBrain.State.Defeated);

            Tick(boss, 2f); // past the default 1.5 s delay
            Check("defeated boss is deactivated after the delay, not destroyed",
                boss != null && !boss.gameObject.activeSelf);
        }

        private void CaseResetBossArenaRearms()
        {
            ResetCounters();
            BossBrain boss = MakeBoss(NextSpot());
            var health = boss.GetComponent<Health>();
            boss.BeginEncounter();
            health.TryApplyDamage(Hit(5));
            Tick(boss, 2f); // deactivated

            int healthEventsBefore = _bossHealthChanged;
            boss.ResetFor(RetryScope.BossArena);
            bool restored = boss.gameObject.activeSelf && health.Current == health.Max &&
                            boss.Current == BossBrain.State.Idle && !boss.EncounterBegun;
            bool resetRaisedHealth = _bossHealthChanged > healthEventsBefore;

            boss.BeginEncounter();
            Check("ResetFor(BossArena) restores HP/state and re-arms the encounter guard",
                restored && resetRaisedHealth && _encounterStarted == 2);

            health.TryApplyDamage(Hit(5));
            Check("after ResetFor the boss can raise BossDefeated again (2 total)", _bossDefeated == 2);
        }

        private void CaseResetMainAreaIgnored()
        {
            BossBrain boss = MakeBoss(NextSpot());
            var health = boss.GetComponent<Health>();
            boss.BeginEncounter();
            health.TryApplyDamage(Hit(1));
            boss.ResetFor(RetryScope.MainArea);
            Check("ResetFor(MainArea) leaves the boss untouched", health.Current == 2 && boss.EncounterBegun);
        }

        private void CaseSlamHitsOncePerSlam()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            Health player = MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();
            boss.BeginEncounter();

            bool telegraphed = TickUntil(boss, BossBrain.State.Telegraph, 3f);
            Check("near player: boss telegraphs a Slam", telegraphed && boss.CurrentAttack == BossBrain.AttackKind.Slam);
            bool stillDuringWarning = player.Current == 10;
            TickUntil(boss, BossBrain.State.Attack, 3f);
            Call(boss, "Tick", Dt); // extra tick inside Attack must not hit again
            Check("slam damages a two-collider player exactly once", stillDuringWarning && player.Current == 8);

            TickUntil(boss, BossBrain.State.Idle, 4f);
            Check("recover passes without further damage", player.Current == 8);

            TickUntil(boss, BossBrain.State.Attack, 4f);
            Check("a second slam hurts once more (2 slams = 4 damage)", player.Current == 6);
        }

        private void CaseChoosesSlamNearRangedFar()
        {
            Vector3 spot = NextSpot();
            BossBrain near = MakeBoss(spot);
            MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();
            near.BeginEncounter();
            TickUntil(near, BossBrain.State.Telegraph, 3f);
            bool nearSlam = near.CurrentAttack == BossBrain.AttackKind.Slam;

            Vector3 farSpot = NextSpot();
            BossBrain far = MakeBoss(farSpot);
            MakePlayer(farSpot + Vector3.forward * 15f);
            Physics.SyncTransforms();
            far.BeginEncounter();
            TickUntil(far, BossBrain.State.Telegraph, 3f);
            bool farRanged = far.CurrentAttack == BossBrain.AttackKind.Ranged;

            Tick(far, 0.5f);
            bool progress = far.TelegraphProgress > 0f && far.TelegraphProgress < 1f;
            Check("Slam chosen when the player is near, Ranged when far (telegraph progress runs 0..1)",
                nearSlam && farRanged && progress);
        }

        private void CaseRangedShotHurtsPlayer()
        {
            var template = NewGo("BossSelfTest_ProjectileTemplate", NextSpot());
            template.AddComponent<Projectile>();

            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot, template);
            Health player = MakePlayer(spot + Vector3.forward * 15f);
            Physics.SyncTransforms();
            boss.BeginEncounter();

            TickUntil(boss, BossBrain.State.Attack, 4f);
            // The clone is hidden (copied flags) so only FindObjectsOfTypeAll sees it; find it, then step it with fixed deltas like CombatSelfTest does.
            foreach (var p in Resources.FindObjectsOfTypeAll<Projectile>())
            {
                if (p.gameObject == template || p.gameObject.hideFlags != HideFlags.HideAndDontSave) continue;
                for (int i = 0; i < 40 && p != null; i++) p.Step(Dt);
            }
            Check("ranged shot fires a projectile that hurts the player once (10 -> 9)", player.Current == 9);
        }

        private void CasePlayerDiedReturnsToIdle()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();
            boss.BeginEncounter();
            TickUntil(boss, BossBrain.State.Telegraph, 3f);

            GameEvents.RaisePlayerDied();
            bool idle = boss.Current == BossBrain.State.Idle;
            Tick(boss, 3f); // would have telegraphed again if it were still hunting
            Check("PlayerDied returns the boss to Idle and it stays there",
                idle && boss.Current == BossBrain.State.Idle && boss.EncounterBegun);
            boss.ResetFor(RetryScope.BossArena);
        }

        private void CasePlayerDiedBeforeEncounterDoesNotLatch()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();

            GameEvents.RaisePlayerDied(); // player dies in the main area, boss fight not started
            boss.BeginEncounter();        // then walks into the arena without any boss retry
            bool telegraphed = TickUntil(boss, BossBrain.State.Telegraph, 3f);
            Check("PlayerDied before BeginEncounter does not latch: the boss still attacks", telegraphed);
        }

        private void CasePlayerDiedThenMainAreaResetUnlatches()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            var health = boss.GetComponent<Health>();
            MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();
            boss.BeginEncounter();
            health.TryApplyDamage(Hit(1));
            GameEvents.RaisePlayerDied();

            boss.ResetFor(RetryScope.MainArea);
            bool untouched = health.Current == 2 && boss.EncounterBegun && boss.Current == BossBrain.State.Idle;

            // Un-latched: with the latch still set the boss would sit in Idle forever.
            bool hunts = TickUntil(boss, BossBrain.State.Telegraph, 3f);

            boss.ResetFor(RetryScope.BossArena);
            boss.BeginEncounter();
            bool worksAfterArenaReset = health.Current == 3 && TickUntil(boss, BossBrain.State.Telegraph, 3f);
            Check("MainArea reset un-latches PlayerDied without touching HP/state; BossArena reset + BeginEncounter works",
                untouched && hunts && worksAfterArenaReset);
        }

        private void CaseResetBossArenaDestroysLiveShots()
        {
            var template = NewGo("BossSelfTest_ProjectileTemplate2", NextSpot());
            template.AddComponent<Projectile>();

            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot, template);
            MakePlayer(spot + Vector3.forward * 15f);
            Physics.SyncTransforms();
            boss.BeginEncounter();
            TickUntil(boss, BossBrain.State.Attack, 4f);

            // Edit mode never runs OnEnable, so register the shot in the live list by hand.
            var shots = new List<Projectile>();
            foreach (var p in Resources.FindObjectsOfTypeAll<Projectile>())
            {
                if (p.gameObject == template || p.gameObject.hideFlags != HideFlags.HideAndDontSave) continue;
                Call(p, "OnEnable");
                shots.Add(p);
            }
            bool fired = shots.Count > 0;

            boss.ResetFor(RetryScope.BossArena);
            bool gone = true;
            foreach (var p in shots) gone &= p == null;
            Check("a live boss shot is destroyed by ResetFor(BossArena)", fired && gone);
        }

        private void CaseTelegraphVisualRingAndTint()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();

            var ring = NewGo("BossSelfTest_Ring", spot);
            ring.transform.SetParent(boss.transform, false);
            var body = NewGo("BossSelfTest_Body", spot);
            body.transform.SetParent(boss.transform, false);
            var renderer = body.AddComponent<MeshRenderer>();

            var visual = NewGo("BossSelfTest_Visual", spot).AddComponent<BossTelegraphVisual>();
            Set(visual, "_brain", boss);
            Set(visual, "_slamRing", ring.transform);
            Set(visual, "_bodyRenderer", renderer);

            visual.Refresh();
            bool hiddenIdle = !ring.activeSelf;

            boss.BeginEncounter();
            TickUntil(boss, BossBrain.State.Telegraph, 3f);
            Tick(boss, 0.5f); // slam telegraph is 1 s: progress 0.5
            visual.Refresh();
            // Convention: full diameter = SlamRadius (5) * 2 * progress (0.5) = 5 at unit parent scale.
            bool shown = ring.activeSelf && Mathf.Abs(ring.transform.localScale.x - 5f) < 0.2f &&
                         Mathf.Abs(ring.transform.localScale.z - 5f) < 0.2f;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            bool tinted = !block.isEmpty;

            TickUntil(boss, BossBrain.State.Recover, 3f);
            visual.Refresh();
            renderer.GetPropertyBlock(block);
            bool restored = !ring.activeSelf && block.isEmpty;

            Check("telegraph visual: ring hidden idle, shown at radius*2*progress during slam telegraph, hidden and tint cleared after",
                hiddenIdle && shown && tinted && restored);
        }

        private void CaseNoAttackBeforeEncounter()
        {
            Vector3 spot = NextSpot();
            BossBrain boss = MakeBoss(spot);
            Health player = MakePlayer(spot + Vector3.forward * 2f);
            Physics.SyncTransforms();
            Tick(boss, 5f);
            Check("boss stays Idle and harmless until BeginEncounter",
                boss.Current == BossBrain.State.Idle && player.Current == 10);
        }
    }
}
#endif
