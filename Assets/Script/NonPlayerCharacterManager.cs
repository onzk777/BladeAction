using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 Non-Player Character (Enemy, NPC)의 영속 인스턴스 관리
/// 
/// 역할:
/// - 모든 NPC/Enemy의 런타임 인스턴스를 영속 보관
/// - Instance ID로 Character 조회 제공
/// - CharacterDatabaseManager에서 생성한 인스턴스를 등록 및 관리
/// - Character 런타임 데이터의 모든 변경사항 추적 (아이템, 능력치, 레벨, 골드, 위치 등)
/// 
/// CoreSystemScene에 배치, DontDestroyOnLoad 적용
/// </summary>
public class NonPlayerCharacterManager : MonoBehaviour
{
    public static NonPlayerCharacterManager Instance { get; private set; }
    
    // === 영속 Character 인스턴스 저장소 ===
    // Key: Instance ID, Value: Character 인스턴스
    private Dictionary<string, Character> characters = new Dictionary<string, Character>();
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            
            // root GameObject일 때만 DontDestroyOnLoad 적용
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[NonPlayerCharacterManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다. 부모에서 분리하거나 root로 이동하세요.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 의존성 대기 후 초기화
        StartCoroutine(WaitForDependenciesAndInitialize());
    }
    
    private System.Collections.IEnumerator WaitForDependenciesAndInitialize()
    {
        // CharacterDatabaseManager 대기
        while (CharacterDatabaseManager.Instance == null)
        {
            yield return null;
        }
        
        // 완전 초기화를 위해 추가 1프레임 대기
        yield return null;
        
        InitializeCharacters();
    }
    
    /// <summary>
    /// 초기화: CharacterDatabase에 등록된 모든 NPC/Enemy 인스턴스 생성
    /// </summary>
    private void InitializeCharacters()
    {
        Debug.Log("[NonPlayerCharacterManager] NPC/Enemy 초기화 시작");
        
        // CharacterDatabaseManager에서 모든 Enemy Entry 가져오기
        // (현재는 CharacterDatabase에 직접 접근, 향후 GetAllEnemyEntries() API 추가 가능)
        
        // ⚠️ 초기화 시점에는 모든 Character를 미리 생성하지 않고,
        // GetCharacter() 호출 시점에 Lazy 생성하는 방식 사용
        // (메모리 효율 + 로딩 시간 단축)
        
        Debug.Log("[NonPlayerCharacterManager] 초기화 완료 (Lazy 생성 방식)");
    }
    
    /// <summary>
    /// Instance ID로 Character 조회 (없으면 생성)
    /// </summary>
    /// <param name="instanceId">Character의 Instance ID</param>
    /// <returns>Character 인스턴스 (없으면 새로 생성)</returns>
    public Character GetCharacter(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("[NonPlayerCharacterManager] instanceId가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        // 이미 생성된 인스턴스가 있으면 반환
        if (characters.TryGetValue(instanceId, out var character))
        {
            return character;
        }
        
        // 없으면 새로 생성
        var newCharacter = CreateCharacter(instanceId);
        if (newCharacter != null)
        {
            characters[instanceId] = newCharacter;
            Debug.Log($"[NonPlayerCharacterManager] Character 생성 및 등록: {newCharacter.Name} (ID: {instanceId})");
        }
        
        return newCharacter;
    }
    
    /// <summary>
    /// Character 생성 (CharacterDatabaseManager의 Factory 호출)
    /// </summary>
    private Character CreateCharacter(string instanceId)
    {
        if (CharacterDatabaseManager.Instance == null)
        {
            Debug.LogError("[NonPlayerCharacterManager] CharacterDatabaseManager.Instance가 null입니다!");
            return null;
        }
        
        // CharacterDatabaseManager의 CreateCharacter() 호출
        var character = CharacterDatabaseManager.Instance.CreateCharacter(instanceId);
        if (character == null)
        {
            Debug.LogError($"[NonPlayerCharacterManager] Character 생성 실패: {instanceId}");
            return null;
        }
        
        return character;
    }
    
    /// <summary>
    /// 등록된 모든 Character 목록 출력 (디버그용)
    /// </summary>
    public void PrintAllCharacters()
    {
        Debug.Log($"[NonPlayerCharacterManager] === 등록된 Character 목록 ({characters.Count}개) ===");
        foreach (var kvp in characters)
        {
            Debug.Log($"  - ID: '{kvp.Key}' → {kvp.Value.Name}");
        }
    }
    
    /// <summary>
    /// Character가 이미 등록되어 있는지 확인
    /// </summary>
    public bool HasCharacter(string instanceId)
    {
        return characters.ContainsKey(instanceId);
    }
    
    /// <summary>
    /// 특정 Character를 영구 삭제 (사망, 탈출 등)
    /// </summary>
    public void RemoveCharacter(string instanceId)
    {
        if (characters.Remove(instanceId))
        {
            Debug.Log($"[NonPlayerCharacterManager] Character 삭제: {instanceId}");
        }
        else
        {
            Debug.LogWarning($"[NonPlayerCharacterManager] Character 삭제 실패 (존재하지 않음): {instanceId}");
        }
    }
}

