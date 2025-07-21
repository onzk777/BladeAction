using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class EnemyCombatant : Combatant
{
    private EnemyController controller; // EnemyController ����
    public EnemyCombatant(string name, EnemyController controller) : base("???")
    {
        this.controller = controller;
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
