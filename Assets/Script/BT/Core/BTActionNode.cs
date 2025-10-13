using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 액션 노드의 기본 클래스
    /// 모든 액션 노드는 이 클래스를 상속받습니다.
    /// 
    /// 중요:
    /// - executeOncePerCombat 상태는 BT 자체가 아닌 Blackboard에 저장됨
    /// - 같은 BT를 여러 NPC가 공유해도 각자 독립적으로 실행 가능
    /// </summary>
    public abstract class BTActionNode : BTNode
    {
        [Header("우선순위 설정")]
        [Tooltip("우선순위 (높을수록 우선, 음수 불가)")]
        [Min(0)]
        public int priority = 0;
        
        [Tooltip("이 전투에서 한 번만 실행할지 여부")]
        public bool executeOncePerCombat = false;
        
        // ========================================
        // 상태 제거: Blackboard로 이동
        // ========================================
        // [System.NonSerialized]
        // private bool hasExecutedThisCombat = false;  ← 제거!
        
        /// <summary>
        /// 액션을 실행합니다.
        /// </summary>
        /// <param name="context">BT 실행 컨텍스트</param>
        public abstract void Execute(BehaviorTreeContext context);
        
        /// <summary>
        /// 액션 실행 (executeOncePerCombat 처리 포함)
        /// 
        /// 작동 방식:
        /// - executeOncePerCombat = true일 때
        /// - Blackboard에서 이 액션의 실행 여부를 확인
        /// - 이미 실행되었으면 건너뜀
        /// - 실행 후 Blackboard에 기록
        /// </summary>
        public void ExecuteAction(BehaviorTreeContext context)
        {
            if (!IsValid())
                return;
            
            // Blackboard 확인
            if (context.blackboard == null)
            {
                Debug.LogWarning($"[BTActionNode] Blackboard가 null - {name} 실행 불가");
                return;
            }
            
            // executeOncePerCombat 체크 (Blackboard 사용)
            string actionKey = GetActionKey();
            if (executeOncePerCombat && context.blackboard.HasExecuted(actionKey))
            {
                Debug.Log($"[BTActionNode] '{name}' 이미 실행됨 - 건너뜀 (executeOncePerCombat)");
                return;
            }
            
            // 액션 실행
            Execute(context);
            
            // Blackboard에 기록
            if (executeOncePerCombat)
            {
                context.blackboard.MarkAsExecuted(actionKey);
            }
        }
        
        /// <summary>
        /// 액션 식별 키 생성
        /// </summary>
        private string GetActionKey()
        {
            // ScriptableObject의 이름 + 인스턴스 ID 사용
            // 같은 이름의 액션이 여러 개 있어도 구분 가능
            return $"{GetType().Name}_{name}";
        }
        
        /// <summary>
        /// 전투 실행 상태 리셋 (새 전투 시작 시 호출)
        /// 
        /// 참고:
        /// - 블랙보드 패턴으로 변경되어 이 메서드는 사용하지 않음
        /// - Blackboard.ResetCombat()을 대신 호출
        /// - 하위 호환성을 위해 유지 (아무것도 하지 않음)
        /// </summary>
        public void ResetCombatExecution()
        {
            // 블랙보드 패턴으로 변경되어 여기서는 아무것도 하지 않음
            // Blackboard.ResetCombat()이 대신 호출됨
        }
        
        /// <summary>
        /// 노드가 유효한지 확인 (액션 노드용)
        /// 
        /// 참고:
        /// - executeOncePerCombat 체크는 ExecuteAction()에서 Blackboard로 처리
        /// - 여기서는 기본 유효성만 체크
        /// </summary>
        public override bool IsValid()
        {
            return base.IsValid();
        }
    }
}

