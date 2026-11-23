using System.Collections.Generic;
using Echoheart.AI;
using Echoheart.Combat;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.World
{
    /// <summary>One reset transaction. Spawners retain ownership of their inactive enemies.</summary>
    public sealed class RetryServiceV13 : MonoBehaviour
    {
        public void ResetWorld(RetryScope scope)
        {
            Projectile.DestroyAllActive();
            MonoBehaviour[] components = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ownedEnemies = new HashSet<EnemyBrain>();
            foreach (MonoBehaviour component in components)
                if (component is EnemySpawner spawner && spawner.Current != null) ownedEnemies.Add(spawner.Current);

            foreach (MonoBehaviour component in components)
            {
                if (component == null || !(component is IResettable resettable)) continue;
                if (component is EnemyBrain enemy && ownedEnemies.Contains(enemy)) continue;
                resettable.ResetFor(scope);
            }
            Physics.SyncTransforms();
        }
    }
}
