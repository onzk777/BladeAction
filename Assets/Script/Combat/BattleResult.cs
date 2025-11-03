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
    
    // 🆕 보상 관련 필드
    public bool isVictory => EndReason == BattleEndReason.EnemyDefeated;
    public int goldReward { get; set; } = 50; // 기본 보상 (향후 적별로 다르게 설정)
    public int expReward { get; set; } = 100; // 기본 경험치 (향후 적별로 다르게 설정)

    public void InitializeBattle()
    {
        winner = null;
        loser = null;
        EndReason = BattleEndReason.None;
        EndTime = 0f;
        goldReward = 50; // 기본값
        expReward = 100; // 기본값
    }
}
