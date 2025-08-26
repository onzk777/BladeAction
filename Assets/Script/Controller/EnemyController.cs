using UnityEngine;
using Spine.Unity;

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
        
        // 유파 장착 후 Spine 애니메이션 애셋 연결
        ConnectSpineAnimationAsset();
    }
    
    /// <summary>
    /// 장착된 유파의 Spine 애니메이션 애셋을 SkeletonAnimation 컴포넌트에 연결
    /// </summary>
    private void ConnectSpineAnimationAsset()
    {
        if (equippedStyle == null)
        {
            Debug.LogWarning("[EnemyController] 장착된 유파가 없어서 Spine 애니메이션 애셋을 연결할 수 없습니다.");
            return;
        }
        
        var spineAnimation = GetComponent<SkeletonAnimation>();
        if (spineAnimation == null)
        {
            Debug.LogWarning("[EnemyController] SkeletonAnimation 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        var spineAsset = equippedStyle.SpineAnimationAsset;
        if (spineAsset == null)
        {
            Debug.LogWarning($"[EnemyController] 유파 '{equippedStyle.styleName}'에 Spine 애니메이션 애셋이 설정되지 않았습니다.");
            return;
        }
        
        // Spine 애니메이션 애셋 연결
        spineAnimation.skeletonDataAsset = spineAsset;
        Debug.Log($"[EnemyController] Spine 애니메이션 애셋 연결 완료: {spineAsset.name} (유파: {equippedStyle.styleName})");
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

    public ActionCommandData GetSelectedCommand()
    {
        int idx = GetSelectedCommandIndex();
        return combatant.AvailableCommands[idx];
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
    /// 공격 커맨드 애니메이션 재생
    /// </summary>
    public void OnPlayActionCommand()
    {
        Debug.Log("[EnemyController] OnPlayActionCommand 호출됨");
        
        var spineAnimation = GetComponent<SkeletonAnimation>();
        if (spineAnimation == null)
        {
            Debug.LogError("[EnemyController] SkeletonAnimation 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"[EnemyController] SkeletonAnimation 컴포넌트 찾음: {spineAnimation.name}");
        
        if (equippedStyle == null)
        {
            Debug.LogError("[EnemyController] equippedStyle이 null입니다.");
            return;
        }
        
        Debug.Log($"[EnemyController] 유파 정보: {equippedStyle.styleName}");
        
        var command = GetSelectedCommand(); // 테스트 설정을 반영한 커맨드 사용
        if (command == null)
        {
            Debug.LogError("[EnemyController] 선택된 커맨드가 null입니다.");
            return;
        }
        
        Debug.Log($"[EnemyController] 선택된 커맨드: {command.commandName}");
        
        if (string.IsNullOrEmpty(command.animationName))
        {
            Debug.LogError($"[EnemyController] 커맨드 '{command.commandName}'의 animationName이 설정되지 않았습니다.");
            return;
        }
        
        Debug.Log($"[EnemyController] 애니메이션 이름: {command.animationName}");
        
        // Spine 애니메이션 재생
        try
        {
            spineAnimation.AnimationState.SetAnimation(0, command.animationName, false);
            Debug.Log($"[EnemyController] 공격 애니메이션 재생 성공: {command.animationName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemyController] 애니메이션 재생 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 중단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        var spineAnimation = GetComponent<SkeletonAnimation>();
        if (spineAnimation != null)
        {
            // 중단 애니메이션 재생
            spineAnimation.AnimationState.SetAnimation(0, AnimationNameTable.INTERRUPTED, false);
            Debug.Log("[EnemyController] 중단 애니메이션 재생");
        }
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
