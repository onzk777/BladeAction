using UnityEngine;

namespace BladeAction.Combat
{
    public static class StatLimiter
    {
        public static float Clamp(string statKey, float value, StatLimitRules rules)
        {
            if (rules == null) return value;
            if (rules.TryGetRange(statKey, out var min, out var max))
                return Mathf.Clamp(value, min, max);
            return value;
        }

        public static CombatStats ClampAll(in CombatStats src, StatLimitRules rules)
        {
            if (rules == null) return src;
            CombatStats dst = src;

            dst.attack = Clamp("attack", src.attack, rules);
            dst.defenseDR = Clamp("defenseDR", src.defenseDR, rules);
            dst.maxHP = Clamp("maxHP", src.maxHP, rules);
            dst.maxPoise = Clamp("maxPoise", src.maxPoise, rules);
            dst.parryPoiseDamage = Clamp("parryPoiseDamage", src.parryPoiseDamage, rules);
            dst.blockPoiseConsumption = Clamp("blockPoiseConsumption", src.blockPoiseConsumption, rules);
            dst.parryPoiseConsumption = Clamp("parryPoiseConsumption", src.parryPoiseConsumption, rules);
            dst.parryPoiseAttackPower = Clamp("parryPoiseAttackPower", src.parryPoiseAttackPower, rules);
            dst.poiseGain = Clamp("poiseGain", src.poiseGain, rules);

            dst.critChance = Clamp("critChance", src.critChance, rules);
            dst.guardDamageReduction = Clamp("guardDamageReduction", src.guardDamageReduction, rules);
            dst.damageReduction = Clamp("damageReduction", src.damageReduction, rules);
            dst.blockEfficiency = Clamp("blockEfficiency", src.blockEfficiency, rules);
            dst.parryEfficiency = Clamp("parryEfficiency", src.parryEfficiency, rules);

            dst.critMultiplier = Clamp("critMultiplier", src.critMultiplier, rules);

            // HP 특례는 매니저 커밋 시점에서 MaxHP 선행 후 HP Clamp 처리
            return dst;
        }
    }
}


