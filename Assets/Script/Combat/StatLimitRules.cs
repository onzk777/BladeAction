using UnityEngine;

namespace BladeAction.Combat
{
    [System.Serializable]
    public struct MinMax
    {
        public float min;
        public float max;
    }

    [CreateAssetMenu(fileName = "StatLimitRules", menuName = "Combat/Stat Limit Rules", order = 1)]
    public class StatLimitRules : ScriptableObject
    {
        // CombatStats와 1:1 매핑되는 범위 정의
        public MinMax attack;
        public MinMax defenseDR;
        public MinMax maxHP;
        public MinMax maxPoise;
        public MinMax parryPoiseDamage;
        public MinMax blockPoiseConsumption;
        public MinMax parryPoiseConsumption;
        public MinMax parryPoiseAttackPower;
        public MinMax poiseGain;

        public MinMax critChance;              // 0~1
        public MinMax guardDamageReduction;    // 0~1
        public MinMax damageReduction;         // 0~1
        public MinMax blockEfficiency;         // 0~1
        public MinMax parryEfficiency;         // 0~1

        public MinMax critMultiplier;          // multiplier

        public bool TryGetRange(string statKey, out float min, out float max)
        {
            // 키명은 CombatStats/필드명과 동일하게 사용
            switch (statKey)
            {
                case "attack": min = attack.min; max = attack.max; return true;
                case "defenseDR": min = defenseDR.min; max = defenseDR.max; return true;
                case "maxHP": min = maxHP.min; max = maxHP.max; return true;
                case "maxPoise": min = maxPoise.min; max = maxPoise.max; return true;
                case "parryPoiseDamage": min = parryPoiseDamage.min; max = parryPoiseDamage.max; return true;
                case "blockPoiseConsumption": min = blockPoiseConsumption.min; max = blockPoiseConsumption.max; return true;
                case "parryPoiseConsumption": min = parryPoiseConsumption.min; max = parryPoiseConsumption.max; return true;
                case "parryPoiseAttackPower": min = parryPoiseAttackPower.min; max = parryPoiseAttackPower.max; return true;
                case "poiseGain": min = poiseGain.min; max = poiseGain.max; return true;

                case "critChance": min = critChance.min; max = critChance.max; return true;
                case "guardDamageReduction": min = guardDamageReduction.min; max = guardDamageReduction.max; return true;
                case "damageReduction": min = damageReduction.min; max = damageReduction.max; return true;
                case "blockEfficiency": min = blockEfficiency.min; max = blockEfficiency.max; return true;
                case "parryEfficiency": min = parryEfficiency.min; max = parryEfficiency.max; return true;

                case "critMultiplier": min = critMultiplier.min; max = critMultiplier.max; return true;
            }
            min = float.NegativeInfinity; max = float.PositiveInfinity; return false;
        }
    }
}


