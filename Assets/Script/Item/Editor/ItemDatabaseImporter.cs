#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BladeAction.Item.Excel;

namespace BladeAction.Item.Editor
{
    /// <summary>
    /// ItemDatabase CSV 임포트/익스포트 에디터 윈도우
    /// </summary>
    public class ItemDatabaseImporter : EditorWindow
    {
        [MenuItem("Tools/Database/Item Import Export")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemDatabaseImporter>("Item DB Import/Export");
            window.minSize = new Vector2(600, 500);
        }
        
        private string csvFilePath = "";
        private ItemDatabase itemDatabase;
        
        private List<ItemCSVData> loadedData = new List<ItemCSVData>();
        private Vector2 scrollPos;
        
        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ItemDatabase Import/Export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("ItemDatabase ↔ CSV 파일 간 데이터를 Import/Export 합니다.\n주의: Unity Asset 참조(icon, weaponType 등)는 CSV에 포함되지 않으며, Inspector에서 수동 설정해야 합니다.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // CSV 파일 경로
            EditorGUILayout.LabelField("1. CSV 파일 선택", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            csvFilePath = EditorGUILayout.TextField("CSV File Path", csvFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                csvFilePath = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // ItemDatabase 참조
            EditorGUILayout.LabelField("2. ItemDatabase 선택", EditorStyles.boldLabel);
            itemDatabase = (ItemDatabase)EditorGUILayout.ObjectField(
                "Item Database", itemDatabase, typeof(ItemDatabase), false);
            
            EditorGUILayout.Space();
            
            // 버튼들
            EditorGUILayout.LabelField("3. 작업 실행", EditorStyles.boldLabel);
            
            // Export 버튼
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = itemDatabase != null;
            if (GUILayout.Button("Export Database to CSV", GUILayout.Height(30)))
            {
                ExportToCSV();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Import 버튼들
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !string.IsNullOrEmpty(csvFilePath);
            if (GUILayout.Button("Load CSV", GUILayout.Height(30)))
            {
                LoadCSV();
            }
            GUI.enabled = true;
            
            GUI.enabled = loadedData.Count > 0 && itemDatabase != null;
            if (GUILayout.Button("Import to Database (전체 교체)", GUILayout.Height(30)))
            {
                ImportToDatabase();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("Clear", GUILayout.Width(80), GUILayout.Height(30)))
            {
                loadedData.Clear();
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 로드된 데이터 표시
            if (loadedData.Count > 0)
            {
                EditorGUILayout.LabelField("4. 로드된 데이터 미리보기", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"총 {loadedData.Count}개의 아이템", EditorStyles.miniLabel);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
                
                foreach (var row in loadedData)
                {
                    EditorGUILayout.BeginVertical("box");
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{row.Key}]", EditorStyles.boldLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField(row.Name, GUILayout.Width(150));
                    EditorGUILayout.LabelField($"({row.Type})", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"레벨: {row.RequiredLevel}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"스택: {row.MaxStack}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"스탯: {row.StatKey}", GUILayout.Width(150));
                    EditorGUILayout.EndHorizontal();
                    
                    if (!string.IsNullOrEmpty(row.Description))
                    {
                        EditorGUILayout.LabelField(row.Description, EditorStyles.wordWrappedMiniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        /// <summary>
        /// CSV 파일 로드
        /// </summary>
        private void LoadCSV()
        {
            if (string.IsNullOrEmpty(csvFilePath))
            {
                EditorUtility.DisplayDialog("오류", "CSV 파일을 선택해주세요.", "확인");
                return;
            }
            
            loadedData = ItemCSVReader.ReadCSV(csvFilePath);
            
            if (loadedData.Count > 0)
            {
                Debug.Log($"✅ {loadedData.Count}개의 아이템 데이터를 로드했습니다.");
            }
            else
            {
                EditorUtility.DisplayDialog("경고", "로드된 데이터가 없습니다. CSV 파일을 확인해주세요.", "확인");
            }
            
            Repaint();
        }
        
        /// <summary>
        /// ItemDatabase로 임포트 (전체 교체 방식)
        /// </summary>
        private void ImportToDatabase()
        {
            if (itemDatabase == null)
            {
                EditorUtility.DisplayDialog("오류", "ItemDatabase를 선택해주세요.", "확인");
                return;
            }
            
            if (loadedData.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "먼저 CSV를 로드해주세요.", "확인");
                return;
            }
            
            // 경고: 기존 데이터가 모두 삭제됨
            if (itemDatabase.items.Count > 0)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "경고",
                    $"기존 아이템 {itemDatabase.items.Count}개가 삭제되고 CSV 데이터로 완전히 교체됩니다.\n\n" +
                    $"주의: Unity Asset 참조(icon, weaponType 등)는 초기화됩니다!\n\n" +
                    $"계속하시겠습니까?",
                    "예, 교체합니다",
                    "취소"
                );
                
                if (!confirm)
                {
                    return;
                }
            }
            
            // 기존 데이터 전체 삭제
            itemDatabase.items.Clear();
            
            // CSV 데이터 전체 추가
            int importedCount = 0;
            
            foreach (var csvData in loadedData)
            {
                var newItem = ItemMapper.MapCSVToItem(csvData);
                if (newItem != null)
                {
                    itemDatabase.items.Add(newItem);
                    importedCount++;
                }
            }
            
            // 변경사항 저장
            EditorUtility.SetDirty(itemDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"✅ 임포트 완료!\n\n" +
                           $"CSV 데이터로 완전히 교체되었습니다.\n" +
                           $"임포트된 아이템: {importedCount}개\n\n" +
                           $"⚠️ Unity Asset 참조(icon, weaponType 등)는\n" +
                           $"ItemDatabase Inspector에서 수동으로 설정해주세요.";
            
            EditorUtility.DisplayDialog("성공", message, "확인");
            Debug.Log(message);
        }
        
        /// <summary>
        /// ItemDatabase를 CSV로 Export
        /// </summary>
        private void ExportToCSV()
        {
            if (itemDatabase == null)
            {
                EditorUtility.DisplayDialog("오류", "ItemDatabase를 선택해주세요.", "확인");
                return;
            }
            
            if (itemDatabase.items.Count == 0)
            {
                EditorUtility.DisplayDialog("경고", "ItemDatabase에 아이템이 없습니다.", "확인");
                return;
            }
            
            // 저장 경로 선택
            string savePath = EditorUtility.SaveFilePanel(
                "Export Item CSV",
                "Assets",
                "ItemData_Export.csv",
                "csv"
            );
            
            if (string.IsNullOrEmpty(savePath))
            {
                return; // 취소
            }
            
            try
            {
                using (var writer = new System.IO.StreamWriter(savePath, false, System.Text.Encoding.UTF8))
                {
                    // 헤더 작성
                    writer.WriteLine("Key,Name,Description,Type,RequiredLevel,MaxStack,StatKey,WeaponTypeKey,ArmorTypeKey,AccessoryTypeKey");
                    
                    // 데이터 작성
                    foreach (var item in itemDatabase.items)
                    {
                        if (item == null || string.IsNullOrEmpty(item.itemKey))
                            continue;
                        
                        var line = string.Format(
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                            EscapeCSV(item.itemKey),
                            EscapeCSV(item.itemName),
                            EscapeCSV(item.description),
                            ItemMapper.ItemTypeToString(item.itemType),
                            item.requiredLevel,
                            item.maxStack,
                            EscapeCSV(item.statTableKey),
                            EscapeCSV(item.weaponTypeKey),
                            EscapeCSV(item.armorTypeKey),
                            EscapeCSV(item.accessoryTypeKey)
                        );
                        
                        writer.WriteLine(line);
                    }
                }
                
                string message = $"✅ Export 완료!\n\n" +
                               $"파일: {savePath}\n" +
                               $"아이템 개수: {itemDatabase.items.Count}개\n\n" +
                               $"ℹ️ Unity Asset 참조(icon, weaponType 등)는\n" +
                               $"CSV에 포함되지 않습니다.";
                
                EditorUtility.DisplayDialog("성공", message, "확인");
                Debug.Log(message);
                
                // CSV 파일 경로를 자동으로 설정
                csvFilePath = savePath;
                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("오류", $"Export 실패: {ex.Message}", "확인");
                Debug.LogError($"CSV Export 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// CSV 특수문자 처리 (쉼표, 따옴표, 줄바꿈)
        /// </summary>
        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            // 쉼표, 따옴표, 줄바꿈이 있으면 따옴표로 감싸기
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\""); // 따옴표 이스케이프
                return $"\"{value}\"";
            }
            
            return value;
        }
    }
}
#endif

