using UnityEngine;
using BladeAction.Item;

/// <summary>
/// 플레이어 캐릭터의 영속 데이터 관리
/// 레벨, 경험치, 골드, 인벤토리, 장비, 검술 등을 관리하며
/// Scene 전환 시에도 데이터가 유지됩니다.
/// CoreSystemScene에 배치, DontDestroyOnLoad 적용
/// </summary>
public class PlayerCharacterManager : MonoBehaviour
{
    public static PlayerCharacterManager Instance { get; private set; }
    
    // === 진행도 데이터 ===
    [Header("플레이어 진행도")]
    [SerializeField] private int level = 1;
    [SerializeField] private int experience = 0;
    [SerializeField] private int gold = 100;
    
    public int Level => level;
    public int Experience => experience;
    public int Gold => gold;
    
    // === 영속 데이터 ===
    [Header("영속 데이터")]
    public CharacterInventory Inventory { get; private set; }
    
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
                Debug.LogWarning("[PlayerCharacterManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다. 부모에서 분리하거나 root로 이동하세요.");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // CharacterDatabaseManager가 초기화될 때까지 대기 후 초기화
        StartCoroutine(WaitForDependenciesAndInitialize());
    }
    
    private System.Collections.IEnumerator WaitForDependenciesAndInitialize()
    {
        // 의존성 대기 (CharacterDatabaseManager만 필요)
        while (CharacterDatabaseManager.Instance == null)
        {
            yield return null;
        }
        
        // 완전 초기화를 위해 추가 1프레임 대기
        yield return null;
        
        InitializePlayerData();
    }
    
    private void InitializePlayerData()
    {
        Debug.Log("[PlayerCharacterManager] 플레이어 데이터 초기화 시작");
        
        // 1. CharacterDatabaseManager에서 Player Entry 조회
        var playerEntry = CharacterDatabaseManager.Instance.GetPlayerEntry();
        if (playerEntry == null)
        {
            Debug.LogError("[PlayerCharacterManager] Player Entry를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log($"[PlayerCharacterManager] Player Entry: ID={playerEntry.instanceId}, 템플릿={playerEntry.initDataKey}");
        
        // 2. Resources에서 템플릿 로드
        var initData = CharacterInitDataLoader.Load(playerEntry.initDataKey);
        if (initData == null)
        {
            Debug.LogError($"[PlayerCharacterManager] 템플릿 '{playerEntry.initDataKey}'를 로드할 수 없습니다!");
            return;
        }
        
        // 3. 인벤토리 생성 (영속)
        int accessorySlots = initData.initialAccessorySlots;
        Inventory = new CharacterInventory(accessorySlots);
        
        // 4. 초기 아이템 및 장비 추가
        InitializeInventory(initData);
        
        Debug.Log($"[PlayerCharacterManager] 플레이어 데이터 초기화 완료 - Lv.{level}, 골드 {gold}");
    }
    
    /// <summary>
    /// 인벤토리 초기화
    /// </summary>
    private void InitializeInventory(CharacterInitData initData)
    {
        Debug.Log($"[PlayerCharacterManager] 인벤토리 초기화 시작");
        
        // 초기 아이템 추가
        if (initData.initialItems != null && initData.initialItems.Count > 0)
        {
            foreach (var itemEntry in initData.initialItems)
            {
                if (!string.IsNullOrEmpty(itemEntry.itemId))
                {
                    bool added = Inventory.AddItem(itemEntry.itemId, itemEntry.quantity);
                    if (added)
                    {
                        Debug.Log($"[PlayerCharacterManager] 초기 아이템 추가: {itemEntry.itemId} x{itemEntry.quantity}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerCharacterManager] 초기 아이템 추가 실패: {itemEntry.itemId}");
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
                    // 아이템이 인벤토리에 없으면 자동 추가
                    if (!Inventory.HasItem(equipEntry.itemId))
                    {
                        Inventory.AddItem(equipEntry.itemId, 1);
                    }
                    
                    // 장착
                    bool equipped = Inventory.EquipItem(equipEntry.itemId, equipEntry.slotType);
                    if (equipped)
                    {
                        Debug.Log($"[PlayerCharacterManager] 초기 장비 장착: {equipEntry.itemId} → {equipEntry.slotType}");
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerCharacterManager] 초기 장비 장착 실패: {equipEntry.itemId}");
                    }
                }
            }
        }
        
        Debug.Log($"[PlayerCharacterManager] 인벤토리 초기화 완료 - {Inventory.GetDebugInfo()}");
    }
    
    // === 전투용 PlayerCharacter 인스턴스 생성 ===
    public PlayerCharacter CreatePlayerCharacterForBattle()
    {
        Debug.Log("[PlayerCharacterManager] 전투용 PlayerCharacter 생성 시작");
        
        // 1. CharacterDatabaseManager에서 Player Entry 조회
        var playerEntry = CharacterDatabaseManager.Instance.GetPlayerEntry();
        if (playerEntry == null)
        {
            Debug.LogError("[PlayerCharacterManager] Player Entry를 찾을 수 없습니다!");
            return null;
        }
        
        // 2. Resources에서 템플릿 로드
        var initData = CharacterInitDataLoader.Load(playerEntry.initDataKey);
        if (initData == null)
        {
            Debug.LogError($"[PlayerCharacterManager] 템플릿 '{playerEntry.initDataKey}'를 로드할 수 없습니다!");
            return null;
        }
        
        // 3. 템플릿 복사 (BT 인스턴스화)
        CharacterInitData battleInitData = Instantiate(initData);
        battleInitData.InstantiateBehaviorTrees();
        
        // 4. PlayerCharacter 생성
        var player = new PlayerCharacter(playerEntry.instanceId, battleInitData, null);
        
        // 5. 영속 데이터 적용
        player.Inventory = this.Inventory; // 참조 공유 (전투 중 변경사항이 자동 반영됨)
        
        // 6. 검술 초기화
        InitializeActions(player, battleInitData);
        
        Debug.Log($"[PlayerCharacterManager] 전투용 Player 생성: {player.Name} (ID: {player.InstanceId}), Lv.{level}, 골드 {gold}");
        return player;
    }
    
    /// <summary>
    /// Character의 검술 초기화
    /// </summary>
    private void InitializeActions(Character character, CharacterInitData initData)
    {
        var database = ActionCommandDatabase.Instance;
        if (database == null)
        {
            Debug.LogError($"[PlayerCharacterManager] ActionCommandDatabase를 찾을 수 없습니다! {character.Name}의 검술 초기화 실패.");
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
                else
                {
                    Debug.LogWarning($"[PlayerCharacterManager] 검술 키 '{entry.actionKey}'를 ActionCommandDatabase에서 찾을 수 없습니다.");
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
            else
            {
                Debug.LogWarning($"[PlayerCharacterManager] 검술 키 '{key}'를 ActionCommandDatabase에서 찾을 수 없습니다.");
            }
        }
        
        Debug.Log($"[PlayerCharacterManager] {character.Name} 검술 초기화 완료 - 습득: {character.GetAcquiredActions().Count}개, 장착: {character.AvailableCommands.Count}개");
    }
    
    // === 전투 후 상태 동기화 ===
    public void SyncPlayerStateAfterBattle(PlayerCharacter battleInstance)
    {
        // 전투 중 변경된 인벤토리는 이미 참조 공유로 반영됨
        // (Inventory를 참조로 전달했으므로 자동 동기화)
        
        // 추가로 동기화가 필요한 데이터가 있다면 여기서 처리
        // 예: 레벨업, 스탯 변경 등
        
        Debug.Log("[PlayerCharacterManager] 전투 후 플레이어 상태 동기화 완료");
    }
    
    // === 진행도 관리 ===
    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[PlayerCharacterManager] 골드 획득: +{amount} (현재: {gold})");
    }
    
    public void AddExperience(int amount)
    {
        experience += amount;
        Debug.Log($"[PlayerCharacterManager] 경험치 획득: +{amount} (현재: {experience})");
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        int requiredExp = level * 100; // 임시 공식 (향후 밸런싱 필요)
        while (experience >= requiredExp)
        {
            level++;
            experience -= requiredExp;
            requiredExp = level * 100;
            Debug.Log($"[PlayerCharacterManager] ★ 레벨업! Lv.{level}");
            
            // 레벨업 보상 (향후 구현)
            // - 스탯 증가
            // - 스킬 포인트 획득 등
        }
    }
}
