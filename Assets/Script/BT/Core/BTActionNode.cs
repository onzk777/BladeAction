using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 액션 노드의 기본 클래스
    /// 모든 액션 노드는 이 클래스를 상속받습니다.
    /// </summary>
    public abstract class BTActionNode : BTNode
    {
        [Header("우선순위 설정")]
        [Tooltip("우선순위 (높을수록 우선, 음수 불가)")]
        [Min(0)]
        public int priority = 0;
        
        [Tooltip("이 전투에서 한 번만 실행할지 여부")]
        public bool executeOncePerCombat = false;
        
        [System.NonSerialized]
        private bool hasExecutedThisCombat = false;
        
        /// <summary>
        /// 액션을 실행합니다.
        /// </summary>
        /// <param name="context">BT 실행 컨텍스트</param>
        public abstract void Execute(BehaviorTreeContext context);
        
        /// <summary>
        /// 액션 실행 (executeOncePerCombat 처리 포함)
        /// </summary>
        public void ExecuteAction(BehaviorTreeContext context)
        {
            if (!IsValid())
                return;
                
            if (executeOncePerCombat && hasExecutedThisCombat)
                return;
                
            Execute(context);
            
            if (executeOncePerCombat)
                hasExecutedThisCombat = true;
        }
        
        /// <summary>
        /// 전투 실행 상태 리셋 (새 전투 시작 시 호출)
        /// </summary>
        public void ResetCombatExecution()
        {
            hasExecutedThisCombat = false;
        }
        
        /// <summary>
        /// 노드가 유효한지 확인 (액션 노드용)
        /// </summary>
        public override bool IsValid()
        {
            return base.IsValid() && (!executeOncePerCombat || !hasExecutedThisCombat);
        }
    }
}

