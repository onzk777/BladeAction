using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatant : Combatant
{
    public int selectedIndex = 0; // 인스펙터에서 지정하는 테스트용 인덱스
    public bool useTestMode;  // true면 테스트 모드로 동작

    private PlayerController controller; // PlayerController 인스턴스 참조
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
