using System.Collections.Generic;
using Echoheart.Core;
using UnityEngine;

namespace Echoheart.Combat
{
    /// <summary>
    /// Single entry point for the player's ability holder: TryUse picks the IAbilityAttack that handles the
    /// ability's type. Handlers are assigned in the Inspector, or auto-collected from children if the list is empty.
    /// </summary>
    public class AbilityAttackRunner : MonoBehaviour
    {
        [Tooltip("Components implementing IAbilityAttack. Empty = auto-collect from children.")]
        [SerializeField] private List<MonoBehaviour> _handlerComponents = new List<MonoBehaviour>();

        private readonly List<IAbilityAttack> _handlers = new List<IAbilityAttack>();

        private void Awake() => CollectHandlers();

        private void OnValidate()
        {
            for (int i = 0; i < _handlerComponents.Count; i++)
            {
                if (_handlerComponents[i] != null && !(_handlerComponents[i] is IAbilityAttack))
                {
                    Debug.LogWarning($"{_handlerComponents[i].name} does not implement IAbilityAttack; removed from {name}.", this);
                    _handlerComponents[i] = null;
                }
            }
        }

        public bool TryUse(AbilityDefinition ability, Transform origin, Vector3 aimDirection, GameObject owner)
        {
            if (ability == null || ability.Type == AbilityType.None) return false;
            if (_handlers.Count == 0) CollectHandlers();

            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i].Handles(ability.Type))
                    return _handlers[i].TryUse(ability, origin, aimDirection, owner);
            }
            return false;
        }

        private void CollectHandlers()
        {
            _handlers.Clear();
            if (_handlerComponents.Count == 0)
            {
                GetComponentsInChildren(true, _handlers);
                return;
            }
            foreach (MonoBehaviour component in _handlerComponents)
                if (component is IAbilityAttack handler) _handlers.Add(handler);
        }
    }
}
