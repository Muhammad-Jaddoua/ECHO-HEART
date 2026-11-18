using System.Collections.Generic;
using Echoheart.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Echoheart.Combat
{
    /// <summary>
    /// Fires Bow and Fire projectiles. Both powers share this component; they differ only by AbilityDefinition
    /// data (damage, cooldown, range, speed, prefab, VFX). Cooldown is tracked per power type so switching
    /// powers cannot bypass it. This is the PLAYER's power; the team is still read from the owner. Enemies do not
    /// use it: they use EnemyAttackBehaviour subclasses (RangedEnemyAttack) driven by EnemyDefinition, sharing only Projectile.
    /// </summary>
    public class ProjectileAttack : MonoBehaviour, IAbilityAttack
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private UnityEvent _onFired;

        private readonly Dictionary<AbilityType, float> _readyAt = new Dictionary<AbilityType, float>();
        private readonly HashSet<AbilityType> _warnedMissingPrefab = new HashSet<AbilityType>();

        public bool Handles(AbilityType type) => type == AbilityType.Bow || type == AbilityType.Fire;

        public bool TryUse(AbilityDefinition ability, Transform origin, Vector3 aimDirection, GameObject owner)
        {
            if (ability == null || origin == null || !Handles(ability.Type)) return false;

            if (ability.ProjectilePrefab == null)
            {
                if (_warnedMissingPrefab.Add(ability.Type))
                    Debug.LogWarning($"{ability.name} has no ProjectilePrefab; {ability.Type} cannot fire.", this);
                return false;
            }

            if (_readyAt.TryGetValue(ability.Type, out float readyAt) && Time.time < readyAt) return false;

            Vector3 aim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : origin.forward;
            GameObject instance = Instantiate(ability.ProjectilePrefab, origin.position, Quaternion.LookRotation(aim));

            if (!instance.TryGetComponent(out Projectile projectile))
                projectile = instance.GetComponentInChildren<Projectile>();
            if (projectile == null)
            {
                Debug.LogWarning($"{ability.ProjectilePrefab.name} has no Projectile component.", this);
                Destroy(instance);
                return false;
            }

            Team team = ResolveTeam(owner);
            bool playerShot = team == Team.Player;
            SetLayerRecursive(instance, playerShot ? Layers.PlayerAttack : Layers.EnemyAttack);
            LayerMask mask = playerShot ? Layers.EnemiesMask : Layers.PlayerMask;

            projectile.Launch(aim, ability.ProjectileSpeed, ability.Range, ability.Damage, team, owner, mask);
            _readyAt[ability.Type] = Time.time + ability.Cooldown;

            if (_audioSource != null && ability.UseSfx != null) _audioSource.PlayOneShot(ability.UseSfx);
            if (ability.UseVfx != null) Instantiate(ability.UseVfx, origin.position, Quaternion.LookRotation(aim));

            _onFired.Invoke();
            return true;
        }

        private static Team ResolveTeam(GameObject owner)
        {
            if (owner == null) return Team.Player;
            IDamageable damageable = owner.GetComponentInParent<IDamageable>();
            if (damageable == null) damageable = owner.GetComponentInChildren<IDamageable>();
            return damageable != null ? damageable.Team : Team.Player;
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }
    }
}
