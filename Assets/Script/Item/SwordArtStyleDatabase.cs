using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 검술 유파 키 ↔ SwordArtStyleData 자산 매핑 테이블
/// </summary>
[CreateAssetMenu(fileName = "SwordArtStyleDatabase", menuName = "Item/SwordArtStyle Database", order = 11)]
public class SwordArtStyleDatabase : ScriptableObject
{
    private static SwordArtStyleDatabase _instance;
    private static bool _hasSearchedForInstance = false;
    
    /// <summary>
    /// 싱글톤 인스턴스
    /// </summary>
    public static SwordArtStyleDatabase Instance
    {
        get
        {
            if (_instance == null && !_hasSearchedForInstance)
            {
                FindAndCacheInstance();
            }
            return _instance;
        }
    }
    
    /// <summary>
    /// Resources 폴더에서 SwordArtStyleDatabase 찾기 (강화된 검색)
    /// </summary>
    private static void FindAndCacheInstance()
    {
        _hasSearchedForInstance = true;
        
        try
        {
            // 1단계: 일반적인 경로들 우선 시도
            string[] commonPaths = new string[]
            {
                "Data/Item/SwordArtStyleDB",
                "Data/SwordArt/SwordArtStyleDB",
                "Item/SwordArtStyleDB",
                "SwordArtStyleDB",
                "SwordArtStyleDatabase"
            };
            
            foreach (var path in commonPaths)
            {
                _instance = Resources.Load<SwordArtStyleDatabase>(path);
                if (_instance != null)
                {
                    Debug.Log($"[SwordArtStyleDatabase] 인스턴스 발견 (경로: {path}): '{_instance.name}' ({_instance.styles?.Count ?? 0}개 유파)");
                    return;
                }
            }
            
            // 2단계: 전체 스캔 (Fallback)
            Debug.Log("[SwordArtStyleDatabase] 일반 경로에서 찾지 못함. Resources 전체 스캔 시작...");
            SwordArtStyleDatabase[] foundDatabases = Resources.LoadAll<SwordArtStyleDatabase>("");
            
            if (foundDatabases != null && foundDatabases.Length > 0)
            {
                _instance = foundDatabases[0];
                Debug.Log($"[SwordArtStyleDatabase] 인스턴스 발견 (전체 스캔): '{_instance.name}' ({_instance.styles?.Count ?? 0}개 유파)");
            }
            else
            {
                Debug.LogError("[SwordArtStyleDatabase] Resources 폴더에서 SwordArtStyleDatabase를 찾을 수 없습니다!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SwordArtStyleDatabase] 인스턴스 검색 중 오류 발생: {ex.Message}");
        }
    }
    
    [System.Serializable]
    public class Entry
    {
        public string key; // 고유 키
        public string displayName; // 표시용 이름 (선택)
        public SwordArtStyleData asset; // 실제 SO
    }

    [Header("검술 유파 매핑 테이블")]
    public List<Entry> styles = new List<Entry>();

    /// <summary>
    /// 키로 스타일 SO 조회
    /// </summary>
    public SwordArtStyleData GetStyle(string key)
    {
        if (string.IsNullOrEmpty(key) || styles == null) return null;
        var e = styles.Find(x => x != null && x.key == key);
        return e != null ? e.asset : null;
    }

    /// <summary>
    /// 포함 여부
    /// </summary>
    public bool ContainsKey(string key)
    {
        return !string.IsNullOrEmpty(key) && styles != null && styles.Exists(x => x != null && x.key == key);
    }
}


