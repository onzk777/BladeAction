using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    public EnemyCombatant(string name) : base("???")
    {

    }

    public void Init(string name)
    {
        Name = name;  // Name ������Ƽ�� setter�� ����ؾ� �մϴ�.
    }


    public override CommandSelection ChooseCommand()
    {
        //�ϴ��� ���� �������� �����ϵ���
        int idx = Random.Range(0, AvailableCommands.Count);
        return new CommandSelection { selectedIndex = idx };
    }

}
