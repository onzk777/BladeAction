using System;

/// <summary>
/// 턴 단위로 공격자/방어자 정보를 묶어 전달하기 위한 컨텍스트 객체.
/// 슬롯, 컨트롤러, 캐릭터, 입력 핸들러 등 전투 흐름에 필요한 참조를 포함한다.
/// </summary>
public class CombatTurnContext
{
    public CombatCharacterManager.CombatantSlot AttackerSlot { get; }
    public CombatCharacterManager.CombatantSlot DefenderSlot { get; }

    public ICombatController AttackerController => AttackerSlot?.Controller;
    public ICombatController DefenderController => DefenderSlot?.Controller;

    public Character AttackerCharacter => AttackerSlot?.Character;
    public Character DefenderCharacter => DefenderSlot?.Character;

    public CombatCharacterManager.CombatTeam AttackerTeam => AttackerSlot?.Team ?? CombatCharacterManager.CombatTeam.TeamA;
    public CombatCharacterManager.CombatTeam DefenderTeam => DefenderSlot?.Team ?? CombatCharacterManager.CombatTeam.TeamB;

    public AttackerInputHandler AttackerInputHandler { get; }
    public DefenderInputHandler DefenderInputHandler { get; }

    public CombatTurnContext(
        CombatCharacterManager.CombatantSlot attackerSlot,
        CombatCharacterManager.CombatantSlot defenderSlot,
        AttackerInputHandler attackerInputHandler,
        DefenderInputHandler defenderInputHandler)
    {
        AttackerSlot = attackerSlot;
        DefenderSlot = defenderSlot;
        AttackerInputHandler = attackerInputHandler;
        DefenderInputHandler = defenderInputHandler;
    }

    public bool IsValid =>
        AttackerSlot != null &&
        DefenderSlot != null &&
        AttackerCharacter != null &&
        DefenderCharacter != null;

    public override string ToString()
    {
        string attackerName = AttackerCharacter != null ? AttackerCharacter.Name : "null";
        string defenderName = DefenderCharacter != null ? DefenderCharacter.Name : "null";
        return $"[CombatTurnContext] {attackerName}({AttackerTeam}) -> {defenderName}({DefenderTeam})";
    }
}

