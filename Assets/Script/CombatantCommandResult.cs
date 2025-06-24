using UnityEngine;

public class CombatantCommandResult
{
    public ActionCommandData command;      // 어떤 커맨드였는가
    public HitResult[] hitResults;         // 각 히트의 결과 배열
    public bool wasInterrupted = false;    // 이번 액션이 중단되었는가
    public CombatantCommandResult(ActionCommandData command)
    {
        this.command = command;
        int count = command.hitCount;
        hitResults = (count > 0) ? new HitResult[count] : new HitResult[0]; // 0일 경우 빈 배열
        for (int i = 0; i < hitResults.Length; i++)
        {
            hitResults[i] = new HitResult();  // ← 이게 누락되면 해당 인스턴스가 null임!
        }
    }
}
