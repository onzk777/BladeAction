#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BladeAction.BT.Editor
{
    /// <summary>
    /// BehaviorTreeData Custom Editor
    /// Condition 노드와 Action 노드의 세부 설정을 인라인으로 표시합니다.
    /// </summary>
    [CustomEditor(typeof(BehaviorTreeData))]
    public class BehaviorTreeDataEditor : UnityEditor.Editor
    {
        private SerializedProperty entriesProperty;
        private Dictionary<int, bool> entryFoldouts = new Dictionary<int, bool>();
        private Dictionary<string, UnityEditor.Editor> nodeEditors = new Dictionary<string, UnityEditor.Editor>();
        
        private void OnEnable()
        {
            entriesProperty = serializedObject.FindProperty("entries");
        }
        
        private void OnDisable()
        {
            // 생성된 Editor 정리
            foreach (var editor in nodeEditors.Values)
            {
                if (editor != null)
                    DestroyImmediate(editor);
            }
            nodeEditors.Clear();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Description 필드
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("BT Entries", EditorStyles.boldLabel);
            
            // Entry 리스트
            if (entriesProperty != null && entriesProperty.isArray)
            {
                for (int i = 0; i < entriesProperty.arraySize; i++)
                {
                    DrawEntry(i);
                }
                
                // Entry 추가 버튼
                EditorGUILayout.Space(5);
                if (GUILayout.Button("+ Entry 추가"))
                {
                    entriesProperty.InsertArrayElementAtIndex(entriesProperty.arraySize);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Entry 하나를 그립니다
        /// </summary>
        private void DrawEntry(int index)
        {
            SerializedProperty entryProp = entriesProperty.GetArrayElementAtIndex(index);
            SerializedProperty conditionProp = entryProp.FindPropertyRelative("condition");
            SerializedProperty actionsProp = entryProp.FindPropertyRelative("actions");
            SerializedProperty isEnabledProp = entryProp.FindPropertyRelative("isEnabled");
            SerializedProperty descriptionProp = entryProp.FindPropertyRelative("description");
            
            // Foldout 상태 가져오기
            if (!entryFoldouts.ContainsKey(index))
                entryFoldouts[index] = false;
            
            // Entry 박스
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Entry 헤더
            EditorGUILayout.BeginHorizontal();
            
            // 활성화 체크박스
            bool wasEnabled = isEnabledProp.boolValue;
            bool isEnabled = EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(20));
            if (isEnabled != wasEnabled)
            {
                isEnabledProp.boolValue = isEnabled;
            }
            
            // Foldout
            string entryLabel = string.IsNullOrEmpty(descriptionProp.stringValue) 
                ? $"Entry [{index}]" 
                : $"Entry [{index}]: {descriptionProp.stringValue}";
            
            entryFoldouts[index] = EditorGUILayout.Foldout(entryFoldouts[index], entryLabel, true);
            
            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                entriesProperty.DeleteArrayElementAtIndex(index);
                entryFoldouts.Remove(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Entry 내용 (펼쳐진 경우만)
            if (entryFoldouts[index])
            {
                EditorGUI.indentLevel++;
                
                // Description
                EditorGUILayout.PropertyField(descriptionProp, new GUIContent("설명"));
                
                EditorGUILayout.Space(5);
                
                // Condition 노드
                EditorGUILayout.LabelField("조건", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(conditionProp, new GUIContent("Condition Node"));
                
                // Condition 노드 인라인 편집 ✨
                if (conditionProp.objectReferenceValue != null)
                {
                    DrawInlineNodeEditor(conditionProp.objectReferenceValue, "Condition");
                }
                
                EditorGUILayout.Space(5);
                
                // Actions
                EditorGUILayout.LabelField("액션들", EditorStyles.boldLabel);
                DrawActionWrapperList(actionsProp);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        /// <summary>
        /// ActionWrapper 리스트 그리기
        /// </summary>
        private void DrawActionWrapperList(SerializedProperty actionsProp)
        {
            if (actionsProp == null || !actionsProp.isArray)
                return;
            
            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                SerializedProperty wrapperProp = actionsProp.GetArrayElementAtIndex(i);
                SerializedProperty nodeProp = wrapperProp.FindPropertyRelative("node");
                SerializedProperty wrapperEnabledProp = wrapperProp.FindPropertyRelative("isEnabled");
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                
                // 활성화 체크박스
                bool wasEnabled = wrapperEnabledProp.boolValue;
                bool isEnabled = EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(20));
                if (isEnabled != wasEnabled)
                {
                    wrapperEnabledProp.boolValue = isEnabled;
                }
                
                // 액션 노드
                EditorGUILayout.PropertyField(nodeProp, new GUIContent($"Action {i}"));
                
                // 삭제 버튼
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    actionsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 액션 노드 인라인 편집 ✨
                if (nodeProp.objectReferenceValue != null)
                {
                    DrawInlineNodeEditor(nodeProp.objectReferenceValue, $"Action{i}");
                }
                
                EditorGUILayout.EndVertical();
            }
            
            // 액션 추가 버튼
            if (GUILayout.Button("+ 액션 추가"))
            {
                actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
            }
        }
        
        /// <summary>
        /// 노드의 Inspector를 인라인으로 그립니다
        /// </summary>
        private void DrawInlineNodeEditor(Object nodeObject, string key)
        {
            if (nodeObject == null)
                return;
            
            // Editor 캐싱
            string editorKey = $"{key}_{nodeObject.GetInstanceID()}";
            if (!nodeEditors.ContainsKey(editorKey) || nodeEditors[editorKey] == null)
            {
                nodeEditors[editorKey] = UnityEditor.Editor.CreateEditor(nodeObject);
            }
            
            if (nodeEditors[editorKey] != null)
            {
                EditorGUI.indentLevel++;
                
                // 인라인 Inspector 표시
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"└ {nodeObject.name} 설정", EditorStyles.miniLabel);
                
                // 노드의 Inspector 그리기 (기본 Inspector)
                EditorGUI.BeginChangeCheck();
                nodeEditors[editorKey].OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(nodeObject);
                }
                
                // Composite 노드의 children도 재귀적으로 표시
                DrawCompositeChildren(nodeObject);
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Composite 노드의 children을 재귀적으로 표시
        /// </summary>
        private void DrawCompositeChildren(Object nodeObject)
        {
            // Sequence 또는 Selector 노드인지 확인
            if (nodeObject is BTComposite_Sequence || nodeObject is BTComposite_Selector)
            {
                SerializedObject nodeSO = new SerializedObject(nodeObject);
                SerializedProperty childrenProp = nodeSO.FindProperty("children");
                
                if (childrenProp != null && childrenProp.isArray && childrenProp.arraySize > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("└ 자식 조건들:", EditorStyles.miniLabel);
                    
                    EditorGUI.indentLevel++;
                    
                    for (int i = 0; i < childrenProp.arraySize; i++)
                    {
                        SerializedProperty childProp = childrenProp.GetArrayElementAtIndex(i);
                        if (childProp.objectReferenceValue != null)
                        {
                            EditorGUILayout.LabelField($"  [{i}] {childProp.objectReferenceValue.name}", EditorStyles.miniLabel);
                            
                            // 자식 노드도 인라인 편집 가능 (재귀)
                            DrawInlineNodeEditor(childProp.objectReferenceValue, $"Child{i}");
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
#endif


















