using UnityEngine;
using Spine.Unity;

public class AIController : MonoBehaviour, ICombatController
{
    private CombatCharacterManager.CombatantSlot combatantSlot;
    public Character Character => combatantSlot?.Character;

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = true;
    [SerializeField] public bool useRandomAction = false;

    [SerializeField] private int testCommandIndex;
    
    [Header("Spine 애니메이션 연동")]
    [Tooltip("CombatAnimation 오브젝트 (SkeletonMecanim 컴포넌트가 포함된 하위 오브젝트)")]
    [SerializeField] private GameObject combatAnimationObject;
    
    /// <summary>
    /// 인벤토리에서 장착된 유파를 반환합니다 (ICombatController 구현)
    /// </summary>
    public SwordArtStyleData EquippedStyle 
    { 
        get 
        {
            var styleItem = Character?.Inventory?.GetEquippedItem(BladeAction.Item.EquipmentSlotType.SwordArtStyle);
            if (styleItem == null)
                return null;
            
            // swordArtStyleData 직접 참조 또는 Key로 조회
            var styleData = styleItem.swordArtStyleData;
            
            if (styleData == null && !string.IsNullOrEmpty(styleItem.swordArtStyleKey))
            {
                // Key로 조회
                var styleDb = SwordArtStyleDatabase.Instance;
                if (styleDb != null)
                {
                    styleData = styleDb.GetStyle(styleItem.swordArtStyleKey);
                }
            }
            
            return styleData;
        } 
    }

    public void BindCombatantSlot(CombatCharacterManager.CombatantSlot slot)
    {
        combatantSlot = slot;
    }
    
    /// <summary>
    /// CombatAnimation 오브젝트에 접근하기 위한 프로퍼티
    /// </summary>
    public GameObject CombatAnimationObject => combatAnimationObject;
    
    public int TestCommandIndex
    {
        get => testCommandIndex;
        set => testCommandIndex = value;
    }
    
    public bool UseTestMode => useTestMode;

    // 현재 턴에 사용할 커맨드를 반환
    public ActionCommandData GetCurrentActionCommand(int commandIndex)
    {
        if (Character?.AvailableCommands == null || commandIndex < 0 || commandIndex >= Character.AvailableCommands.Count)
        {
        Debug.LogError($"[AIController] 유효하지 않은 커맨드 인덱스: {commandIndex} (사용 가능한 검술 수: {Character?.AvailableCommands?.Count ?? 0})");
            return null;
        }
        return Character.AvailableCommands[commandIndex];
    }

    // 외부에서 combatant에 접근할 수 있도록 프로퍼티로 공개
    public int CommandCount => Character?.AvailableCommands.Count ?? 0;
    
    private int currentCommandIndex;
    private int? cachedSelectedIndex = null; // 선택 결과 캐시 (턴당 한 번만 계산)
    
    public void SetSelectedCommandIndex(int commandIndex)
    {
        currentCommandIndex = commandIndex;
        Debug.Log($"[AIController] 선택된 검술 인덱스: {commandIndex}");
    }

    void Awake()
    {
        // CombatCharacterManager 초기화 대기 후 Spine 애니메이션 설정
        StartCoroutine(WaitForCharacterManagerAndSetup());
    }

    private System.Collections.IEnumerator WaitForCharacterManagerAndSetup()
    {
        // Combatant 슬롯이 연결될 때까지 대기
        while (combatantSlot == null)
        {
            yield return null;
        }

        // Character가 준비될 때까지 대기
        while (Character == null)
        {
            yield return null;
        }

        // Inventory가 준비될 때까지 대기
        while (Character.Inventory == null)
        {
            yield return null;
        }
        
        // Spine 애니메이션 애셋을 Skeleton Mecanim에 연결
        SetupSkeletonMecanim();
    }
    
