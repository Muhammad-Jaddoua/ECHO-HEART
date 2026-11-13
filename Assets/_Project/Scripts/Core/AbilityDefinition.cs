using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// Data for one copyable power. One asset per power lives in Data/Abilities.
    /// Tuning (damage, cooldown, range) happens here so balancing never needs code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Echoheart/Ability Definition", fileName = "Ability_")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private AbilityType _type;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [Tooltip("Small prop shown on the spirit while this power is held (hat, bow, flame).")]
        [SerializeField] private GameObject _accessoryPrefab;

        [Header("Attack")]
        [SerializeField, Min(0)] private int _damage = 1;
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;
        [Tooltip("Sword: reach of the hit box. Bow/Fire: max projectile travel distance.")]
        [SerializeField, Min(0f)] private float _range = 2f;
        [Tooltip("Bow/Fire only. Leave empty for Sword.")]
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField, Min(0f)] private float _projectileSpeed = 20f;

        [Header("Feedback")]
        [SerializeField] private AudioClip _useSfx;
        [SerializeField] private GameObject _useVfx;

        public AbilityType Type => _type;
        public string DisplayName => _displayName;
        public Sprite Icon => _icon;
        public GameObject AccessoryPrefab => _accessoryPrefab;
        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float Range => _range;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public bool IsRanged => _projectilePrefab != null;
        public AudioClip UseSfx => _useSfx;
        public GameObject UseVfx => _useVfx;
    }
}
