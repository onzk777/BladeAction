using UnityEngine;

namespace BladeAction.Item.Excel
{
    /// <summary>
    /// CSV 데이터를 StatTable로 매핑
    /// </summary>
    public static class StatTableMapper
    {
        /// <summary>
        /// CSV 데이터를 StatTable로 변환
        /// </summary>
        public static StatTable MapCSVToStatTable(StatTableCSVData csvData)
        {
            if (csvData == null)
            {
                Debug.LogWarning("CSV 데이터가 null입니다.");
                return null;
            }
            
            if (string.IsNullOrEmpty(csvData.TableKey))
            {
                Debug.LogWarning("TableKey가 비어있습니다.");
                return null;
            }
            
            var statTable = new StatTable
            {
                tableKey = csvData.TableKey,
                description = csvData.Description,
                stats = new EquipmentStats
                {
                    // 공격 관련
                    attackPower = csvData.AttackPower,
                    
                    // 막기 관련
                    blockEfficiency = csvData.BlockEff,
                    blockPoiseConsumption = csvData.BlockPoiseCost,
                    
                    // 쳐내기 관련
                    parryEfficiency = csvData.ParryEff,
                    parryPoiseConsumption = csvData.ParryPoiseCost,
                    parryPoiseAttackPower = csvData.ParryPoiseAtk,
                    
                    // 생존 관련
                    maxHP = csvData.MaxHP,
                    damageReduction = csvData.DamageReduction,
                    poise = csvData.Poise
                }
            };
            
            return statTable;
        }
        
        /// <summary>
        /// 기존 StatTable 업데이트
        /// </summary>
        public static void UpdateStatTable(StatTable existing, StatTableCSVData csvData)
        {
            if (existing == null || csvData == null)
                return;
            
            existing.description = csvData.Description;
            existing.stats.attackPower = csvData.AttackPower;
            existing.stats.blockEfficiency = csvData.BlockEff;
            existing.stats.blockPoiseConsumption = csvData.BlockPoiseCost;
            existing.stats.parryEfficiency = csvData.ParryEff;
            existing.stats.parryPoiseConsumption = csvData.ParryPoiseCost;
            existing.stats.parryPoiseAttackPower = csvData.ParryPoiseAtk;
            existing.stats.maxHP = csvData.MaxHP;
            existing.stats.damageReduction = csvData.DamageReduction;
            existing.stats.poise = csvData.Poise;
        }
    }
}

