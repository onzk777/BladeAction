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

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = true;
    [SerializeField] public bool useRandomAction = false;

    [Tooltip("테스트 모드에서 사용할 커맨드 인덱스")]
    [SerializeField] private int testCommandIndex;
    [SerializeField] private SwordArtStyleData equippedStyle;
    public SwordArtStyleData EquippedStyle => equippedStyle;
    
    [Header("Spine 애니메이션 연동")]
    // SpineAttackTestAdapter는 같은 GameObject에 직접 추가됨
    
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
        
        // 유파 장착 후 Spine 애니메이션 애셋 연결
        ConnectSpineAnimationAsset();
    }
    
    /// <summary>
    /// 장착된 유파의 Spine 애니메이션 애셋을 SkeletonAnimation 컴포넌트에 연결
    /// </summary>
    private void ConnectSpineAnimationAsset()
    {
        Debug.Log("[PlayerController] ConnectSpineAnimationAsset 시작");
        
        if (equippedStyle == null)
        {
            Debug.LogError("[PlayerController] 장착된 유파가 없어서 Spine 애니메이션 애셋을 연결할 수 없습니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] 유파 정보: {equippedStyle.styleName}");
        
        var spineAnimation = GetComponent<SkeletonAnimation>();
        if (spineAnimation == null)
        {
            Debug.LogError("[PlayerController] SkeletonAnimation 컴포넌트를 찾을 수 없습니다. 컴포넌트를 추가해주세요.");
            return;
        }
        
        Debug.Log($"[PlayerController] SkeletonAnimation 컴포넌트 찾음: {spineAnimation.name}");
        
        var spineAsset = equippedStyle.SpineAnimationAsset;
        if (spineAsset == null)
        {
            Debug.LogError($"[PlayerController] 유파 '{equippedStyle.styleName}'에 Spine 애니메이션 애셋이 설정되지 않았습니다. SwordArtStyleData에서 SpineAnimationAsset을 설정해주세요.");
            return;
        }
        
        Debug.Log($"[PlayerController] Spine 애셋 찾음: {spineAsset.name}");
        
        // Spine 애니메이션 애셋 연결
        spineAnimation.skeletonDataAsset = spineAsset;
        Debug.Log($"[PlayerController] Spine 애니메이션 애셋 연결 완료: {spineAsset.name} (유파: {equippedStyle.styleName})");
        
        // 연결 후 상태 확인
        if (spineAnimation.skeletonDataAsset != null)
        {
            Debug.Log($"[PlayerController] 연결 확인됨: {spineAnimation.skeletonDataAsset.name}");
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
    /// 공격 커맨드 실행 시 호출 - Spine 애니메이션 재생
    /// </summary>
    public void OnPlayActionCommand()
    {
        Debug.Log("[PlayerController] OnPlayActionCommand 호출됨");
        
        var spineAnimation = GetComponent<SkeletonAnimation>();
        if (spineAnimation == null)
        {
            Debug.LogError("[PlayerController] SkeletonAnimation 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] SkeletonAnimation 컴포넌트 찾음: {spineAnimation.name}");
        
        if (equippedStyle == null)
        {
            Debug.LogError("[PlayerController] equippedStyle이 null입니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] 유파 정보: {equippedStyle.styleName}");
        
        var command = GetSelectedCommand(); // 테스트 설정을 반영한 커맨드 사용
        if (command == null)
        {
            Debug.LogError("[PlayerController] 선택된 커맨드가 null입니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] 선택된 커맨드: {command.commandName}");
        
        if (string.IsNullOrEmpty(command.animationName))
        {
            Debug.LogError($"[PlayerController] 커맨드 '{command.commandName}'의 animationName이 설정되지 않았습니다.");
            return;
        }
        
        Debug.Log($"[PlayerController] 애니메이션 이름: {command.animationName}");
        
        // Spine 애니메이션 재생
        try
        {
            spineAnimation.AnimationState.SetAnimation(0, command.animationName, false);
            Debug.Log($"[PlayerController] 공격 애니메이션 재생 성공: {command.animationName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerController] 애니메이션 재생 실패: {e.Message}");
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
            Debug.Log("[PlayerController] 중단 애니메이션 재생");
        }
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
        // TODO: 쳐내기 성공 애니메이션 구현
    }
    
    /// <summary>
    /// 피격 시 호출 - 피격 애니메이션 재생
    /// </summary>
    public void OnBeHitted()
    {
        Debug.Log("[PlayerController] 플레이어 피격 애니메이션");
        // TODO: 피격 애니메이션 구현
    }
    
    /// <summary>
    /// 방어 시 호출 - 방어 애니메이션 재생
    /// </summary>
    public void OnPlayDefence()
    {
        Debug.Log("[PlayerController] 플레이어 방어 애니메이션");
        // TODO: 플레이어 방어 애니메이션 구현
    }
}
