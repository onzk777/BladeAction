using NUnit.Framework;
using UnityEngine;

public class PlayerCombatant : Combatant
{
    


    public PlayerCombatant(string name) : base(name)
    {
    }

    public float PerfectStartSeconds;
    public float PerfectDurationSeconds;
}
