using UnityEngine;
public class Combatant
{
    public string Name;
    public ActionCommand[] Commands = new ActionCommand[5];
    public bool[] IsPerfectTiming = new bool[5]; // 커맨드별 타이밍 판정 결과

    public Combatant(string name)
    {
        Name = name;
    }
}