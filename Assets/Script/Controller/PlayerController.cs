using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour, ICombatController
{
    private PlayerCombatant combatant;
    public Combatant Combatant => combatant;
    [SerializeField] private SwordArtStyleData equippedStyle;
    
    private int currentCommandIndex = 0;

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = false;

    [Tooltip("테스트 모드에서 사용할 커맨드 인덱스")]
    [SerializeField] private int testCommandIndex = 0;

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
        // UI 모드
        if (!useTestMode)
            return currentCommandIndex;
        // 테스트 모드
        return Mathf.Clamp(testCommandIndex, 0, CommandCount - 1);
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
    public void ReceiveCommandResult(CombatantCommandResult result)
    {
        // 1) 판정 결과를 UI에 보여줍니다.
       
        for (int i = 0; i < result.HitCount; i++)
        {
            bool isPerfect = result.HitResults[i].IsPerfect;  // 또는 hitResults[i].IsPerfect
            CombatStatusDisplay.Instance.ShowPlayerTurnResult(i + 1, isPerfect);
        }

        // 2) 필요하면 애니메이션 트리거, 사운드 재생 등 후속 연출
        //    e.g., animator.SetTrigger(perfects == total ? "PerfectCombo" : "Hit");
    }

    public void OnHitResult(int hitIndex, bool isPerfect)
    {
        // 히트 결과를 UI에 표시합니다.
        CombatStatusDisplay.Instance.ShowPlayerHitResult(hitIndex, isPerfect);
    }

}
