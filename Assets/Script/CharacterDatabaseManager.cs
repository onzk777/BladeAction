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
    
    /// <summary>
    /// 등록된 모든 Enemy Entry 목록 반환
    /// </summary>
    public List<CharacterDatabaseEntry> GetAllEnemyEntries()
    {
        if (databaseCopy?.enemyEntries != null)
        {
            return new List<CharacterDatabaseEntry>(databaseCopy.enemyEntries);
        }
        
        return new List<CharacterDatabaseEntry>();
    }
    
    /// <summary>
    /// 등록된 모든 Enemy의 Instance ID 목록 반환
    /// </summary>
    public List<string> GetAllEnemyIds()
    {
        var ids = new List<string>();
        
        if (databaseCopy?.enemyEntries != null)
        {
            foreach (var entry in databaseCopy.enemyEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.instanceId))
                {
                    ids.Add(entry.instanceId);
                }
            }
        }
        
        return ids;
    }
    
    /// <summary>
    /// Character Factory: Instance ID로 Character 인스턴스 생성
    /// 
    /// 역할: 템플릿(CharacterInitData)을 기반으로 Character 인스턴스를 생성만 함
    /// 관리: 생성한 인스턴스는 호출자가 관리함 (이 클래스는 관리 안 함)
    /// </summary>
    /// <param name="instanceId">생성할 Character의 Instance ID</param>
    /// <returns>생성된 Character 인스턴스 (Player는 null, Enemy/NPC는 EnemyCharacter)</returns>
    public Character CreateCharacter(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("[CharacterDatabaseManager] instanceId가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        // 1. Entry 조회
        var entry = GetEntry(instanceId);
        if (entry == null)
        {
            Debug.LogError($"[CharacterDatabaseManager] Instance '{instanceId}'를 찾을 수 없습니다!");
            return null;
        }
        
        // Player는 PlayerCharacterManager가 관리하므로 여기서 생성 안 함
        if (entry == databaseCopy.playerEntry)
        {
            Debug.LogWarning($"[CharacterDatabaseManager] Player Character는 PlayerCharacterManager에서 생성합니다. instanceId: {instanceId}");
            return null;
        }
        
        // 2. Resources에서 템플릿 로드
        var initData = CharacterInitDataLoader.Load(entry.initDataKey);
        if (initData == null)
        {
            Debug.LogError($"[CharacterDatabaseManager] 템플릿 '{entry.initDataKey}'를 로드할 수 없습니다!");
            return null;
        }
        
        // 3. 템플릿 복사 (BT 인스턴스화)
        CharacterInitData characterInitData = Instantiate(initData);
        characterInitData.InstantiateBehaviorTrees();
        
        // 4. EnemyCharacter 생성
        var character = new EnemyCharacter(instanceId, characterInitData, null);
        
        // 5. 인벤토리 초기화
        InitializeInventory(character, characterInitData);
        
        // 6. 검술 초기화
        InitializeActions(character, characterInitData);
        
        Debug.Log($"[CharacterDatabaseManager] ✅ Character 생성: {character.Name} (ID: {instanceId}, 템플릿: {entry.initDataKey})");
        return character;
    }
    
    /// <summary>
    /// Character의 인벤토리 초기화
    /// </summary>
    private void InitializeInventory(Character character, CharacterInitData initData)
    {
        // Inventory 생성
        var inventory = new BladeAction.Item.CharacterInventory(character.CurrentAccessorySlots);
        inventory.Owner = character;
        character.Inventory = inventory;
        
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
                        Debug.LogWarning($"[CharacterDatabaseManager] {character.Name} 초기 아이템 추가 실패: {itemEntry.itemId}");
                    }
                }
            }
        }
        
        // 초기 장비 장착 (슬롯별)
        EquipItemIfValid(inventory, initData.weaponSlot, BladeAction.Item.EquipmentSlotType.Weapon, "무기", character.Name);
        EquipItemIfValid(inventory, initData.armorSlot, BladeAction.Item.EquipmentSlotType.Armor, "갑옷", character.Name);
        EquipItemIfValid(inventory, initData.swordArtStyleSlot, BladeAction.Item.EquipmentSlotType.SwordArtStyle, "유파", character.Name);
        
        // 장신구 슬롯 (개수는 initialAccessorySlots 값만큼)
        string[] accessories = initData.GetAccessorySlots();
        for (int i = 0; i < accessories.Length; i++)
        {
            if (!string.IsNullOrEmpty(accessories[i]))
            {
                EquipItemIfValid(inventory, accessories[i], BladeAction.Item.EquipmentSlotType.Accessory, $"장신구 {i + 1}", character.Name);
            }
        }
    }
    
    /// <summary>
    /// 아이템 ID가 유효하면 인벤토리에 추가 및 장착
    /// </summary>
    private void EquipItemIfValid(BladeAction.Item.CharacterInventory inventory, string itemId, BladeAction.Item.EquipmentSlotType slotType, string slotName, string characterName)
    {
        if (string.IsNullOrEmpty(itemId))
            return;
        
        // 아이템이 인벤토리에 없으면 자동 추가
        if (!inventory.HasItem(itemId))
        {
            inventory.AddItem(itemId, 1);
        }
        
        // 장착
        bool equipped = inventory.EquipItem(itemId, slotType);
        if (equipped)
        {
            Debug.Log($"[CharacterDatabaseManager] {characterName} 초기 장비 장착: {itemId} → {slotName} ({slotType})");
        }
        else
        {
            Debug.LogWarning($"[CharacterDatabaseManager] {characterName} 초기 장비 장착 실패: {itemId} → {slotName}");
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
            Debug.LogError($"[CharacterDatabaseManager] ActionCommandDatabase를 찾을 수 없습니다! {character.Name}의 검술 초기화 실패.");
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
        
        Debug.Log($"[CharacterDatabaseManager] {character.Name} 검술 초기화 완료 - 습득: {character.GetAcquiredActions().Count}개, 장착: {character.AvailableCommands.Count}개");
    }
}

