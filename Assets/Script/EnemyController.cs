using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private SwordArtStyleData equippedStyle;
    private EnemyCombatant combatant;

    // 외부에서 combatant에 접근할 수 있도록 프로퍼티로 공개
    public EnemyCombatant Combatant => combatant;

    public int CommandCount => combatant.AvailableCommands.Count;

    void Awake()
    {
        combatant = new EnemyCombatant("Enemy");
        combatant.EquipSwordArtStyle(equippedStyle);
    }

    // (AI 로직에서 호출) 현재 커맨드를 반환
    public ActionCommandData GetCommandByIndex(int turnIndex)
    {
        if (CommandCount == 0) return null;
        var list = combatant.AvailableCommands;
        // 예시: 순환 선택
        return list[turnIndex % list.Count];
    }
}
