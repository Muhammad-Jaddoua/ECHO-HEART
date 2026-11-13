using UnityEngine;

namespace Echoheart.Core
{
    /// <summary>
    /// Implemented by normal enemies (and test dummies). The player's inhale talks only to this interface,
    /// so inhale (Player) and enemies (AI) can be built in parallel.
    /// Expected call order: OnInhaleStart -> (OnInhaleCancel | OnCaptured). OnCaptured must run at most once.
    /// </summary>
    public interface IInhalable
    {
        /// <summary>False for the boss, and for enemies already captured, defeated, or mid-capture by something else.</summary>
        bool CanBeInhaled { get; }

        /// <summary>Power the player receives on capture.</summary>
        AbilityDefinition GrantedAbility { get; }

        /// <summary>World position the inhale aims at and pulls toward the player.</summary>
        Transform InhaleTarget { get; }

        /// <summary>Pull started: stop AI, play struggle feedback.</summary>
        void OnInhaleStart(Transform inhaler);

        /// <summary>Player released E or lost line of sight before the pull finished: resume AI.</summary>
        void OnInhaleCancel();

        /// <summary>Pull finished: remove/despawn this enemy and notify its spawner. Exactly once.</summary>
        void OnCaptured();
    }
}
