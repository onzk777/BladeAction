#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Reflection;

namespace BladeAction.Editor
{
    /// <summary>
    /// DatabaseKey Attribute를 위한 범용 PropertyDrawer
    /// 리플렉션을 사용하여 어떤 데이터베이스, 어떤 리스트, 어떤 키 필드도 처리 가능
    /// 프로젝트 전체에서 사용 가능한 범용 모듈
    /// </summary>
    [CustomPropertyDrawer(typeof(DatabaseKeyAttribute))]
    public class DatabaseKeyDrawer : PropertyDrawer
    {
        // 캐시된 키 목록 (성능 최적화)
        private KeyDisplayPair[] cachedKeys;
        private double lastUpdateTime;
        private const double UPDATE_INTERVAL = 0.5; // 0.5초마다 갱신
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "DatabaseKey는 string 타입에만 사용 가능합니다.");
                return;
            }
            
            var attr = attribute as DatabaseKeyAttribute;
            if (attr == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }
            
            EditorGUI.BeginProperty(position, label, property);
            
            // 데이터베이스 찾기 (경로 지정 또는 자동 검색)
            var database = FindDatabase(attr.DatabaseType, attr.DatabasePath);
            
            if (database == null)
            {
                // 데이터베이스를 찾지 못하면 일반 텍스트 필드
                EditorGUI.PropertyField(position, property, label);
                
                var warningRect = new Rect(position.xMax - 20, position.y, 20, position.height);
                EditorGUI.LabelField(warningRect, new GUIContent("⚠", $"{attr.DatabaseType.Name}을(를) 찾을 수 없습니다."));
            }
            else
            {
                // 주기적으로 또는 캐시가 없으면 키 목록 갱신
                bool shouldUpdate = cachedKeys == null || 
                                   EditorApplication.timeSinceStartup - lastUpdateTime > UPDATE_INTERVAL;
                
                if (shouldUpdate)
                {
                    cachedKeys = ExtractKeys(database, attr.ListFieldName, attr.KeyFieldName, attr.DisplayNameField);
                    lastUpdateTime = EditorApplication.timeSinceStartup;
                }
                
                var keys = cachedKeys;
                
                if (keys.Length == 0)
                {
                    EditorGUI.PropertyField(position, property, label);
                    
                    var infoRect = new Rect(position.xMax - 20, position.y, 20, position.height);
                    EditorGUI.LabelField(infoRect, new GUIContent("ℹ", $"{attr.ListFieldName}에 데이터가 없습니다."));
                }
                else
                {
                    // 드롭다운 표시
                    DrawDropdown(position, property, label, keys);
                }
            }
            
            EditorGUI.EndProperty();
        }
        
        /// <summary>
        /// 프로젝트에서 데이터베이스 찾기 (범용)
        /// </summary>
        private ScriptableObject FindDatabase(Type databaseType, string databasePath = null)
        {
            if (databaseType == null || !typeof(ScriptableObject).IsAssignableFrom(databaseType))
                return null;
            
            // 1. 경로가 지정된 경우 해당 경로에서 로드
            if (!string.IsNullOrEmpty(databasePath))
            {
                var db = AssetDatabase.LoadAssetAtPath(databasePath, databaseType) as ScriptableObject;
                if (db != null)
                    return db;
                
                Debug.LogWarning($"지정된 경로에서 Database를 찾을 수 없습니다: {databasePath}");
            }
            
            // 2. Resources 폴더에서 검색
            var resourcePath = databaseType.Name;
            var resourceDB = Resources.Load(resourcePath, databaseType) as ScriptableObject;
            if (resourceDB != null)
                return resourceDB;
            
            // 3. 프로젝트 전체에서 검색
            var typeName = databaseType.Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}");
            
            if (guids.Length == 0)
                return null;
            
            // 여러 개 있으면 경고
            if (guids.Length > 1)
            {
                Debug.LogWarning($"{typeName} 타입의 Database가 {guids.Length}개 발견되었습니다. " +
                                $"첫 번째 것을 사용합니다. 특정 Database를 지정하려면 databasePath 파라미터를 사용하세요.");
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath(path, databaseType) as ScriptableObject;
        }
        
        /// <summary>
        /// 리스트에서 키 목록 추출 (범용 - 리플렉션 사용)
        /// </summary>
        private KeyDisplayPair[] ExtractKeys(ScriptableObject database, string listFieldName, string keyFieldName, string displayNameField)
        {
            try
            {
                // 리스트 필드 가져오기
                var listField = database.GetType().GetField(listFieldName, BindingFlags.Public | BindingFlags.Instance);
                if (listField == null)
                {
                    Debug.LogWarning($"필드 '{listFieldName}'을(를) {database.GetType().Name}에서 찾을 수 없습니다.");
                    return new KeyDisplayPair[0];
                }
                
                var list = listField.GetValue(database) as IList;
                if (list == null || list.Count == 0)
                    return new KeyDisplayPair[0];
                
                var results = new System.Collections.Generic.List<KeyDisplayPair>();
                
                foreach (var item in list)
                {
                    if (item == null)
                        continue;
                    
                    // 키 필드 값 가져오기
                    var keyField = item.GetType().GetField(keyFieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (keyField == null)
                        continue;
                    
                    var keyValue = keyField.GetValue(item) as string;
                    if (string.IsNullOrEmpty(keyValue))
                        continue;
                    
                    // 표시 이름 가져오기 (옵션)
                    string displayValue = keyValue;
                    if (!string.IsNullOrEmpty(displayNameField))
                    {
                        var displayField = item.GetType().GetField(displayNameField, BindingFlags.Public | BindingFlags.Instance);
                        if (displayField != null)
                        {
                            var displayName = displayField.GetValue(item) as string;
                            if (!string.IsNullOrEmpty(displayName))
                            {
                                displayValue = $"{keyValue} ({displayName})";
                            }
                        }
                    }
                    
                    results.Add(new KeyDisplayPair { Key = keyValue, Display = displayValue });
                }
                
                return results.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"키 추출 중 오류 발생: {ex.Message}");
                return new KeyDisplayPair[0];
            }
        }
        
        /// <summary>
        /// 드롭다운 UI 그리기
        /// </summary>
        private void DrawDropdown(Rect position, SerializedProperty property, GUIContent label, KeyDisplayPair[] keys)
        {
            string currentValue = property.stringValue;
            
            // 현재 값의 인덱스 찾기
            int currentIndex = -1;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Key == currentValue)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // 현재 값이 목록에 없으면 커스텀 항목 추가
            string[] displayOptions;
            if (currentIndex == -1 && !string.IsNullOrEmpty(currentValue))
            {
                var tempList = keys.Select(k => k.Display).ToList();
                tempList.Insert(0, currentValue + " (커스텀)");
                displayOptions = tempList.ToArray();
                currentIndex = 0;
            }
            else
            {
                displayOptions = keys.Select(k => k.Display).ToArray();
            }
            
            // 드롭다운 표시
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayOptions);
            
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < keys.Length)
                {
                    property.stringValue = keys[newIndex].Key;
                }
                else if (newIndex == 0 && displayOptions[0].EndsWith(" (커스텀)"))
                {
                    // 커스텀 값 유지
                    property.stringValue = currentValue;
                }
            }
        }
        
        /// <summary>
        /// 키와 표시 이름 쌍
        /// </summary>
        private struct KeyDisplayPair
        {
            public string Key;
            public string Display;
        }
    }
}
#endif

