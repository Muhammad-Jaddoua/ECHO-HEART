using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using Echoheart.UI;
using UnityEngine;

namespace Echoheart.Testing
{
    /// <summary>
    /// Scripted play-mode check of the combat slice in Sandbox_Muhammad. Starts only when _runOnStart is ticked,
    /// drives the CombatTestHarness (same code path as the keys) and logs "[PlayTest] PASS/FAIL: ..." lines,
    /// also written to Temp/CombatPlayTest.log. Sandbox only; leave _runOnStart off when committing.
    /// </summary>
    public class CombatPlayTest : MonoBehaviour
    {
        [SerializeField] private bool _runOnStart;
        [SerializeField, Range(1f, 3f)] private float _timeScale = 2f;
        [SerializeField] private CombatTestHarness _harness;
        [SerializeField] private Health _player;
        [SerializeField] private BossBrain _boss;
        [SerializeField] private Health _dummy;
        [SerializeField] private AbilityDefinition _sword;
        [SerializeField] private AbilityDefinition _bow;
        [SerializeField] private AbilityDefinition _fire;

        private readonly StringBuilder _report = new StringBuilder();
        private readonly List<string> _errors = new List<string>();
        private readonly HashSet<string> _warnings = new HashSet<string>();
        private CharacterController _controller;
        private Health _bossHealth;
        private BossHealthBar _bar;
        private int _passes, _fails, _playerHealthEvents, _playerDeaths, _encounterStarts, _bossDefeats, _bossHealthEvents;
        private int _lastPlayerHp;
        private bool _playerDamagedByBossSlam, _playerDamagedByBossShot;

        private void Start()
        {
            if (!_runOnStart) return;
            _controller = _harness.GetComponent<CharacterController>();
            _bossHealth = _boss.GetComponent<Health>();
            _bar = FindFirstObjectByType<BossHealthBar>(FindObjectsInactive.Include);
            Application.logMessageReceived += OnLog;
            GameEvents.PlayerHealthChanged += OnPlayerHealth;
            GameEvents.PlayerDied += () => _playerDeaths++;
            GameEvents.BossEncounterStarted += () => _encounterStarts++;
            GameEvents.BossDefeated += () => _bossDefeats++;
            GameEvents.BossHealthChanged += (c, m) => _bossHealthEvents++;
            _lastPlayerHp = _player.Max;
            StartCoroutine(Run());
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors.Add(message);
            else if (type == LogType.Warning) _warnings.Add(message);
        }

        private void OnPlayerHealth(int current, int max)
        {
            _playerHealthEvents++;
            if (current < _lastPlayerHp && _boss.Current != BossBrain.State.Idle)
            {
                if (_boss.CurrentAttack == BossBrain.AttackKind.Slam) _playerDamagedByBossSlam = true;
                else _playerDamagedByBossShot = true;
            }
            _lastPlayerHp = current;
        }

        private void Check(bool ok, string text)
        {
            if (ok) _passes++; else _fails++;
            string line = $"[PlayTest] {(ok ? "PASS" : "FAIL")}: {text}";
            _report.AppendLine(line);
            Debug.Log(line);
        }

        private void Note(string text)
        {
            _report.AppendLine("[PlayTest] INFO: " + text);
            Debug.Log("[PlayTest] INFO: " + text);
        }

