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
        Commands = new ActionCommandData[5]; // �⺻��
        IsPerfectTiming = new bool[5];
    }

    // �˼� ���� �޼���
    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        EquippedStyle = styleData;

        // ��Ÿ�Ͽ� ���ǵ� �׼� Ŀ�ǵ� �迭�� �÷��̾� ������� ����
        if (EquippedStyle != null)
        {
            Commands = EquippedStyle.GetActionCommands();
            IsPerfectTiming = new bool[Commands.Length];
        }
    }

}