using UnityEngine;

/// <summary>
/// GlobalConfig 기반 기본 AI 방어 의사결정 클래스
/// </summary>
public class DefaultAIDefenseDecisionMaker : IAIDefenseDecisionMaker
{
    [Header("디버그 설정")]
    [SerializeField] private bool debugMode = true; // 디버그 모드 (기본값: On)
    
    [Header("AI 설정 오버라이드")]
    [SerializeField] private bool useCustomSettings = false; // 커스텀 설정 사용 여부
    [SerializeField] private float customDefenseSuccessRate = 0.5f; // 커스텀 방어 성공률
    [SerializeField] private float customGuardAttemptRate = 0.3f; // 커스텀 막기 시도 확률
    
    /// <summary>
    /// AI 방어 의사결정을 수행합니다
    /// </summary>
    public AIDefenseDecision MakeDefenseDecision(Projectile projectile, AIContext context)
    {
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 AI 방어 의사결정 시작 - hitIndex:{context.hitIndex}, turnTime:{context.turnElapsedTime:F2}");
        }
        
        // 🆕 AI 설정 값 결정 (커스텀 설정 또는 GlobalConfig)
        float aiDefenseSuccessRate = useCustomSettings ? customDefenseSuccessRate : GlobalConfig.Instance.NpcDefensePerfectRate;
        bool canParryWhileGuarding = GlobalConfig.Instance.NpcParryWhileGuarding;
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 AI 설정 값 - useCustom:{useCustomSettings}, 성공률:{aiDefenseSuccessRate:F2}, 막기중쳐내기:{canParryWhileGuarding}");
        }
        
        // 🆕 막기 중일 때 쳐내기 시도 허용 여부 확인
        if (context.isGuarding && !canParryWhileGuarding)
        {
            if (debugMode)
            {
                Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 막기 중이므로 쳐내기 시도 안함");
            }
            return new AIDefenseDecision(false, false, 0f);
        }
        
        // 🆕 AI 방어 시도 여부 결정
        bool willAttempt = DetermineDefenseAttempt(projectile, context, aiDefenseSuccessRate);
        
        // 🆕 AI 방어 성공 여부 결정
        bool willSucceed = DetermineDefenseSuccess(projectile, context, aiDefenseSuccessRate);
        
        // 🆕 AI 반응 시간 결정 (즉시 반응)
        float reactionTime = 0f;
        
        var decision = new AIDefenseDecision(willAttempt, willSucceed, reactionTime);
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 AI 방어 의사결정 완료 - 시도:{decision.willAttempt}, 성공:{decision.willSucceed}, 반응시간:{decision.reactionTime:F2}초");
        }
        
        return decision;
    }
    
    /// <summary>
    /// AI 방어 시도 여부를 결정합니다
    /// </summary>
    private bool DetermineDefenseAttempt(Projectile projectile, AIContext context, float successRate)
    {
        // 🆕 기본적으로는 항상 시도 (추후 AI 패턴에 따라 확장 가능)
        // TODO: AI 전투 패턴에 따른 방어 시도 여부 결정 로직 추가
        
        // 🆕 중단 상태에서는 방어 시도하지 않음
        if (context.isInterrupted)
        {
            if (debugMode)
            {
                Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 중단 상태로 인해 방어 시도 안함");
            }
            return false;
        }
        
        
        return true; // 기본적으로 항상 시도
    }
    
    /// <summary>
    /// AI 방어 성공 여부를 결정합니다
    /// </summary>
    private bool DetermineDefenseSuccess(Projectile projectile, AIContext context, float successRate)
    {
        // 🆕 기본 확률 기반 성공 여부 결정
        float randomValue = Random.value;
        bool willSucceed = randomValue < successRate;
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 방어 성공 판정 - 성공률:{successRate:F2}, 랜덤값:{randomValue:F2}, 결과:{willSucceed}");
        }
        
        return willSucceed;
    }
    
    
    /// <summary>
    /// 디버그 모드 설정
    /// </summary>
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 디버그 모드: {(enabled ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// AI 막기 의사결정을 수행합니다
    /// </summary>
    public bool MakeGuardDecision(AIContext context)
    {
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 AI 막기 의사결정 시작 - turnTime:{context.turnElapsedTime:F2}");
        }
        
        // 🆕 AI 설정 값 결정 (커스텀 설정 또는 GlobalConfig)
        float aiGuardAttemptRate = useCustomSettings ? customGuardAttemptRate : GlobalConfig.Instance.NpcGuardAttemptRate;
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 AI 막기 설정 값 - useCustom:{useCustomSettings}, 막기확률:{aiGuardAttemptRate:F2}");
        }
        
        // 🆕 중단 상태에서는 막기 시도하지 않음
        if (context.isInterrupted)
        {
            if (debugMode)
            {
                Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 중단 상태로 인해 막기 시도 안함");
            }
            return false;
        }
        
        // 🆕 확률 기반 막기 시도 여부 결정
        float randomValue = Random.value;
        bool willGuard = randomValue < aiGuardAttemptRate;
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 막기 시도 판정 - 확률:{aiGuardAttemptRate:F2}, 랜덤값:{randomValue:F2}, 결과:{willGuard}");
        }
        
        return willGuard;
    }
    
    /// <summary>
    /// 커스텀 설정 사용 여부 설정
    /// </summary>
    public void SetCustomSettings(bool useCustom, float successRate = 0.5f, float guardRate = 0.3f)
    {
        useCustomSettings = useCustom;
        customDefenseSuccessRate = successRate;
        customGuardAttemptRate = guardRate;
        
        if (debugMode)
        {
            Debug.Log($"[DefaultAIDefenseDecisionMaker] 🆕 커스텀 설정: {(useCustom ? "사용" : "미사용")}, 성공률:{successRate:F2}, 막기확률:{guardRate:F2}");
        }
    }
}
