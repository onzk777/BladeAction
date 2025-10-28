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
            DontDestroyOnLoad(gameObject);
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
            foreach (var itemEntry in data.initialItems)
            {
                if (!string.IsNullOrEmpty(itemEntry.itemId))
                {
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
