using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 턴 수 조건 노드
    /// 현재 턴 번호를 비교합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TurnCount", menuName = "BT/Conditions/Turn Count", order = 3)]
    public class BTCondition_TurnCount : BTConditionNode
    {
        [Header("턴 수 비교 설정")]
        [Tooltip("비교 연산자")]
        public ComparisonOperator comparisonOperator = ComparisonOperator.Greater;
        
        [Tooltip("비교할 턴 수")]
        [Min(0)]
        public int turnCount = 1;
        
        public enum ComparisonOperator
        {
            [Tooltip("> (초과)")]
            Greater,
            [Tooltip("< (미만)")]
            Less,
            [Tooltip(">= (이상)")]
            GreaterOrEqual,
            [Tooltip("<= (이하)")]
            LessOrEqual,
            [Tooltip("== (같음)")]
            Equal,
            [Tooltip("!= (다름)")]
            NotEqual
        }
        
        public override bool Evaluate(BehaviorTreeContext context)
        {
            if (context == null)
                return false;
            
            int currentTurn = context.currentTurn;
            return CompareValues(currentTurn, turnCount, comparisonOperator);
        }
        
        private bool CompareValues(int value, int threshold, ComparisonOperator op)
        {
            switch (op)
            {
                case ComparisonOperator.Greater:
                    return value > threshold;
                case ComparisonOperator.Less:
                    return value < threshold;
                case ComparisonOperator.GreaterOrEqual:
                    return value >= threshold;
                case ComparisonOperator.LessOrEqual:
                    return value <= threshold;
                case ComparisonOperator.Equal:
                    return value == threshold;
                case ComparisonOperator.NotEqual:
                    return value != threshold;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                string operatorName = GetOperatorDisplayName(comparisonOperator);
                description = $"현재 턴 수 {operatorName} {turnCount}";
            }
        }
        
        private string GetOperatorDisplayName(ComparisonOperator op)
        {
            switch (op)
            {
                case ComparisonOperator.Greater: return ">";
                case ComparisonOperator.Less: return "<";
                case ComparisonOperator.GreaterOrEqual: return ">=";
                case ComparisonOperator.LessOrEqual: return "<=";
                case ComparisonOperator.Equal: return "==";
                case ComparisonOperator.NotEqual: return "!=";
                default: return "?";
            }
        }
    }
}

