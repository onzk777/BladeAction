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
        
        private void OnValidate()
        {
            // 에셋 편집 시에도 전역 룰로 Clamp 적용
            var rules = Resources.Load<BladeAction.Combat.StatLimitRules>("Data/Stat/StatLimitRules");
            if (rules == null) return;
            for (int i = 0; i < statTables.Count; i++)
            {
                var t = statTables[i];
                if (t?.stats == null) continue;
                t.stats.attackPower = Clamp("attack", t.stats.attackPower, rules);
                t.stats.blockEfficiency = Clamp("blockEfficiency", t.stats.blockEfficiency, rules);
                t.stats.blockPoiseConsumption = Clamp("blockPoiseConsumption", t.stats.blockPoiseConsumption, rules);
                t.stats.parryEfficiency = Clamp("parryEfficiency", t.stats.parryEfficiency, rules);
                t.stats.parryPoiseConsumption = Clamp("parryPoiseConsumption", t.stats.parryPoiseConsumption, rules);
                t.stats.parryPoiseAttackPower = Clamp("parryPoiseAttackPower", t.stats.parryPoiseAttackPower, rules);
                t.stats.maxHP = Clamp("maxHP", t.stats.maxHP, rules);
                t.stats.damageReduction = Clamp("damageReduction", t.stats.damageReduction, rules);
                t.stats.poise = Clamp("maxPoise", t.stats.poise, rules);
            }
        }
        
        private float Clamp(string key, float value, BladeAction.Combat.StatLimitRules rules)
        {
			if (rules.TryGetRange(key, out var min, out var max))
			{
				return Mathf.Clamp(value, min, max);
			}
            return value;
        }
        
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

