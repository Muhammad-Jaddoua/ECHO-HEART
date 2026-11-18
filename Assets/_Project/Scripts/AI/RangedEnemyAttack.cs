using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// Ranged attack (Thorn Archer, Emberling): fires the shared Projectile. Arrow vs fire bolt is only
    /// the prefab and numbers in the EnemyDefinition.
    /// </summary>
    public class RangedEnemyAttack : EnemyAttackBehaviour
    {
        [SerializeField] private Transform _muzzle;
        [Tooltip("Aim this high above the target's feet so shots hit the body, not the floor.")]
        [SerializeField] private float _aimHeight = 1f;

        // Projectiles stop on walls too, so cover works.
        private static readonly int HitMask = Layers.PlayerMask | Layers.EnvironmentMask;

        public override void Execute(EnemyDefinition definition, Transform target)
        {
            if (definition.ProjectilePrefab == null)
            {
                Debug.LogError($"[{name}] EnemyDefinition '{definition.name}' has no projectile prefab.", this);
                return;
            }

            Vector3 origin = _muzzle != null ? _muzzle.position : transform.position + Vector3.up;
            Vector3 aimPoint = target.position + Vector3.up * _aimHeight;
            Vector3 direction = (aimPoint - origin).normalized;

            GameObject instance = Instantiate(definition.ProjectilePrefab, origin, Quaternion.LookRotation(direction));
            if (!instance.TryGetComponent(out Projectile projectile))
            {
                Debug.LogError($"[{name}] Projectile prefab '{definition.ProjectilePrefab.name}' has no Projectile component.", this);
                Destroy(instance);
                return;
            }

            projectile.Launch(direction, definition.ProjectileSpeed, definition.ProjectileRange,
                definition.Damage, Team.Enemy, gameObject, HitMask);
        }
    }
}
