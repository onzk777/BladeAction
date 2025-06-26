using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private SwordArtStyleData equippedStyle;
    private PlayerCombatant combatant;
    private int currentCommandIndex = 0;

    [Header("테스트 모드 설정")]
    [Tooltip("테스트 모드 ON/OFF")]
    [SerializeField] private bool useTestMode = false;

    [Tooltip("테스트 모드에서 사용할 커맨드 인덱스")]
    [SerializeField] private int testCommandIndex = 0;

    public PlayerCombatant Combatant => combatant;
    public int CommandCount => combatant.AvailableCommands.Count;

    void Awake()
    {
        combatant = new PlayerCombatant("Player");
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
    public int GetSelectedIndex()
    {
        // UI 모드
        if (!useTestMode)
            return currentCommandIndex;
        // 테스트 모드
        return Mathf.Clamp(testCommandIndex, 0, CommandCount - 1);
    }

    public ActionCommandData GetSelectedCommand()
    {
        int idx = GetSelectedIndex();
        return combatant.AvailableCommands[idx];
    }

    private void UpdateCommandDisplay()
    {
        var cmd = GetSelectedCommand();
        CombatStatusDisplay.Instance?.SetPlayerActionCommandName(cmd?.commandName);
    }
}
