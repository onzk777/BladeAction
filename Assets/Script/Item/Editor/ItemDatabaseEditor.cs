#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BladeAction.Item.Editor
{
    /// <summary>
    /// ItemDatabase Inspector 커스텀 에디터
    /// Item 리스트를 편집할 때 ItemType에 따라 관련 필드만 표시
    /// </summary>
    [CustomEditor(typeof(ItemDatabase))]
    public class ItemDatabaseEditor : UnityEditor.Editor
    {
        private SerializedProperty typeDatabaseProp;
        private SerializedProperty statDatabaseProp;
        private SerializedProperty itemsProp;
        
        private bool showItems = true;
        
        void OnEnable()
        {
            typeDatabaseProp = serializedObject.FindProperty("typeDatabase");
            statDatabaseProp = serializedObject.FindProperty("statDatabase");
            itemsProp = serializedObject.FindProperty("items");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("아이템 데이터베이스 (순수 콘텐츠)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 데이터베이스는 순수하게 아이템 콘텐츠만 관리합니다. CSV Import/Export는 Tools > Database > Item Import Export 메뉴를 사용하세요.", MessageType.Info);
            EditorGUILayout.Space();
            
            // 데이터베이스 참조
            EditorGUILayout.LabelField("데이터베이스 참조", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(typeDatabaseProp, new GUIContent("타입 데이터베이스 (게임 룰)"));
            EditorGUILayout.PropertyField(statDatabaseProp, new GUIContent("스탯 데이터베이스 (프리셋)"));
            EditorGUILayout.HelpBox("타입과 스탯은 별도 데이터베이스에서 관리됩니다.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 아이템 리스트
            EditorGUILayout.LabelField("모든 아이템", EditorStyles.boldLabel);
            
            // 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 새 아이템 추가", GUILayout.Height(25)))
            {
                itemsProp.InsertArrayElementAtIndex(itemsProp.arraySize);
            }
            
            if (GUILayout.Button("Tools > Database > Item Import Export 열기", GUILayout.Height(25)))
            {
                ItemDatabaseImporter.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 스크롤 영역
            showItems = EditorGUILayout.Foldout(showItems, $"아이템 ({itemsProp.arraySize}개)");
            if (showItems)
            {
                // 각 아이템 표시 (조건부 필드 포함)
                for (int i = 0; i < itemsProp.arraySize; i++)
                {
                    DrawItemElement(itemsProp.GetArrayElementAtIndex(i), i);
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 개별 Item 요소 그리기 (조건부 필드 표시)
        /// </summary>
        private void DrawItemElement(SerializedProperty itemProp, int index)
        {
            EditorGUILayout.BeginVertical("box");
            
            // 아이템 기본 정보
            var itemKeyProp = itemProp.FindPropertyRelative("itemKey");
            var itemNameProp = itemProp.FindPropertyRelative("itemName");
            var itemTypeProp = itemProp.FindPropertyRelative("itemType");
            
            // 헤더
            EditorGUILayout.BeginHorizontal();
            string headerText = string.IsNullOrEmpty(itemNameProp.stringValue) 
                ? $"Item {index}" 
                : $"[{itemKeyProp.stringValue}] {itemNameProp.stringValue}";
            itemProp.isExpanded = EditorGUILayout.Foldout(itemProp.isExpanded, headerText, true);
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                itemsProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            if (itemProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Excel 데이터 - Key 강조
                EditorGUILayout.LabelField("기본 정보 (Excel에서 관리)", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginVertical("box");
                
                var keyLabelStyle = new GUIStyle(EditorStyles.boldLabel);
                keyLabelStyle.normal.background = MakeBackgroundTexture(new Color(0.1f, 0.1f, 0.1f)); // 밝은 회색 배경
                keyLabelStyle.padding = new RectOffset(5, 5, 2, 2);
                
                EditorGUILayout.PropertyField(itemKeyProp, new GUIContent("Item Key"), true);
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(itemNameProp);
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("description"));
                EditorGUILayout.PropertyField(itemTypeProp);
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("maxStack"));
                
                EditorGUILayout.Space();
                
                // Unity Asset 참조
                EditorGUILayout.LabelField("Unity Asset 참조", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("icon"));
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("appearance"));
                
                // ItemType에 따라 조건부 표시
                ItemType itemType = (ItemType)itemTypeProp.intValue;
                
                EditorGUILayout.LabelField("타입 참조", EditorStyles.miniLabel);
                
                switch (itemType)
                {
                    case ItemType.Weapon:
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("weaponTypeKey"), new GUIContent("무기 타입"));
                        break;
                        
                    case ItemType.Armor:
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("armorTypeKey"), new GUIContent("방어구 타입"));
                        break;
                        
                    case ItemType.Accessory:
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("accessoryTypeKey"), new GUIContent("보조장비 타입"));
                        break;
                        
                    case ItemType.SwordArtStyle:
                        EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("swordArtStyle"), new GUIContent("검술 유파"));
                        break;
                }
                
                EditorGUILayout.Space();
                
                // 스탯
                EditorGUILayout.LabelField("스탯", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(itemProp.FindPropertyRelative("statTableKey"));
                
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


