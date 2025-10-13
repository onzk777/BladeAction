using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Sequence 노드 (AND 조건)
    /// 모든 자식 조건이 true일 때만 true를 반환합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Sequence", menuName = "BT/Composite/Sequence (AND)", order = 0)]
    public class BTComposite_Sequence : BTCompositeNode
    {
        [Header("Sequence 설정")]
        [Tooltip("Short-circuit 활성화 (하나라도 false면 즉시 중단)")]
        public bool useShortCircuit = true;
        
        public override bool Evaluate(BehaviorTreeContext context)
        {
            if (!AreChildrenValid())
                return false;
            
            foreach (var child in children)
            {
                if (child == null) continue;
                
                bool childResult = child.EvaluateCondition(context);
                
                if (!childResult)
                {
                    // Short-circuit: 하나라도 false면 즉시 false 반환
                    if (useShortCircuit)
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                description = $"모든 조건이 만족되어야 함 (AND)\n자식 노드 수: {children?.Count ?? 0}";
            }
        }
    }
}

