using System.Linq;
using UnityEngine;
using BladeAction.Item;

namespace BladeAction.Combat
{
    public sealed class StatsCalculationManager : MonoBehaviour
    {
        public static StatsCalculationManager Instance { get; private set; }

        [Header("Rules")]
        [Tooltip("스탯 제한 규칙 에셋 (없으면 Clamp 미적용)")]
        public StatLimitRules statLimitRules;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (statLimitRules == null)
            {
                // 선택: Resources 경로에서 자동 로드 시도
                statLimitRules = Resources.Load<StatLimitRules>("Data/Stat/StatLimitRules");
            }
        }

        /// <summary>
        /// Character의 최종 스탯을 계산하고 반환합니다 (Base + Equipment → Clamp)
        /// </summary>
        public CombatStats GetFinalStats(Character character)
        {
            if (character == null || character.CharacterInitData == null)
                return default;

            var baseStats = ConvertToCombatStats(character.CharacterInitData);
            var equipDelta = CalculateEquipmentDelta(character);
            var raw = baseStats + equipDelta;
            var clamped = StatLimiter.ClampAll(raw, statLimitRules);
            return clamped;
        }

        /// <summary>
        /// Character의 최종 공격력을 반환합니다
        /// </summary>
        public int GetFinalATK(Character character)
        {
            var stats = GetFinalStats(character);
            return Mathf.RoundToInt(stats.attack);
        }

        /// <summary>
        /// Character의 특정 스탯을 키로 조회합니다
        /// </summary>
        public float GetFinalStat(Character character, string statKey)
        {
            var stats = GetFinalStats(character);
            return GetStatByKey(stats, statKey);
        }

        /// <summary>
        /// 모든 스탯을 재계산하고 Character에 커밋합니다
        /// MaxHP 선반영 후 HP를 [0..MaxHP] 보정 (비율 보존)
        /// </summary>
        public CombatStats RecalculateAndCommit(Character character)
        {
            if (character == null)
            {
                Debug.LogWarning("[StatsCalculationManager] RecalculateAndCommit: Character is null");
                return default;
            }

            var finalStats = GetFinalStats(character);

            // 현재 HP/Poise 비율 저장
            float oldMaxHP = character.stats.maxHP;
            float oldMaxPoise = character.stats.maxPoise;
            float hpRatio = oldMaxHP > 0 ? character.stats.currentHP / oldMaxHP : 1f;
            float poiseRatio = oldMaxPoise > 0 ? character.stats.currentPoise / oldMaxPoise : 1f;

            // finalStats를 character.stats에 커밋
            character.stats = finalStats;

            // HP/Poise 비율 보존
            character.stats.currentHP = Mathf.Clamp(character.stats.maxHP * hpRatio, 0, character.stats.maxHP);
            character.stats.currentPoise = Mathf.Clamp(character.stats.maxPoise * poiseRatio, 0, character.stats.maxPoise);

            // 스탯 변경 이벤트 발행
            character.NotifyStatsChanged();

            Debug.Log($"[StatsCalculationManager] {character.Name} 스탯 재계산 완료 - " +
                      $"ATK:{finalStats.attack:F0}, MaxHP:{finalStats.maxHP:F0}, HP:{character.stats.currentHP:F0}, MaxPoise:{finalStats.maxPoise:F0}");

            return finalStats;
        }

        /// <summary>
        /// CombatStats에서 키로 스탯 값 조회
        /// </summary>
        private float GetStatByKey(CombatStats stats, string key)
        {
            switch (key)
            {
                case "attack": return stats.attack;
                case "maxHP": return stats.maxHP;
                case "maxPoise": return stats.maxPoise;
                case "critChance": return stats.critChance;
                case "critMultiplier": return stats.critMultiplier;
                case "damageReduction": return stats.damageReduction;
                case "blockEfficiency": return stats.blockEfficiency;
                case "parryEfficiency": return stats.parryEfficiency;
                case "blockPoiseConsumption": return stats.blockPoiseConsumption;
                case "parryPoiseConsumption": return stats.parryPoiseConsumption;
                case "parryPoiseAttackPower": return stats.parryPoiseAttackPower;
                case "guardDamageReduction": return stats.guardDamageReduction;
                case "defenseDR": return stats.defenseDR;
                case "parryPoiseDamage": return stats.parryPoiseDamage;
                case "poiseGain": return stats.poiseGain;
                default:
                    Debug.LogWarning($"[StatsCalculationManager] Unknown stat key: {key}");
                    return 0f;
            }
        }

        private CombatStats ConvertToCombatStats(CharacterInitData data)
        {
            // CharacterInitData의 baseStats를 그대로 복사 (구조체이므로 값 복사)
            return data.baseStats;
        }

        private CombatStats CalculateEquipmentDelta(Character character)
        {
            CombatStats delta = new CombatStats();

            // Character가 직접 보유한 인벤토리 참조
            if (character.Inventory == null)
                return delta;

            var equippedItems = character.Inventory.GetAllEquippedItems();
            foreach (var item in equippedItems)
            {
                if (item == null) continue;
                var stats = item.GetStats(ItemDatabase.Instance?.statDatabase);
                if (stats == null) continue;

                // EquipmentStats -> CombatStats delta (%, 배율 변환 포함)
                CombatStats d = new CombatStats();
                d.attack = stats.attackPower;
                d.maxHP = stats.maxHP;
                // EquipmentStats는 인스펙터에서 StatLimitRules를 통해 0~1로 관리됨
                d.damageReduction = Mathf.Clamp01(stats.damageReduction);
                d.blockEfficiency = Mathf.Clamp01(stats.blockEfficiency);
                d.parryEfficiency = Mathf.Clamp01(stats.parryEfficiency);
                d.maxPoise = stats.poise;
                d.blockPoiseConsumption = stats.blockPoiseConsumption;
                d.parryPoiseConsumption = stats.parryPoiseConsumption;
                d.parryPoiseAttackPower = stats.parryPoiseAttackPower;

                delta = delta + d;
            }

            return delta;
        }
    }
}


