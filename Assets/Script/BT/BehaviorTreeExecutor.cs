using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 실행기
    /// BT를 평가하고 실행하는 핵심 클래스입니다.
    /// </summary>
    public static class BehaviorTreeExecutor
    {
        /// <summary>
        /// Behavior Tree를 평가하고 실행합니다.
        /// </summary>
        /// <param name="tree">실행할 BT</param>
        /// <param name="self">NPC 자신</param>
        /// <param name="target">상대방 (플레이어)</param>
        /// <param name="currentTurn">현재 턴 번호</param>
        /// <param name="isAttackTurn">공격 턴 여부</param>
        /// <returns>실행 결과 컨텍스트</returns>
        public static BehaviorTreeContext EvaluateTree(
            BehaviorTreeData tree, 
            Combatant self, 
            Combatant target,
            int currentTurn,
            bool isAttackTurn)
        {
            if (tree == null || !tree.IsValid())
            {
                Debug.LogWarning("[BT Executor] 유효하지 않은 Behavior Tree입니다.");
                return new BehaviorTreeContext();
            }
            
            var context = new BehaviorTreeContext();
            context.Initialize(self, target, currentTurn, isAttackTurn);
            
            // BT Entry를 순차적으로 평가 (우선순위 순서)
            foreach (var entry in tree.entries)
            {
                if (entry == null || !entry.isEnabled)
                    continue;
                
                if (entry.condition == null)
                {
                    Debug.LogWarning($"[BT Executor] Entry의 조건이 null입니다: {entry.description}");
                    continue;
                }
                
                // 조건 평가
                bool conditionResult = entry.condition.EvaluateCondition(context);
                
                if (conditionResult)
                {
                    // 조건 만족 시 Actions 실행
                    ExecuteActions(entry.actions, context);
                    
                    // 상위 조건 만족 시 하위 미체크 (우선순위 처리)
                    break;
                }
            }
            
            return context;
        }
        
        /// <summary>
        /// 여러 BT를 순차적으로 평가합니다.
        /// </summary>
        /// <param name="trees">평가할 BT 리스트</param>
        /// <param name="self">NPC 자신</param>
        /// <param name="target">상대방</param>
        /// <param name="currentTurn">현재 턴 번호</param>
        /// <param name="isAttackTurn">공격 턴 여부</param>
        /// <returns>병합된 실행 결과 컨텍스트</returns>
        public static BehaviorTreeContext EvaluateMultipleTrees(
            List<BehaviorTreeData> trees,
            Combatant self,
            Combatant target,
            int currentTurn,
            bool isAttackTurn)
        {
            var finalContext = new BehaviorTreeContext();
            finalContext.Initialize(self, target, currentTurn, isAttackTurn);
            
            if (trees == null || trees.Count == 0)
                return finalContext;
            
            // 모든 BT 순차 평가
            foreach (var tree in trees)
            {
                if (tree == null)
                    continue;
                
                var context = EvaluateTree(tree, self, target, currentTurn, isAttackTurn);
                finalContext.MergeFrom(context);
            }
            
            return finalContext;
        }
        
        /// <summary>
        /// 액션들을 실행합니다.
        /// </summary>
        /// <param name="actions">실행할 액션 리스트</param>
        /// <param name="context">BT 실행 컨텍스트</param>
        private static void ExecuteActions(List<BTActionNode> actions, BehaviorTreeContext context)
        {
            if (actions == null || actions.Count == 0)
                return;
            
            // Priority별로 그룹화
            var groupedActions = actions
                .Where(action => action != null && action.IsValid())
                .GroupBy(action => action.priority)
                .OrderByDescending(group => group.Key); // 높은 우선순위부터
            
            // 각 우선순위 그룹에서 실행
            foreach (var group in groupedActions)
            {
                foreach (var action in group)
                {
                    try
                    {
                        action.ExecuteAction(context);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[BT Executor] 액션 실행 중 오류 발생: {action.name}\n{e}");
                    }
                }
            }
        }
        
        /// <summary>
        /// BT 실행 로그를 출력합니다.
        /// </summary>
        /// <param name="context">BT 실행 컨텍스트</param>
        public static void LogExecutionResult(BehaviorTreeContext context)
        {
            if (context == null)
                return;
            
            Debug.Log($"[BT Executor] 실행 결과:\n" +
                     $"  확률 Override: {context.probabilityOverrides.Count}개\n" +
                     $"  선택된 검술 인덱스: {context.selectedCommandIndex}\n" +
                     $"  선택된 검술 태그: {context.selectedCommandTag}\n" +
                     $"  강제 행동: {context.forcedBehavior}");
        }
    }
}

