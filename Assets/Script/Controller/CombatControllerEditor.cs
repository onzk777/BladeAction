#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonoBehaviour), true)]
public class CombatControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ICombatController controller = target as ICombatController;
        if (controller == null || controller.EquippedStyle == null || controller.EquippedStyle.CommandSet == null)
            return;

        var commands = controller.EquippedStyle.CommandSet;
        string[] commandNames = new string[commands.Count];
        for (int i = 0; i < commands.Count; i++)
        {
            commandNames[i] = commands[i].commandName;
        }

        int selectedIndex = controller.TestCommandIndex;
        int newIndex = EditorGUILayout.Popup("Test Command", selectedIndex, commandNames);
        if (newIndex != selectedIndex)
        {
            controller.TestCommandIndex = newIndex;
            EditorUtility.SetDirty((Object)target);
        }
    }
}
#endif
