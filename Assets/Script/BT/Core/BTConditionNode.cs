using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 조건 노드의 기본 클래스
    /// 모든 조건 노드는 이 클래스를 상속받습니다.
    /// </summary>
    public abstract class BTConditionNode : BTNode
    {
        [Header("조건 설정")]
        [Tooltip("조건 반전 (true면 결과를 반대로)")]
        public bool invertResult = false;
        
        /// <summary>
        /// 조건을 평가합니다.
        /// </summary>
        /// <param name="context">BT 실행 컨텍스트</param>
        /// <returns>조건 만족 여부</returns>
        public abstract bool Evaluate(BehaviorTreeContext context);
        
        /// <summary>
        /// 조건 평가 (invertResult 적용)
        /// </summary>
        public bool EvaluateCondition(BehaviorTreeContext context)
        {
            if (!IsValid())
                return false;
                
            bool result = Evaluate(context);
            return invertResult ? !result : result;
        }
        
        /// <summary>
        /// 노드가 유효한지 확인 (조건 노드용)
        /// </summary>
        public override bool IsValid()
        {
            return base.IsValid();
        }
    }
}

