using System.Collections.Generic;
using UnityEngine;
public class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }

    public ActionCommandData[] Commands { get; protected set; }
    public List<CombatantCommandResult> SelectedCommandResults { get; set; } = new List<CombatantCommandResult>();
    public bool[] IsPerfectTiming { get; set; }

    public Combatant(string name)
    {
        Name = name;
        Commands = new ActionCommandData[5]; // 기본값
        IsPerfectTiming = new bool[5];
    }

    // 검술 장착 메서드
    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        EquippedStyle = styleData;

        // 스타일에 정의된 액션 커맨드 배열을 플레이어 명령으로 설정
        if (EquippedStyle != null)
        {
            Commands = EquippedStyle.GetActionCommands();
            IsPerfectTiming = new bool[Commands.Length];
        }
    }

}