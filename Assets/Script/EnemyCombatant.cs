using NUnit.Framework;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    public EnemyCombatant(string name) : base(name)
    {
    }

    // �߰������� AI �ൿ �����̳� ���̵� �Ӽ��� �ο��� �� ����
    // public float AggressionLevel; // ����
    public float PerfectStartSeconds;
    public float PerfectDurationSeconds;
}
