namespace Echoheart.Core
{
    /// <summary>Anything that can be hit: player, enemies, boss, breakables.</summary>
    public interface IDamageable
    {
        Team Team { get; }

        /// <summary>
        /// Apply a hit. Returns false when ignored (same team, already dead, invulnerable,
        /// or this AttackId was already taken).
        /// </summary>
        bool TryApplyDamage(in DamageInfo info);
    }
}
