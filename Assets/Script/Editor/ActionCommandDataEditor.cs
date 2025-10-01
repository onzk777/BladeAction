#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// ActionCommandData Custom Editor
/// ActionCommandTagList를 이용한 태그 Dropdown 제공
/// </summary>
[CustomEditor(typeof(ActionCommandData))]
[CanEditMultipleObjects]
public class ActionCommandDataEditor : Editor
{
    private ActionCommandTagList tagList;
    private int selectedTagIndex = 0;
    
    void OnEnable()
    {
        // Resources에서 TagList 로드
        tagList = Resources.Load<ActionCommandTagList>("ActionCommandTagList");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // tags 필드를 제외한 나머지 필드들을 그리기
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // "tags" 필드는 건너뛰기 (Custom UI로 대체)
            if (property.name == "tags")
                continue;
            
            // Script 필드는 읽기 전용으로 표시
            if (property.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // 태그 관리 섹션
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("태그 관리", EditorStyles.boldLabel);
        
        if (tagList == null)
        {
            EditorGUILayout.HelpBox(
                "ActionCommandTagList를 찾을 수 없습니다.\n" +
                "Resources/ActionCommandTagList.asset을 생성하세요.\n\n" +
                "생성 방법:\n" +
                "1. Project 창에서 Resources 폴더 생성 (없다면)\n" +
                "2. Resources 폴더 우클릭 → Create → Combat → Tag List\n" +
                "3. 파일 이름을 'ActionCommandTagList'로 설정", 
                MessageType.Warning);
            return;
        }
        
        // 사용 가능한 태그 목록 가져오기
        var availableTags = tagList.GetAllTagNames();
        if (availableTags.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "사용 가능한 태그가 없습니다.\n" +
                "ActionCommandTagList에 태그를 추가하세요.", 
                MessageType.Info);
            return;
        }
        
        var actionCommandData = (ActionCommandData)target;
        
        // 태그 추가 UI
        EditorGUILayout.BeginHorizontal();
        selectedTagIndex = EditorGUILayout.Popup("태그 추가", selectedTagIndex, availableTags.ToArray());
        
        if (GUILayout.Button("추가", GUILayout.Width(60)))
        {
            string selectedTag = availableTags[selectedTagIndex];
            
            // Multi-Edit 지원
            foreach (Object obj in targets)
            {
                var data = obj as ActionCommandData;
                if (data == null) continue;
                
                if (data.tags == null)
                {
                    data.tags = new List<string>();
                }
                
                if (!data.tags.Contains(selectedTag))
                {
                    data.tags.Add(selectedTag);
                    EditorUtility.SetDirty(data);
                }
            }
            
            Debug.Log($"[ActionCommandDataEditor] 태그 '{selectedTag}' 추가됨 ({targets.Length}개 오브젝트)");
            
            // 변경사항 즉시 반영
            serializedObject.Update();
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        
        // 현재 태그 리스트 표시
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 태그 목록:", EditorStyles.boldLabel);
        
        // 디버그: 태그 개수 표시
        if (actionCommandData.tags != null)
        {
            EditorGUILayout.LabelField($"  총 {actionCommandData.tags.Count}개의 태그", EditorStyles.miniLabel);
        }
        
        // Multi-Edit 시 첫 번째 오브젝트의 태그만 표시
        if (actionCommandData.tags == null || actionCommandData.tags.Count == 0)
        {
            EditorGUILayout.LabelField("  (태그 없음)", EditorStyles.miniLabel);
        }
        else
        {
            // 박스로 감싸서 명확하게 표시
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 정상 순회 (Count로 제어)
            int tagCount = actionCommandData.tags.Count;
            Debug.Log($"[ActionCommandDataEditor] 태그 개수: {tagCount}, 첫 번째 태그: {(tagCount > 0 ? actionCommandData.tags[0] : "없음")}");
            
            for (int i = 0; i < tagCount; i++)
            {
                string currentTag = actionCommandData.tags[i];
                
                // null이나 빈 문자열 체크
                if (string.IsNullOrEmpty(currentTag))
                {
                    currentTag = "(빈 태그)";
                }
                
                EditorGUILayout.BeginHorizontal();
                
                // 간단한 텍스트 표시 (스타일 없이)
                GUILayout.Label($"● {currentTag}", GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("제거", GUILayout.Width(60)))
                {
                    string tagToRemove = actionCommandData.tags[i];
                    
                    // Multi-Edit 지원
                    foreach (Object obj in targets)
                    {
                        var data = obj as ActionCommandData;
                        if (data == null) continue;
                        
                        if (data.tags != null && data.tags.Contains(tagToRemove))
                        {
                            data.tags.Remove(tagToRemove);
                            EditorUtility.SetDirty(data);
                        }
                    }
                    
                    Debug.Log($"[ActionCommandDataEditor] 태그 '{tagToRemove}' 제거됨 ({targets.Length}개 오브젝트)");
                    
                    // 변경사항 즉시 반영
                    serializedObject.Update();
                    Repaint();
                    break; // 리스트가 수정되었으므로 루프 탈출
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        if (targets.Length > 1)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"{targets.Length}개의 오브젝트를 동시에 편집 중입니다.\n첫 번째 오브젝트의 태그 목록이 표시됩니다.", MessageType.Info);
        }
        
        // 경고: TagList에 없는 태그 체크
        EditorGUILayout.Space();
        if (actionCommandData.tags != null)
        {
            foreach (var tag in actionCommandData.tags)
            {
                if (!tagList.IsValidTag(tag))
                {
                    EditorGUILayout.HelpBox(
                        $"경고: 태그 '{tag}'는 ActionCommandTagList에 없습니다.\n" +
                        "제거하거나 TagList에 추가하세요.", 
                        MessageType.Warning);
                }
            }
        }
    }
}
#endif

