using UnityEngine;
using System.Collections.Generic;
using BladeAction.Item;

/// <summary>
/// 현재 전투 중인 양측 캐릭터의 런타임 인스턴스 관리
/// PlayerCharacter와 EnemyCharacter(s)를 전투 시작 시 생성하고
/// 전투 종료 시 플레이어 상태를 동기화합니다.
/// CombatScene에 배치, DontDestroyOnLoad 적용 안함 (Scene 전용)
/// </summary>
public class CombatCharacterManager : MonoBehaviour
{
    public static CombatCharacterManager Instance { get; private set; }
    
    // === 전투 참가자 정보 (누가 싸우는가) ===
    public string PlayerInstanceId { get; private set; }
    public List<string> EnemyInstanceIds { get; private set; }
    
    // === 전투 중인 캐릭터 인스턴스 (참조만 보관, 소유 안 함) ===
    /// <summary>
    /// PlayerCharacterManager의 영속 PlayerCharacter 참조
    /// </summary>
    public PlayerCharacter PlayerCharacter => PlayerCharacterManager.Instance?.PlayerCharacter;
    
    /// <summary>
    /// NonPlayerCharacterManager의 Enemy 인스턴스 참조 목록
    /// </summary>
    public List<EnemyCharacter> EnemyCharacters
    {
        get
        {
            if (EnemyInstanceIds == null || EnemyInstanceIds.Count == 0)
                return null;
            
            var enemies = new List<EnemyCharacter>();
            foreach (var enemyId in EnemyInstanceIds)
            {
                var enemy = NonPlayerCharacterManager.Instance?.GetCharacter(enemyId) as EnemyCharacter;
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }
            return enemies;
        }
    }
    
    // === 편의 프로퍼티 ===
    public EnemyCharacter CurrentEnemy => EnemyCharacters != null && EnemyCharacters.Count > 0 ? EnemyCharacters[0] : null;
    
    // === 초기화 ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ⚠️ DontDestroyOnLoad 적용 안함 (Scene 전용)
            // Scene 언로드 시 자동으로 파괴됨
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // === 전투 초기화 ===
    /// <summary>
    /// 전투를 초기화합니다
    /// 전투 참가자 ID를 저장하고, 영속 Character 인스턴스를 참조합니다.
    /// </summary>
    /// <param name="playerInstanceId">플레이어 Character의 Instance ID</param>
    /// <param name="enemyInstanceIds">적 Character(s)의 Instance ID 배열</param>
    public void InitializeBattle(string playerInstanceId, params string[] enemyInstanceIds)
    {
        Debug.Log($"[CombatCharacterManager] === 전투 초기화 시작 ===");
        
        // === 전투 참가자 정보 저장 (ID만 저장) ===
        PlayerInstanceId = playerInstanceId;
        EnemyInstanceIds = new List<string>(enemyInstanceIds);
        
        Debug.Log($"[CombatCharacterManager] 전투 참가자: {PlayerInstanceId} vs [{string.Join(", ", EnemyInstanceIds)}]");
        
        // === Character 참조 확인 및 생성 트리거 ===
        
        // 1. PlayerCharacter 확인
        if (PlayerCharacter == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacter 참조 실패! PlayerCharacterManager가 초기화되지 않았습니다.");
            return;
        }
        
        Debug.Log($"[CombatCharacterManager] ✅ PlayerCharacter: {PlayerCharacter.Name}");
        
        // 2. NonPlayerCharacterManager 확인
        if (NonPlayerCharacterManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] NonPlayerCharacterManager.Instance가 null입니다!");
            return;
        }
        
        // 3. Enemy 생성 트리거 (GetCharacter로 Lazy 생성)
        foreach (var enemyId in enemyInstanceIds)
        {
            var enemy = NonPlayerCharacterManager.Instance.GetCharacter(enemyId);
            if (enemy == null)
            {
                Debug.LogError($"[CombatCharacterManager] Enemy 생성 실패: {enemyId}");
                return;
            }
            Debug.Log($"[CombatCharacterManager] ✅ Enemy: {enemy.Name} (ID: {enemyId})");
        }
        
        // 4. EnemyCharacters 프로퍼티 최종 확인
        if (EnemyCharacters == null || EnemyCharacters.Count == 0)
        {
            Debug.LogError("[CombatCharacterManager] EnemyCharacter 참조 실패! NonPlayerCharacterManager에 Enemy가 없습니다.");
            return;
        }
        
        Debug.Log($"[CombatCharacterManager] === 전투 초기화 완료: {PlayerCharacter.Name} vs {CurrentEnemy.Name} (외 {EnemyCharacters.Count - 1}명) ===");
    }
    
    // === 전투 종료 ===
    /// <summary>
    /// 전투를 종료하고 플레이어 상태를 저장합니다
    /// </summary>
    /// <param name="result">전투 결과</param>
    public void FinalizeBattle(BattleResult result)
    {
        Debug.Log($"[CombatCharacterManager] 전투 종료: {(result.isVictory ? "승리" : "패배")}");
        
        if (PlayerCharacterManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacterManager.Instance가 null입니다!");
            return;
        }
        
        // 1. 플레이어 상태 저장 (영속 인스턴스이므로 자동 동기화됨)
        PlayerCharacterManager.Instance.SyncPlayerStateAfterBattle();
        
        // 2. 보상 적용
        if (result.isVictory)
        {
            if (result.goldReward > 0)
            {
                PlayerCharacterManager.Instance.AddGold(result.goldReward);
            }
            
            if (result.expReward > 0)
            {
                PlayerCharacterManager.Instance.AddExperience(result.expReward);
            }
            
            Debug.Log($"[CombatCharacterManager] 보상 획득: 골드 +{result.goldReward}, 경험치 +{result.expReward}");
        }
        
        // 3. Character 인스턴스는 영속 관리자가 계속 보관함
        // 4. 전투 참가자 ID는 Scene 언로드 시 자동 소멸
        Debug.Log("[CombatCharacterManager] 전투 후처리 완료");
    }
    
    // === Controller 연결 ===
    /// <summary>
    /// Controller를 Character에 연결합니다
    /// </summary>
    /// <param name="type">캐릭터 타입 (Player/Enemy)</param>
    /// <param name="controller">연결할 Controller</param>
    public void ConnectController(CharacterType type, ICombatController controller)
    {
        if (type == CharacterType.Player)
        {
            if (PlayerCharacter is PlayerCharacter playerChar)
            {
                playerChar.SetController(controller as PlayerController);
                Debug.Log($"[CombatCharacterManager] PlayerController 연결: {PlayerCharacter.Name} (ID: {PlayerCharacter.InstanceId})");
            }
            else
            {
                Debug.LogError("[CombatCharacterManager] PlayerCharacter가 null이거나 타입이 맞지 않습니다!");
            }
        }
        else if (type == CharacterType.Enemy)
        {
            if (CurrentEnemy is EnemyCharacter enemyChar)
            {
                enemyChar.SetController(controller as EnemyController);
                Debug.Log($"[CombatCharacterManager] EnemyController 연결: {CurrentEnemy.Name} (ID: {CurrentEnemy.InstanceId})");
            }
            else
            {
                Debug.LogError("[CombatCharacterManager] CurrentEnemy가 null이거나 타입이 맞지 않습니다!");
            }
        }
    }
    
    private void OnDestroy()
    {
        Debug.Log("[CombatCharacterManager] OnDestroy - Scene 언로드 시 자동 파괴됨");
    }
}
