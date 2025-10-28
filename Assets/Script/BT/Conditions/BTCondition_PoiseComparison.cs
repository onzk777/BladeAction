using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 자세(Poise) 비교 조건 노드
    /// NPC나 플레이어의 자세 포인트를 비교합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PoiseComparison", menuName = "BT/Conditions/Poise Comparison", order = 1)]
    public class BTCondition_PoiseComparison : BTConditionNode
    {
        [Header("비교 대상")]
        [Tooltip("비교할 대상 (자신 또는 상대)")]
        public ComparisonTarget target = ComparisonTarget.Self;
        
        [Header("비교 설정")]
        [Tooltip("비교 연산자")]
        public ComparisonOperator comparisonOperator = ComparisonOperator.Less;
        
        [Tooltip("값 타입 (절대값 또는 비율)")]
        public ValueType valueType = ValueType.Percentage;
        
        [Tooltip("비교할 임계값")]
        public float threshold = 0.5f;
        
        public enum ComparisonTarget
        {
            [Tooltip("자신 (NPC)")]
            Self,
            [Tooltip("상대 (플레이어)")]
            Target
        }
        
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
        
        public enum ValueType
        {
            [Tooltip("절대값 (자세 포인트)")]
            Absolute,
            [Tooltip("비율 (0~1)")]
            Percentage
        }
        
        public override bool Evaluate(BehaviorTreeContext context)
        {
            if (context?.self == null || context?.target == null)
                return false;
            
            Character targetCharacter = (target == ComparisonTarget.Self) ? context.self : context.target;
            
            if (targetCharacter == null)
                return false;
            
            float currentPoise = targetCharacter.CurrentPoise;
            float maxPoise = targetCharacter.MaxPoise;
            
            if (maxPoise <= 0)
                return false;
            
            float compareValue;
            if (valueType == ValueType.Absolute)
            {
                compareValue = currentPoise;
            }
            else // Percentage
            {
                compareValue = currentPoise / maxPoise;
            }
            
            return CompareValues(compareValue, threshold, comparisonOperator);
        }
        
        private bool CompareValues(float value, float threshold, ComparisonOperator op)
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
                    return Mathf.Approximately(value, threshold);
                case ComparisonOperator.NotEqual:
                    return !Mathf.Approximately(value, threshold);
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
                string targetName = (target == ComparisonTarget.Self) ? "자신" : "상대";
                string valueTypeName = (valueType == ValueType.Absolute) ? "자세" : "자세%";
                string operatorName = GetOperatorDisplayName(comparisonOperator);
                
                description = $"{targetName}의 {valueTypeName} {operatorName} {threshold}";
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

