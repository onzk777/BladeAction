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
                // 공유 위반(엑셀 열림 등) 상황에서도 읽을 수 있도록 ReadWrite 공유 + 재시도
                const int maxRetry = 5;
                const int retryDelayMs = 200;
                int attempt = 0;

                while (true)
                {
                    try
                    {
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            int lineIndex = 0;
                            string header = reader.ReadLine(); // 헤더
                            if (header == null)
                            {
                                Debug.LogWarning("CSV 파일이 비어있습니다.");
                                return result;
                            }

                            // 헤더 컬럼 수 점검 (유연 처리)
                            var headerCols = SplitCSVLine(header);
                            if (headerCols.Length < 9)
                            {
                                Debug.LogWarning($"CSV 헤더 컬럼 수가 예상보다 적습니다({headerCols.Length}). 파싱은 계속 시도합니다.");
                            }

                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                lineIndex++;
                                line = line.Trim();
                                if (string.IsNullOrEmpty(line))
                                    continue;
                                var row = ParseLine(line, lineIndex + 1); // 실제 라인 번호(헤더 포함)
                                if (row != null)
                                    result.Add(row);
                            }
                        }
                        Debug.Log($"✅ CSV 로드 성공: {result.Count}개의 Item 데이터");
                        return result;
                    }
                    catch (IOException ioEx) when (IsSharingViolation(ioEx))
                    {
                        attempt++;
                        if (attempt >= maxRetry)
                            throw;
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                }
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
                
                if (values.Length < 9)
                {
                    Debug.LogWarning($"라인 {lineNumber}: 컬럼 수가 부족합니다 (최소 9개 필요, 현재 {values.Length}개)");
                    return null;
                }
                
                // 트림된 값들 보관
                string v0 = values[0].Trim();
                string v1 = values[1].Trim();
                string v2 = values[2].Trim();
                string v3 = values[3].Trim();
                string v4 = values[4].Trim();
                string v5 = values[5].Trim();
                string v6 = values[6].Trim();
                string v7 = values[7].Trim();
                string v8 = values[8].Trim();
                string v9 = values.Length > 9 ? values[9].Trim() : "";

                return new ItemCSVData
                {
                    Key = v0,
                    Name = v1,
                    Description = v2,
                    Type = v3,
                    RequiredLevel = 0, // (옵션) 현재 포맷에 없으면 0
                    MaxStack = ParseInt(v4, lineNumber, "MaxStack"),
                    StatKey = v5,
                    WeaponTypeKey = v6,
                    ArmorTypeKey = v7,
                    AccessoryTypeKey = v8,
                    SwordArtStyleKey = v9,

                    HasName = !string.IsNullOrEmpty(v1),
                    HasDescription = !string.IsNullOrEmpty(v2),
                    HasType = !string.IsNullOrEmpty(v3),
                    HasRequiredLevel = false, // 컬럼 미포함
                    HasMaxStack = !string.IsNullOrEmpty(v4),
                    HasStatKey = !string.IsNullOrEmpty(v5),
                    HasWeaponTypeKey = !string.IsNullOrEmpty(v6),
                    HasArmorTypeKey = !string.IsNullOrEmpty(v7),
                    HasAccessoryTypeKey = !string.IsNullOrEmpty(v8),
                    HasSwordArtStyleKey = !string.IsNullOrEmpty(v9)
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

        private static bool IsSharingViolation(IOException ex)
        {
            // HResult 0x20 또는 0x21 계열이 공유 위반인 경우가 많음
            const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
            return ex.HResult == ERROR_SHARING_VIOLATION || ex.Message.Contains("Sharing violation");
        }
    }
}

