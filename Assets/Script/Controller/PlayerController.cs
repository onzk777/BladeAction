using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Spine.Unity;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour, ICombatController
{
    private PlayerCombatant combatant;
    public Combatant Combatant => combatant;    
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
    public int CommandCount => combatant.AvailableCommands.Count;

    void Awake()
    {
        combatant = new PlayerCombatant("Player", this);
        combatant.EquipSwordArtStyle(equippedStyle);
        
        // 유파 장착 후 Spine 애니메이션 애셋을 Skeleton Mecanim에 연결
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

    // 현재 턴(=currentCommandIndex)에 사용할 커맨드를 반환
    public int GetSelectedCommandIndex()
    {
        int index = 0;
        if (useTestMode)
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
    
    public void ReceiveCommandResult(CombatantCommandResult result)
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
        
        // Skeleton Mecanim을 통한 방어 애니메이션 재생
        animator.SetTrigger("guard");
        Debug.Log("[PlayerController] 방어 애니메이션 시작 (Skeleton Mecanim)");
    }
}
