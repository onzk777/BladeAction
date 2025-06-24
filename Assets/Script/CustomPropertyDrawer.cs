using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CommandSelection))]
public class CommandSelectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var parent = property.serializedObject.targetObject as PlayerController;
        if (parent == null || parent.equippedStyle == null || parent.equippedStyle.commandSet == null)
        {
            EditorGUI.LabelField(position, "No Style Assigned");
            return;
        }

        var commands = parent.equippedStyle.commandSet;
        string[] options = new string[commands.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            options[i] = commands[i]?.commandName ?? $"Command {i}";
        }

        var indexProp = property.FindPropertyRelative("index");
        indexProp.intValue = EditorGUI.Popup(position, label.text, indexProp.intValue, options);
    }
}
