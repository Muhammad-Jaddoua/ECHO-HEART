using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// Physics layer indices, matching ProjectSettings/TagManager. Use these instead of magic numbers.
    /// The collision matrix makes PlayerAttack ignore Player and EnemyAttack ignore Enemy/Boss.
    /// </summary>
    public static class Layers
    {
        public const int Player = 8;
        public const int Enemy = 9;
        public const int Boss = 10;
        public const int PlayerAttack = 11;
        public const int EnemyAttack = 12;
        public const int Environment = 13;
        public const int Collectible = 14;
        public const int Trigger = 15;

        public static readonly int EnvironmentMask = 1 << Environment;
        public static readonly int EnemiesMask = (1 << Enemy) | (1 << Boss);
        public static readonly int PlayerMask = 1 << Player;
    }
}
