using UnityEngine;

/// <summary>
/// AI 방어 의사결정을 담당하는 인터페이스
/// </summary>
public interface IAIDefenseDecisionMaker
{
    /// <summary>
    /// AI 방어 의사결정을 수행합니다
    /// </summary>
    /// <param name="projectile">충돌한 발사체</param>
    /// <param name="context">AI 컨텍스트 정보</param>
    /// <returns>AI 방어 의사결정 결과</returns>
    AIDefenseDecision MakeDefenseDecision(Projectile projectile, AIContext context);
    
    /// <summary>
    /// AI 막기 의사결정을 수행합니다
    /// </summary>
    /// <param name="context">AI 컨텍스트 정보</param>
    /// <returns>막기 시도 여부</returns>
    bool MakeGuardDecision(AIContext context);
}

/// <summary>
/// AI 방어 의사결정 결과
/// </summary>
[System.Serializable]
public struct AIDefenseDecision
{
    public bool willAttempt;    // 방어 입력을 시도할 것인가
    public bool willSucceed;    // 방어 입력이 성공할 것인가
    public float reactionTime;  // 반응 시간 (초)
    
    public AIDefenseDecision(bool willAttempt, bool willSucceed, float reactionTime)
    {
        this.willAttempt = willAttempt;
        this.willSucceed = willSucceed;
        this.reactionTime = reactionTime;
    }
}

/// <summary>
/// AI 의사결정에 필요한 컨텍스트 정보
/// </summary>
[System.Serializable]
public struct AIContext 
{
    public int hitIndex;                    // 현재 히트 인덱스
    public float turnElapsedTime;           // 턴 경과 시간
    public bool isPlayerAttacker;           // 플레이어가 공격자인지 여부
    public int totalHitCount;               // 총 히트 수
    public float posturePoints;             // 현재 자세 포인트
    public bool isInterrupted;              // 중단 상태인지 여부
    public bool isGuarding;                 // 현재 막기 상태인지 여부
    
    public AIContext(int hitIndex, float turnElapsedTime, bool isPlayerAttacker, 
                     int totalHitCount, float posturePoints, bool isInterrupted, bool isGuarding = false)
    {
        this.hitIndex = hitIndex;
        this.turnElapsedTime = turnElapsedTime;
        this.isPlayerAttacker = isPlayerAttacker;
        this.totalHitCount = totalHitCount;
        this.posturePoints = posturePoints;
        this.isInterrupted = isInterrupted;
        this.isGuarding = isGuarding;
    }
}
