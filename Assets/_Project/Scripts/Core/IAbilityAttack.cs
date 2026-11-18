using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// How a held power is fired. The player's ability holder calls TryUse when the attack button is pressed;
    /// Combat provides one implementation per power type (Sword, Bow/Fire). The holder never needs to know how
    /// a swing or arrow works, so Player and Combat can be built in parallel.
    /// Each implementation owns its own cooldown (read from the AbilityDefinition) and returns false while on cooldown.
    /// The attacker's team is read from the owner's IDamageable, so enemies can reuse the same attacks.
    /// </summary>
    public interface IAbilityAttack
    {
        /// <summary>The power this implementation handles. Bow and Fire share one implementation but each has its own type entry.</summary>
        bool Handles(AbilityType type);

        /// <summary>
        /// Fire the power once.
        /// <paramref name="origin"/>: where the attack starts (spirit's hand / muzzle).
        /// <paramref name="aimDirection"/>: normalized world direction (camera aim for Bow/Fire, facing for Sword).
        /// </summary>
        /// <returns>True if the attack started; false if on cooldown or invalid.</returns>
        bool TryUse(AbilityDefinition ability, Transform origin, Vector3 aimDirection, GameObject owner);
    }
}
