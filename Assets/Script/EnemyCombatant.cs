using NUnit.Framework;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    public EnemyCombatant(string name) : base(name)
    {
    }

    // 추가적으로 AI 행동 성향이나 난이도 속성을 부여할 수 있음
    // public float AggressionLevel; // 예시
    public float PerfectStartSeconds;
    public float PerfectDurationSeconds;
}
