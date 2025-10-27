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

        public CombatStats GetEffectiveStats(Combatant combatant)
        {
            if (combatant == null || combatant.CharacterData == null)
                return default;

            var baseStats = MapCharacterDataToCombatStats(combatant.CharacterData);
            var equipDelta = MapEquipmentsToDelta(combatant);
            var raw = baseStats + equipDelta;
            var clamped = StatLimiter.ClampAll(raw, statLimitRules);
            return clamped;
        }

        public int GetEffectiveATK(Combatant combatant)
        {
            var stats = GetEffectiveStats(combatant);
            return Mathf.RoundToInt(stats.attack);
        }

        private CombatStats MapCharacterDataToCombatStats(CharacterData cd)
        {
            CombatStats s = new CombatStats();
            s.attack = cd.ATK;
            s.defenseDR = cd.DR;
            s.maxHP = cd.MaxHP;
            s.maxPoise = cd.MaxPoise;
            s.parryPoiseDamage = cd.ParryPoiseDamage;
            s.critChance = cd.CritChance;              // 0~1
            s.critMultiplier = cd.CritMultiplier;      // multiplier
            s.guardDamageReduction = cd.guardDamageReduction; // 0~1
            // 나머지는 0 기본
            return s;
        }

        private CombatStats MapEquipmentsToDelta(Combatant combatant)
        {
            CombatStats delta = new CombatStats();

            // CombatantInventory 연결 지점은 UI 쪽에서만 보유 중. CharacterManager 등에서 접근 가능하도록 여유 구현이 없으므로, 
            // 우선 장비 합산은 ItemDatabase/StatDatabase를 통해 ItemDetailPanel 경로가 아닌, Combatant가 보유한 인벤토리 참조가 필요.
            // 현재 구조에서는 테스트를 위해 CharacterManager에서 플레이어/적 컨트롤러가 가진 인벤토리를 노출하고 있다면 그 참조를 사용해야 한다.
            // 본 1차 구현에서는 합산 로직만 정의하고, 실제 인벤토리 소스 연결은 추후 Combatant에 주입하는 단계에서 마무리한다.

            var inventoryProvider = Object.FindFirstObjectByType<InventoryProvider>();
            CombatantInventory inv = inventoryProvider != null ? inventoryProvider.GetInventoryFor(combatant) : null;
            if (inv == null) return delta;

            var equippedItems = inv.GetAllEquippedItems();
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

    /// <summary>
    /// Combatant별 인벤토리를 제공하기 위한 간단한 훅(임시).
    /// 실제 프로젝트 구조에 맞게 Combatant가 직접 보유/주입받도록 교체 예정.
    /// </summary>
    public class InventoryProvider : MonoBehaviour
    {
        public BladeAction.Item.CombatantInventory playerInventory;
        public BladeAction.Item.CombatantInventory enemyInventory;

        public BladeAction.Item.CombatantInventory GetInventoryFor(Combatant combatant)
        {
            // 단순 매핑(이후 Combatant 참조 기반으로 개선)
            if (CharacterManager.Instance != null)
            {
                if (combatant == CharacterManager.Instance.PlayerCombatant) return playerInventory;
                if (combatant == CharacterManager.Instance.EnemyCombatant) return enemyInventory;
            }
            return null;
        }
    }
}


