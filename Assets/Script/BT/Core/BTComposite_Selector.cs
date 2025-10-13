using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Selector 노드 (OR 조건)
    /// 자식 조건 중 하나라도 true이면 true를 반환합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Selector", menuName = "BT/Composite/Selector (OR)", order = 1)]
    public class BTComposite_Selector : BTCompositeNode
    {
        [Header("Selector 설정")]
        [Tooltip("Short-circuit 활성화 (하나라도 true면 즉시 중단)")]
        public bool useShortCircuit = true;
        
        public override bool Evaluate(BehaviorTreeContext context)
        {
            if (!AreChildrenValid())
                return false;
            
            foreach (var child in children)
            {
                if (child == null) continue;
                
                bool childResult = child.EvaluateCondition(context);
                
                if (childResult)
                {
                    // Short-circuit: 하나라도 true면 즉시 true 반환
                    if (useShortCircuit)
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                description = $"하나라도 조건이 만족되면 됨 (OR)\n자식 노드 수: {children?.Count ?? 0}";
            }
        }
    }
}

