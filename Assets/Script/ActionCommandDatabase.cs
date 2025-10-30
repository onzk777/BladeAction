using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 검술 데이터베이스 (검술 키 ↔ ActionCommandData 자산 매핑)
/// 프로젝트 전체에서 검술을 Key로 참조할 수 있도록 중앙 관리
/// </summary>
[CreateAssetMenu(fileName = "ActionCommandDatabase", menuName = "Combat/ActionCommand Database", order = 0)]
public class ActionCommandDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("검술 고유 키")]
        public string key;
        
        [Tooltip("ActionCommandData SO")]
        public ActionCommandData data;
    }
    
    [Header("검술 매핑 테이블")]
    [Tooltip("검술 키 ↔ ActionCommandData 매핑")]
    public List<Entry> actions = new List<Entry>();
    
    // 싱글톤 인스턴스 (Resources에서 로드)
    private static ActionCommandDatabase _instance;
    private static bool _hasSearchedForInstance = false;
    
    public static ActionCommandDatabase Instance
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
    /// Resources 폴더에서 ActionCommandDatabase 찾기 (강화된 검색)
    /// </summary>
    private static void FindAndCacheInstance()
    {
        _hasSearchedForInstance = true;
        
        try
        {
            // 1단계: 일반적인 경로들 우선 시도 (빠른 로드)
            string[] commonPaths = new string[]
            {
                "Data/SwordArt/ActionCommandDatabase",
                "Data/Combat/ActionCommandDatabase",
                "SwordArt/ActionCommandDatabase",
                "Combat/ActionCommandDatabase",
                "ActionCommandDatabase"
            };
            
            foreach (var path in commonPaths)
            {
                _instance = Resources.Load<ActionCommandDatabase>(path);
                if (_instance != null)
                {
                    Debug.Log($"[ActionCommandDatabase] 인스턴스 발견 (경로: {path}): '{_instance.name}' ({_instance.actions?.Count ?? 0}개 검술)");
                    return;
                }
            }
            
            // 2단계: 전체 스캔 (Fallback)
            Debug.Log("[ActionCommandDatabase] 일반 경로에서 찾지 못함. Resources 전체 스캔 시작...");
            ActionCommandDatabase[] foundDatabases = Resources.LoadAll<ActionCommandDatabase>("");
            
            if (foundDatabases != null && foundDatabases.Length > 0)
            {
                if (foundDatabases.Length == 1)
                {
                    _instance = foundDatabases[0];
                    Debug.Log($"[ActionCommandDatabase] 인스턴스 발견 (전체 스캔): '{_instance.name}' ({_instance.actions?.Count ?? 0}개 검술)");
                }
                else
                {
                    // 여러 개 발견 시 가장 많은 검술을 가진 것 선택
                    _instance = foundDatabases.OrderByDescending(db => db.actions?.Count ?? 0).First();
                    Debug.LogWarning($"[ActionCommandDatabase] {foundDatabases.Length}개 발견. 가장 많은 검술을 가진 '{_instance.name}' 선택 ({_instance.actions?.Count ?? 0}개 검술)");
                }
            }
            else
            {
                Debug.LogError("[ActionCommandDatabase] Resources 폴더에서 ActionCommandDatabase를 찾을 수 없습니다!\n" +
                    "확인 사항:\n" +
                    "1. ActionCommandDatabase asset이 Resources 폴더 또는 하위 폴더에 있는지 확인\n" +
                    "2. asset 파일이 ActionCommandDatabase 타입인지 확인\n" +
                    "3. Unity 에디터를 재시작해보세요");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ActionCommandDatabase] 인스턴스 검색 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 캐시 초기화 (에디터에서 필요시 사용)
    /// </summary>
    public static void ClearCache()
    {
        _instance = null;
        _hasSearchedForInstance = false;
    }
    
    /// <summary>
    /// 키로 ActionCommandData 조회
    /// </summary>
    public ActionCommandData GetAction(string key)
    {
        if (string.IsNullOrEmpty(key) || actions == null)
            return null;
        
        var entry = actions.Find(e => e != null && e.key == key);
        return entry?.data;
    }
    
    /// <summary>
    /// 여러 키로 ActionCommandData 리스트 조회
    /// </summary>
    public List<ActionCommandData> GetActions(List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return new List<ActionCommandData>();
        
        return keys
            .Select(key => GetAction(key))
            .Where(data => data != null)
            .ToList();
    }
    
    /// <summary>
    /// 키 존재 여부 확인
    /// </summary>
    public bool ContainsKey(string key)
    {
        return !string.IsNullOrEmpty(key) && actions != null && 
               actions.Exists(e => e != null && e.key == key);
    }
    
    /// <summary>
    /// 모든 키 목록 반환
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (actions == null)
            return new List<string>();
        
        return actions
            .Where(e => e != null && !string.IsNullOrEmpty(e.key))
            .Select(e => e.key)
            .ToList();
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: ActionCommandData 이름으로 Key 자동 생성
    /// </summary>
    [ContextMenu("Auto Generate Keys from Asset Names")]
    private void AutoGenerateKeys()
    {
        int syncCount = 0;
        foreach (var entry in actions)
        {
            if (entry != null && entry.data != null)
            {
                // Entry.key가 비어있으면 ActionCommandData의 asset name을 사용
                if (string.IsNullOrEmpty(entry.key) && !string.IsNullOrEmpty(entry.data.name))
                {
                    entry.key = entry.data.name.ToLower().Replace(" ", "_");
                    syncCount++;
                }
            }
        }
        
        Debug.Log($"[ActionCommandDatabase] {syncCount}개 Entry의 Key가 자동 생성되었습니다.");
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// 에디터 전용: 중복 키 검증
    /// </summary>
    [ContextMenu("Validate Keys")]
    private void ValidateKeys()
    {
        var duplicates = actions
            .Where(e => e != null && !string.IsNullOrEmpty(e.key))
            .GroupBy(e => e.key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicates.Count > 0)
        {
            Debug.LogError($"[ActionCommandDatabase] 중복 키 발견: {string.Join(", ", duplicates)}");
        }
        else
        {
            Debug.Log("[ActionCommandDatabase] 중복 키 없음. 검증 통과!");
        }
    }
#endif
}

