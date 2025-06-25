using System.Collections.Generic;
using UnityEngine;

public class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }

    // 스타일 데이터로부터 가져온 커맨드 목록
    public List<ActionCommandData> AvailableCommands { get; protected set; } = new List<ActionCommandData>();

    public Combatant(string name)
    {
        Name = name;
    }

    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        EquippedStyle = styleData;
        if (EquippedStyle != null)
        {
            // 스타일에 설정된 액션 커맨드를 리스트로 복사
            AvailableCommands = new List<ActionCommandData>(EquippedStyle.GetActionCommands());
        }
    }
}