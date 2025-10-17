using UnityEngine;

namespace BladeAction.Item
{
    [System.Serializable]
    public class EquipmentStats
    {
        [Header("공격 관련")]
        [Tooltip("공격력")]
        public float attackPower = 0f;
        
        [Header("막기 관련")]
        [Tooltip("막기 효율 (%)")]
        [Range(0f, 100f)]
        public float blockEfficiency = 0f;
        
        [Tooltip("막기 Poise 소모량")]
        public float blockPoiseConsumption = 0f;
        
        [Header("쳐내기 관련")]
        [Tooltip("쳐내기 효율 (%)")]
        [Range(0f, 100f)]
        public float parryEfficiency = 0f;
        
        [Tooltip("쳐내기 Poise 소모량")]
        public float parryPoiseConsumption = 0f;
        
        [Tooltip("쳐내기 Poise 공격력")]
        public float parryPoiseAttackPower = 0f;
        
        [Header("생존 관련")]
        [Tooltip("HP 증가량")]
        public float maxHP = 0f;
        
        [Tooltip("피해 감소율 (%)")]
        [Range(0f, 100f)]
        public float damageReduction = 0f;
        
        [Tooltip("Poise 증가량")]
        public float poise = 0f;
        
        /// <summary>
        /// 두 스탯을 더한 결과 반환
        /// </summary>
        public static EquipmentStats operator +(EquipmentStats a, EquipmentStats b)
        {
            return new EquipmentStats
            {
                attackPower = a.attackPower + b.attackPower,
                blockEfficiency = a.blockEfficiency + b.blockEfficiency,
                blockPoiseConsumption = a.blockPoiseConsumption + b.blockPoiseConsumption,
                parryEfficiency = a.parryEfficiency + b.parryEfficiency,
                parryPoiseConsumption = a.parryPoiseConsumption + b.parryPoiseConsumption,
                parryPoiseAttackPower = a.parryPoiseAttackPower + b.parryPoiseAttackPower,
                maxHP = a.maxHP + b.maxHP,
                damageReduction = a.damageReduction + b.damageReduction,
                poise = a.poise + b.poise
            };
        }
    }
}

