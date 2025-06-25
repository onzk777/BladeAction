// CombatantCommandResult.cs (변경된 전체)

using System;
using System.Collections.Generic;

public class CombatantCommandResult
{
    public ActionCommandData Command { get; }
    public bool WasInterrupted { get; set; } = false;

    private List<HitResult> hitResults;
    public IReadOnlyList<HitResult> HitResults => hitResults;

    public CombatantCommandResult(ActionCommandData cmd)
    {
        Command = cmd;
        hitResults = new List<HitResult>(cmd.hitCount);
        for (int i = 0; i < cmd.hitCount; i++)
            hitResults.Add(new HitResult());
    }

    /// <summary>
    /// 히트 인덱스에 대한 퍼펙트 성공 여부를 설정합니다.
    /// </summary>
    public void SetHitResult(int index, bool isPerfect)
    {
        if (index < 0 || index >= hitResults.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        hitResults[index].IsPerfect = isPerfect;
    }

    public class HitResult
    {
        public bool IsPerfect { get; set; } = false;
    }
}
