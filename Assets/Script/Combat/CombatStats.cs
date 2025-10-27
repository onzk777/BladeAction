using UnityEngine;

namespace BladeAction.Combat
{
    [System.Serializable]
    public struct CombatStats
    {
        // 정수형(내부 계산은 float, 커밋 시 반올림)
        public float attack;
        public float defenseDR;
        public float maxHP;
        public float maxPoise;
        public float parryPoiseDamage;
        public float blockPoiseConsumption;
        public float parryPoiseConsumption;
        public float parryPoiseAttackPower;
        public float poiseGain;

        // 비율(0~1)
        public float critChance;              // 0~1
        public float guardDamageReduction;    // 0~1
        public float damageReduction;         // 0~1
        public float blockEfficiency;         // 0~1
        public float parryEfficiency;         // 0~1

        // 배율(multiplier)
        public float critMultiplier;          // 예: 1.5

        public static CombatStats operator +(CombatStats a, CombatStats b)
        {
            return new CombatStats
            {
                attack = a.attack + b.attack,
                defenseDR = a.defenseDR + b.defenseDR,
                maxHP = a.maxHP + b.maxHP,
                maxPoise = a.maxPoise + b.maxPoise,
                parryPoiseDamage = a.parryPoiseDamage + b.parryPoiseDamage,
                blockPoiseConsumption = a.blockPoiseConsumption + b.blockPoiseConsumption,
                parryPoiseConsumption = a.parryPoiseConsumption + b.parryPoiseConsumption,
                parryPoiseAttackPower = a.parryPoiseAttackPower + b.parryPoiseAttackPower,
                poiseGain = a.poiseGain + b.poiseGain,

                critChance = a.critChance + b.critChance,
                guardDamageReduction = a.guardDamageReduction + b.guardDamageReduction,
                damageReduction = a.damageReduction + b.damageReduction,
                blockEfficiency = a.blockEfficiency + b.blockEfficiency,
                parryEfficiency = a.parryEfficiency + b.parryEfficiency,

                // multiplier는 곱 연산이 일반적이지만, 첫 단계에선 합산 후 별도 룰로 Clamp
                critMultiplier = a.critMultiplier + b.critMultiplier - 1f // 1을 기준으로 가산되도록 보정
            };
        }
    }
}


