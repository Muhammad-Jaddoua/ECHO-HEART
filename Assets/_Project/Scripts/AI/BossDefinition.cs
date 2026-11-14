using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Every tunable of the Corrupted Guardian. BossBrain contains no numbers, so balancing ("each power
    /// alone can win") is a data change, not a code change.
    /// </summary>
    [CreateAssetMenu(menuName = "Echoheart/Boss Definition", fileName = "Boss_")]
    public class BossDefinition : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Must match the Health component on the prefab (Health has no runtime setter); BossBrain warns once if not.")]
        [SerializeField, Min(1)] private int _maxHealth = 14;

        [Header("Movement and senses")]
        [Tooltip("Slow on purpose: the boss is a patient threat, the player's dodge is the skill check.")]
        [SerializeField, Min(0f)] private float _moveSpeed = 2f;
        [SerializeField, Min(0f)] private float _turnSpeed = 180f;
        [Tooltip("Aggro radius: the boss notices the player (Player layer) inside it once the encounter has begun.")]
        [SerializeField, Min(0f)] private float _sightRange = 30f;

        [Header("Rhythm")]
        [Tooltip("Pause in Idle before the next attack is chosen. The boss still walks toward the player during it.")]
        [SerializeField, Min(0f)] private float _idlePause = 0.6f;
        [Tooltip("How long the attack state lasts (the strike pose) before recovery starts.")]
        [SerializeField, Min(0f)] private float _attackActiveSeconds = 0.25f;

        [Header("Slam (close range)")]
        [Tooltip("Player closer than this when the boss decides = Slam, otherwise = Ranged.")]
        [SerializeField, Min(0f)] private float _slamRange = 4f;
        [SerializeField, Min(0f)] private float _slamTelegraph = 1f;
        [SerializeField, Min(0f)] private float _slamRadius = 4.5f;
        [SerializeField, Min(0)] private int _slamDamage = 2;
        [Tooltip("Vulnerable, non-attacking window after a slam: the Sword's safe approach and attack time.")]
        [SerializeField, Min(0f)] private float _slamRecover = 1.5f;

        [Header("Ranged shot")]
        [SerializeField, Min(0f)] private float _rangedTelegraph = 0.9f;
        [SerializeField, Min(0)] private int _rangedDamage = 1;
        [Tooltip("Needs a Projectile component on the prefab root.")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _projectileSpeed = 12f;
        [SerializeField, Min(0f)] private float _projectileRange = 40f;
        [SerializeField, Min(0f)] private float _rangedRecover = 1.2f;

        [Header("Defeat")]
        [Tooltip("The boss stays active this long after BossDefeated so listeners (Heart) and the death feedback can react.")]
        [SerializeField, Min(0f)] private float _defeatDeactivateDelay = 1.5f;

        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float TurnSpeed => _turnSpeed;
        public float SightRange => _sightRange;
        public float IdlePause => _idlePause;
        public float AttackActiveSeconds => _attackActiveSeconds;
        public float SlamRange => _slamRange;
        public float SlamTelegraph => _slamTelegraph;
        public float SlamRadius => _slamRadius;
        public int SlamDamage => _slamDamage;
        public float SlamRecover => _slamRecover;
        public float RangedTelegraph => _rangedTelegraph;
        public int RangedDamage => _rangedDamage;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRange => _projectileRange;
        public float RangedRecover => _rangedRecover;
        public float DefeatDeactivateDelay => _defeatDeactivateDelay;
    }
}
