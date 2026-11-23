namespace Echoheart.World
{
    /// <summary>Presentation fallback for a player impact left behind by recovery/teleport.</summary>
    public static class HitFeedbackPointV123
    {
        public const float MaximumDistanceSquared = 64f;
        public static bool UseActor(bool isPlayer, float squaredDistance) => isPlayer &&
            (float.IsNaN(squaredDistance) || float.IsInfinity(squaredDistance) || squaredDistance < 0f ||
             squaredDistance > MaximumDistanceSquared);
    }
}
