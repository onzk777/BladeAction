using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 턴 타입 조건 노드
    /// 현재 턴이 공격 턴인지 방어 턴인지 확인합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "TurnType", menuName = "BT/Conditions/Turn Type", order = 2)]
    public class BTCondition_TurnType : BTConditionNode
    {
        [Header("턴 타입 설정")]
        [Tooltip("확인할 턴 타입")]
        public TurnType turnType = TurnType.DefenseTurn;
        
        public enum TurnType
        {
            [Tooltip("공격 턴")]
            AttackTurn,
            [Tooltip("방어 턴")]
            DefenseTurn
        }
        
        public override bool Evaluate(BehaviorTreeContext context)
        {
            if (context == null)
                return false;
            
            bool isAttackTurn = context.isAttackTurn;
            
            switch (turnType)
            {
                case TurnType.AttackTurn:
                    return isAttackTurn;
                case TurnType.DefenseTurn:
                    return !isAttackTurn;
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
                string turnTypeName = (turnType == TurnType.AttackTurn) ? "공격 턴" : "방어 턴";
                description = $"현재 턴이 {turnTypeName}인가?";
            }
        }
    }
}

