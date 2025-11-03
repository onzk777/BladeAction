#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace BladeAction.Editor
{
    /// <summary>
    /// CharacterInitDataKeyAttribute를 위한 PropertyDrawer
    /// 프로젝트의 모든 CharacterInitData 에셋을 자동으로 검색하여 key 목록을 콤보박스로 표시
    /// </summary>
    [CustomPropertyDrawer(typeof(CharacterInitDataKeyAttribute))]
    public class CharacterInitDataKeyDrawer : PropertyDrawer
    {
        private static List<string> cachedKeys;
        private static double lastUpdateTime;
        private const double UPDATE_INTERVAL = 5.0; // 5초마다 갱신
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "CharacterInitDataKey는 string 타입에만 사용 가능합니다.");
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            
            // 캐시 갱신 필요 여부 확인
            bool needsUpdate = cachedKeys == null || 
                              EditorApplication.timeSinceStartup - lastUpdateTime > UPDATE_INTERVAL;
            
            if (needsUpdate)
            {
                RefreshKeys();
            }
            
            if (cachedKeys.Count == 0)
            {
                // CharacterInitData가 없으면 일반 텍스트 필드
                EditorGUI.PropertyField(position, property, label);
                
                var warningRect = new Rect(position.xMax - 20, position.y, 20, position.height);
                EditorGUI.LabelField(warningRect, new GUIContent("⚠", "프로젝트에 CharacterInitData가 없습니다."));
            }
            else
            {
                // 드롭다운 표시
                DrawDropdown(position, property, label);
            }
            
            EditorGUI.EndProperty();
        }
        
        /// <summary>
        /// 프로젝트의 모든 CharacterInitData 에셋을 검색하여 key 목록 갱신
        /// </summary>
        private static void RefreshKeys()
        {
            cachedKeys = new List<string>();
            
            // 프로젝트 전체에서 CharacterInitData 에셋 검색
            string[] guids = AssetDatabase.FindAssets("t:CharacterInitData");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterInitData initData = AssetDatabase.LoadAssetAtPath<CharacterInitData>(path);
                
                if (initData != null && !string.IsNullOrEmpty(initData.key))
                {
                    if (!cachedKeys.Contains(initData.key))
                    {
                        cachedKeys.Add(initData.key);
                    }
                }
            }
            
            // 정렬 (가독성)
            cachedKeys.Sort();
            
            lastUpdateTime = EditorApplication.timeSinceStartup;
            
            Debug.Log($"[CharacterInitDataKeyDrawer] Key 목록 갱신: {cachedKeys.Count}개");
        }
        
        /// <summary>
        /// 드롭다운 UI 그리기
        /// </summary>
        private void DrawDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            string currentValue = property.stringValue;
            
            // 현재 값의 인덱스 찾기
            int currentIndex = cachedKeys.IndexOf(currentValue);
            
            // 현재 값이 목록에 없으면 커스텀 항목 추가
            string[] displayOptions;
            if (currentIndex == -1 && !string.IsNullOrEmpty(currentValue))
            {
                var tempList = new List<string>(cachedKeys);
                tempList.Insert(0, currentValue + " (커스텀)");
                displayOptions = tempList.ToArray();
                currentIndex = 0;
            }
            else
            {
                displayOptions = cachedKeys.ToArray();
            }
            
            // 드롭다운 표시
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < cachedKeys.Count)
                {
                    property.stringValue = cachedKeys[newIndex];
                }
                else if (newIndex == 0 && displayOptions[0].EndsWith(" (커스텀)"))
                {
                    // 커스텀 값 유지
                    property.stringValue = currentValue;
                }
            }
        }
        
        /// <summary>
        /// 캐시 초기화 (메뉴 아이템)
        /// </summary>
        [MenuItem("Tools/Character/Clear InitData Key Cache")]
        public static void ClearCache()
        {
            cachedKeys = null;
            Debug.Log("✅ CharacterInitDataKeyDrawer 캐시가 초기화되었습니다.");
        }
    }
}
#endif