        private IEnumerator Run()
        {
            Time.timeScale = _timeScale;
            yield return new WaitForSeconds(1f);
            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            var main = new List<EnemySpawner>(); var arena = new List<EnemySpawner>();
            foreach (var s in spawners) (s.name.Contains("_Arena") ? arena : main).Add(s);

            yield return TestHitOnce();
            yield return TestEnemyAi(main);
            yield return TestCapture(main);
            yield return TestPlayerDamage(main);
            yield return TestMainReset(main);
            yield return TestBossEncounter(arena);
            yield return TestBossTimeToKill();
            yield return TestBossRetry(arena, main);

            Check(_errors.Count == 0, $"no errors logged during the run ({_errors.Count}) {string.Join(" | ", _errors)}");
            Note("warnings: " + string.Join(" || ", _warnings));
            Note($"DONE passes={_passes} fails={_fails}");
            Time.timeScale = 1f;
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Temp", "CombatPlayTest.log"), _report.ToString());
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ---- helpers ----

        private void Teleport(Vector3 position, Vector3 lookAt)
        {
            _controller.enabled = false; // CharacterController overrides direct transform moves
            Vector3 look = lookAt - position; look.y = 0f;
            _harness.transform.SetPositionAndRotation(position, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity);
            _controller.enabled = true;
        }

        private DamageInfo Hit(int amount) =>
            new DamageInfo(amount, transform.position, Vector3.forward, Team.Player, AttackIds.Next(), gameObject);

        private void KeepPlayerAlive() { if (_player.Current <= 3) _player.ResetHealth(); }

        // ---- (b) each attack lands exactly once ----

        private IEnumerator TestHitOnce()
        {
            _dummy.transform.position = new Vector3(-27f, 1f, 14f);
            Teleport(new Vector3(-27f, 1.1f, 12f), _dummy.transform.position);
            yield return new WaitForSeconds(0.5f);
            foreach (AbilityDefinition ability in new[] { _sword, _bow, _fire })
            {
                _dummy.ResetHealth();
                _harness.Equip(ability);
                int before = _dummy.Current;
                bool fired = _harness.Attack();
                yield return new WaitForSeconds(1.0f); // longer than sword window, projectile flight and cooldown
                int lost = before - _dummy.Current;
                Check(fired && lost == ability.Damage, $"{ability.Type}: one attack removed exactly {ability.Damage} HP from the dummy (fired={fired}, lost={lost})");
            }
            _dummy.ResetHealth();
        }

        // ---- (a) enemies spawned on the NavMesh, Idle -> Chase -> Attack ----

        private IEnumerator TestEnemyAi(List<EnemySpawner> main)
        {
            _harness.Equip(_sword);
            foreach (EnemySpawner spawner in main)
            {
                EnemyBrain enemy = spawner.Current;
                bool exists = enemy != null && enemy.gameObject.activeInHierarchy;
                bool onMesh = exists && enemy.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh;
                Check(exists && onMesh, $"{spawner.name}: enemy spawned and standing on the NavMesh");
                if (!exists) continue;

                var def = enemy.Definition;
                float distance = Mathf.Min(def.SightRange * 0.85f, def.AttackRange + 3f);
                Vector3 dir = spawner.name.Contains("Beetle") ? Vector3.left : Vector3.forward;
                Teleport(enemy.transform.position + dir * distance + Vector3.up * 1.1f, enemy.transform.position);
                float startDistance = Vector3.Distance(enemy.transform.position, _harness.transform.position);
                int hpBefore = _player.Max; _player.ResetHealth();
                bool sawIdle = enemy.Current == EnemyBrain.State.Idle, sawChase = false, sawAttack = false, damaged = false;
                for (float t = 0f; t < 8f; t += Time.deltaTime)
                {
                    sawChase |= enemy.Current == EnemyBrain.State.Chase;
                    sawAttack |= enemy.Current == EnemyBrain.State.Attack;
                    damaged |= _player.Current < hpBefore;
                    if (_player.Current <= 2) { _player.ResetHealth(); }
                    if (sawAttack && damaged) break;
                    yield return null;
                }
                Check(sawIdle && sawChase && sawAttack, $"{spawner.name}: FSM Idle->Chase->Attack seen (idle={sawIdle} chase={sawChase} attack={sawAttack}, start distance {startDistance:F1} m)");
                Check(damaged, $"{spawner.name}: enemy attack damaged the player");
                _harness.Retry(RetryScope.MainArea);
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ---- (c) capture grants the ability, removes enemy, spawner respawns it ----

        private IEnumerator TestCapture(List<EnemySpawner> main)
        {
            EnemySpawner spawner = main.Find(s => s.name.Contains("Beetle"));
            EnemyBrain enemy = spawner.Current;
            _harness.Equip(_bow);
            int activeBefore = EnemySpawner.ActiveCount;
            Teleport(enemy.transform.position + Vector3.left * 3f + Vector3.up * 1.1f, enemy.transform.position);
            bool captured = _harness.SimulateCapture();
            Check(captured, "C key path: nearest enemy in front captured");
            Check(_harness.Held != null && _harness.Held.Type == AbilityType.Sword, $"capture granted the beetle's power (held={_harness.Held?.Type})");
            Check(!enemy.gameObject.activeSelf && !spawner.IsEnemyActive && spawner.PendingRespawn, "captured enemy removed and spawner is waiting to respawn it");
            Check(EnemySpawner.ActiveCount == activeBefore - 1, $"shared active count dropped by one ({activeBefore} -> {EnemySpawner.ActiveCount})");

            Teleport(new Vector3(-26f, 1.1f, -14f), Vector3.zero); // beyond the 10 m no-spawn radius
            float t0 = Time.time;
            while (!spawner.IsEnemyActive && Time.time - t0 < 15f) yield return null;
            Check(spawner.IsEnemyActive && enemy.gameObject.activeSelf, $"spawner respawned the beetle after {Time.time - t0:F1} s (delay field value)");
            Check(EnemySpawner.ActiveCount == activeBefore, $"active count restored ({EnemySpawner.ActiveCount})");

            // defeat by damage must not grant a power
            AbilityDefinition held = _harness.Held;
            enemy.GetComponent<Health>().TryApplyDamage(Hit(99));
            yield return new WaitForSeconds(0.2f);
            Check(enemy.Current == EnemyBrain.State.Defeated && _harness.Held == held, "defeating by damage removes the enemy and grants no power");
        }

        // ---- (d) player damage events and a single death ----

        private IEnumerator TestPlayerDamage(List<EnemySpawner> main)
        {
            foreach (EnemySpawner s in main) if (s.Current != null) s.Current.gameObject.SetActive(false); // isolate from enemy hits
            Teleport(new Vector3(-26f, 1.1f, -14f), Vector3.zero);
            _player.ResetHealth();
            yield return new WaitForSeconds(0.7f);
            int events = _playerHealthEvents; int lastHp = -1;
            void Capture(int c, int m) => lastHp = c;
            GameEvents.PlayerHealthChanged += Capture;
            _player.TryApplyDamage(new DamageInfo(1, Vector3.zero, Vector3.forward, Team.Enemy, AttackIds.Next(), gameObject));
            GameEvents.PlayerHealthChanged -= Capture;
            Check(_playerHealthEvents == events + 1 && lastHp == _player.Max - 1, $"damage raised PlayerHealthChanged ({lastHp}/{_player.Max})");

            int deathsBefore = _playerDeaths;
            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForSeconds(0.65f); // past the 0.6 s invulnerability
                _player.TryApplyDamage(new DamageInfo(1, Vector3.zero, Vector3.forward, Team.Enemy, AttackIds.Next(), gameObject));
            }
            yield return new WaitForSeconds(0.5f);
            Check(_player.IsDead && _playerDeaths == deathsBefore + 1, $"PlayerDied raised exactly once (deaths +{_playerDeaths - deathsBefore}); no auto retry (still dead={_player.IsDead})");
            Check(!_harness.Attack(), "dead player cannot attack");
        }

        // ---- (h) main-area reset ----

        private IEnumerator TestMainReset(List<EnemySpawner> main)
        {
            EnemyBrain beetle = main.Find(s => s.name.Contains("Beetle")).Current;
            beetle.gameObject.SetActive(true);
            beetle.GetComponent<Health>().TryApplyDamage(Hit(1));
            _harness.Retry(RetryScope.MainArea);
            yield return new WaitForSeconds(0.5f);
            bool all = true;
            foreach (EnemySpawner s in main)
            {
                EnemyBrain e = s.Current;
                all &= e != null && e.gameObject.activeSelf && s.IsEnemyActive && e.GetComponent<Health>().Current == e.GetComponent<Health>().Max
                       && Vector3.Distance(e.transform.position, s.transform.position) < 0.6f;
            }
            Check(all, "ResetFor(MainArea): every main enemy active, full HP, back at its spawn point");
            Check(!_player.IsDead && _player.Current == _player.Max, "harness retry refilled and revived the player");
        }

        // ---- (e) boss encounter, telegraphs, damage ----

        private IEnumerator TestBossEncounter(List<EnemySpawner> arena)
        {
            foreach (EnemySpawner s in arena) { if (s.Current != null) s.Current.gameObject.SetActive(false); s.gameObject.SetActive(false); }
            _harness.Equip(_sword);
            Check(_bar != null && !_bar.IsVisible, "boss bar hidden before the encounter");
            Teleport(new Vector3(32f, 1.1f, 0f), _boss.transform.position);
            int starts = _encounterStarts;
            _harness.BeginBoss(); _harness.BeginBoss();
            yield return null; yield return null;
            Check(_encounterStarts == starts + 1, "BeginEncounter twice raised BossEncounterStarted once");
            Check(_bar.IsVisible, "boss bar visible after the encounter started");

            bool slamTelegraph = false, shotTelegraph = false, sawAttack = false, sawRecover = false;
            float progressMax = 0f;
            for (float t = 0f; t < 40f; t += Time.deltaTime)
            {
                KeepPlayerAlive();
                if (_boss.Current == BossBrain.State.Telegraph)
                {
                    slamTelegraph |= _boss.CurrentAttack == BossBrain.AttackKind.Slam;
                    shotTelegraph |= _boss.CurrentAttack == BossBrain.AttackKind.Ranged;
                    progressMax = Mathf.Max(progressMax, _boss.TelegraphProgress);
                }
                sawAttack |= _boss.Current == BossBrain.State.Attack;
                sawRecover |= _boss.Current == BossBrain.State.Recover;
                if (slamTelegraph && shotTelegraph && _playerDamagedByBossSlam && _playerDamagedByBossShot) break;
                yield return null;
            }
            Check(slamTelegraph && shotTelegraph, $"boss telegraphed both Slam ({slamTelegraph}) and Ranged ({shotTelegraph}); progress reached {progressMax:F2}");
            Check(sawAttack && sawRecover, $"boss went Telegraph->Attack->Recover (attack={sawAttack}, recover={sawRecover})");
            Check(_playerDamagedByBossSlam && _playerDamagedByBossShot, $"boss damaged the player with slam ({_playerDamagedByBossSlam}) and shot ({_playerDamagedByBossShot})");
        }

        // ---- (f) each power alone defeats the boss ----

        private IEnumerator TestBossTimeToKill()
        {
            var results = new List<string>();
            foreach (var (ability, distance) in new[] { (_sword, 2.2f), (_bow, 8f), (_fire, 8f) })
            {
                _boss.ResetFor(RetryScope.BossArena);
                _player.ResetHealth();
                yield return new WaitForSeconds(0.3f);
                int defeats = _bossDefeats;
                _harness.Equip(ability);
                _harness.BeginBoss();
                float t0 = Time.time, tFirst = -1f;
                while (!_bossHealth.IsDead && Time.time - t0 < 60f)
                {
                    KeepPlayerAlive();
                    Vector3 flat = _harness.transform.position - _boss.transform.position; flat.y = 0f;
                    Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.left;
                    Teleport(_boss.transform.position + dir * distance + Vector3.up * 1.1f, _boss.transform.position);
                    if (_harness.Attack() && tFirst < 0f) tFirst = Time.time;
                    yield return null;
                }
                float ttk = Time.time - (tFirst < 0f ? t0 : tFirst);
                yield return new WaitForSeconds(0.3f);
                Check(_bossHealth.IsDead && _bossDefeats == defeats + 1, $"{ability.Type} alone defeated the boss; BossDefeated raised {_bossDefeats - defeats}x; time-to-kill {ttk:F1} s (player held at {distance} m, kept alive)");
                results.Add($"{ability.Type}={ttk:F1}s");
                // The bar hides on unscaled time (1 s) and the boss on scaled time (1.5 s): wait long enough for both.
                yield return new WaitForSecondsRealtime(1.3f);
                yield return new WaitForSeconds(1.6f);
                Check(!_bar.IsVisible, $"{ability.Type}: boss bar hidden after BossDefeated");
                Check(!_boss.gameObject.activeSelf, $"{ability.Type}: defeated boss switched off");
            }
            Note("TIME-TO-KILL " + string.Join(" ", results));
        }

        // ---- (g) boss-arena retry ----

        private IEnumerator TestBossRetry(List<EnemySpawner> arena, List<EnemySpawner> main)
        {
            foreach (EnemySpawner s in arena) s.gameObject.SetActive(true);
            EnemyBrain mainBeetle = main.Find(s => s.name.Contains("Beetle")).Current;
            mainBeetle.GetComponent<Health>().TryApplyDamage(Hit(1));
            int mainHp = mainBeetle.GetComponent<Health>().Current;

            _harness.Equip(_bow);
            Teleport(new Vector3(30f, 1.1f, 0f), new Vector3(42f, 1.1f, 0f));
            _harness.Attack(); // a live projectile that the retry must clear
            yield return null;
            int live = FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            _harness.Retry(RetryScope.BossArena);
            yield return null; yield return null;
            int after = FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            Check(live > 0 && after == 0, $"ResetFor(BossArena) cleared live projectiles ({live} -> {after})");
            Check(_boss.gameObject.activeSelf && _bossHealth.Current == _bossHealth.Max && _boss.Current == BossBrain.State.Idle && !_boss.EncounterBegun, "boss restored: active, full HP, Idle, encounter not begun");
            Check(mainBeetle.GetComponent<Health>().Current == mainHp, "main-area enemy left alone by the boss-arena retry");

            int starts = _encounterStarts;
            _harness.BeginBoss();
            yield return new WaitForSeconds(0.2f);
            Check(_encounterStarts == starts + 1 && _bar.IsVisible, "BeginEncounter works again after the retry (event raised, bar visible)");

            yield return new WaitForSeconds(0.5f);
            int arenaActive = 0;
            foreach (EnemySpawner s in arena) if (s.IsEnemyActive) arenaActive++;
            Check(arenaActive == arena.Count, $"arena adds reset/spawned by the boss retry ({arenaActive}/{arena.Count})");
        }
    }
}
