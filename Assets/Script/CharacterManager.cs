using UnityEngine;

public enum CharacterType { Player, Enemy }

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("캐릭터 데이터 에셋")]
    [SerializeField] private CharacterData playerDataAsset;
    [SerializeField] private CharacterData enemyDataAsset;
    
    public CharacterData PlayerData { get; private set; }
    public CharacterData EnemyData { get; private set; }

    public PlayerCombatant PlayerCombatant { get; private set; }
    public EnemyCombatant EnemyCombatant { get; private set; }

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

        // Combatant 인스턴스 생성 (CharacterData를 통해 1차 스탯 초기화)
        PlayerCombatant = new PlayerCombatant(PlayerData, null);
        EnemyCombatant = new EnemyCombatant(EnemyData, null);

        Debug.Log("[CharacterManager] CharacterData, BT 인스턴스화 및 Combatant 초기화 완료.");
    }

    /// <summary>
    /// Controller를 Combatant에 연결합니다.
    /// </summary>
    public void ConnectController(CharacterType type, ICombatController controller)
    {
        if (type == CharacterType.Player)
        {
            if (PlayerCombatant is PlayerCombatant playerCombatantInstance)
            {
                playerCombatantInstance.SetController(controller as PlayerController);
                Debug.Log($"[CharacterManager] PlayerController 연결 완료: {controller.Combatant.Name}");
            }
        }
        else if (type == CharacterType.Enemy)
        {
            if (EnemyCombatant is EnemyCombatant enemyCombatantInstance)
            {
                enemyCombatantInstance.SetController(controller as EnemyController);
                Debug.Log($"[CharacterManager] EnemyController 연결 완료: {controller.Combatant.Name}");
            }
        }
    }
}
