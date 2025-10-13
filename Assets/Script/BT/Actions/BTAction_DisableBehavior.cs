using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 행동 비활성화 액션 노드
    /// 특정 행동을 임시로 비활성화합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DisableBehavior", menuName = "BT/Actions/Disable Behavior", order = 3)]
    public class BTAction_DisableBehavior : BTActionNode
    {
        [Header("행동 비활성화 설정")]
        [Tooltip("비활성화할 행동 타입")]
        public BehaviorType behaviorType = BehaviorType.ParryWhileGuarding;
        
        public enum BehaviorType
        {
            [Tooltip("막기")]
            Guard,
            [Tooltip("쳐내기")]
            Parry,
            [Tooltip("막기 중 쳐내기")]
            ParryWhileGuarding,
            [Tooltip("공격")]
            Attack
        }
        
        public override void Execute(BehaviorTreeContext context)
        {
            if (context == null)
                return;
            
            // 해당 행동의 확률을 0%로 설정
            SetProbabilityToZero(context, behaviorType);
        }
        
        private void SetProbabilityToZero(BehaviorTreeContext context, BehaviorType behavior)
        {
            switch (behavior)
            {
                case BehaviorType.Guard:
                    context.SetProbabilityOverride("GuardAttemptRate", 0f);
                    break;
                case BehaviorType.Parry:
                    context.SetProbabilityOverride("ParryPerfectRate", 0f);
                    break;
                case BehaviorType.ParryWhileGuarding:
                    context.SetProbabilityOverride("ParryWhileGuardingRate", 0f);
                    break;
                case BehaviorType.Attack:
                    context.SetProbabilityOverride("AttackPerfectRate", 0f);
                    break;
            }
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                string behaviorName = GetBehaviorDisplayName(behaviorType);
                description = $"{behaviorName} 비활성화";
            }
        }
        
        private string GetBehaviorDisplayName(BehaviorType behavior)
        {
            switch (behavior)
            {
                case BehaviorType.Guard:
                    return "막기";
                case BehaviorType.Parry:
                    return "쳐내기";
                case BehaviorType.ParryWhileGuarding:
                    return "막기 중 쳐내기";
                case BehaviorType.Attack:
                    return "공격";
                default:
                    return "알 수 없음";
            }
        }
    }
}

