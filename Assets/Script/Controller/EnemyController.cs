using UnityEngine;

public class EnemyController : MonoBehaviour, ICombatController
{
    private EnemyCombatant combatant;
    public Combatant Combatant => combatant;

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = true;
    [SerializeField] public bool useRandomAction = false;

    [SerializeField] private int testCommandIndex;
    [SerializeField] private SwordArtStyleData equippedStyle;
    public SwordArtStyleData EquippedStyle => equippedStyle;
    public int TestCommandIndex
    {
        get => testCommandIndex;
        set => testCommandIndex = value;
    }

    // 현재 턴에 사용할 커맨드를 반환
    public ActionCommandData GetCurrentActionCommand(int commandIndex)
    {
        return equippedStyle.CommandSet[commandIndex];
    }

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
        int index = 0;
        if(useTestMode)
        {
            if (useRandomAction)
            {
                int len = equippedStyle.CommandSet.Count;
                if (len == 0) return testCommandIndex; // 보호 코드

                int randomIndex = UnityEngine.Random.Range(0, len);
                index = randomIndex;
            }
            else
            {
                index = testCommandIndex;
            }
        }
        else
            index = testCommandIndex;
        return index;
    }

    public void ReceiveCommandResult(CombatantCommandResult result)
    {
        // 아직 쓸데 없음
    }
    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        // 히트 결과를 UI에 표시합니다.
        string msg = isPerfect ? "Perfect!" : "Miss!";
        if (isPerfect)
        {
            CombatStatusDisplay.Instance.ShowEnemyHitResult(hitIndex, msg);
        }
        else
        {
            CombatStatusDisplay.Instance.ShowEnemyHitResult(hitIndex, msg);
        }
    }
    
    // ✅ 애니메이션 재생 관련 메서드 구현 (정규 Feature)
    /// <summary>
    /// 공격 커맨드 실행 시 호출 - Spine 애니메이션 재생
    /// </summary>
    public void OnPlayActionCommand()
    {
        var selectedCommand = GetCurrentActionCommand(GetSelectedCommandIndex());
        if (selectedCommand != null)
        {
            Debug.Log($"[EnemyController] AI 공격 커맨드 실행 - 커맨드: {selectedCommand.commandName}");
            
            // Spine 애니메이션 재생 (같은 GameObject에서 컴포넌트 찾기)
            var spineAdapter = GetComponent<SpineAttackTestAdapter>();
            if (spineAdapter != null)
            {
                spineAdapter.PlayForCommand(selectedCommand);
                Debug.Log($"[EnemyController] AI Spine 애니메이션 재생 시작: {selectedCommand.commandName}");
            }
            else
            {
                Debug.LogWarning("[EnemyController] AI Spine 어댑터가 연결되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning("[EnemyController] AI 공격 커맨드 실행 실패: 선택된 커맨드가 null");
        }
    }
    
    /// <summary>
    /// 공격이 차단되었을 때 호출 - 차단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        Debug.Log("[EnemyController] AI 공격 차단 애니메이션");
        // TODO: AI 공격 차단 애니메이션 구현
    }
    
    /// <summary>
    /// 쳐내기 성공 시 호출 - 쳐내기 애니메이션 재생
    /// </summary>
    public void OnSuccessParry()
    {
        Debug.Log("[EnemyController] AI 쳐내기 성공 애니메이션");
        // TODO: AI 쳐내기 성공 애니메이션 구현
    }
    
    /// <summary>
    /// 피격 시 호출 - 피격 애니메이션 재생
    /// </summary>
    public void OnBeHitted()
    {
        Debug.Log("[EnemyController] AI 피격 애니메이션");
        // TODO: AI 피격 애니메이션 구현
    }
    
    /// <summary>
    /// 방어 시 호출 - 방어 애니메이션 재생
    /// </summary>
    public void OnPlayDefence()
    {
        Debug.Log("[EnemyController] AI 방어 애니메이션");
        // TODO: AI 방어 애니메이션 구현
    }
}
