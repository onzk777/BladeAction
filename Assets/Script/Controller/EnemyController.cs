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
        combatant = new EnemyCombatant("Enemy", this);
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
    public int GetSelectedCommandIndex()
    {
        return testCommandIndex;
    }

    public void ReceiveCommandResult(CombatantCommandResult result)
    {
        // 아직 쓸데 없음
    }
    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        // 히트 결과를 UI에 표시합니다.
        string msg = isPerfect ? "완벽한 일격!" : "타이밍 놓침!";
        if (isPerfect)
        {
            CombatStatusDisplay.Instance.ShowEnemyHitResult(hitIndex, msg);
        }
        else
        {
            CombatStatusDisplay.Instance.ShowEnemyHitResult(hitIndex, msg);
        }
    }
}
