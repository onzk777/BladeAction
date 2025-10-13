using UnityEngine;

/// <summary>
/// NPC의 런타임 행동 확률 관리 클래스
/// 
/// 역할:
/// - CharacterData의 원본 확률을 복사하여 런타임에서 안전하게 수정
/// - BT의 확률 Override 결과를 적용
/// - 턴 종료 시 원본 확률로 복원
/// 
/// 왜 필요한가?
/// - CharacterData는 ScriptableObject이므로 직접 수정하면 에셋이 영구적으로 변경됨
/// - 런타임 복사본을 만들어서 BT 효과를 임시로 적용하고, 턴마다 리셋 가능
/// 
/// 중요: 이 클래스는 확률 데이터만 관리합니다!
/// - 강제 행동(forcedBehavior)은 BehaviorTreeContext에서 별도로 관리
/// - 이 클래스는 "공격 성공률을 80%로 높여라" 같은 확률 조정만 담당
/// </summary>
public class NPCRuntimeProbabilities
{
    // ========================================
    // 필드 (Fields)
    // ========================================
    
    /// <summary>
    /// 원본 확률 (CharacterData에서 복사, 절대 수정하지 않음)
    /// </summary>
    private NPCBehaviorProbabilities original;
    
    /// <summary>
    /// 현재 확률 (BT Override가 적용된 상태, 매 턴마다 수정됨)
    /// </summary>
    private NPCBehaviorProbabilities current;
    
    
    // ========================================
    // 생성자 (Constructor)
    // ========================================
    
    /// <summary>
    /// NPCRuntimeProbabilities를 생성합니다.
    /// 원본 확률을 복사하여 current에 저장합니다.
    /// </summary>
    /// <param name="originalProbabilities">CharacterData의 원본 확률</param>
    public NPCRuntimeProbabilities(NPCBehaviorProbabilities originalProbabilities)
    {
        // 원본 확률 저장 (참조만 저장, 수정하지 않음)
        this.original = originalProbabilities;
        
        // 현재 확률을 원본으로 초기화 (복사본 생성)
        this.current = CopyProbabilities(originalProbabilities);
        
        Debug.Log($"[NPCRuntimeProbabilities] 생성됨 - 원본 확률 복사 완료");
    }
    
    
    // ========================================
    // Public 프로퍼티 (Properties)
    // ========================================
    
    /// <summary>
    /// 현재 공격 성공률 (BT Override 적용 후)
    /// </summary>
    public float AttackPerfectRate => current.attackPerfectRate;
    
    /// <summary>
    /// 현재 쳐내기 성공률 (BT Override 적용 후)
    /// </summary>
    public float ParryPerfectRate => current.parryPerfectRate;
    
    /// <summary>
    /// 현재 막기 시도 확률 (BT Override 적용 후)
    /// </summary>
    public float GuardAttemptRate => current.guardAttemptRate;
    
    /// <summary>
    /// 막기 중 쳐내기 시도 여부 (BT Override 적용 후)
    /// </summary>
    public bool ParryWhileGuarding => current.parryWhileGuarding;
    
    /// <summary>
    /// 막기 중 쳐내기 성공률 (BT Override 적용 후)
    /// </summary>
    public float ParryWhileGuardingRate => current.parryWhileGuardingRate;
    
    
    // ========================================
    // Public 메서드 (Methods)
    // ========================================
    
