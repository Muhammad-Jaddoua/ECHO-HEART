namespace Echoheart.Core
{
    /// <summary>
    /// Anything that must return to a known state on retry: player, enemies, spawners, boss, collectibles.
    /// The RetryService finds every IResettable in the scene and calls ResetFor with the scope.
    /// Each implementer decides what the scope means for it (e.g. collectibles only reset on NewGame).
    /// </summary>
    public interface IResettable
    {
        void ResetFor(RetryScope scope);
    }
}
