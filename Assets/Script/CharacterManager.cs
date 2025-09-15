using UnityEngine;

public enum CharacterType { Player, Enemy }

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

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
        // 기본 스테이터스로 CharacterData 생성
        PlayerData = new CharacterData("Player", maxHp: 100, atk: 20, dr: 0, crit: 0, critRatio: 150, maxPoise: 100, parryPoiseDamage: 25);
        EnemyData = new CharacterData("Enemy", maxHp: 100, atk: 20, dr: 0, crit: 0, critRatio: 150, maxPoise: 100, parryPoiseDamage: 25);

        // Combatant 인스턴스 생성 (아직 Controller는 연결되지 않음)
        PlayerCombatant = new PlayerCombatant(PlayerData, null);
        EnemyCombatant = new EnemyCombatant(EnemyData, null);

        Debug.Log("[CharacterManager] CharacterData 및 Combatant 초기화 완료.");
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
