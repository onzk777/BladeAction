using UnityEngine;
public class CombatantCommandResult
{
    public ActionCommandData command;     // 어떤 커맨드였는가
    public HitResult[] hitResults;        // 각 히트의 결과 배열
    public bool wasInterrupted = false; // 이번 액션이 중단되었는가

    public CombatantCommandResult(ActionCommandData command)
    {
        this.command = command;
        int count = Mathf.Max(1, command.hitCount);  // 최소 1 이상 보장
        hitResults = new HitResult[count];
        for (int i = 0; i < hitResults.Length; i++)
        {
            hitResults[i] = new HitResult();
        }
    }
}
