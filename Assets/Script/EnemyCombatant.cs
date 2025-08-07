using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class EnemyCombatant : Combatant
{
    private EnemyController controller; // EnemyController 참조
    public EnemyCombatant(string name, EnemyController controller) : base("???")
    {
        this.controller = controller;
    }

    public void Init(string name)
    {
        Name = name;  // Name 프로퍼티가 setter를 허용해야 합니다.
    }


    public override CommandSelection ChooseCommand()
    {
        //일단은 적은 무작위로 선택하도록
        int idx = Random.Range(0, AvailableCommands.Count);
        return new CommandSelection { selectedIndex = idx };
    }

}
