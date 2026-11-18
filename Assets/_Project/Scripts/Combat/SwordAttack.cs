using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Combat
{
    /// <summary>
    /// Sword power: one short melee slash. Reads damage, cooldown and reach from the AbilityDefinition
    /// and opens the MeleeHitbox window in front of the attacker. This is the PLAYER's power (driven by the
    /// ability holder via IAbilityAttack); the team is still read from the owner. Enemies do not use it: they use
    /// EnemyAttackBehaviour subclasses (e.g. MeleeEnemyAttack) driven by EnemyDefinition, sharing only MeleeHitbox.
    /// </summary>
    public class SwordAttack : MonoBehaviour, IAbilityAttack
    {
        [SerializeField] private MeleeHitbox _hitbox;
        [Tooltip("Width of the slash box (reach comes from AbilityDefinition.Range).")]
        [SerializeField, Min(0.1f)] private float _width = 1.6f;
        [Tooltip("Leave at Nothing to use the default: enemy layers when the owner is Player team, player layer when Enemy team.")]
        [SerializeField] private LayerMask _hitMaskOverride;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private UnityEvent _onSwing;

        private float _nextUseTime;

        public bool Handles(AbilityType type) => type == AbilityType.Sword;

        public bool TryUse(AbilityDefinition ability, Transform origin, Vector3 aimDirection, GameObject owner)
        {
            if (ability == null || ability.Type != AbilityType.Sword || _hitbox == null || origin == null) return false;
            if (Time.time < _nextUseTime) return false;
            _nextUseTime = Time.time + ability.Cooldown;

            // Sword aims along the ground plane so looking up/down does not tilt the box.
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.0001f) aimDirection = origin.forward;
            _hitbox.transform.SetPositionAndRotation(origin.position, Quaternion.LookRotation(aimDirection.normalized, Vector3.up));
            _hitbox.Configure(ability.Range, _width);

            Team team = Team.Player;
            if (owner != null)
            {
                IDamageable damageable = owner.GetComponentInParent<IDamageable>();
                if (damageable != null) team = damageable.Team;
            }
            LayerMask mask = _hitMaskOverride.value != 0
                ? _hitMaskOverride
                : (team == Team.Player ? Layers.EnemiesMask : Layers.PlayerMask);

            _hitbox.Swing(ability.Damage, team, owner, mask);

            if (_audioSource != null && ability.UseSfx != null) _audioSource.PlayOneShot(ability.UseSfx);
            if (ability.UseVfx != null) Instantiate(ability.UseVfx, origin.position, _hitbox.transform.rotation);
            _onSwing.Invoke();
            return true;
        }
    }
}
