using Echoheart.Core;

namespace Echoheart.World
{
    /// <summary>A chest is a one-time reveal of an existing relic, not another collectible currency.</summary>
    public sealed class ChestStateV15
    {
        public bool Opened { get; private set; }
        public bool TryOpen(GameState state, bool inRange)
        {
            if (Opened || state != GameState.Playing || !inRange) return false;
            Opened = true;
            return true;
        }
        public void ResetFor(RetryScope scope)
        {
            if (scope == RetryScope.NewGame) Opened = false;
        }
    }
}
