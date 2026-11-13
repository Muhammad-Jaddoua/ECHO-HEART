using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    public enum EnemyVariant
    {
        BladeBeetle, // Sword source, close attack
        ThornArcher, // Bow source, ranged
        Emberling    // Fire source, ranged
    }

    /// <summary>
    /// Tunables for one enemy variant. Variants differ ONLY by this asset plus which EnemyAttackBehaviour
    /// is on the prefab (Melee for Sword, Ranged for Bow/Fire), so balancing never needs code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Echoheart/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Label only; behaviour comes from the numbers below and the attached attack component.")]
        [SerializeField] private EnemyVariant _variant;
        [Tooltip("Power the player gets when this enemy is CAPTURED (inhaled). Defeating it grants nothing.")]
        [SerializeField] private AbilityDefinition _grantedAbility;

        [Header("Health")]
        [Tooltip("Must match the Health component on the prefab (Health has no runtime setter); EnemyBrain warns once if not.")]
        [SerializeField, Min(1)] private int _maxHealth = 3;

        [Header("Movement and senses")]
        [SerializeField, Min(0f)] private float _moveSpeed = 3f;
        [SerializeField, Min(0f)] private float _sightRange = 10f;
        [Tooltip("Must be >= sight range; the gap stops enemies flickering between Idle and Chase at the edge.")]
        [SerializeField, Min(0f)] private float _loseSightRange = 14f;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float _attackRange = 2f;
        [Tooltip("Telegraph time before the hit lands. The player's dodge window.")]
        [SerializeField, Min(0f)] private float _attackWindup = 0.5f;
        [Tooltip("Recovery after the attack, before the enemy may act again. The player's counter window.")]
        [SerializeField, Min(0f)] private float _attackCooldown = 1.2f;
        [SerializeField, Min(0)] private int _damage = 1;

        [Header("Ranged only (Thorn Archer, Emberling)")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _projectileSpeed = 12f;
        [SerializeField, Min(0f)] private float _projectileRange = 14f;

        public EnemyVariant Variant => _variant;
        public AbilityDefinition GrantedAbility => _grantedAbility;
        public int MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float SightRange => _sightRange;
        public float LoseSightRange => Mathf.Max(_loseSightRange, _sightRange);
        public float AttackRange => _attackRange;
        public float AttackWindup => _attackWindup;
        public float AttackCooldown => _attackCooldown;
        public int Damage => _damage;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileRange => _projectileRange;
    }
}
