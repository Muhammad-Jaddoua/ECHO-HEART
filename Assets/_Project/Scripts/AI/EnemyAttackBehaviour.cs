using UnityEngine;

namespace Echoheart.AI
{
    /// <summary>
    /// The one thing that differs between melee and ranged enemies: what happens at the moment the
    /// attack lands. EnemyBrain owns timing (windup, recovery); this only performs the hit.
    /// </summary>
    public abstract class EnemyAttackBehaviour : MonoBehaviour
    {
        /// <summary>Called once per attack, after the windup. The brain has already turned to face the target.</summary>
        public abstract void Execute(EnemyDefinition definition, Transform target);

        /// <summary>Abort any attack still in progress (defeat, inhale, reset). Default: nothing is lingering.</summary>
        public virtual void Cancel() { }
    }
}
