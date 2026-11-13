using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// Everything a target needs to react to a hit.
    /// AttackId identifies one swing or one projectile: a target ignores an id it has already taken,
    /// which is how "one attack damages each target at most once" is guaranteed.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly Team SourceTeam;
        public readonly int AttackId;
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector3 point, Vector3 direction, Team sourceTeam, int attackId, GameObject source)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            SourceTeam = sourceTeam;
            AttackId = attackId;
            Source = source;
        }
    }

    /// <summary>Hands out unique attack ids. Call once per swing / per projectile spawn.</summary>
    public static class AttackIds
    {
        private static int _next;

        public static int Next() => ++_next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _next = 0;
    }
}
