using UnityEngine;

public class BattleResult
{
    public enum BattleEndReason
    {
        None,
        HPZero,
        PlayerDefeated,
        EnemyDefeated,
        Timeout,
        ManualEnd
    }

    public Character winner { get; set; }
    public Character loser { get; set; }
    public BattleEndReason EndReason { get; set; }
    public float EndTime { get; set; }

    public void InitializeBattle()
    {
        winner = null;
        loser = null;
        EndReason = BattleEndReason.None;
        EndTime = 0f;
    }
}
