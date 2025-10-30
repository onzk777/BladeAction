#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// ActionCommandDatabase Custom Editor
/// Inspector 높이 조정 및 편의 기능 제공
/// </summary>
[CustomEditor(typeof(ActionCommandDatabase))]
public class ActionCommandDatabaseEditor : Editor
{
    private SerializedProperty actionsProperty;
    
    private void OnEnable()
    {
        actionsProperty = serializedObject.FindProperty("actions");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // 스크립트 필드 (읽기 전용)
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }
        
        EditorGUILayout.Space();
        
        // Actions 리스트 (Unity 기본 렌더링 사용 - + - 버튼 포함)
        if (actionsProperty != null)
        {
            EditorGUILayout.PropertyField(actionsProperty, new GUIContent("검술 매핑 테이블"), true);
        }
        
        EditorGUILayout.Space();
        
        // 유틸리티 버튼들
        EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto Sync Keys", GUILayout.Height(30)))
        {
            var db = target as ActionCommandDatabase;
            if (db != null)
            {
                // ContextMenu 메서드를 리플렉션으로 호출
                var method = typeof(ActionCommandDatabase).GetMethod("AutoSyncKeys", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(db, null);
                }
            }
        }
        
        if (GUILayout.Button("Validate Keys", GUILayout.Height(30)))
        {
            var db = target as ActionCommandDatabase;
            if (db != null)
            {
                // ContextMenu 메서드를 리플렉션으로 호출
                var method = typeof(ActionCommandDatabase).GetMethod("ValidateKeys", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(db, null);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

