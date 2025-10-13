using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 강제 행동 액션 노드
    /// 특정 행동을 확정적으로 수행하도록 설정합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ForceBehavior", menuName = "BT/Actions/Force Behavior", order = 1)]
    public class BTAction_ForceBehavior : BTActionNode
    {
        [Header("강제 행동 설정")]
        [Tooltip("강제할 행동 타입")]
        public BehaviorType behaviorType = BehaviorType.Guard;
        
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
            
            string behaviorKey = GetBehaviorKey(behaviorType);
            context.forcedBehavior = behaviorKey;
            
            // 강제 행동 시 관련 확률을 100%로 설정
            SetProbabilityToMax(context, behaviorType);
        }
        
        private string GetBehaviorKey(BehaviorType behavior)
        {
            switch (behavior)
            {
                case BehaviorType.Guard:
                    return "ForceGuard";
                case BehaviorType.Parry:
                    return "ForceParry";
                case BehaviorType.ParryWhileGuarding:
                    return "ForceParryWhileGuarding";
                case BehaviorType.Attack:
                    return "ForceAttack";
                default:
                    return "Unknown";
            }
        }
        
        private void SetProbabilityToMax(BehaviorTreeContext context, BehaviorType behavior)
        {
            switch (behavior)
            {
                case BehaviorType.Guard:
                    context.SetProbabilityOverride("GuardAttemptRate", 1f);
                    break;
                case BehaviorType.Parry:
                    context.SetProbabilityOverride("ParryPerfectRate", 1f);
                    break;
                case BehaviorType.ParryWhileGuarding:
                    context.SetProbabilityOverride("ParryWhileGuardingRate", 1f);
                    break;
                case BehaviorType.Attack:
                    context.SetProbabilityOverride("AttackPerfectRate", 1f);
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
                description = $"{behaviorName} 강제 활성화";
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

