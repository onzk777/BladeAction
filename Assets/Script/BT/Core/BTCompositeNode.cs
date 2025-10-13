using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 복합 노드의 기본 클래스
    /// 여러 조건을 조합하는 노드입니다.
    /// </summary>
    public abstract class BTCompositeNode : BTConditionNode
    {
        [Header("자식 노드")]
        [Tooltip("자식 조건 노드들")]
        public List<BTConditionNode> children = new List<BTConditionNode>();
        
        /// <summary>
        /// 자식 노드가 유효한지 확인
        /// </summary>
        protected bool AreChildrenValid()
        {
            if (children == null || children.Count == 0)
                return false;
                
            foreach (var child in children)
            {
                if (child == null)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 노드가 유효한지 확인 (복합 노드용)
        /// </summary>
        public override bool IsValid()
        {
            return base.IsValid() && AreChildrenValid();
        }
    }
}

