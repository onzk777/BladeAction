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
            EditorGUILayout.HelpBox("ItemDatabase ↔ CSV 파일 간 데이터를 Import/Export 합니다.\n\n⚠️ Import는 완전 동기화 방식입니다:\n  • CSV에 있는 Key → 업데이트 또는 추가\n  • CSV에 없는 Key → 삭제됨\n  • Unity Asset 참조(icon 등)는 보존됨", MessageType.Info);
            
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
            if (GUILayout.Button("Import (CSV와 완전 동기화)", GUILayout.Height(30)))
            {
                ImportMergeToDatabase();
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
        /// ItemDatabase로 임포트 (완전 동기화 방식)
        /// - CSV에 있는 Key: 업데이트 또는 추가 (Unity Asset 참조는 보존)
        /// - CSV에 없는 Key: 삭제
        /// </summary>
        private void ImportMergeToDatabase()
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

            // CSV Key 집합 생성
            var csvKeys = new HashSet<string>();
            foreach (var csv in loadedData)
            {
                if (!string.IsNullOrEmpty(csv.Key))
                    csvKeys.Add(csv.Key);
            }

            int updated = 0;
            int added = 0;
            
            // 1단계: CSV 데이터로 업데이트/추가
            foreach (var csv in loadedData)
            {
                if (string.IsNullOrEmpty(csv.Key))
                    continue;
                var existing = itemDatabase.GetItem(csv.Key);
                if (existing != null)
                {
                    ItemMapper.UpdateItem(existing, csv);
                    updated++;
                }
                else
                {
                    var created = ItemMapper.MapCSVToItem(csv);
                    if (created != null)
                    {
                        itemDatabase.items.Add(created);
                        added++;
                    }
                }
            }

            // 2단계: CSV에 없는 아이템 삭제
            int deleted = 0;
            for (int i = itemDatabase.items.Count - 1; i >= 0; i--)
            {
                var item = itemDatabase.items[i];
                if (item != null && !string.IsNullOrEmpty(item.itemKey))
                {
                    if (!csvKeys.Contains(item.itemKey))
                    {
                        itemDatabase.items.RemoveAt(i);
                        deleted++;
                    }
                }
            }

            EditorUtility.SetDirty(itemDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("성공",
                $"✅ Sync Import 완료\n업데이트: {updated}개, 추가: {added}개, 삭제: {deleted}개\nUnity Asset 참조(아이콘 등)는 보존되었습니다.",
                "확인");
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
        
        // 전체 교체 Import는 요구사항에 따라 제거됨
        
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
                // 디렉터리 보장
                var dir = System.IO.Path.GetDirectoryName(savePath);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                // 공유 위반 대응: 재시도 + 대체 파일명 저장
                WriteCsvWithRetry(savePath);

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

        private void WriteCsvWithRetry(string path)
        {
            const int maxRetry = 5;
            const int retryDelayMs = 200;
            int attempt = 0;

            while (true)
            {
                try
                {
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                    using (var writer = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Key,Name,Description,Type,MaxStack,StatKey,WeaponTypeKey,ArmorTypeKey,AccessoryTypeKey,SwordArtStyleKey");
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
                                item.maxStack,
                                EscapeCSV(item.statTableKey),
                                EscapeCSV(item.weaponTypeKey),
                                EscapeCSV(item.armorTypeKey),
                                EscapeCSV(item.accessoryTypeKey),
                                EscapeCSV(item.swordArtStyleKey)
                            );
                            writer.WriteLine(line);
                        }
                    }
                    return;
                }
                catch (System.IO.IOException ioEx) when (ioEx.HResult == unchecked((int)0x80070020) || ioEx.Message.Contains("Sharing violation"))
                {
                    attempt++;
                    if (attempt >= maxRetry)
                    {
                        // 대체 파일명으로 저장 시도 (타임스탬프)
                        var dir = System.IO.Path.GetDirectoryName(path);
                        var name = System.IO.Path.GetFileNameWithoutExtension(path);
                        var ext = System.IO.Path.GetExtension(path);
                        var alt = System.IO.Path.Combine(dir, $"{name}_{System.DateTime.Now:yyyyMMdd_HHmmss}{ext}");
                        using (var fs = new System.IO.FileStream(alt, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                        using (var writer = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8))
                        {
                            writer.WriteLine("Key,Name,Description,Type,MaxStack,StatKey,WeaponTypeKey,ArmorTypeKey,AccessoryTypeKey,SwordArtStyleKey");
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
                                    item.maxStack,
                                    EscapeCSV(item.statTableKey),
                                    EscapeCSV(item.weaponTypeKey),
                                    EscapeCSV(item.armorTypeKey),
                                    EscapeCSV(item.accessoryTypeKey),
                                    EscapeCSV(item.swordArtStyleKey)
                                );
                                writer.WriteLine(line);
                            }
                        }
                        throw new System.Exception($"원본 경로가 다른 프로세스에서 사용 중입니다. 대체 파일로 저장했습니다: {alt}");
                    }
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }
        }
    }
}
#endif