    /// <summary>
    /// BT의 확률 Override를 적용합니다.
    /// Dictionary의 키-값 쌍을 current 확률에 반영합니다.
    /// 
    /// 작동 원리:
    /// 1. Dictionary를 순회하면서 각 확률 타입별로 처리
    /// 2. 키 이름(예: "AttackPerfectRate")을 보고 어떤 확률인지 판단
    /// 3. 해당 확률을 새로운 값으로 설정
    /// </summary>
    /// <param name="overrides">BT에서 생성한 확률 Override Dictionary</param>
    public void ApplyOverrides(System.Collections.Generic.Dictionary<string, float> overrides)
    {
        if (overrides == null || overrides.Count == 0)
        {
            Debug.Log("[NPCRuntimeProbabilities] Override 없음 - 원본 확률 유지");
            return;
        }
        
        Debug.Log($"[NPCRuntimeProbabilities] Override 적용 시작 - {overrides.Count}개 항목");
        
        foreach (var kvp in overrides)
        {
            string key = kvp.Key;
            float value = kvp.Value;
            
            // 키 이름을 보고 어떤 확률을 수정할지 판단
            switch (key)
            {
                case "AttackPerfectRate":
                    float oldAttack = current.attackPerfectRate;
                    current.attackPerfectRate = Mathf.Clamp01(value); // 0~1 범위로 제한
                    Debug.Log($"  - 공격 성공률: {oldAttack:F2} → {current.attackPerfectRate:F2}");
                    break;
                    
                case "ParryPerfectRate":
                    float oldParry = current.parryPerfectRate;
                    current.parryPerfectRate = Mathf.Clamp01(value);
                    Debug.Log($"  - 쳐내기 성공률: {oldParry:F2} → {current.parryPerfectRate:F2}");
                    break;
                    
                case "GuardAttemptRate":
                    float oldGuard = current.guardAttemptRate;
                    current.guardAttemptRate = Mathf.Clamp01(value);
                    Debug.Log($"  - 막기 시도율: {oldGuard:F2} → {current.guardAttemptRate:F2}");
                    break;
                    
                case "DoParryWhileGuarding":
                    // bool 필드: float 값을 bool로 변환 (0.5 이상이면 true)
                    bool oldParryWhileGuarding = current.parryWhileGuarding;
                    current.parryWhileGuarding = value >= 0.5f;
                    Debug.Log($"  - 막기 중 쳐내기 시도: {oldParryWhileGuarding} → {current.parryWhileGuarding} (입력: {value:F2})");
                    break;
                    
                case "ParryWhileGuardingRate":
                    float oldParryGuard = current.parryWhileGuardingRate;
                    current.parryWhileGuardingRate = Mathf.Clamp01(value);
                    Debug.Log($"  - 막기중 쳐내기 성공률: {oldParryGuard:F2} → {current.parryWhileGuardingRate:F2}");
                    break;
                    
                default:
                    Debug.LogWarning($"[NPCRuntimeProbabilities] 알 수 없는 확률 키: {key}");
                    break;
            }
        }
        
        Debug.Log("[NPCRuntimeProbabilities] Override 적용 완료");
    }
    
    /// <summary>
    /// 현재 확률을 원본으로 복원합니다.
    /// 턴 종료 시 호출하여 BT 효과를 제거합니다.
    /// 
    /// 작동 원리:
    /// - current를 original의 복사본으로 다시 교체
    /// - 이전 턴의 모든 BT 효과가 사라짐
    /// </summary>
    public void ResetToOriginal()
    {
        Debug.Log("[NPCRuntimeProbabilities] 확률 리셋 시작");
        
        // 원본을 복사하여 current를 새로 생성
        current = CopyProbabilities(original);
        
        Debug.Log($"[NPCRuntimeProbabilities] 확률 리셋 완료 - 원본 확률로 복원");
        Debug.Log($"  - 공격 성공률: {current.attackPerfectRate:F2}");
        Debug.Log($"  - 쳐내기 성공률: {current.parryPerfectRate:F2}");
        Debug.Log($"  - 막기 시도율: {current.guardAttemptRate:F2}");
        Debug.Log($"  - 막기 중 쳐내기 시도: {current.parryWhileGuarding}");
    }
    
    
    // ========================================
    // Private 헬퍼 메서드 (Helper Methods)
    // ========================================
    
    /// <summary>
    /// NPCBehaviorProbabilities를 깊은 복사(Deep Copy)합니다.
    /// 
    /// 왜 필요한가?
    /// - C#에서 클래스는 "참조 타입"입니다
    /// - current = original; 이렇게 하면 같은 객체를 가리키게 됨 (얕은 복사)
    /// - current를 수정하면 original도 같이 변경됨 (위험!)
    /// - 깊은 복사: 새로운 객체를 만들어서 값만 복사 (안전!)
    /// </summary>
    /// <param name="source">복사할 원본 확률</param>
    /// <returns>새로 생성된 복사본</returns>
    private NPCBehaviorProbabilities CopyProbabilities(NPCBehaviorProbabilities source)
    {
        // 새 객체 생성 후 값만 복사
        return new NPCBehaviorProbabilities
        {
            attackPerfectRate = source.attackPerfectRate,
            parryPerfectRate = source.parryPerfectRate,
            guardAttemptRate = source.guardAttemptRate,
            parryWhileGuarding = source.parryWhileGuarding,
            parryWhileGuardingRate = source.parryWhileGuardingRate
        };
    }
}

