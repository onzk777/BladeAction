using UnityEngine;
public class Combatant
{
    public string Name;
    public ActionCommand[] Commands = new ActionCommand[5];
    public bool[] IsPerfectTiming = new bool[5]; // Ŀ�ǵ庰 Ÿ�̹� ���� ���

    public Combatant(string name)
    {
        Name = name;
    }
}