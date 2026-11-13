namespace Echoheart.Core
{
    /// <summary>The copyable powers. None = no power held (start of game, after main-area retry).</summary>
    public enum AbilityType { None, Sword, Bow, Fire }

    /// <summary>Which side an attack belongs to. Damage is ignored when source and target share a team.</summary>
    public enum Team { Player, Enemy }

    /// <summary>Top-level game flow, owned by the GameManager.</summary>
    public enum GameState { Title, Playing, Paused, Dead, Complete }

    /// <summary>
    /// What a retry resets. MainArea: back to entry, no power, enemies reset.
    /// BossArena: arena retry point, player + boss HP restored, selected power kept.
    /// NewGame: everything, including collected items.
    /// </summary>
    public enum RetryScope { MainArea, BossArena, NewGame }
}
