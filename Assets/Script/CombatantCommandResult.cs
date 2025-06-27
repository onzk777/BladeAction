// CombatantCommandResult.cs (변경된 전체)

using System;
using System.Collections.Generic;
using static CombatantCommandResult;

public class CombatantCommandResult
{
    public ActionCommandData Command { get; }
    public bool WasInterrupted { get; set; } = false;

    private List<HitResult> hitResults;
    public IReadOnlyList<HitResult> HitResults => hitResults;

    public CombatantCommandResult(ActionCommandData cmd)
    {
        Command = cmd;
        if (cmd == null) throw new ArgumentNullException(nameof(cmd)); // cmd가 null인 경우 예외 발생
        hitResults = new List<HitResult>(cmd.hitCount); // hitCount에 따라 초기화
        for (int i = 0; i < cmd.hitCount; i++) // hitCount만큼 HitResult 객체를 생성
            hitResults.Add(new HitResult()); // HitResult 객체를 리스트에 추가
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

    /// 전체 히트 횟수
    public int HitCount => hitResults.Count;

    /// 완벽 입력(성공) 히트 횟수
    public int GetPerfectHitCount()
    {
        int count = 0;
        foreach (var hit in hitResults)
            if (hit.IsPerfect) count++;
        return count;
    }
}
