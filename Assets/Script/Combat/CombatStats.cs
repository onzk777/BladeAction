using UnityEngine;

namespace BladeAction.Combat
{
    [System.Serializable]
    public struct CombatStats
    {
        // ===== 최대치 =====
        [Header("최대치")]
        public float maxHP;
        public float maxPoise;
        
        // ===== 현재값 (런타임) =====
        [Header("현재값 (런타임)")]
        public float currentHP;
        public float currentPoise;
        
        // ===== 공격 관련 =====
        [Header("공격")]
        public float attack;
        public float critChance;              // 0~1
        public float critMultiplier;          // 예: 1.5
        
        // ===== 방어 관련 =====
        [Header("방어")]
        public float defenseDR;
        public int tempDRBonus;               // 임시 DR 보너스
        public float damageReduction;         // 0~1
        public float guardDamageReduction;    // 0~1 (막기 시 피해 감소)
        public int guardDRBonus;              // 막기 시 DR 보너스
        
        // ===== 막기 관련 =====
        [Header("막기")]
        public float blockEfficiency;         // 0~1
        public float blockPoiseConsumption;
        
        // ===== 쳐내기 관련 =====
        [Header("쳐내기")]
        public float parryEfficiency;         // 0~1
        public float parryPoiseConsumption;
        public float parryPoiseAttackPower;
        public float parryPoiseDamage;
        
        // ===== 기타 =====
        [Header("기타")]
        [Tooltip("Poise 회복률 (0~1, 1.0 = 100% 회복)")]
        [Range(0f, 1f)]
        public float poiseGain;

        public static CombatStats operator +(CombatStats a, CombatStats b)
        {
            return new CombatStats
            {
                // 최대치는 합산
                maxHP = a.maxHP + b.maxHP,
                maxPoise = a.maxPoise + b.maxPoise,
                
                // 현재값은 a 유지 (장비 합산 시 현재값은 변경하지 않음)
                currentHP = a.currentHP,
                currentPoise = a.currentPoise,
                
                // 공격 관련 합산
                attack = a.attack + b.attack,
                critChance = a.critChance + b.critChance,
                critMultiplier = a.critMultiplier + b.critMultiplier - 1f, // 1 기준 가산
                
                // 방어 관련 합산
                defenseDR = a.defenseDR + b.defenseDR,
                tempDRBonus = a.tempDRBonus + b.tempDRBonus,
                damageReduction = a.damageReduction + b.damageReduction,
                guardDamageReduction = a.guardDamageReduction + b.guardDamageReduction,
                guardDRBonus = a.guardDRBonus + b.guardDRBonus,
                
                // 막기 관련 합산
                blockEfficiency = a.blockEfficiency + b.blockEfficiency,
                blockPoiseConsumption = a.blockPoiseConsumption + b.blockPoiseConsumption,
                
                // 쳐내기 관련 합산
                parryEfficiency = a.parryEfficiency + b.parryEfficiency,
                parryPoiseConsumption = a.parryPoiseConsumption + b.parryPoiseConsumption,
                parryPoiseAttackPower = a.parryPoiseAttackPower + b.parryPoiseAttackPower,
                parryPoiseDamage = a.parryPoiseDamage + b.parryPoiseDamage,
                
                // 기타 합산
                poiseGain = a.poiseGain + b.poiseGain
            };
        }
    }
}


