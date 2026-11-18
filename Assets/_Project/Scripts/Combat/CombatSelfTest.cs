#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Reflection;
using Echoheart.AI;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Combat
{
    /// <summary>
    /// Self-checking test for the hit-once and resolve-once guarantees of melee, projectiles and EnemyBrain.
    /// Runs in EDIT mode (no Play mode needed): Awake/OnEnable/Update do not run there, so the test invokes
    /// them through reflection, and Projectile is driven by its internal Step(). Test-only, never in a scene.
    /// </summary>
    public class CombatSelfTest : MonoBehaviour
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [SerializeField] private bool _runOnStart;

        private int _passed;
        private int _failed;
        private int _slot;
        private readonly List<GameObject> _temp = new List<GameObject>();
        private readonly List<Object> _tempAssets = new List<Object>();

        private void Start()
        {
            if (_runOnStart) Run();
        }

        [ContextMenu("Run Combat Self Test")]
        public void Run()
        {
            _passed = 0;
            _failed = 0;
            try
            {
                CaseMeleeHitsOncePerSwing();
                CaseMeleeCancelClosesWindow();
                CaseProjectileSingleHitAndDespawn();
                CaseProjectileDoesNotTunnel();
                CaseProjectileRangeExpiry();
                CaseDoubleCaptureRejected();
                CaseDefeatThenCaptureRejected();
                CaseCaptureThenDiedIgnored();
                CaseCancelAfterDefeatNoResurrect();
                CaseLethalDuringInhaleThenCapture();
                CaseLethalDuringInhaleThenCancel();
                CaseNonLethalDuringInhaleRefunded();
                CaseInhaleTimeoutRecovers();
                CaseInhaleCancelsAttackWindow();
            }
            finally
            {
                foreach (var go in _temp)
                    if (go != null) DestroyImmediate(go);
                foreach (var asset in _tempAssets)
                    if (asset != null) DestroyImmediate(asset);
                _temp.Clear();
                _tempAssets.Clear();
            }
            Debug.Log($"[CombatSelfTest] {(_failed == 0 ? "ALL PASS" : "FAILURES")}: {_passed} passed, {_failed} failed");
        }

        // ---- helpers ----

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Any).SetValue(target, value);

        private static object Get(object target, string field) =>
            target.GetType().GetField(field, Any).GetValue(target);

        private static void Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Any).Invoke(target, args);

        private void Check(string name, bool ok)
        {
            if (ok) _passed++; else _failed++;
            Debug.Log($"[CombatSelfTest] {(ok ? "PASS" : "FAIL")}: {name}");
        }

        // Each case gets its own far-away spot so cases cannot see each other's colliders or scene geometry.
        private Vector3 NextSpot() => new Vector3(5000f + 100f * _slot++, 0f, 5000f);

        private GameObject NewGo(string name, Vector3 position)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.position = position;
            _temp.Add(go);
            return go;
        }

        /// <summary>A damageable target on the Enemy layer with TWO colliders (to catch double hits).</summary>
        private Health MakeTarget(Vector3 position, Vector3 colliderSize, int maxHealth)
        {
            var go = NewGo("CombatSelfTest_Target", position);
            go.layer = Layers.Enemy;
            var health = go.AddComponent<Health>();
            Set(health, "_maxHealth", maxHealth);
            health.ResetHealth(); // Awake does not run in edit mode
            go.AddComponent<BoxCollider>().size = colliderSize;

            var child = new GameObject("SecondCollider") { hideFlags = HideFlags.HideAndDontSave, layer = Layers.Enemy };
            child.transform.SetParent(go.transform, false);
            child.AddComponent<BoxCollider>().size = colliderSize;
            return health;
        }

        private static void Sync() => Physics.SyncTransforms();

        private static DamageInfo Hit(int amount) =>
            new DamageInfo(amount, Vector3.zero, Vector3.forward, Team.Player, AttackIds.Next(), null);

        // ---- melee ----

        private void CaseMeleeHitsOncePerSwing()
        {
            Vector3 spot = NextSpot();
            var hitbox = NewGo("Hitbox", spot).AddComponent<MeleeHitbox>();
            var owner = NewGo("Owner", spot - Vector3.forward * 3f);
            Health target = MakeTarget(spot + Vector3.forward, Vector3.one, 5);
            Sync();

            hitbox.Swing(1, Team.Player, owner, Layers.EnemiesMask);
            // FixedUpdate would rescan every physics step while the window is open; simulate several.
            for (int i = 0; i < 5; i++) Call(hitbox, "ScanAndDamage");
            Check("melee swing hits a two-collider target once across 6 scans", target.Current == 4);

            hitbox.Swing(1, Team.Player, owner, Layers.EnemiesMask);
            Check("a new swing (new AttackId) can hit again", target.Current == 3);
        }

        private void CaseMeleeCancelClosesWindow()
        {
            var hitbox = NewGo("Hitbox", NextSpot()).AddComponent<MeleeHitbox>();
            hitbox.Swing(1, Team.Enemy, null, Layers.PlayerMask);
            bool wasActive = hitbox.IsActive;
            hitbox.Cancel();
            Check("MeleeHitbox.Cancel closes an open window", wasActive && !hitbox.IsActive);
        }

        // ---- projectile ----

        private Projectile MakeProjectile(Vector3 position)
        {
            return NewGo("Projectile", position).AddComponent<Projectile>();
        }

        private void CaseProjectileSingleHitAndDespawn()
        {
            Vector3 spot = NextSpot();
            Health target = MakeTarget(spot + Vector3.forward * 5f, Vector3.one * 2f, 5);
            Projectile p = MakeProjectile(spot);
            Sync();

            p.Launch(Vector3.forward, 20f, 30f, 1, Team.Player, null, Layers.EnemiesMask);
            for (int i = 0; i < 20 && p != null; i++) p.Step(0.1f);
            Check("projectile hits a two-collider target once", target.Current == 4);
            Check("projectile is destroyed after resolving", p == null);
        }

        private void CaseProjectileDoesNotTunnel()
        {
            Vector3 spot = NextSpot();
            // 2 cm thick wall, projectile covers 50 m in a single step.
            Health target = MakeTarget(spot + Vector3.forward * 10f, new Vector3(4f, 4f, 0.02f), 5);
            Projectile p = MakeProjectile(spot);
            Sync();

            p.Launch(Vector3.forward, 1000f, 50f, 1, Team.Player, null, Layers.EnemiesMask);
            p.Step(0.1f);
            Check("fast projectile does not tunnel through a thin target", target.Current == 4 && p == null);
        }

        private void CaseProjectileRangeExpiry()
        {
            Vector3 spot = NextSpot();
            Health target = MakeTarget(spot + Vector3.forward * 100f, Vector3.one * 2f, 5);
            Projectile p = MakeProjectile(spot);
            Sync();

            p.Launch(Vector3.forward, 10f, 20f, 1, Team.Player, null, Layers.EnemiesMask);
            for (int i = 0; i < 5 && p != null; i++) p.Step(1f);
            Check("projectile despawns at max range without damaging anything", p == null && target.Current == 5);
        }

        // ---- enemy brain ----

        private sealed class Rig
        {
            public EnemyBrain Brain;
            public Health Health;
            public int Removed;
        }

        private Rig MakeEnemy()
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            def.hideFlags = HideFlags.HideAndDontSave;
            _tempAssets.Add(def);
            Set(def, "_maxHealth", 3);

            var go = NewGo("CombatSelfTest_Enemy", NextSpot());
            go.layer = Layers.Enemy;
            // Inactive while components are added: a NavMeshAgent that wakes up off the NavMesh logs an error.
            go.SetActive(false);
            var brain = go.AddComponent<EnemyBrain>(); // RequireComponent adds Health and NavMeshAgent
            var health = go.GetComponent<Health>();
            go.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            Set(health, "_maxHealth", 3);
            health.ResetHealth();
            Set(brain, "_definition", def);
            go.SetActive(true);

            // Edit mode skips Awake/OnEnable (OnEnable would throw before Awake anyway), so run them by hand.
            Call(brain, "Awake");
            Call(brain, "OnEnable");

            var rig = new Rig { Brain = brain, Health = health };
            brain.Removed += _ => rig.Removed++;
            return rig;
        }

        private static void Tick(EnemyBrain brain) => Call(brain, "Update");

        private void CaseDoubleCaptureRejected()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            r.Brain.OnCaptured();
            r.Brain.OnCaptured(); // logs a warning by design
            Check("double OnCaptured raises Removed once",
                r.Removed == 1 && r.Brain.Current == EnemyBrain.State.Defeated);
        }

        private void CaseDefeatThenCaptureRejected()
        {
            Rig r = MakeEnemy();
            r.Health.TryApplyDamage(Hit(3));
            r.Brain.OnCaptured(); // logs a warning by design
            Check("defeat then OnCaptured: Removed once, capture rejected",
                r.Removed == 1 && r.Brain.Current == EnemyBrain.State.Defeated);
        }

        private void CaseCaptureThenDiedIgnored()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            r.Brain.OnCaptured();
            r.Health.TryApplyDamage(Hit(3)); // Health.Died fires after capture
            Check("capture then Health.Died is ignored (Removed once)", r.Removed == 1);
        }

        private void CaseCancelAfterDefeatNoResurrect()
        {
            Rig r = MakeEnemy();
            r.Health.TryApplyDamage(Hit(3));
            r.Brain.OnInhaleCancel();
            Check("OnInhaleCancel after defeat does not resurrect",
                r.Brain.Current == EnemyBrain.State.Defeated && r.Removed == 1);
        }

        private void CaseLethalDuringInhaleThenCapture()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            r.Health.TryApplyDamage(Hit(3));
            bool heldBack = r.Removed == 0 && r.Brain.Current == EnemyBrain.State.BeingInhaled;
            r.Brain.OnCaptured();
            r.Brain.OnInhaleCancel();
            Check("lethal damage during inhale, then capture: resolves once as a capture",
                heldBack && r.Removed == 1 && r.Brain.Current == EnemyBrain.State.Defeated);
        }

        private void CaseLethalDuringInhaleThenCancel()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            r.Health.TryApplyDamage(Hit(3));
            r.Brain.OnInhaleCancel();
            Check("lethal damage during inhale, then cancel: defeated once",
                r.Removed == 1 && r.Brain.Current == EnemyBrain.State.Defeated);
        }

        private void CaseNonLethalDuringInhaleRefunded()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            r.Health.TryApplyDamage(Hit(1));
            Check("non-lethal damage during inhale is refunded",
                r.Health.Current == 3 && r.Brain.Current == EnemyBrain.State.BeingInhaled && r.Removed == 0);
        }

        private void CaseInhaleTimeoutRecovers()
        {
            Rig r = MakeEnemy();
            r.Brain.OnInhaleStart(null);
            float first = (float)Get(r.Brain, "_inhaleDeadline");
            r.Brain.OnInhaleStart(null); // a live inhale re-calls this; must stay BeingInhaled and keep the deadline
            bool stillInhaled = r.Brain.Current == EnemyBrain.State.BeingInhaled &&
                                (float)Get(r.Brain, "_inhaleDeadline") >= first;

            Tick(r.Brain);
            bool notYet = r.Brain.Current == EnemyBrain.State.BeingInhaled;

            Set(r.Brain, "_inhaleDeadline", Time.time - 1f); // simulate the player dying mid-inhale for 2 s
            Tick(r.Brain);
            Check("BeingInhaled times out back to Idle without cancel or capture (no Removed)",
                stillInhaled && notYet && r.Brain.Current == EnemyBrain.State.Idle && r.Removed == 0);
        }

        private void CaseInhaleCancelsAttackWindow()
        {
            Rig r = MakeEnemy();
            var hitbox = NewGo("EnemyHitbox", r.Brain.transform.position).AddComponent<MeleeHitbox>();
            var attack = r.Brain.gameObject.AddComponent<MeleeEnemyAttack>();
            Set(attack, "_hitbox", hitbox);
            Set(r.Brain, "_attack", attack);

            attack.Execute(r.Brain.Definition, null);
            bool open = hitbox.IsActive;
            r.Brain.OnInhaleStart(null);
            Check("starting an inhale cancels an open enemy attack window", open && !hitbox.IsActive);

            attack.Execute(r.Brain.Definition, null);
            r.Brain.OnInhaleCancel();
            r.Health.TryApplyDamage(Hit(3));
            Check("defeat cancels an open enemy attack window", !hitbox.IsActive && r.Removed == 1);
        }
    }
}
#endif
