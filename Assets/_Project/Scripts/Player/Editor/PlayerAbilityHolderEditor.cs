using Echoheart.Core;
using Echoheart.Player;
using UnityEditor;
using UnityEngine;

namespace Echoheart.EditorTools
{
    /// <summary>
    /// Shows the held power at the top of the PlayerAbilityHolder inspector. Read-only on purpose: the only way to
    /// get a power in game is inhaling, so the inspector must not become a second way to set it.
    /// </summary>
    [CustomEditor(typeof(PlayerAbilityHolder))]
    public class PlayerAbilityHolderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var holder = (PlayerAbilityHolder)target;
            using (new EditorGUI.DisabledScope(true))
            {
                if (Application.isPlaying)
                    EditorGUILayout.ObjectField("Held Power", holder.Current, typeof(AbilityDefinition), false);
                else
                    EditorGUILayout.LabelField("Held Power", "(shown in Play mode)");
            }
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        // Keep the field live while playing; the inspector otherwise only repaints on mouse movement.
        public override bool RequiresConstantRepaint() => Application.isPlaying;
    }
}
