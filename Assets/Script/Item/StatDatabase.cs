using UnityEngine;
using System.Collections.Generic;

namespace BladeAction.Item
{
    /// <summary>
    /// 스탯 테이블 전용 데이터베이스
    /// 재사용 가능한 스탯 프리셋을 대량으로 관리
    /// </summary>
    [CreateAssetMenu(fileName = "StatDatabase", menuName = "Item/Stat Database", order = 9)]
    public class StatDatabase : ScriptableObject
    {
        [Header("스탯 테이블 (인라인 편집)")]
        [Tooltip("모든 스탯 프리셋 - 여기서 대량 관리")]
        public List<StatTable> statTables = new List<StatTable>();
        
        /// <summary>
        /// Key로 스탯 테이블 검색
        /// </summary>
        public StatTable GetStatTable(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
                
            return statTables.Find(table => table.tableKey == key);
        }
        
        /// <summary>
        /// 스탯 테이블 추가
        /// </summary>
        public void AddStatTable(StatTable table)
        {
            if (table == null || string.IsNullOrEmpty(table.tableKey))
            {
                Debug.LogWarning("Invalid StatTable");
                return;
            }
            
            // 중복 체크
            if (GetStatTable(table.tableKey) != null)
            {
                Debug.LogWarning($"StatTable with key '{table.tableKey}' already exists");
                return;
            }
            
            statTables.Add(table);
        }
        
        /// <summary>
        /// 스탯 테이블이 존재하는지 확인
        /// </summary>
        public bool HasStatTable(string key)
        {
            return GetStatTable(key) != null;
        }
    }
}

