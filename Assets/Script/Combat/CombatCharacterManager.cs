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
    
    // === 전투 중인 캐릭터 인스턴스 ===
    public PlayerCharacter PlayerCharacter { get; private set; }
    public List<EnemyCharacter> EnemyCharacters { get; private set; }
    
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
    /// 전투 참가자 정보를 받아서 캐릭터 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="playerInstanceId">플레이어 Character의 Instance ID</param>
    /// <param name="enemyInstanceIds">적 Character(s)의 Instance ID 배열</param>
    public void InitializeBattle(string playerInstanceId, params string[] enemyInstanceIds)
    {
        Debug.Log($"[CombatCharacterManager] === 전투 초기화 시작 ===");
        
        // === 전투 참가자 정보 저장 ===
        PlayerInstanceId = playerInstanceId;
        EnemyInstanceIds = new List<string>(enemyInstanceIds);
        
        Debug.Log($"[CombatCharacterManager] 전투 참가자: {PlayerInstanceId} vs [{string.Join(", ", EnemyInstanceIds)}]");
        
        // === 플레이어 생성 ===
        PlayerCharacter = CreatePlayer(playerInstanceId);
        if (PlayerCharacter == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacter 생성 실패!");
            return;
        }
        
        // === 적 생성 ===
        EnemyCharacters = new List<EnemyCharacter>();
        foreach (var enemyId in enemyInstanceIds)
        {
            var enemy = CreateEnemy(enemyId);
            if (enemy != null)
            {
                EnemyCharacters.Add(enemy);
            }
        }
        
        if (EnemyCharacters.Count == 0)
        {
            Debug.LogError("[CombatCharacterManager] 생성된 Enemy가 없습니다!");
            return;
        }
        
        Debug.Log($"[CombatCharacterManager] === 전투 초기화 완료: {PlayerCharacter.Name} vs {EnemyCharacters[0].Name} (외 {EnemyCharacters.Count - 1}명) ===");
    }
    
    /// <summary>
    /// Player Character 생성
    /// </summary>
    private PlayerCharacter CreatePlayer(string instanceId)
    {
        if (PlayerCharacterManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacterManager.Instance가 null입니다!");
            return null;
        }
        
        var player = PlayerCharacterManager.Instance.CreatePlayerCharacterForBattle();
        if (player == null)
        {
            Debug.LogError("[CombatCharacterManager] PlayerCharacter 생성 실패!");
            return null;
        }
        
        Debug.Log($"[CombatCharacterManager] ✅ Player 생성: {player.Name} (ID: {player.InstanceId})");
        return player;
    }
    
    /// <summary>
    /// Enemy Character 생성
    /// </summary>
    private EnemyCharacter CreateEnemy(string instanceId)
    {
        // 1. CharacterDatabaseManager에서 Entry 조회
        if (CharacterDatabaseManager.Instance == null)
        {
            Debug.LogError("[CombatCharacterManager] CharacterDatabaseManager.Instance가 null입니다!");
            return null;
        }
        
        var enemyEntry = CharacterDatabaseManager.Instance.GetEntry(instanceId);
        if (enemyEntry == null)
        {
            Debug.LogError($"[CombatCharacterManager] Enemy Instance '{instanceId}'를 CharacterDatabase에서 찾을 수 없습니다!");
            return null;
        }
        
        Debug.Log($"[CombatCharacterManager] Enemy Entry: ID={enemyEntry.instanceId}, 템플릿={enemyEntry.initDataKey}");
        
        // 2. Resources에서 템플릿 로드
        var initData = CharacterInitDataLoader.Load(enemyEntry.initDataKey);
        if (initData == null)
        {
            Debug.LogError($"[CombatCharacterManager] 템플릿 '{enemyEntry.initDataKey}'를 로드할 수 없습니다!");
            return null;
        }
        
        Debug.Log($"[CombatCharacterManager] ✅ InitData 로드: {initData.characterName}");
        
        // 3. 템플릿 복사 (BT 인스턴스화)
        CharacterInitData battleInitData = Instantiate(initData);
        battleInitData.InstantiateBehaviorTrees();
        
        // 4. EnemyCharacter 생성
        var enemy = new EnemyCharacter(instanceId, battleInitData, null);
        
        // 5. 인벤토리 초기화
        InitializeEnemyInventory(enemy, battleInitData);
        
        // 6. 검술 초기화
        InitializeActions(enemy, battleInitData);
        
        Debug.Log($"[CombatCharacterManager] ✅ Enemy 생성: {enemy.Name} (ID: {enemy.InstanceId})");
        return enemy;
    }
    
    /// <summary>
    /// Enemy의 인벤토리 초기화
    /// </summary>
    private void InitializeEnemyInventory(EnemyCharacter enemy, CharacterInitData initData)
    {
        // Inventory 생성
        var inventory = new CharacterInventory(enemy.CurrentAccessorySlots);
        inventory.Owner = enemy;
        enemy.Inventory = inventory;
        
        // 초기 아이템 추가
        if (initData.initialItems != null && initData.initialItems.Count > 0)
        {
            foreach (var itemEntry in initData.initialItems)
            {
                if (!string.IsNullOrEmpty(itemEntry.itemId))
                {
                    bool added = inventory.AddItem(itemEntry.itemId, itemEntry.quantity);
                    if (!added)
                    {
                        Debug.LogWarning($"[CombatCharacterManager] {enemy.Name} 초기 아이템 추가 실패: {itemEntry.itemId}");
                    }
                }
            }
        }
        
        // 초기 장비 장착
        if (initData.initialEquipment != null && initData.initialEquipment.Count > 0)
        {
            foreach (var equipEntry in initData.initialEquipment)
            {
                if (!string.IsNullOrEmpty(equipEntry.itemId))
                {
                    if (!inventory.HasItem(equipEntry.itemId))
                    {
                        inventory.AddItem(equipEntry.itemId, 1);
                    }
                    
                    inventory.EquipItem(equipEntry.itemId, equipEntry.slotType);
                }
            }
        }
    }
    
    /// <summary>
    /// Character의 검술 초기화
    /// </summary>
    private void InitializeActions(Character character, CharacterInitData initData)
    {
        var database = ActionCommandDatabase.Instance;
        if (database == null)
        {
            Debug.LogError($"[CombatCharacterManager] ActionCommandDatabase를 찾을 수 없습니다! {character.Name}의 검술 초기화 실패.");
            return;
        }
        
        // 습득 검술 초기화
        if (initData.initialAcquiredActions != null && initData.initialAcquiredActions.Count > 0)
        {
            foreach (var entry in initData.initialAcquiredActions)
            {
                if (entry == null || string.IsNullOrEmpty(entry.actionKey))
                    continue;
                
                var action = database.GetAction(entry.actionKey);
                if (action != null)
                {
                    character.AcquireAction(action);
                }
            }
        }
        
        // 장착 검술 초기화 (4개 슬롯)
        string[] slotKeys = new string[] 
        { 
            initData.equippedActionSlot1, 
            initData.equippedActionSlot2, 
            initData.equippedActionSlot3, 
            initData.equippedActionSlot4 
        };
        
        for (int i = 0; i < 4; i++)
        {
            var key = slotKeys[i];
            if (string.IsNullOrEmpty(key))
                continue;
            
            var action = database.GetAction(key);
            if (action != null)
            {
                character.EquipAction(action, i);
            }
        }
        
        Debug.Log($"[CombatCharacterManager] {character.Name} 검술 초기화 완료 - 습득: {character.GetAcquiredActions().Count}개, 장착: {character.AvailableCommands.Count}개");
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
        
        // 1. 플레이어 상태 저장
        if (PlayerCharacter != null)
        {
            PlayerCharacterManager.Instance.SyncPlayerStateAfterBattle(PlayerCharacter);
        }
        
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
        
        // 3. 인스턴스들은 Scene 언로드 시 자동 파괴됨
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
