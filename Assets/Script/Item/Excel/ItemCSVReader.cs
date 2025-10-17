using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace BladeAction.Item.Excel
{
    /// <summary>
    /// Item CSV 파일 읽기 전용 클래스
    /// </summary>
    public static class ItemCSVReader
    {
        /// <summary>
        /// CSV 파일에서 Item 데이터 읽기
        /// </summary>
        public static List<ItemCSVData> ReadCSV(string filePath)
        {
            var result = new List<ItemCSVData>();
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {filePath}");
                return result;
            }
            
            try
            {
                var lines = File.ReadAllLines(filePath);
                
                if (lines.Length < 2) // 헤더 + 최소 1개 데이터
                {
                    Debug.LogWarning("CSV 파일이 비어있거나 데이터가 없습니다.");
                    return result;
                }
                
                // 첫 줄은 헤더이므로 건너뛰기
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    
                    // 빈 줄 건너뛰기
                    if (string.IsNullOrEmpty(line))
                        continue;
                    
                    var row = ParseLine(line, i);
                    if (row != null)
                    {
                        result.Add(row);
                    }
                }
                
                Debug.Log($"✅ CSV 로드 성공: {result.Count}개의 Item 데이터");
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CSV 파일 읽기 실패: {ex.Message}");
                return result;
            }
        }
        
        /// <summary>
        /// CSV 한 줄 파싱
        /// </summary>
        private static ItemCSVData ParseLine(string line, int lineNumber)
        {
            try
            {
                var values = SplitCSVLine(line);
                
                if (values.Length < 10)
                {
                    Debug.LogWarning($"라인 {lineNumber}: 컬럼 수가 부족합니다 (최소 10개 필요, 현재 {values.Length}개)");
                    return null;
                }
                
                return new ItemCSVData
                {
                    Key = values[0].Trim(),
                    Name = values[1].Trim(),
                    Description = values[2].Trim(),
                    Type = values[3].Trim(),
                    RequiredLevel = ParseInt(values[4], lineNumber, "RequiredLevel"),
                    MaxStack = ParseInt(values[5], lineNumber, "MaxStack"),
                    StatKey = values[6].Trim(),
                    WeaponTypeKey = values[7].Trim(),
                    ArmorTypeKey = values[8].Trim(),
                    AccessoryTypeKey = values[9].Trim()
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"라인 {lineNumber} 파싱 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// CSV 라인 분리 (따옴표 처리)
        /// </summary>
        private static string[] SplitCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString());
            return result.ToArray();
        }
        
        /// <summary>
        /// int 파싱 (안전)
        /// </summary>
        private static int ParseInt(string value, int lineNumber, string fieldName)
        {
            value = value.Trim();
            
            if (string.IsNullOrEmpty(value))
                return 0;
            
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            
            Debug.LogWarning($"라인 {lineNumber}, 필드 '{fieldName}': int 변환 실패 ('{value}') - 0으로 처리");
            return 0;
        }
    }
}

