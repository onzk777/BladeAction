using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace BladeAction.Item.Excel
{
    /// <summary>
    /// StatTable CSV 파일 읽기 전용 클래스
    /// </summary>
    public static class StatTableCSVReader
    {
        /// <summary>
        /// CSV 파일에서 StatTable 데이터 읽기
        /// </summary>
        public static List<StatTableCSVData> ReadCSV(string filePath)
        {
            var result = new List<StatTableCSVData>();
            
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
                
                Debug.Log($"✅ CSV 로드 성공: {result.Count}개의 StatTable 데이터");
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
        private static StatTableCSVData ParseLine(string line, int lineNumber)
        {
            try
            {
                var values = line.Split(',');
                
                if (values.Length < 11)
                {
                    Debug.LogWarning($"라인 {lineNumber}: 컬럼 수가 부족합니다 (최소 11개 필요, 현재 {values.Length}개)");
                    return null;
                }
                
                return new StatTableCSVData
                {
                    TableKey = values[0].Trim(),
                    Description = values[1].Trim(),
                    AttackPower = ParseFloat(values[2], lineNumber, "AttackPower"),
                    BlockEff = ParseFloat(values[3], lineNumber, "BlockEff"),
                    BlockPoiseCost = ParseFloat(values[4], lineNumber, "BlockPoiseCost"),
                    ParryEff = ParseFloat(values[5], lineNumber, "ParryEff"),
                    ParryPoiseCost = ParseFloat(values[6], lineNumber, "ParryPoiseCost"),
                    ParryPoiseAtk = ParseFloat(values[7], lineNumber, "ParryPoiseAtk"),
                    MaxHP = ParseFloat(values[8], lineNumber, "MaxHP"),
                    DamageReduction = ParseFloat(values[9], lineNumber, "DamageReduction"),
                    Poise = ParseFloat(values[10], lineNumber, "Poise")
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"라인 {lineNumber} 파싱 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// float 파싱 (안전)
        /// </summary>
        private static float ParseFloat(string value, int lineNumber, string fieldName)
        {
            value = value.Trim();
            
            if (string.IsNullOrEmpty(value))
                return 0f;
            
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }
            
            Debug.LogWarning($"라인 {lineNumber}, 필드 '{fieldName}': float 변환 실패 ('{value}') - 0으로 처리");
            return 0f;
        }
    }
}

