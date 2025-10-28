using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Spine.Unity;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour, ICombatController
{
    public Character Character => CharacterManager.Instance?.PlayerCharacter;    
    private int currentCommandIndex;
    // Skeleton Mecanim의 Animator 컴포넌트 참조

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = true;
    [SerializeField] public bool useRandomAction = false;

    [Tooltip("테스트 모드에서 사용할 커맨드 인덱스")]
    [SerializeField] private int testCommandIndex;
    [SerializeField] private SwordArtStyleData equippedStyle;
    public SwordArtStyleData EquippedStyle => equippedStyle;
    
    [Header("Spine 애니메이션 연동")]
    // Skeleton Mecanim을 통한 Unity Animator 기반 애니메이션 제어
    [Tooltip("CombatAnimation 오브젝트 (SkeletonMecanim 컴포넌트가 포함된 하위 오브젝트)")]
    [SerializeField] private GameObject combatAnimationObject;
    
    /// <summary>
    /// CombatAnimation 오브젝트에 접근하기 위한 프로퍼티
    /// </summary>
    public GameObject CombatAnimationObject => combatAnimationObject;
    
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
    public int CommandCount => Character?.AvailableCommands.Count ?? 0;
    
    public void SetSelectedCommandIndex(int commandIndex)
    {
        currentCommandIndex = commandIndex;
    }

    // InputSystem의 ActionSelect 1D Axis에 연결될 메서드들
    public void OnActionSelect(float value)
    {
        Debug.Log($"[PlayerController] OnActionSelect(float) 호출됨: {value}");
        HandleActionSelectInput(value);
    }

    public void OnActionSelect(InputValue value)
    {
        Debug.Log($"[PlayerController] OnActionSelect(InputValue) 호출됨");
        float floatValue = value.Get<float>();
        HandleActionSelectInput(floatValue);
    }

    public void OnActionSelect(InputAction.CallbackContext context)
    {
        Debug.Log($"[PlayerController] OnActionSelect(CallbackContext) 호출됨");
        float floatValue = context.ReadValue<float>();
        HandleActionSelectInput(floatValue);
    }

    private void HandleActionSelectInput(float value)
    {
        Debug.Log($"[PlayerController] HandleActionSelectInput: {value}");
        
        // 포커스 이동 기능이 제거됨 - 키보드 입력 무시
        if (Mathf.Abs(value) > 0.1f)
        {
            Debug.Log("[PlayerController] 키보드 입력 감지됨 (포커스 이동 기능 제거됨)");
        }
    }


    public bool UseTestMode => useTestMode;

    public void SetTestMode(bool testMode)
    {
        if (useTestMode != testMode)
        {
            useTestMode = testMode;
            Debug.Log($"[PlayerController] 테스트 모드 변경: {testMode}");
            
            // ActionCommandSelectionManager를 통해 UI 접근 (Scene 분리 대비)
            if (ActionCommandSelectionManager.Instance != null && 
                ActionCommandSelectionManager.Instance.playerActionSelectUI != null)
            {
                ActionCommandSelectionManager.Instance.playerActionSelectUI.RefreshButtons();
            }
        }
    }

    void Awake()
    {
        // CharacterManager 초기화 대기 후 유파 장착
        StartCoroutine(WaitForCharacterManagerAndSetup());
    }

    private System.Collections.IEnumerator WaitForCharacterManagerAndSetup()
    {
        // CharacterManager가 초기화될 때까지 대기
        while (CharacterManager.Instance == null)
        {
            yield return null;
        }

        // Character가 준비될 때까지 대기
        while (Character == null)
        {
            yield return null;
        }

        // 유파 장착
        Character?.EquipSwordArtStyle(equippedStyle);
        
        // Spine 애니메이션 애셋을 Skeleton Mecanim에 연결
        SetupSkeletonMecanim();
    }
    
    /// <summary>
    /// 장착된 유파의 Spine 애니메이션 애셋을 SkeletonMecanim 컴포넌트에 연결
    /// </summary>
    private void SetupSkeletonMecanim()
    {
        Debug.Log("[PlayerController] SetupSkeletonMecanim 시작");
        
        if (equippedStyle == null)
        {
            Debug.LogError("[PlayerController] 장착된 유파가 없어서 Spine 애니메이션 애셋을 연결할 수 없습니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] 유파 정보: {equippedStyle.styleName}");
        
        // Inspector에서 연결된 CombatAnimation 오브젝트 확인
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 Inspector에서 연결되지 않았습니다. PlayerController의 Combat Animation Object 필드에 연결해주세요.");
            return;
        }
        
        Debug.Log($"[PlayerController] CombatAnimation 오브젝트 찾음: {combatAnimationObject.name}");
        
        // SkeletonMecanim 컴포넌트 찾기
        var skeletonMecanim = combatAnimationObject.GetComponent<SkeletonMecanim>();
        if (skeletonMecanim == null)
        {
            Debug.LogError("[PlayerController] SkeletonMecanim 컴포넌트를 찾을 수 없습니다. CombatAnimation 오브젝트에 SkeletonMecanim 컴포넌트를 추가해주세요.");
            return;
        }
        
        Debug.Log($"[PlayerController] SkeletonMecanim 컴포넌트 찾음: {skeletonMecanim.name}");
        
        var spineAsset = equippedStyle.SpineAnimationAsset;
        if (spineAsset == null)
        {
            Debug.LogError($"[PlayerController] 유파 '{equippedStyle.styleName}'에 Spine 애니메이션 애셋이 설정되지 않았습니다. SwordArtStyleData에서 SpineAnimationAsset을 설정해주세요.");
            return;
        }
        
        Debug.Log($"[PlayerController] Spine 애셋 찾음: {spineAsset.name}");
        
        // SkeletonMecanim에 Spine 애니메이션 애셋 연결
        skeletonMecanim.skeletonDataAsset = spineAsset;
        Debug.Log($"[PlayerController] Spine 애니메이션 애셋 연결 완료: {spineAsset.name} (유파: {equippedStyle.styleName})");
        
        // 연결 후 상태 확인
        if (skeletonMecanim.skeletonDataAsset != null)
        {
            Debug.Log($"[PlayerController] 연결 확인됨: {skeletonMecanim.skeletonDataAsset.name}");
        }
        else
        {
            Debug.LogError("[PlayerController] 연결 실패: skeletonDataAsset이 null입니다.");
        }
    }

    void Start()
    {
        UpdateCommandDisplay();
    }

    public void NextCommand()
    {
        if (CommandCount == 0) return;
        currentCommandIndex = (currentCommandIndex + 1) % CommandCount;
        UpdateCommandDisplay();
    }

    public void PreviousCommand()
    {
        if (CommandCount == 0) return;
        currentCommandIndex = (currentCommandIndex - 1 + CommandCount) % CommandCount;
        UpdateCommandDisplay();
    }

    /// <summary>
    /// 현재 턴에 사용할 검술 인덱스를 반환합니다.
    /// 
    /// 모드별 동작:
    /// - TestMode = true: testCommandIndex 또는 랜덤 사용
    /// - TestMode = false: Combatant.ChooseCommand() 호출 (현재는 UI 기반, 향후 BT 지원 가능)
    /// 
    /// 중요:
    /// - EnemyController와 동일한 구조로 설계
    /// - 향후 자동 전투 시스템 추가 시 BT 지원 가능
    /// </summary>
    public int GetSelectedCommandIndex()
    {
        if (useTestMode)
        {
            // ========================================
            // 테스트 모드: 에디터에서 설정한 값 사용
            // ========================================
            if (useRandomAction)
            {
                int len = equippedStyle.CommandSet.Count;
                if (len == 0) return testCommandIndex; // 보호 코드
                
                int randomIndex = UnityEngine.Random.Range(0, len);
                Debug.Log($"[PlayerController] 테스트 모드 - 랜덤 선택: {randomIndex}");
                return randomIndex;
            }
            else
            {
                Debug.Log($"[PlayerController] 테스트 모드 - 고정 인덱스: {testCommandIndex}");
                return testCommandIndex;
            }
        }
        else
        {
            // ========================================
            // 일반 모드: Combatant의 ChooseCommand() 호출
            // ========================================
            // PlayerCombatant.ChooseCommand()가 실행되며:
            // - 현재: UI 기반 선택
            // - 향후: BT 기반 선택 가능 (자동 전투 등)
            
            var selection = Character?.ChooseCommand();
            int selectedIndex = selection?.selectedIndex ?? 0;
            
            Debug.Log($"[PlayerController] 일반 모드 - 선택된 인덱스: {selectedIndex}");
            return Mathf.Clamp(selectedIndex, 0, CommandCount - 1);
        }
    }

    public ActionCommandData GetSelectedCommand()
    {
        int idx = GetSelectedCommandIndex();
        
        // equippedStyle의 CommandSet에서 가져오기
        if (equippedStyle != null && idx >= 0 && idx < equippedStyle.CommandSet.Count)
        {
            return equippedStyle.CommandSet[idx];
        }
        
        return null;
    }

    private void UpdateCommandDisplay()
    {
        var cmd = GetSelectedCommand();
        CombatStatusDisplay.Instance?.SetPlayerActionCommandName(cmd?.commandName);
    }
    
    /// <summary>
    /// 공격 커맨드 실행 시 호출 - Skeleton Mecanim을 통한 애니메이션 제어
    /// </summary>
    public void OnPlayActionCommand()
    {
        Debug.Log("[PlayerController] OnPlayActionCommand 호출됨");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 현재 선택된 검술 액션의 애니메이션 이름 가져오기
        var currentCommand = GetSelectedCommand();
        if (currentCommand == null)
        {
            Debug.LogError("[PlayerController] 현재 선택된 커맨드가 없습니다.");
            return;
        }
        
        string animationName = currentCommand.animationName;
        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogError("[PlayerController] 현재 커맨드에 애니메이션 이름이 설정되지 않았습니다.");
            return;
        }
        
        // 현재 애니메이션 상태가 같은 액션이면 추가 실행 무시
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            Debug.Log($"[PlayerController] 이미 {animationName} 애니메이션이 재생 중입니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 검술 액션 애니메이션 재생
        animator.SetTrigger(animationName);
        Debug.Log($"[PlayerController] {animationName} 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 중단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 중단 애니메이션 재생
        animator.SetTrigger("interrupted");
        Debug.Log("[PlayerController] 중단 애니메이션 재생 (Skeleton Mecanim)");
    }
    
    public void ReceiveCommandResult(CharacterCommandResult result)
    {
        // 아직 쓸데없음
    }
    
    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        // 히트 결과를 UI에 표시합니다.
        string msg = isPerfect ? "Perfect!" : "Miss!";
        if (isPerfect)
        {
            CombatStatusDisplay.Instance.ShowPlayerHitResult(hitIndex, msg);
        }
        else
        {
            CombatStatusDisplay.Instance.ShowPlayerHitResult(hitIndex, msg);
        }
    }
    
    /// <summary>
    /// 쳐내기 성공 시 호출 - 쳐내기 애니메이션 재생
    /// </summary>
    public void OnSuccessParry()
    {
        Debug.Log("[PlayerController] 플레이어 쳐내기 성공 애니메이션");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 쳐내기 성공 애니메이션 재생
        animator.SetTrigger("parry");
        Debug.Log("[PlayerController] 쳐내기 성공 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 피격 시 호출 - 피격 애니메이션 재생
    /// </summary>
    public void OnBeHitted()
    {
        Debug.Log("[PlayerController] 플레이어 피격 애니메이션");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // Skeleton Mecanim을 통한 피격 애니메이션 재생
        animator.SetTrigger("hit");
        Debug.Log("[PlayerController] 피격 애니메이션 시작 (Skeleton Mecanim)");
    }
    
    /// <summary>
    /// 방어 시 호출 - 방어 애니메이션 재생
    /// </summary>
    public void OnPlayDefence()
    {
        Debug.Log("[PlayerController] 플레이어 방어 애니메이션");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 🆕 Bool 파라미터로 막기 상태 지속 (Trigger 대신)
        animator.SetBool("isGuarding", true);
        Debug.Log("[PlayerController] 🆕 막기 애니메이션 지속 시작 (Bool 파라미터)");
    }
    
    /// <summary>
    /// 🆕 막기 애니메이션 중단
    /// </summary>
    public void OnStopDefence()
    {
        Debug.Log("[PlayerController] 🆕 플레이어 막기 애니메이션 중단");
        
        // CombatAnimation 오브젝트에서 Animator 컴포넌트 찾기
        if (combatAnimationObject == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트가 연결되지 않았습니다.");
            return;
        }
        
        var animator = combatAnimationObject.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[PlayerController] CombatAnimation 오브젝트에서 Animator 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        // 🆕 Bool 파라미터로 막기 상태 해제
        animator.SetBool("isGuarding", false);
        Debug.Log("[PlayerController] 🆕 막기 애니메이션 중단 (Bool 파라미터)");
    }
}
