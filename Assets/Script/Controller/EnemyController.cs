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
    
    [Header("Spine 애니메이션 연동")]
    [Tooltip("CombatAnimation 오브젝트 (SkeletonMecanim 컴포넌트가 포함된 하위 오브젝트)")]
    [SerializeField] private GameObject combatAnimationObject;
    
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
        
        // 유파 장착 후 Spine 애니메이션 애셋을 Skeleton Mecanim에 연결
        SetupSkeletonMecanim();
    }
    
    /// <summary>
    /// 장착된 유파의 Spine 애니메이션 애셋을 SkeletonMecanim 컴포넌트에 연결
    /// </summary>
    private void SetupSkeletonMecanim()
    {
        if (equippedStyle == null)
        {
            Debug.LogWarning("[EnemyController] 장착된 유파가 없어서 Spine 애니메이션 애셋을 연결할 수 없습니다.");
            return;
        }
        
        // Inspector에서 연결된 CombatAnimation 오브젝트 확인
        if (combatAnimationObject == null)
        {
            Debug.LogWarning("[EnemyController] CombatAnimation 오브젝트가 Inspector에서 연결되지 않았습니다. EnemyController의 Combat Animation Object 필드에 연결해주세요.");
            return;
        }
        
        // SkeletonMecanim 컴포넌트 찾기
        var skeletonMecanim = combatAnimationObject.GetComponent<SkeletonMecanim>();
        if (skeletonMecanim == null)
        {
            Debug.LogWarning("[EnemyController] SkeletonMecanim 컴포넌트를 찾을 수 없습니다. CombatAnimation 오브젝트에 SkeletonMecanim 컴포넌트를 추가해주세요.");
            return;
        }
        
        var spineAsset = equippedStyle.SpineAnimationAsset;
        if (spineAsset == null)
        {
            Debug.LogWarning($"[EnemyController] 유파 '{equippedStyle.styleName}'에 Spine 애니메이션 애셋이 설정되지 않았습니다.");
            return;
        }
        
        // SkeletonMecanim에 Spine 애니메이션 애셋 연결
        skeletonMecanim.skeletonDataAsset = spineAsset;
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
    /// 공격 커맨드 애니메이션 재생 - Skeleton Mecanim을 통한 애니메이션 제어
    /// </summary>
    public void OnPlayActionCommand()
    {
        Debug.Log("[EnemyController] OnPlayActionCommand 호출됨");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[EnemyController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[EnemyController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 현재 애니메이션 상태가 Attack이면 추가 공격 무시
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            Debug.Log("[EnemyController] 이미 공격 중입니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 공격 애니메이션 재생
        animator.SetTrigger("Attack");
        Debug.Log("[EnemyController] 공격 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 중단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[EnemyController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[EnemyController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 중단 애니메이션 재생
        animator.SetTrigger("Interrupted");
        Debug.Log("[EnemyController] 중단 애니메이션 재생 (Skeleton Mecanim)");
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
