#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BladeAction.Item.Editor
{
    /// <summary>
    /// StatDatabase Inspector 커스텀 에디터
    /// 스탯 테이블 리스트의 높이를 충분히 확보
    /// </summary>
    [CustomEditor(typeof(StatDatabase))]
    public class StatDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty statTablesProp;
        private Vector2 scrollPos;
        
        void OnEnable()
        {
            statTablesProp = serializedObject.FindProperty("statTables");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("스탯 데이터베이스", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("재사용 가능한 스탯 프리셋을 관리합니다. CSV Import/Export는 Tools > Database > Import Export 메뉴를 사용하세요.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 스탯 테이블 리스트
            EditorGUILayout.LabelField($"스탯 테이블 ({statTablesProp.arraySize}개)", EditorStyles.boldLabel);
            
            // 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 새 스탯 테이블 추가", GUILayout.Height(25)))
            {
                statTablesProp.InsertArrayElementAtIndex(statTablesProp.arraySize);
            }
            
            if (GUILayout.Button("Tools > Database > Import Export 열기", GUILayout.Height(25)))
            {
                StatDatabaseImporter.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 스크롤 영역 (높이 제한 없음)
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            // 각 스탯 테이블 표시
            for (int i = 0; i < statTablesProp.arraySize; i++)
            {
                DrawStatTableElement(statTablesProp.GetArrayElementAtIndex(i), i);
            }
            
            EditorGUILayout.EndScrollView();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 개별 StatTable 요소 그리기
        /// </summary>
        private void DrawStatTableElement(SerializedProperty tableProp, int index)
        {
            EditorGUILayout.BeginVertical("box");
            
            var tableKeyProp = tableProp.FindPropertyRelative("tableKey");
            var descriptionProp = tableProp.FindPropertyRelative("description");
            var statsProp = tableProp.FindPropertyRelative("stats");
            
            // 헤더
            EditorGUILayout.BeginHorizontal();
            string headerText = string.IsNullOrEmpty(tableKeyProp.stringValue)
                ? $"Stat Table {index}"
                : $"[{tableKeyProp.stringValue}] {descriptionProp.stringValue}";
            tableProp.isExpanded = EditorGUILayout.Foldout(tableProp.isExpanded, headerText, true);
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                statTablesProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            if (tableProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // 기본 정보 - Key 강조
                EditorGUILayout.BeginVertical("box");
                
                var keyLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                keyLabelStyle.normal.background = MakeBackgroundTexture(new Color(0.1f, 0.1f, 0.1f)); // 밝은 회색 배경
                keyLabelStyle.padding = new RectOffset(5, 5, 2, 2);
                
                EditorGUILayout.LabelField("Table Key", keyLabelStyle);
                EditorGUILayout.PropertyField(tableKeyProp, GUIContent.none);
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(descriptionProp);
                
                EditorGUILayout.Space();
                
                // 스탯 (전체 표시)
                EditorGUILayout.LabelField("스탯", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(statsProp, true);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        /// <summary>
        /// 배경 텍스처 생성 (Unity Inspector 배경색용)
        /// </summary>
        private static Texture2D MakeBackgroundTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
#endif


