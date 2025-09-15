using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    private EnemyController controller; // EnemyController 참조
    public EnemyCombatant(CharacterData data, EnemyController controller) : base(data)
    {
        this.controller = controller;
    }

    public void SetController(EnemyController newController)
    {
        controller = newController;
    }

    public override CommandSelection ChooseCommand()
    {
        //일단은 적은 무작위로 선택하도록
        int idx = Random.Range(0, AvailableCommands.Count);
        return new CommandSelection { selectedIndex = idx };
    }
}
