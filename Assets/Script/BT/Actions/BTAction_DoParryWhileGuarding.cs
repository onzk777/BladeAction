using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// 막기 중 쳐내기 시도 여부 설정 액션 노드
    /// NPC가 막기 상태에서 쳐내기를 시도할 수 있는지 제어합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "DoParryWhileGuarding", menuName = "BT/Actions/Do Parry While Guarding", order = 4)]
    public class BTAction_DoParryWhileGuarding : BTActionNode
    {
        [Header("막기 중 쳐내기 설정")]
        [Tooltip("막기 중 쳐내기 시도 활성화 여부")]
        public bool enableParryWhileGuarding = true;
        
        public override void Execute(BehaviorTreeContext context)
        {
            if (context == null)
                return;
            
            // float로 변환하여 context에 저장 (0 = false, 1 = true)
            float value = enableParryWhileGuarding ? 1f : 0f;
            context.SetProbabilityOverride("DoParryWhileGuarding", value);
            
            Debug.Log($"[BTAction_DoParryWhileGuarding] 막기 중 쳐내기 시도: {(enableParryWhileGuarding ? "활성화" : "비활성화")}");
        }
        
        /// <summary>
        /// 노드 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                description = $"막기 중 쳐내기 시도: {(enableParryWhileGuarding ? "활성화" : "비활성화")}";
            }
        }
    }
}

