using UnityEngine;

public class EnemyController : MonoBehaviour, ICombatController
{
    [SerializeField] private SwordArtStyleData equippedStyle;
    private EnemyCombatant combatant;
    public Combatant Combatant => combatant;

    [Header("테스트 모드 설정")]
    [SerializeField] private bool useTestMode = false;
    [SerializeField][Min(0)] private int testCommandIndex = 0;

    // 외부에서 combatant에 접근할 수 있도록 프로퍼티로 공개

    public int CommandCount => combatant.AvailableCommands.Count;

    void Awake()
    {
        combatant = new EnemyCombatant("Enemy");
        combatant.EquipSwordArtStyle(equippedStyle);
    }

    // (AI 로직에서 호출) 현재 커맨드를 반환
    public ActionCommandData FetchNextCommand()
    {
        if (CommandCount == 0) return null;
        int idx;
        if (useTestMode)
        {
            // 테스트 모드용 인덱스
            idx = Mathf.Clamp(testCommandIndex, 0, CommandCount - 1);
        }
        else
        {
            // 도메인 모델에게 선택 로직 위임
            var selection = combatant.ChooseCommand();
            idx = Mathf.Clamp(selection.selectedIndex, 0, CommandCount - 1);
        }
        return combatant.AvailableCommands[idx];
    }
    public int GetSelectedIndex()
    {
        return testCommandIndex;
    }

    public void ReceiveCommandResult(CombatantCommandResult result)
    {
        for (int i = 0; i < result.HitCount; i++)
        {
            bool isPerfect = result.HitResults[i].IsPerfect;  // 또는 hitResults[i].IsPerfect
            CombatStatusDisplay.Instance.ShowEnemyTurnResult(i + 1, isPerfect);
        }
    }
    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        CombatStatusDisplay.Instance.ShowEnemyHitResult(hitIndex, isPerfect);
    }
}
