using UnityEngine;
public class Combatant
{
    public string Name;
    public ActionCommand[] Commands = new ActionCommand[5];

    public Combatant(string name)
    {
        Name = name;
    }
}