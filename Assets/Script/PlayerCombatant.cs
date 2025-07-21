using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatant : Combatant
{
    public int selectedIndex = 0; // �ν����Ϳ��� �����ϴ� �׽�Ʈ�� �ε���
    public bool useTestMode;  // true�� �׽�Ʈ ���� ����

    private PlayerController controller; // PlayerController �ν��Ͻ� ����
    public PlayerCombatant(string name, PlayerController controller) : base(name)
    {
        this.controller = controller;
    }
    public override CommandSelection ChooseCommand()
    {
        int idx = Mathf.Clamp(controller.GetSelectedCommandIndex(), 0, AvailableCommands.Count - 1);
        return new CommandSelection { selectedIndex = idx };
    }
}