    /// <summary>
    /// 인벤토리에 장착된 유파의 Spine 애니메이션 애셋을 SkeletonMecanim 컴포넌트에 연결
    /// </summary>
    private void SetupSkeletonMecanim()
    {
        Debug.Log("[AIController] SetupSkeletonMecanim 시작");
        
        // Inspector에서 연결된 CombatAnimation 오브젝트 확인
        if (combatAnimationObject == null)
        {
            Debug.LogWarning("[AIController] CombatAnimation 오브젝트가 Inspector에서 연결되지 않았습니다. AIController의 Combat Animation Object 필드에 연결해주세요.");
            return;
        }
        
        // SkeletonMecanim 컴포넌트 찾기
        var skeletonMecanim = combatAnimationObject.GetComponent<SkeletonMecanim>();
        if (skeletonMecanim == null)
        {
            Debug.LogWarning("[AIController] SkeletonMecanim 컴포넌트를 찾을 수 없습니다. CombatAnimation 오브젝트에 SkeletonMecanim 컴포넌트를 추가해주세요.");
            return;
        }
        
        // 인벤토리에서 장착된 유파 가져오기
        var equippedStyleItem = Character.Inventory?.GetEquippedItem(BladeAction.Item.EquipmentSlotType.SwordArtStyle);
        if (equippedStyleItem == null)
        {
            // 유파 미장착은 정상 상황 (경고 제거)
            return;
        }
        
        // swordArtStyleData 직접 참조 또는 Key로 조회
        var styleData = equippedStyleItem.swordArtStyleData;
        
        if (styleData == null && !string.IsNullOrEmpty(equippedStyleItem.swordArtStyleKey))
        {
            // Key로 조회
            var styleDb = SwordArtStyleDatabase.Instance;
            if (styleDb != null)
            {
                styleData = styleDb.GetStyle(equippedStyleItem.swordArtStyleKey);
            }
        }
        
        if (styleData == null)
        {
            Debug.LogError($"[AIController] 유파 아이템 '{equippedStyleItem.itemName}'에 SwordArtStyleData를 찾을 수 없습니다. (Key: {equippedStyleItem.swordArtStyleKey})");
            return;
        }
        
        Debug.Log($"[AIController] 유파 정보: {styleData.styleName}");
        
        var spineAsset = styleData.SpineAnimationAsset;
        if (spineAsset == null)
        {
            Debug.LogWarning($"[AIController] 유파 '{styleData.styleName}'에 Spine 애니메이션 애셋이 설정되지 않았습니다.");
            return;
        }
        
        // SkeletonMecanim에 Spine 애니메이션 애셋 연결
        skeletonMecanim.skeletonDataAsset = spineAsset;
        Debug.Log($"[AIController] Spine 애니메이션 애셋 연결 완료: {spineAsset.name} (유파: {styleData.styleName})");
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
            var selection = Character?.ChooseCommand();
            idx = Mathf.Clamp(selection?.selectedIndex ?? 0, 0, CommandCount - 1);
        }
        return Character?.AvailableCommands[idx];
    }
    
    /// <summary>
    /// 현재 턴에 사용할 검술 인덱스를 반환합니다.
    /// 
    /// 모드별 동작:
    /// - TestMode = true: testCommandIndex 또는 랜덤 사용
    /// - TestMode = false: BT 시스템 사용 (Combatant.ChooseCommand() 호출)
    /// 
    /// 중요:
    /// - BT 모드에서는 EnemyCombatant.ChooseCommand()가 호출됨
    /// - ChooseCommand()는 BT 평가 → 확률 적용 → 검술 선택을 모두 수행
    /// - 캐싱: 한 턴에 여러 번 호출되어도 첫 번째 결과를 재사용 (BT 재평가 방지)
    /// </summary>
    public int GetSelectedCommandIndex()
    {
        // 이미 이번 턴에 선택했으면 캐시된 값 반환
        if (cachedSelectedIndex.HasValue)
        {
            return cachedSelectedIndex.Value;
        }
        
        int selectedIndex;
        
        if (useTestMode)
        {
            // ========================================
            // 테스트 모드: 에디터에서 설정한 값 사용
            // ========================================
            if (useRandomAction)
            {
                int len = CommandCount;
                if (len == 0) 
                {
                    selectedIndex = 0;
                    Debug.LogWarning("[AIController] 사용 가능한 검술이 없습니다. 인덱스 0 반환");
                }
                else
                {
                    selectedIndex = UnityEngine.Random.Range(0, len);
                }
            }
            else
            {
                selectedIndex = testCommandIndex;
            }
        }
        else
        {
            // ========================================
            // BT 모드: Combatant의 ChooseCommand() 호출
            // ========================================
            // EnemyCombatant.ChooseCommand()가 실행되며:
            // 1. BT 평가 (ExecuteBehaviorTrees)
            // 2. 확률 적용 (ApplyBehaviorTreeResults)
            // 3. 검술 선택 (GetSelectedCommandFromBT)
            
            var selection = Character?.ChooseCommand();
            selectedIndex = selection?.selectedIndex ?? 0;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, CommandCount - 1);
        }
        
        // 선택 결과 캐싱 (이번 턴에는 재사용)
        cachedSelectedIndex = selectedIndex;
        
        return selectedIndex;
    }
    
    /// <summary>
    /// 새 턴 시작 시 캐시 초기화 (CombatManager에서 호출)
    /// </summary>
    public void ResetSelectionCache()
    {
        cachedSelectedIndex = null;
    }

    public ActionCommandData GetSelectedCommand()
    {
        int idx = GetSelectedCommandIndex();
        return Character?.AvailableCommands[idx];
    }

    public void ReceiveCommandResult(CharacterCommandResult result)
    {
        // 아직 쓸데 없음
    }
    
    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        // 히트 결과를 UI에 표시합니다.
        string msg = isPerfect ? "Perfect!" : "Miss!";
        if (isPerfect)
        {
            CombatDebugDisplay.Instance?.ShowEnemyHitResult(hitIndex, msg);
        }
        else
        {
            CombatDebugDisplay.Instance?.ShowEnemyHitResult(hitIndex, msg);
        }
    }
    
    // ✅ 애니메이션 재생 관련 메서드 구현 (정규 Feature)
    /// <summary>
    /// 공격 커맨드 애니메이션 재생 - Skeleton Mecanim을 통한 애니메이션 제어
    /// </summary>
    public void OnPlayActionCommand()
    {
        Debug.Log("[AIController] OnPlayActionCommand 호출됨");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 현재 선택된 검술 액션의 애니메이션 이름 가져오기
        var currentCommand = GetSelectedCommand();
        if (currentCommand == null)
        {
            Debug.LogError("[AIController] 현재 선택된 커맨드가 없습니다.");
            return;
        }
        
        string animationName = currentCommand.animationName;
        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning("[AIController] 현재 커맨드에 애니메이션 이름이 설정되지 않았습니다.");
            return;
        }
        
        // 현재 애니메이션 상태가 같은 액션이면 추가 실행 무시
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            Debug.Log($"[AIController] 이미 {animationName} 애니메이션 재생 중입니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 검술 액션 애니메이션 재생
        animator.SetTrigger(animationName);
        Debug.Log($"[AIController] {animationName} 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 중단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 중단 애니메이션 재생
        animator.SetTrigger("interrupted");
        Debug.Log("[AIController] 중단 애니메이션 재생 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 쳐내기 성공 시 호출 - 쳐내기 애니메이션 재생
    /// </summary>
    public void OnSuccessParry()
    {
        Debug.Log("[AIController] AI 쳐내기 성공 애니메이션");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 쳐내기 성공 애니메이션 재생
        animator.SetTrigger("parry");
        Debug.Log("[AIController] 쳐내기 성공 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 피격 시 호출 - 피격 애니메이션 재생
    /// </summary>
    public void OnBeHitted()
    {
        Debug.Log("[AIController] AI 피격 애니메이션");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 피격 애니메이션 재생
        animator.SetTrigger("hit");
        Debug.Log("[AIController] 피격 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 방어 시 호출 - 방어 애니메이션 재생
    /// </summary>
    public void OnPlayDefence()
    {
        Debug.Log("[AIController] 🆕 OnPlayDefence 호출됨");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 🆕 현재 isGuarding 상태 확인
        bool currentIsGuarding = animator.GetBool("isGuarding");
        Debug.Log($"[AIController] 🆕 현재 isGuarding 상태: {currentIsGuarding}");
        
        // 🆕 Bool 파라미터로 막기 상태 지속 (Trigger 대신)
        animator.SetBool("isGuarding", true);
        
        // 🆕 설정 후 상태 확인
        bool newIsGuarding = animator.GetBool("isGuarding");
        Debug.Log($"[AIController] 🆕 isGuarding 설정 후 상태: {newIsGuarding}");
        Debug.Log("[AIController] 🆕 막기 애니메이션 지속 시작 (Bool 파라미터)");
    }
    
    /// <summary>
    /// 🆕 막기 애니메이션 중단
    /// </summary>
    public void OnStopDefence()
    {
        Debug.Log("[AIController] 🆕 OnStopDefence 호출됨");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[AIController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 🆕 현재 isGuarding 상태 확인
        bool currentIsGuarding = animator.GetBool("isGuarding");
        Debug.Log($"[AIController] 🆕 현재 isGuarding 상태: {currentIsGuarding}");
        
        // 🆕 Bool 파라미터로 막기 상태 해제
        animator.SetBool("isGuarding", false);
        
        // 🆕 설정 후 상태 확인
        bool newIsGuarding = animator.GetBool("isGuarding");
        Debug.Log($"[AIController] 🆕 isGuarding 설정 후 상태: {newIsGuarding}");
        Debug.Log("[AIController] 🆕 막기 애니메이션 중단 (Bool 파라미터)");
    }
}
