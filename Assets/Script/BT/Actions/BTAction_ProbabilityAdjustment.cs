using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 확률 조정 액션 노드
    /// NPC의 행동 확률을 임시로 조정합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ProbabilityAdjustment", menuName = "BT/Actions/Probability Adjustment", order = 0)]
    public class BTAction_ProbabilityAdjustment : BTActionNode
    {
        [Header("확률 조정 설정")]
        [Tooltip("조정할 확률 타입")]
        public TargetProbability targetProbability = TargetProbability.AttackPerfectRate;
        
        [Tooltip("조정 방식")]
        public AdjustmentType adjustmentType = AdjustmentType.Absolute;
        
        [Tooltip("조정할 값 (0~1)")]
        [Range(0f, 1f)]
        public float value = 0.5f;
        
        public enum TargetProbability
        {
            [Tooltip("공격 성공률")]
            AttackPerfectRate,
            [Tooltip("쳐내기 성공률")]
            ParryPerfectRate,
            [Tooltip("막기 시도 확률")]
            GuardAttemptRate,
            [Tooltip("막기 중 쳐내기 성공률")]
            ParryWhileGuardingRate
        }
        
        public enum AdjustmentType
        {
            [Tooltip("절대값 설정")]
            Absolute,
            [Tooltip("상대값 증감")]
            Relative
        }
        
        public override void Execute(BehaviorTreeContext context)
        {
            if (context == null)
                return;
            
            string key = targetProbability.ToString();
            float finalValue;
            
            if (adjustmentType == AdjustmentType.Absolute)
            {
                finalValue = value;
            }
            else // Relative
            {
                float currentValue = context.GetProbabilityOverride(key, GetOriginalValue(context, key));
                finalValue = Mathf.Clamp01(currentValue + value);
            }
            
            context.SetProbabilityOverride(key, finalValue);
        }
        
        private float GetOriginalValue(BehaviorTreeContext context, string key)
        {
            if (context?.self?.CharacterData?.npcBehavior == null)
                return 0f;
            
            var npcBehavior = context.self.CharacterData.npcBehavior;
            
            switch (key)
            {
                case nameof(TargetProbability.AttackPerfectRate):
                    return npcBehavior.attackPerfectRate;
                case nameof(TargetProbability.ParryPerfectRate):
                    return npcBehavior.parryPerfectRate;
                case nameof(TargetProbability.GuardAttemptRate):
                    return npcBehavior.guardAttemptRate;
                case nameof(TargetProbability.ParryWhileGuardingRate):
                    return npcBehavior.parryWhileGuardingRate;
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                string targetName = GetTargetDisplayName(targetProbability);
                string adjustmentName = (adjustmentType == AdjustmentType.Absolute) ? "설정" : "증감";
                description = $"{targetName} {adjustmentName}: {value:F2}";
            }
        }
        
        private string GetTargetDisplayName(TargetProbability target)
        {
            switch (target)
            {
                case TargetProbability.AttackPerfectRate:
                    return "공격 성공률";
                case TargetProbability.ParryPerfectRate:
                    return "쳐내기 성공률";
                case TargetProbability.GuardAttemptRate:
                    return "막기 시도 확률";
                case TargetProbability.ParryWhileGuardingRate:
                    return "막기 중 쳐내기 성공률";
                default:
                    return "알 수 없음";
            }
        }
    }
}

