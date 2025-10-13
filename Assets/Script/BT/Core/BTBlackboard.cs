using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// BT 블랙보드 - 개체별 BT 실행 상태 저장소
    /// 
    /// 개념:
    /// - BT는 "로직"만 담당 (읽기 전용, 공유 가능)
    /// - Blackboard는 "상태"만 담당 (개체별 독립)
    /// 
    /// 왜 필요한가?
    /// - executeOncePerCombat 같은 상태를 BT 자체에 저장하면
    /// - 같은 BT를 사용하는 다른 개체에게 영향을 줌
    /// - Blackboard에 저장하면 각 개체가 독립적으로 상태 관리
    /// 
    /// 예시:
    /// - Goblin A, B, C가 모두 BT_Goblin.asset을 사용
    /// - 하지만 각자 BlackboardA, B, C를 소유
    /// - A가 궁극기를 썼어도 B는 쓸 수 있음 (독립적)
    /// </summary>
    public class BTBlackboard
    {
        // ========================================
        // 필드 (Fields)
        // ========================================
        
        /// <summary>
        /// 이번 전투에서 실행된 액션들
        /// Key: 액션 이름 (예: "BTAction_UseUltimate")
        /// Value: 실행 여부 (true = 이미 실행됨)
        /// </summary>
        private Dictionary<string, bool> executedActions = new Dictionary<string, bool>();
        
        /// <summary>
        /// 액션별 실행 횟수 (디버깅 및 확장용)
        /// </summary>
        private Dictionary<string, int> executionCounts = new Dictionary<string, int>();
        
        /// <summary>
        /// 이 블랙보드의 소유자 이름 (디버깅용)
        /// </summary>
        private string ownerName;
        
        
        // ========================================
        // 생성자 (Constructor)
        // ========================================
        
        /// <summary>
        /// 블랙보드를 생성합니다.
        /// </summary>
        /// <param name="ownerName">소유자 이름 (디버깅용)</param>
        public BTBlackboard(string ownerName = "Unknown")
        {
            this.ownerName = ownerName;
            Debug.Log($"[BTBlackboard] 생성됨 - 소유자: {ownerName}");
        }
        
        
        // ========================================
        // Public 메서드 (Methods)
        // ========================================
        
        /// <summary>
        /// 특정 액션이 이번 전투에서 실행되었는지 확인합니다.
        /// </summary>
        /// <param name="actionKey">액션 식별자 (보통 액션 이름)</param>
        /// <returns>실행되었으면 true</returns>
        public bool HasExecuted(string actionKey)
        {
            return executedActions.GetValueOrDefault(actionKey, false);
        }
        
        /// <summary>
        /// 액션을 실행됨으로 표시합니다.
        /// </summary>
        /// <param name="actionKey">액션 식별자</param>
        public void MarkAsExecuted(string actionKey)
        {
            bool wasExecuted = executedActions.GetValueOrDefault(actionKey, false);
            executedActions[actionKey] = true;
            
            // 실행 횟수 증가
            int count = executionCounts.GetValueOrDefault(actionKey, 0);
            executionCounts[actionKey] = count + 1;
            
            if (!wasExecuted)
            {
                Debug.Log($"[BTBlackboard] {ownerName} - 액션 최초 실행: '{actionKey}'");
            }
            else
            {
                Debug.LogWarning($"[BTBlackboard] {ownerName} - 액션 중복 실행 시도: '{actionKey}' (총 {count + 1}회)");
            }
        }
        
        /// <summary>
        /// 모든 실행 상태를 리셋합니다 (새 전투 시작 시 호출)
        /// </summary>
        public void ResetCombat()
        {
            int executedCount = executedActions.Count;
            
            executedActions.Clear();
            executionCounts.Clear();
            
            Debug.Log($"[BTBlackboard] {ownerName} - 전투 상태 리셋 (이전 실행: {executedCount}개 액션)");
        }
        
        /// <summary>
        /// 액션의 실행 횟수를 반환합니다 (디버깅용)
        /// </summary>
        public int GetExecutionCount(string actionKey)
        {
            return executionCounts.GetValueOrDefault(actionKey, 0);
        }
        
        /// <summary>
        /// 현재 상태를 로그로 출력합니다 (디버깅용)
        /// </summary>
        public void LogStatus()
        {
            Debug.Log($"[BTBlackboard] {ownerName} 상태:");
            Debug.Log($"  - 실행된 액션 수: {executedActions.Count}개");
            
            foreach (var kvp in executedActions)
            {
                if (kvp.Value)
                {
                    int count = executionCounts.GetValueOrDefault(kvp.Key, 0);
                    Debug.Log($"    • {kvp.Key}: 실행됨 (총 {count}회)");
                }
            }
        }
    }
}

