#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BladeAction.Item.Excel;

namespace BladeAction.Item.Editor
{
    /// <summary>
    /// StatDatabase CSV 임포트 에디터 윈도우
    /// </summary>
    public class StatDatabaseImporter : EditorWindow
    {
        [MenuItem("Tools/Database/Import Export")]
        public static void ShowWindow()
        {
            var window = GetWindow<StatDatabaseImporter>("Database Import/Export");
            window.minSize = new Vector2(600, 400);
        }
        
        private string csvFilePath = "";
        private StatDatabase statDatabase;
        
        private List<StatTableCSVData> loadedData = new List<StatTableCSVData>();
        private Vector2 scrollPos;
        
        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Database Import/Export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("StatDatabase ↔ CSV 파일 간 데이터를 Import/Export 합니다.", MessageType.Info);
            
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
            
            // StatDatabase 참조
            EditorGUILayout.LabelField("2. StatDatabase 선택", EditorStyles.boldLabel);
            statDatabase = (StatDatabase)EditorGUILayout.ObjectField(
                "Stat Database", statDatabase, typeof(StatDatabase), false);
            
            EditorGUILayout.Space();
            
            // 버튼들
            EditorGUILayout.LabelField("3. 작업 실행", EditorStyles.boldLabel);
            
            // Export 버튼 (별도 행)
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = statDatabase != null;
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
            
            GUI.enabled = loadedData.Count > 0 && statDatabase != null;
            if (GUILayout.Button("Import to Database", GUILayout.Height(30)))
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
                EditorGUILayout.LabelField($"총 {loadedData.Count}개의 스탯 테이블", EditorStyles.miniLabel);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
                
                foreach (var row in loadedData)
                {
                    EditorGUILayout.BeginVertical("box");
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{row.TableKey}]", EditorStyles.boldLabel, GUILayout.Width(120));
                    EditorGUILayout.LabelField(row.Description, GUILayout.Width(200));
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"공격: {row.AttackPower}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"HP: {row.MaxHP}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Poise: {row.Poise}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"DR: {row.DamageReduction}%", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    
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
            
            loadedData = StatTableCSVReader.ReadCSV(csvFilePath);
            
            if (loadedData.Count > 0)
            {
                Debug.Log($"✅ {loadedData.Count}개의 스탯 테이블 데이터를 로드했습니다.");
            }
            else
            {
                EditorUtility.DisplayDialog("경고", "로드된 데이터가 없습니다. CSV 파일을 확인해주세요.", "확인");
            }
            
            Repaint();
        }
        
        /// <summary>
        /// StatDatabase로 임포트 (전체 교체 방식)
        /// </summary>
        private void ImportToDatabase()
        {
            if (statDatabase == null)
            {
                EditorUtility.DisplayDialog("오류", "StatDatabase를 선택해주세요.", "확인");
                return;
            }
            
            if (loadedData.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "먼저 CSV를 로드해주세요.", "확인");
                return;
            }
            
            // 경고: 기존 데이터가 모두 삭제됨
            if (statDatabase.statTables.Count > 0)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "경고",
                    $"기존 데이터 {statDatabase.statTables.Count}개가 삭제되고 CSV 데이터로 완전히 교체됩니다.\n\n계속하시겠습니까?",
                    "예, 교체합니다",
                    "취소"
                );
                
                if (!confirm)
                {
                    return;
                }
            }
            
            // 기존 데이터 전체 삭제
            statDatabase.statTables.Clear();
            
            // CSV 데이터 전체 추가
            int importedCount = 0;
            
            foreach (var csvData in loadedData)
            {
                var newTable = StatTableMapper.MapCSVToStatTable(csvData);
                if (newTable != null)
                {
                    statDatabase.statTables.Add(newTable);
                    importedCount++;
                }
            }
            
            // 변경사항 저장
            EditorUtility.SetDirty(statDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            string message = $"✅ 임포트 완료!\n\n" +
                           $"CSV 데이터로 완전히 교체되었습니다.\n" +
                           $"임포트된 스탯 테이블: {importedCount}개";
            
            EditorUtility.DisplayDialog("성공", message, "확인");
            Debug.Log(message);
        }
        
        /// <summary>
        /// StatDatabase를 CSV로 Export
        /// </summary>
        private void ExportToCSV()
        {
            if (statDatabase == null)
            {
                EditorUtility.DisplayDialog("오류", "StatDatabase를 선택해주세요.", "확인");
                return;
            }
            
            if (statDatabase.statTables.Count == 0)
            {
                EditorUtility.DisplayDialog("경고", "StatDatabase에 데이터가 없습니다.", "확인");
                return;
            }
            
            // 저장 경로 선택
            string savePath = EditorUtility.SaveFilePanel(
                "Export StatTable CSV",
                "Assets",
                "StatTable_Export.csv",
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
                    writer.WriteLine("TableKey,Description,AttackPower,BlockEff,BlockPoiseCost,ParryEff,ParryPoiseCost,ParryPoiseAtk,MaxHP,DamageReduction,Poise");
                    
                    // 데이터 작성
                    foreach (var table in statDatabase.statTables)
                    {
                        if (table == null || string.IsNullOrEmpty(table.tableKey))
                            continue;
                        
                        var stats = table.stats;
                        
                        var line = string.Format(
                            System.Globalization.CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                            EscapeCSV(table.tableKey),
                            EscapeCSV(table.description),
                            stats.attackPower,
                            stats.blockEfficiency,
                            stats.blockPoiseConsumption,
                            stats.parryEfficiency,
                            stats.parryPoiseConsumption,
                            stats.parryPoiseAttackPower,
                            stats.maxHP,
                            stats.damageReduction,
                            stats.poise
                        );
                        
                        writer.WriteLine(line);
                    }
                }
                
                string message = $"✅ Export 완료!\n\n" +
                               $"파일: {savePath}\n" +
                               $"개수: {statDatabase.statTables.Count}개";
                
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


