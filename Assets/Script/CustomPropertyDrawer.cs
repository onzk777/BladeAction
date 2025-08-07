using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CommandSelection))]
public class CommandSelectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var styleProp = property.serializedObject.FindProperty("equippedStyle");
        EditorGUI.PropertyField(position, styleProp, label);
    }
}
