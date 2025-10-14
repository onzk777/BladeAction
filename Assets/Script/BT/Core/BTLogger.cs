using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 전용 로거
    /// BT 실행 과정을 상세하게 추적하고 기록합니다.
    /// </summary>
    public static class BTLogger
    {
        /// <summary>
        /// BT 로그 활성화 여부
        /// </summary>
        public static bool EnableLogging = true;
        
        /// <summary>
        /// 조건 평가 로그 활성화 여부
        /// </summary>
        public static bool EnableConditionLogging = true;
        
        /// <summary>
        /// 액션 실행 로그 활성화 여부
        /// </summary>
        public static bool EnableActionLogging = true;
        
        /// <summary>
        /// 확률 변경 로그 활성화 여부
        /// </summary>
        public static bool EnableProbabilityLogging = true;
        
        /// <summary>
        /// 상세 로그 활성화 여부 (성능에 영향 있을 수 있음)
        /// </summary>
        public static bool EnableVerboseLogging = false;
        
        // 로그 색상 코드 (밝은 배경에서 잘 보이도록 어둡게 조정)
        private const string COLOR_HEADER = "#0066CC";      // 헤더 (진한 파란색)
        private const string COLOR_SUCCESS = "#008800";     // 성공 (진한 녹색)
        private const string COLOR_FAIL = "#CC0000";        // 실패 (진한 빨강)
        private const string COLOR_INFO = "#CC8800";        // 정보 (진한 황금색)
        private const string COLOR_ACTION = "#CC0088";      // 액션 (진한 마젠타)
        private const string COLOR_PROBABILITY = "#CC6600"; // 확률 (진한 주황)
        
        /// <summary>
        /// BT 평가 시작 로그
        /// </summary>
        public static void LogTreeEvaluationStart(BehaviorTreeData tree, BehaviorTreeContext context)
        {
            // 히스토리에 기록
            string combatantName = context.self?.CharacterData?.characterName ?? "Unknown";
            BTLogHistory.Instance.StartEvaluation(tree.name, combatantName, context.currentTurn, context.isAttackTurn);
            
            if (!EnableLogging) return;
            
            Debug.Log($"<color={COLOR_HEADER}>╔═══════════════════════════════════════════════════════════════</color>\n" +
                     $"<color={COLOR_HEADER}>║ BT 평가 시작: {tree.name}</color>\n" +
                     $"<color={COLOR_HEADER}>╠═══════════════════════════════════════════════════════════════</color>\n" +
                     $"  턴: {context.currentTurn}\n" +
                     $"  공격 턴: {(context.isAttackTurn ? "✓" : "✗")}\n" +
                     $"  Self: {context.self?.CharacterData?.characterName ?? "N/A"} (HP: {context.self?.HP ?? 0}/{context.self?.MaxHP ?? 0})\n" +
                     $"  Target: {context.target?.CharacterData?.characterName ?? "N/A"} (HP: {context.target?.HP ?? 0}/{context.target?.MaxHP ?? 0})\n" +
                     $"<color={COLOR_HEADER}>╚═══════════════════════════════════════════════════════════════</color>");
        }
        
        /// <summary>
        /// BT 평가 종료 로그
        /// </summary>
        public static void LogTreeEvaluationEnd(BehaviorTreeData tree, BehaviorTreeContext context, bool foundMatch, int matchedEntryIndex = -1, string matchedEntryDescription = null)
        {
            // 히스토리에 기록
            BTLogHistory.Instance.EndEvaluation(
                foundMatch,
                matchedEntryIndex,
                matchedEntryDescription,
                context.probabilityOverrides,
                context.selectedCommandIndex,
                context.selectedCommandTag
            );
            
            if (!EnableLogging) return;
            
            string matchStatus = foundMatch ? 
                $"<color={COLOR_SUCCESS}>✓ 조건 일치 Entry 발견</color>" : 
                $"<color={COLOR_FAIL}>✗ 일치하는 Entry 없음</color>";
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color={COLOR_HEADER}>╔═══════════════════════════════════════════════════════════════</color>");
            sb.AppendLine($"<color={COLOR_HEADER}>║ BT 평가 완료: {tree.name}</color>");
            sb.AppendLine($"<color={COLOR_HEADER}>╠═══════════════════════════════════════════════════════════════</color>");
            sb.AppendLine($"  결과: {matchStatus}");
            sb.AppendLine($"  확률 Override: {context.probabilityOverrides.Count}개");
            
            if (context.probabilityOverrides.Count > 0)
            {
                sb.AppendLine($"  <color={COLOR_PROBABILITY}>━━━ 확률 변경 내역 ━━━</color>");
                foreach (var kvp in context.probabilityOverrides)
                {
                    sb.AppendLine($"    • {kvp.Key}: <color={COLOR_PROBABILITY}>{kvp.Value:P0}</color>");
                }
            }
            
            if (context.selectedCommandIndex.HasValue)
            {
                sb.AppendLine($"  선택된 검술 인덱스: <color={COLOR_INFO}>{context.selectedCommandIndex.Value}</color>");
            }
            
            if (!string.IsNullOrEmpty(context.selectedCommandTag))
            {
                sb.AppendLine($"  선택된 검술 태그: <color={COLOR_INFO}>{context.selectedCommandTag}</color>");
            }
            
            if (!string.IsNullOrEmpty(context.forcedBehavior))
            {
                sb.AppendLine($"  강제 행동: <color={COLOR_INFO}>{context.forcedBehavior}</color>");
            }
            
            sb.AppendLine($"<color={COLOR_HEADER}>╚═══════════════════════════════════════════════════════════════</color>");
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// Entry 평가 시작 로그
        /// </summary>
        public static void LogEntryEvaluation(int entryIndex, string description, bool isEnabled)
        {
            if (!EnableLogging || !EnableConditionLogging) return;
            
            string status = isEnabled ? "" : " <color=#888888>[비활성화]</color>";
            Debug.Log($"  ▼ Entry[{entryIndex}]: {description}{status}");
        }
        
        /// <summary>
        /// 조건 평가 결과 로그
        /// </summary>
        public static void LogConditionResult(BTConditionNode condition, bool result, BehaviorTreeContext context)
        {
            // 히스토리에 기록
            string details = EnableVerboseLogging ? GetConditionDetailsString(condition, context) : null;
            BTLogHistory.Instance.LogCondition(
                condition.name, 
                condition.GetType().Name, 
                result, 
                condition.invertResult,
                details
            );
            
            if (!EnableLogging || !EnableConditionLogging) return;
            
            string resultIcon = result ? 
                $"<color={COLOR_SUCCESS}>✓</color>" : 
                $"<color={COLOR_FAIL}>✗</color>";
            
            string invertInfo = condition.invertResult ? " <color=#FFA500>[반전]</color>" : "";
            
            Debug.Log($"    {resultIcon} {condition.GetType().Name}: {condition.name}{invertInfo}");
            
            if (EnableVerboseLogging)
            {
                LogConditionDetails(condition, context);
            }
        }
        
        /// <summary>
        /// 조건 상세 정보 로그
        /// </summary>
        private static void LogConditionDetails(BTConditionNode condition, BehaviorTreeContext context)
        {
            // 조건 타입별 상세 정보
            if (condition is BTCondition_HPComparison hpCond)
            {
                Combatant target = hpCond.target == BTCondition_HPComparison.ComparisonTarget.Self ? 
                    context.self : context.target;
                float currentHP = target?.HP ?? 0;
                float maxHP = target?.MaxHP ?? 1;
                float percentage = (currentHP / maxHP) * 100f;
                
                Debug.Log($"      ├─ 대상: {hpCond.target} (HP: {currentHP}/{maxHP} = {percentage:F1}%)");
                Debug.Log($"      └─ 조건: {hpCond.comparisonOperator} {hpCond.threshold:F2} ({hpCond.valueType})");
            }
            else if (condition is BTCondition_PoiseComparison poiseCond)
            {
                Combatant target = poiseCond.target == BTCondition_PoiseComparison.ComparisonTarget.Self ? 
                    context.self : context.target;
                float currentPoise = target?.CurrentPoise ?? 0;
                float maxPoise = target?.MaxPoise ?? 1;
                float percentage = (currentPoise / maxPoise) * 100f;
                
                Debug.Log($"      ├─ 대상: {poiseCond.target} (Poise: {currentPoise}/{maxPoise} = {percentage:F1}%)");
                Debug.Log($"      └─ 조건: {poiseCond.comparisonOperator} {poiseCond.threshold:F2} ({poiseCond.valueType})");
            }
            else if (condition is BTCondition_TurnType turnTypeCond)
            {
                Debug.Log($"      └─ 요구 턴 타입: {turnTypeCond.turnType} (현재: {(context.isAttackTurn ? "공격" : "방어")})");
            }
            else if (condition is BTCondition_TurnCount turnCountCond)
            {
                Debug.Log($"      └─ 조건: 턴 {turnCountCond.comparisonOperator} {turnCountCond.turnCount} (현재: {context.currentTurn})");
            }
        }
        
        /// <summary>
        /// 액션 실행 로그
        /// </summary>
        public static void LogActionExecution(BTActionNode action, BehaviorTreeContext context)
        {
            // 히스토리에 기록
            string details = EnableVerboseLogging ? GetActionDetailsString(action) : null;
            BTLogHistory.Instance.LogAction(
                action.name,
                action.GetType().Name,
                action.priority,
                false,
                null,
                details
            );
            
            if (!EnableLogging || !EnableActionLogging) return;
            
            string priorityInfo = action.priority > 0 ? $" <color=#888888>[Priority: {action.priority}]</color>" : "";
            string onceInfo = action.executeOncePerCombat ? " <color=#FFD700>[1회 실행]</color>" : "";
            
            Debug.Log($"    <color={COLOR_ACTION}>▶</color> {action.GetType().Name}: {action.name}{priorityInfo}{onceInfo}");
            
            if (EnableVerboseLogging)
            {
                LogActionDetails(action, context);
            }
        }
        
        /// <summary>
        /// 액션 상세 정보 로그
        /// </summary>
        private static void LogActionDetails(BTActionNode action, BehaviorTreeContext context)
        {
            if (action is BTAction_ProbabilityAdjustment probAdj)
            {
                string adjustType = probAdj.adjustmentType == BTAction_ProbabilityAdjustment.AdjustmentType.Absolute ? 
                    "절대값" : "상대값";
                Debug.Log($"      ├─ 대상 확률: {probAdj.targetProbability}");
                Debug.Log($"      ├─ 조정 방식: {adjustType}");
                Debug.Log($"      └─ 값: {probAdj.value:P0}");
            }
            else if (action is BTAction_CommandSelection cmdSel)
            {
                if (cmdSel.selectionType == BTAction_CommandSelection.SelectionType.ByIndex)
                {
                    Debug.Log($"      └─ 검술 인덱스: {cmdSel.commandIndex}");
                }
                else
                {
                    Debug.Log($"      └─ 검술 태그: {cmdSel.requiredTag}");
                }
            }
            else if (action is BTAction_ForceBehavior forceBehavior)
            {
                Debug.Log($"      └─ 강제 행동: {forceBehavior.behaviorType}");
            }
            else if (action is BTAction_DoParryWhileGuarding parryGuard)
            {
                Debug.Log($"      └─ 막기 중 쳐내기: {(parryGuard.enableParryWhileGuarding ? "활성화" : "비활성화")}");
            }
        }
        
        /// <summary>
        /// 액션 건너뜀 로그
        /// </summary>
        public static void LogActionSkipped(BTActionNode action, string reason)
        {
            // 히스토리에 기록
            BTLogHistory.Instance.LogAction(
                action.name,
                action.GetType().Name,
                action.priority,
                true,
                reason,
                null
            );
            
            if (!EnableLogging || !EnableActionLogging) return;
            
            Debug.Log($"    <color=#888888>⊘ {action.GetType().Name}: {action.name} - {reason}</color>");
        }
        
        /// <summary>
        /// 확률 적용 로그 (Combatant에서 호출)
        /// </summary>
        public static void LogProbabilityApplied(string combatantName, Dictionary<string, float> overrides)
        {
            if (!EnableLogging || !EnableProbabilityLogging) return;
            
            if (overrides == null || overrides.Count == 0)
            {
                Debug.Log($"<color={COLOR_PROBABILITY}>[확률 적용]</color> {combatantName}: Override 없음");
                return;
            }
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color={COLOR_PROBABILITY}>╔═══ 확률 적용: {combatantName} ═══╗</color>");
            
            foreach (var kvp in overrides)
            {
                sb.AppendLine($"<color={COLOR_PROBABILITY}>║</color>  • {kvp.Key}: {kvp.Value:P0}");
            }
            
            sb.AppendLine($"<color={COLOR_PROBABILITY}>╚═════════════════════════════════════╝</color>");
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// 확률 변경 전후 비교 로그
        /// </summary>
        public static void LogProbabilityChange(string combatantName, string probabilityName, float before, float after)
        {
            if (!EnableLogging || !EnableProbabilityLogging) return;
            
            string arrow = before == after ? "→" : "⇒";
            string colorCode = before == after ? "#888888" : COLOR_PROBABILITY;
            
            Debug.Log($"<color={colorCode}>[확률 변경]</color> {combatantName}.{probabilityName}: " +
                     $"<color={colorCode}>{before:P0} {arrow} {after:P0}</color>");
        }
        
        /// <summary>
        /// 확률 리셋 로그
        /// </summary>
        public static void LogProbabilityReset(string combatantName)
        {
            if (!EnableLogging || !EnableProbabilityLogging) return;
            
            Debug.Log($"<color={COLOR_INFO}>[확률 리셋]</color> {combatantName}: 원본 확률로 복원");
        }
        
        /// <summary>
        /// 에러 로그
        /// </summary>
        public static void LogError(string message)
        {
            if (!EnableLogging) return;
            
            Debug.LogError($"<color={COLOR_FAIL}>[BT ERROR]</color> {message}");
        }
        
        /// <summary>
        /// 경고 로그
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!EnableLogging) return;
            
            Debug.LogWarning($"<color=#FFA500>[BT WARNING]</color> {message}");
        }
        
        /// <summary>
        /// 디버그 정보 로그
        /// </summary>
        public static void LogDebug(string message)
        {
            if (!EnableLogging || !EnableVerboseLogging) return;
            
            Debug.Log($"<color=#888888>[BT DEBUG]</color> {message}");
        }
        
        /// <summary>
        /// 컨텍스트 병합 로그
        /// </summary>
        public static void LogContextMerge(int overrideCount)
        {
            if (!EnableLogging || !EnableVerboseLogging) return;
            
            Debug.Log($"<color={COLOR_INFO}>[컨텍스트 병합]</color> {overrideCount}개의 확률 Override 병합됨");
        }
        
        /// <summary>
        /// 블랙보드 리셋 로그
        /// </summary>
        public static void LogBlackboardReset(string combatantName)
        {
            if (!EnableLogging) return;
            
            Debug.Log($"<color={COLOR_INFO}>[블랙보드 리셋]</color> {combatantName}: 전투 상태 초기화");
        }
        
        /// <summary>
        /// 조건 상세 정보 문자열 생성 (히스토리용)
        /// </summary>
        private static string GetConditionDetailsString(BTConditionNode condition, BehaviorTreeContext context)
        {
            if (condition is BTCondition_HPComparison hpCond)
            {
                Combatant target = hpCond.target == BTCondition_HPComparison.ComparisonTarget.Self ? 
                    context.self : context.target;
                float currentHP = target?.HP ?? 0;
                float maxHP = target?.MaxHP ?? 1;
                float percentage = (currentHP / maxHP) * 100f;
                return $"{hpCond.target} HP: {currentHP}/{maxHP} ({percentage:F1}%) {hpCond.comparisonOperator} {hpCond.threshold} ({hpCond.valueType})";
            }
            else if (condition is BTCondition_PoiseComparison poiseCond)
            {
                Combatant target = poiseCond.target == BTCondition_PoiseComparison.ComparisonTarget.Self ? 
                    context.self : context.target;
                float currentPoise = target?.CurrentPoise ?? 0;
                float maxPoise = target?.MaxPoise ?? 1;
                float percentage = (currentPoise / maxPoise) * 100f;
                return $"{poiseCond.target} Poise: {currentPoise}/{maxPoise} ({percentage:F1}%) {poiseCond.comparisonOperator} {poiseCond.threshold} ({poiseCond.valueType})";
            }
            else if (condition is BTCondition_TurnType turnTypeCond)
            {
                return $"요구: {turnTypeCond.turnType}, 현재: {(context.isAttackTurn ? "공격" : "방어")}";
            }
            else if (condition is BTCondition_TurnCount turnCountCond)
            {
                return $"턴 {turnCountCond.comparisonOperator} {turnCountCond.turnCount} (현재: {context.currentTurn})";
            }
            
            return null;
        }
        
        /// <summary>
        /// 액션 상세 정보 문자열 생성 (히스토리용)
        /// </summary>
        private static string GetActionDetailsString(BTActionNode action)
        {
            if (action is BTAction_ProbabilityAdjustment probAdj)
            {
                string adjustType = probAdj.adjustmentType == BTAction_ProbabilityAdjustment.AdjustmentType.Absolute ? 
                    "절대값" : "상대값";
                return $"{probAdj.targetProbability} {adjustType}: {probAdj.value:P0}";
            }
            else if (action is BTAction_CommandSelection cmdSel)
            {
                if (cmdSel.selectionType == BTAction_CommandSelection.SelectionType.ByIndex)
                {
                    return $"검술 인덱스: {cmdSel.commandIndex}";
                }
                else
                {
                    return $"검술 태그: {cmdSel.requiredTag}";
                }
            }
            else if (action is BTAction_ForceBehavior forceBehavior)
            {
                return $"강제 행동: {forceBehavior.behaviorType}";
            }
            else if (action is BTAction_DoParryWhileGuarding parryGuard)
            {
                return $"막기 중 쳐내기: {(parryGuard.enableParryWhileGuarding ? "활성화" : "비활성화")}";
            }
            
            return null;
        }
    }
}

