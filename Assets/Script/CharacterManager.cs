using UnityEngine;
using BladeAction.Item;

public enum CharacterType { Player, Enemy }

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("캐릭터 데이터 에셋")]
    [SerializeField] private CharacterData playerDataAsset;
    [SerializeField] private CharacterData enemyDataAsset;
    
    public CharacterData PlayerData { get; private set; }
    public CharacterData EnemyData { get; private set; }

    public PlayerCharacter PlayerCharacter { get; private set; }
    public EnemyCharacter EnemyCharacter { get; private set; }

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
                Debug.LogWarning("[CharacterManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다. 부모에서 분리하거나 root로 이동하세요.");
            }
            InitializeCharacterData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCharacterData()
    {
        // ScriptableObject 에셋이 할당되어 있는지 확인
        if (playerDataAsset == null)
        {
            Debug.LogError("[CharacterManager] PlayerDataAsset이 할당되지 않았습니다!");
            return;
        }
        
        if (enemyDataAsset == null)
        {
            Debug.LogError("[CharacterManager] EnemyDataAsset이 할당되지 않았습니다!");
            return;
        }
        
        // ScriptableObject를 복사하여 런타임 인스턴스 생성
        PlayerData = Instantiate(playerDataAsset);
        EnemyData = Instantiate(enemyDataAsset);

        // BT 인스턴스화 (개체별 독립적인 BT 생성)
        PlayerData.InstantiateBehaviorTrees();
        EnemyData.InstantiateBehaviorTrees();

        // Character 인스턴스 생성 (CharacterData를 통해 1차 스탯 초기화)
        PlayerCharacter = new PlayerCharacter(PlayerData, null);
        EnemyCharacter = new EnemyCharacter(EnemyData, null);
        
        // Inventory 생성 및 초기화
        InitializeInventory(PlayerCharacter, PlayerData);
        InitializeInventory(EnemyCharacter, EnemyData);
        
        // 검술 초기화
        InitializeActions(PlayerCharacter, PlayerData);
        InitializeActions(EnemyCharacter, EnemyData);

        Debug.Log("[CharacterManager] CharacterData, BT 인스턴스화 및 Character 초기화 완료.");
    }
    
    /// <summary>
    /// Character의 Inventory를 생성하고 CharacterData의 초기 데이터로 초기화합니다.
    /// </summary>
    private void InitializeInventory(Character character, CharacterData data)
    {
        // Inventory 생성 (생성자에서 자동으로 장비 슬롯 초기화됨)
        var inventory = new CharacterInventory();
        inventory.Owner = character;
        character.Inventory = inventory;
        
        Debug.Log($"[CharacterManager] {character.Name} Inventory 생성 완료");
        
        // 초기 아이템 추가
        if (data.initialItems != null && data.initialItems.Count > 0)
        {
            // ItemDatabase 상태 확인
            var itemDb = ItemDatabase.Instance;
            if (itemDb == null)
            {
                Debug.LogError($"[CharacterManager] {character.Name} - ItemDatabase.Instance가 null입니다!");
            }
            else
            {
                Debug.Log($"[CharacterManager] {character.Name} - ItemDatabase 로드됨: {itemDb.items?.Count ?? 0}개 아이템");
            }
            
            foreach (var itemEntry in data.initialItems)
            {
                if (!string.IsNullOrEmpty(itemEntry.itemId))
                {
                    // 아이템이 DB에 있는지 확인
                    var itemData = ItemDatabase.GetItemSafe(itemEntry.itemId);
                    if (itemData == null)
                    {
                        Debug.LogError($"[CharacterManager] {character.Name} - 아이템 '{itemEntry.itemId}'를 ItemDatabase에서 찾을 수 없습니다!");
                    }
                    
                    bool added = inventory.AddItem(itemEntry.itemId, itemEntry.quantity);
                    if (added)
                    {
                        Debug.Log($"[CharacterManager] {character.Name} 초기 아이템 추가: {itemEntry.itemId} x{itemEntry.quantity}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterManager] {character.Name} 초기 아이템 추가 실패: {itemEntry.itemId}");
                    }
                }
            }
        }
        
        // 초기 장비 장착
        if (data.initialEquipment != null && data.initialEquipment.Count > 0)
        {
            foreach (var equipEntry in data.initialEquipment)
            {
                if (!string.IsNullOrEmpty(equipEntry.itemId))
                {
                    // 아이템이 인벤토리에 없으면 자동 추가
                    if (!inventory.HasItem(equipEntry.itemId))
                    {
                        inventory.AddItem(equipEntry.itemId, 1);
                    }
                    
                    // 장착
                    bool equipped = inventory.EquipItem(equipEntry.itemId, equipEntry.slotType);
                    if (equipped)
                    {
                        Debug.Log($"[CharacterManager] {character.Name} 초기 장비 장착: {equipEntry.itemId} → {equipEntry.slotType}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterManager] {character.Name} 초기 장비 장착 실패: {equipEntry.itemId}");
                    }
                }
            }
        }
        
        Debug.Log($"[CharacterManager] {character.Name} Inventory 초기화 완료 - {inventory.GetDebugInfo()}");
    }
    
    /// <summary>
    /// Character의 검술을 CharacterData의 초기 데이터로 초기화합니다.
    /// </summary>
    private void InitializeActions(Character character, CharacterData data)
    {
        var database = ActionCommandDatabase.Instance;
        if (database == null)
        {
            Debug.LogError($"[CharacterManager] ActionCommandDatabase를 찾을 수 없습니다! {character.Name}의 검술 초기화 실패.");
            return;
        }
        
        // 습득 검술 초기화
        if (data.initialAcquiredActions != null && data.initialAcquiredActions.Count > 0)
        {
            foreach (var entry in data.initialAcquiredActions)
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
                    Debug.LogWarning($"[CharacterManager] 검술 키 '{entry.actionKey}'를 ActionCommandDatabase에서 찾을 수 없습니다.");
                }
            }
        }
        
        // 장착 검술 초기화 (4개 슬롯)
        string[] slotKeys = new string[] 
        { 
            data.equippedActionSlot1, 
            data.equippedActionSlot2, 
            data.equippedActionSlot3, 
            data.equippedActionSlot4 
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
                Debug.LogWarning($"[CharacterManager] 검술 키 '{key}'를 ActionCommandDatabase에서 찾을 수 없습니다.");
            }
        }
        
        Debug.Log($"[CharacterManager] {character.Name} 검술 초기화 완료 - 습득: {character.GetAcquiredActions().Count}개, 장착: {character.AvailableCommands.Count}개");
    }

    /// <summary>
    /// Controller를 Combatant에 연결합니다.
    /// </summary>
    public void ConnectController(CharacterType type, ICombatController controller)
    {
        if (type == CharacterType.Player)
        {
            if (PlayerCharacter is PlayerCharacter playerCharacterInstance)
            {
                playerCharacterInstance.SetController(controller as PlayerController);
                Debug.Log($"[CharacterManager] PlayerController 연결 완료: {controller.Character.Name}");
            }
        }
        else if (type == CharacterType.Enemy)
        {
            if (EnemyCharacter is EnemyCharacter enemyCharacterInstance)
            {
                enemyCharacterInstance.SetController(controller as EnemyController);
                Debug.Log($"[CharacterManager] EnemyController 연결 완료: {controller.Character.Name}");
            }
        }
    }
}
