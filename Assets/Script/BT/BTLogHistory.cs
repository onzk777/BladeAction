using System;
using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// BT 실행 로그를 저장하고 관리하는 히스토리 클래스
    /// UI에서 접근하여 정리된 로그를 표시합니다.
    /// </summary>
    public class BTLogHistory
    {
        /// <summary>
        /// BT 평가 기록 하나
        /// </summary>
        [System.Serializable]
        public class BTEvaluationLog
        {
            public string treeName;
            public string combatantName;
            public int turnNumber;
            public bool isAttackTurn;
            public bool foundMatch;
            public int matchedEntryIndex = -1;
            public string matchedEntryDescription;
            public List<ConditionLog> conditions = new List<ConditionLog>();
            public List<ActionLog> actions = new List<ActionLog>();
            public Dictionary<string, float> probabilityOverrides = new Dictionary<string, float>();
            public int? selectedCommandIndex;
            public string selectedCommandTag;
            public DateTime timestamp;
            
            public BTEvaluationLog()
            {
                timestamp = DateTime.Now;
            }
        }
        
        /// <summary>
        /// 조건 평가 기록
        /// </summary>
        [System.Serializable]
        public class ConditionLog
        {
            public string conditionName;
            public string conditionType;
            public bool result;
            public bool inverted;
            public string details;
        }
        
        /// <summary>
        /// 액션 실행 기록
        /// </summary>
        [System.Serializable]
        public class ActionLog
        {
            public string actionName;
            public string actionType;
            public int priority;
            public bool skipped;
            public string skipReason;
            public string details;
        }
        
        private static BTLogHistory _instance;
        public static BTLogHistory Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BTLogHistory();
                return _instance;
            }
        }
        
        private List<BTEvaluationLog> evaluationLogs = new List<BTEvaluationLog>();
        private BTEvaluationLog currentLog;
        
        [SerializeField] private int maxLogCount = 50; // 최대 로그 수
        
        /// <summary>
        /// 저장된 모든 평가 로그
        /// </summary>
        public List<BTEvaluationLog> EvaluationLogs => evaluationLogs;
        
        /// <summary>
        /// 최대 로그 수 설정
        /// </summary>
        public int MaxLogCount
        {
            get => maxLogCount;
            set => maxLogCount = Mathf.Max(1, value);
        }
        
        /// <summary>
        /// BT 평가 시작
        /// </summary>
        public void StartEvaluation(string treeName, string combatantName, int turnNumber, bool isAttackTurn)
        {
            currentLog = new BTEvaluationLog
            {
                treeName = treeName,
                combatantName = combatantName,
                turnNumber = turnNumber,
                isAttackTurn = isAttackTurn
            };
        }
        
        /// <summary>
        /// 조건 평가 기록
        /// </summary>
        public void LogCondition(string conditionName, string conditionType, bool result, bool inverted, string details = null)
        {
            if (currentLog == null) return;
            
            currentLog.conditions.Add(new ConditionLog
            {
                conditionName = conditionName,
                conditionType = conditionType,
                result = result,
                inverted = inverted,
                details = details
            });
        }
        
        /// <summary>
        /// 액션 실행 기록
        /// </summary>
        public void LogAction(string actionName, string actionType, int priority, bool skipped = false, string skipReason = null, string details = null)
        {
            if (currentLog == null) return;
            
            currentLog.actions.Add(new ActionLog
            {
                actionName = actionName,
                actionType = actionType,
                priority = priority,
                skipped = skipped,
                skipReason = skipReason,
                details = details
            });
        }
        
        /// <summary>
        /// BT 평가 완료
        /// </summary>
        public void EndEvaluation(bool foundMatch, int matchedEntryIndex, string matchedEntryDescription, 
            Dictionary<string, float> probabilityOverrides, int? selectedCommandIndex, string selectedCommandTag)
        {
            if (currentLog == null) return;
            
            currentLog.foundMatch = foundMatch;
            currentLog.matchedEntryIndex = matchedEntryIndex;
            currentLog.matchedEntryDescription = matchedEntryDescription;
            
            if (probabilityOverrides != null)
            {
                foreach (var kvp in probabilityOverrides)
                {
                    currentLog.probabilityOverrides[kvp.Key] = kvp.Value;
                }
            }
            
            currentLog.selectedCommandIndex = selectedCommandIndex;
            currentLog.selectedCommandTag = selectedCommandTag;
            
            // 로그 추가
            evaluationLogs.Add(currentLog);
            
            // 최대 수 초과 시 오래된 로그 제거
            while (evaluationLogs.Count > maxLogCount)
            {
                evaluationLogs.RemoveAt(0);
            }
            
            currentLog = null;
        }
        
        /// <summary>
        /// 모든 로그 삭제
        /// </summary>
        public void Clear()
        {
            evaluationLogs.Clear();
            currentLog = null;
        }
        
        /// <summary>
        /// 특정 턴의 로그 가져오기
        /// </summary>
        public List<BTEvaluationLog> GetLogsByTurn(int turnNumber)
        {
            return evaluationLogs.FindAll(log => log.turnNumber == turnNumber);
        }
        
        /// <summary>
        /// 특정 Combatant의 로그 가져오기
        /// </summary>
        public List<BTEvaluationLog> GetLogsByCombatant(string combatantName)
        {
            return evaluationLogs.FindAll(log => log.combatantName == combatantName);
        }
        
        /// <summary>
        /// 최근 N개 로그 가져오기
        /// </summary>
        public List<BTEvaluationLog> GetRecentLogs(int count)
        {
            int startIndex = Mathf.Max(0, evaluationLogs.Count - count);
            int actualCount = Mathf.Min(count, evaluationLogs.Count);
            return evaluationLogs.GetRange(startIndex, actualCount);
        }
    }
}

