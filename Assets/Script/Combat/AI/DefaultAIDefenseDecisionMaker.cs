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
    /// 
    /// 우선순위:
    /// 1. EnemyCombatant.runtimeProbabilities (BT 적용 후)
    /// 2. GlobalConfig (폴백)
    /// </summary>
    public AIDefenseDecision MakeDefenseDecision(Projectile projectile, AIContext context)
    {
        // 쳐내기 성공률 가져오기 (runtimeProbabilities 우선)
        float aiDefenseSuccessRate = GetParrySuccessRate(context);
        bool canParryWhileGuarding = GetParryWhileGuarding(context);
        
        // 막기 중일 때 쳐내기 시도 허용 여부 확인
        if (context.isGuarding && !canParryWhileGuarding)
        {
            Debug.Log($"[AIDefense] 막기 중 - 쳐내기 불가");
            return new AIDefenseDecision(false, false, 0f);
        }
        
        // AI 방어 시도 여부 결정
        bool willAttempt = DetermineDefenseAttempt(projectile, context, aiDefenseSuccessRate);
        
        // AI 방어 성공 여부 결정
        bool willSucceed = DetermineDefenseSuccess(projectile, context, aiDefenseSuccessRate);
        
        // AI 반응 시간 결정 (즉시 반응)
        float reactionTime = 0f;
        
        var decision = new AIDefenseDecision(willAttempt, willSucceed, reactionTime);
        
        Debug.Log($"[AIDefense] 쳐내기 판정: 확률 {aiDefenseSuccessRate:P0} → {(willSucceed ? "성공" : "실패")}");
        
        return decision;
    }
    
    /// <summary>
    /// 쳐내기 성공률을 가져옵니다 (우선순위: runtimeProbabilities > GlobalConfig)
    /// </summary>
    private float GetParrySuccessRate(AIContext context)
    {
        // 1순위: EnemyCombatant.runtimeProbabilities (BT 적용 후!)
        if (context.defenderCombatant is EnemyCombatant enemyCombatant)
        {
            if (enemyCombatant.RuntimeProbabilities != null)
            {
                float rate = enemyCombatant.RuntimeProbabilities.ParryPerfectRate;
                return rate;
            }
        }
        
        // 2순위: GlobalConfig (폴백)
        return useCustomSettings ? customDefenseSuccessRate : GlobalConfig.Instance.NpcParryPerfectRate;
    }
    
    /// <summary>
    /// 막기 중 쳐내기 여부를 가져옵니다
    /// </summary>
    private bool GetParryWhileGuarding(AIContext context)
    {
        // 1순위: EnemyCombatant.runtimeProbabilities (BT 적용 후!)
        if (context.defenderCombatant is EnemyCombatant enemyCombatant)
        {
            if (enemyCombatant.RuntimeProbabilities != null)
            {
                return enemyCombatant.RuntimeProbabilities.ParryWhileGuarding;
            }
        }
        
        // 2순위: GlobalConfig (폴백)
        return GlobalConfig.Instance.NpcParryWhileGuarding;
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
    /// 
    /// 우선순위:
    /// 1. EnemyCombatant.runtimeProbabilities (BT 적용 후)
    /// 2. CharacterData.npcBehavior (원본)
    /// 3. GlobalConfig (폴백)
    /// </summary>
    public bool MakeGuardDecision(AIContext context)
    {
        // 중단 상태에서는 막기 시도하지 않음
        if (context.isInterrupted)
        {
            Debug.Log($"[AIDefense] 중단 상태 - 막기 불가");
            return false;
        }
        
        // 막기 시도 확률 결정 (우선순위 순서)
        float aiGuardAttemptRate = GetGuardAttemptRate(context);
        
        // 확률 기반 막기 시도 여부 결정
        float randomValue = Random.value;
        bool willGuard = randomValue < aiGuardAttemptRate;
        
        Debug.Log($"[AIDefense] 막기 판정: 확률 {aiGuardAttemptRate:P0}, 랜덤 {randomValue:F2} → {(willGuard ? "시도" : "무시")}");
        
        return willGuard;
    }
    
    /// <summary>
    /// 막기 시도 확률을 가져옵니다 (우선순위: runtimeProbabilities > npcBehavior > GlobalConfig)
    /// </summary>
    private float GetGuardAttemptRate(AIContext context)
    {
        // 1순위: EnemyCombatant.runtimeProbabilities (BT 적용 후!) ⭐
        if (context.defenderCombatant is EnemyCombatant enemyCombatant)
        {
            if (enemyCombatant.RuntimeProbabilities != null)
            {
                float rate = enemyCombatant.RuntimeProbabilities.GuardAttemptRate;
                Debug.Log($"[AIDefense] ✅ RuntimeProbabilities 사용 (BT 적용): {rate:P0}");
                return rate;
            }
            else
            {
                Debug.LogWarning($"[AIDefense] RuntimeProbabilities가 null - 폴백 사용");
            }
        }
        
        // 2순위: 커스텀 설정
        if (useCustomSettings)
        {
            Debug.Log($"[AIDefense] 커스텀 설정 사용: {customGuardAttemptRate:P0}");
            return customGuardAttemptRate;
        }
        
        // 3순위: GlobalConfig (최종 폴백)
        Debug.Log($"[AIDefense] GlobalConfig 사용 (폴백): {GlobalConfig.Instance.NpcGuardAttemptRate:P0}");
        return GlobalConfig.Instance.NpcGuardAttemptRate;
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
