using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private SwordArtStyleData equippedStyle;
    private PlayerCombatant combatant;

    private int currentCommandIndex = 0;

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
    public ActionCommandData GetCommandByIndex(int turnIndex)
    {
        if (CommandCount == 0) return null;
        var list = combatant.AvailableCommands;
        return list[turnIndex % list.Count];
    }

    private void UpdateCommandDisplay()
    {
        var cmd = GetCommandByIndex(currentCommandIndex);
        CombatStatusDisplay.Instance?.SetPlayerActionCommandName(cmd?.commandName);
    }
}
