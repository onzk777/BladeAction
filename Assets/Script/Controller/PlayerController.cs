using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        var selectedCommand = GetSelectedCommand();
        if (selectedCommand != null)
        {
            Debug.Log($"[PlayerController] 공격 커맨드 실행 - 커맨드: {selectedCommand.commandName}");
            
            // Spine 애니메이션 재생 (같은 GameObject에서 컴포넌트 찾기)
            var spineAdapter = GetComponent<SpineAttackTestAdapter>();
            if (spineAdapter != null)
            {
                spineAdapter.PlayForCommand(selectedCommand);
                Debug.Log($"[PlayerController] Spine 애니메이션 재생 시작: {selectedCommand.commandName}");
            }
            else
            {
                Debug.LogWarning("[PlayerController] Spine 어댑터가 연결되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] 공격 커맨드 실행 실패: 선택된 커맨드가 null");
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
    
    // ✅ 애니메이션 재생 관련 메서드 구현 (정규 Feature)
    /// <summary>
    /// 공격이 차단되었을 때 호출 - 차단 애니메이션 재생
    /// </summary>
    public void OnInterrupted()
    {
        Debug.Log("[PlayerController] 플레이어 공격 차단 애니메이션");
        // TODO: 공격 차단 애니메이션 구현
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
