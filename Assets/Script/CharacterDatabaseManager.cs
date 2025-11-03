using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CharacterDatabase의 런타임 관리자
/// ScriptableObject 원본을 복사하여 런타임에 사용하고,
/// Instance ID로 Entry를 조회하는 서비스를 제공합니다.
/// CoreSystemScene에 배치, DontDestroyOnLoad 적용
/// </summary>
public class CharacterDatabaseManager : MonoBehaviour
{
    public static CharacterDatabaseManager Instance { get; private set; }
    
    [Header("데이터베이스 에셋")]
    [SerializeField] private CharacterDatabase databaseAsset;
    
    // 런타임 사본 (원본 보호)
    private CharacterDatabase databaseCopy;
    
    // 빠른 조회를 위한 Dictionary
    private Dictionary<string, CharacterDatabaseEntry> registry;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[CharacterDatabaseManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다.");
            }
            
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        if (databaseAsset == null)
        {
            Debug.LogError("[CharacterDatabaseManager] databaseAsset이 할당되지 않았습니다! Inspector에서 설정해주세요.");
            return;
        }
        
        // 원본 보호: ScriptableObject 복사
        databaseCopy = Instantiate(databaseAsset);
        
        // Dictionary 구축
        registry = new Dictionary<string, CharacterDatabaseEntry>();
        
        // Player 등록
        if (databaseCopy.playerEntry != null && !string.IsNullOrEmpty(databaseCopy.playerEntry.instanceId))
        {
            registry[databaseCopy.playerEntry.instanceId] = databaseCopy.playerEntry;
            Debug.Log($"[CharacterDatabaseManager] Player 등록: {databaseCopy.playerEntry.instanceId} (템플릿: {databaseCopy.playerEntry.initDataKey})");
        }
        else
        {
            Debug.LogError("[CharacterDatabaseManager] playerEntry가 유효하지 않습니다!");
        }
        
        // Enemy 등록
        if (databaseCopy.enemyEntries != null)
        {
            foreach (var entry in databaseCopy.enemyEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.instanceId))
                {
                    if (registry.ContainsKey(entry.instanceId))
                    {
                        Debug.LogWarning($"[CharacterDatabaseManager] 중복된 Instance ID: {entry.instanceId}");
                    }
                    
                    registry[entry.instanceId] = entry;
                    Debug.Log($"[CharacterDatabaseManager] Enemy 등록: {entry.instanceId} (템플릿: {entry.initDataKey})");
                }
            }
        }
        
        Debug.Log($"[CharacterDatabaseManager] 초기화 완료: {registry.Count}개 Character 인스턴스 정의됨");
        
        // 등록된 Instance 목록 출력
        PrintAllEntries();
    }
    
    /// <summary>
    /// Instance ID로 Entry 조회
    /// </summary>
    public CharacterDatabaseEntry GetEntry(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("[CharacterDatabaseManager] instanceId가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        if (registry.TryGetValue(instanceId, out var entry))
        {
            return entry;
        }
        
        Debug.LogError($"[CharacterDatabaseManager] Instance '{instanceId}'를 찾을 수 없습니다! 등록된 목록을 확인하세요.");
        PrintAllEntries();
        return null;
    }
    
    /// <summary>
    /// 등록된 모든 Instance 목록 출력 (디버그용)
    /// </summary>
    private void PrintAllEntries()
    {
        Debug.Log($"[CharacterDatabaseManager] === 등록된 Character 인스턴스 목록 ({registry.Count}개) ===");
        foreach (var kvp in registry)
        {
            Debug.Log($"  - ID: '{kvp.Key}' → 템플릿: '{kvp.Value.initDataKey}'");
        }
    }
    
    /// <summary>
    /// Player Entry 반환
    /// </summary>
    public CharacterDatabaseEntry GetPlayerEntry()
    {
        return databaseCopy?.playerEntry;
    }
    
    /// <summary>
    /// 등록된 첫 번째 Enemy Entry 반환 (테스트용)
    /// </summary>
    public CharacterDatabaseEntry GetFirstEnemyEntry()
    {
        if (databaseCopy?.enemyEntries != null && databaseCopy.enemyEntries.Count > 0)
        {
            return databaseCopy.enemyEntries[0];
        }
        
        Debug.LogError("[CharacterDatabaseManager] 등록된 Enemy가 없습니다!");
        return null;
    }
}

