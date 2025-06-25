using System.Collections.Generic;
using UnityEngine;

public class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }

    // ��Ÿ�� �����ͷκ��� ������ Ŀ�ǵ� ���
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
            // ��Ÿ�Ͽ� ������ �׼� Ŀ�ǵ带 ����Ʈ�� ����
            AvailableCommands = new List<ActionCommandData>(EquippedStyle.GetActionCommands());
        }
    }
}