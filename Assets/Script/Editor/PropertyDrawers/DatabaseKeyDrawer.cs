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
        // Static 캐시 (모든 인스턴스가 공유) - 핵심 최적화!
        private static System.Collections.Generic.Dictionary<string, CachedData> staticCache 
            = new System.Collections.Generic.Dictionary<string, CachedData>();
        private static System.Collections.Generic.Dictionary<Type, ScriptableObject> databaseCache
            = new System.Collections.Generic.Dictionary<Type, ScriptableObject>();
        private const double UPDATE_INTERVAL = 5.0; // 5초마다 갱신 (0.5초는 너무 짧음)
        
        /// <summary>
        /// 캐시 초기화 (메뉴 아이템으로 제공)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Database/Clear Property Drawer Cache")]
        public static void ClearCache()
        {
            staticCache.Clear();
            databaseCache.Clear();
            Debug.Log("✅ DatabaseKeyDrawer 캐시가 초기화되었습니다.");
        }
        
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
                // Static 캐시 키 생성 (데이터베이스 + 필드 조합)
                string cacheKey = $"{attr.DatabaseType.Name}_{attr.ListFieldName}_{attr.KeyFieldName}";
                
                // Static 캐시에서 가져오기
                CachedData cachedData;
                bool needsUpdate = !staticCache.TryGetValue(cacheKey, out cachedData) ||
                                  EditorApplication.timeSinceStartup - cachedData.lastUpdateTime > UPDATE_INTERVAL;
                
                if (needsUpdate)
                {
                    var extractedKeys = ExtractKeys(database, attr.ListFieldName, attr.KeyFieldName, attr.DisplayNameField);
                    cachedData = new CachedData
                    {
                        keys = extractedKeys,
                        lastUpdateTime = EditorApplication.timeSinceStartup
                    };
                    staticCache[cacheKey] = cachedData;
                }
                
                var keys = cachedData.keys;
                
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
        /// 프로젝트에서 데이터베이스 찾기 (범용) - Static 캐싱 적용
        /// </summary>
        private ScriptableObject FindDatabase(Type databaseType, string databasePath = null)
        {
            if (databaseType == null || !typeof(ScriptableObject).IsAssignableFrom(databaseType))
                return null;
            
            // Static 캐시 확인 (경로가 없는 경우만 캐싱)
            if (string.IsNullOrEmpty(databasePath) && databaseCache.ContainsKey(databaseType))
            {
                var cached = databaseCache[databaseType];
                if (cached != null)
                    return cached;
            }
            
            ScriptableObject database = null;
            
            // 1. 경로가 지정된 경우 해당 경로에서 로드
            if (!string.IsNullOrEmpty(databasePath))
            {
                database = AssetDatabase.LoadAssetAtPath(databasePath, databaseType) as ScriptableObject;
                if (database != null)
                    return database;
                
                Debug.LogWarning($"지정된 경로에서 Database를 찾을 수 없습니다: {databasePath}");
            }
            
            // 2. Resources 폴더에서 검색
            var resourcePath = databaseType.Name;
            database = Resources.Load(resourcePath, databaseType) as ScriptableObject;
            if (database != null)
            {
                databaseCache[databaseType] = database;
                return database;
            }
            
            // 3. 프로젝트 전체에서 검색
            var typeName = databaseType.Name;
            var guids = AssetDatabase.FindAssets($"t:{typeName}");
            
            if (guids.Length == 0)
            {
                databaseCache[databaseType] = null; // null도 캐싱 (반복 검색 방지)
                return null;
            }
            
            // 여러 개 있으면 경고 (한 번만)
            if (guids.Length > 1 && !databaseCache.ContainsKey(databaseType))
            {
                Debug.LogWarning($"{typeName} 타입의 Database가 {guids.Length}개 발견되었습니다. " +
                                $"첫 번째 것을 사용합니다. 특정 Database를 지정하려면 databasePath 파라미터를 사용하세요.");
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            database = AssetDatabase.LoadAssetAtPath(path, databaseType) as ScriptableObject;
            
            // 캐시에 저장
            databaseCache[databaseType] = database;
            return database;
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
        
        /// <summary>
        /// Static 캐시 데이터 구조
        /// </summary>
        private struct CachedData
        {
            public KeyDisplayPair[] keys;
            public double lastUpdateTime;
        }
    }
}
#endif

