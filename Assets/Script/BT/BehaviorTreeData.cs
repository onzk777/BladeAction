using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 데이터
    /// BT Entry 리스트를 관리하는 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorTree", menuName = "BT/Behavior Tree", order = 0)]
    public class BehaviorTreeData : ScriptableObject
    {
        [Header("BT 정보")]
        [Tooltip("BT 설명")]
        [TextArea(3, 5)]
        public string description = "";
        
        [Header("BT Entry 리스트")]
        [Tooltip("BT Entry 리스트 (인덱스 = 우선순위)")]
        public List<BTEntry> entries = new List<BTEntry>();
        
        /// <summary>
        /// 액션 노드 래퍼 - 노드와 활성화 상태를 함께 관리
        /// </summary>
        [System.Serializable]
        public class ActionWrapper
        {
            [Tooltip("실행할 액션 노드")]
            public BTActionNode node;
            
            [Tooltip("이 Entry에서 이 액션 활성화 여부")]
            public bool isEnabled = true;
        }
        
        [System.Serializable]
        public class BTEntry
        {
            [Header("조건")]
            [Tooltip("조건 노드 (Composite 또는 Simple Condition)")]
            public BTConditionNode condition;
            
            [Header("액션들")]
            [Tooltip("조건 만족 시 실행할 액션들")]
            public List<ActionWrapper> actions = new List<ActionWrapper>();
            
            [Header("설정")]
            [Tooltip("이 Entry 활성화 여부")]
            public bool isEnabled = true;
            
            [Tooltip("Entry 설명")]
            [TextArea(2, 3)]
            public string description = "";
        }
        
        /// <summary>
        /// BT가 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            if (entries == null || entries.Count == 0)
                return false;
            
            foreach (var entry in entries)
            {
                if (entry == null || !entry.isEnabled)
                    continue;
                    
                if (entry.condition == null)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 활성화된 Entry 개수 반환
        /// </summary>
        public int GetActiveEntryCount()
        {
            if (entries == null)
                return 0;
                
            int count = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.isEnabled)
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// BT 설명 자동 생성
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(description))
            {
                int activeCount = GetActiveEntryCount();
                description = $"Behavior Tree - 활성 Entry: {activeCount}개";
            }
        }
    }
}

