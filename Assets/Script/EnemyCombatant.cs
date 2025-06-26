using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    public EnemyCombatant(string name) : base("???")
    {

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
