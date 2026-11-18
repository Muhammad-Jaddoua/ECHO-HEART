using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>Close attack (Blade Beetle): opens the shared melee hit window. One swing hits the player once.</summary>
    public class MeleeEnemyAttack : EnemyAttackBehaviour
    {
        [Tooltip("Hitbox child placed in front of the enemy. Same component the player's Sword uses.")]
        [SerializeField] private MeleeHitbox _hitbox;

        public override void Execute(EnemyDefinition definition, Transform target)
        {
            // Enemy swings only look for the player layer, so they can never hurt other enemies.
            _hitbox.Swing(definition.Damage, Team.Enemy, gameObject, Layers.PlayerMask);
        }

        public override void Cancel()
        {
            if (_hitbox != null) _hitbox.Cancel();
        }
    }
}
