using UnityEngine;
using BladeAction.Item;
using BladeAction.Combat;

namespace BladeAction.Test
{
    /// <summary>
    /// [DEPRECATED] 전투 시작 시 전투원에게 임시 장비를 장착시켜 StatsCalculationManager 경로로 합산/Clamp 되는지 검증하는 유틸
    /// 
    /// 현재는 Combatant.Inventory를 직접 사용하므로 이 클래스는 더 이상 필요하지 않습니다.
    /// Character 생성 시 Inventory를 직접 할당하고, EquipItem을 호출하면 자동으로 스탯이 재계산됩니다.
    /// 
    /// - 씬에 배치하고, 플레이어/적 인벤토리를 인스펙터에 할당 후 테스트
    /// - 로그로 유효 ATK 전/후를 출력
    /// </summary>
    [System.Obsolete("Use Combatant.Inventory directly instead")]
    public class TemporaryEquipmentApplier : MonoBehaviour
    {
        [Header("Inventory (임시)")]
        public CharacterInventory playerInventory;
        public CharacterInventory enemyInventory;

        [Header("장착할 아이템 키(쉼표 구분)")]
        public string playerEquipKeysCsv;
        public string enemyEquipKeysCsv;

        private void Start()
        {
            // Combatant.Inventory를 직접 할당
            if (CombatCharacterManager.Instance?.PlayerCharacter != null)
            {
                CombatCharacterManager.Instance.PlayerCharacter.Inventory = playerInventory;
                if (playerInventory != null)
                    playerInventory.Owner = CombatCharacterManager.Instance.PlayerCharacter;
            }
            
            if (CombatCharacterManager.Instance?.CurrentEnemy != null)
            {
                CombatCharacterManager.Instance.CurrentEnemy.Inventory = enemyInventory;
                if (enemyInventory != null)
                    enemyInventory.Owner = CombatCharacterManager.Instance.CurrentEnemy;
            }

            TryEquipFor(CombatCharacterManager.Instance?.PlayerCharacter, playerInventory, playerEquipKeysCsv, isPlayer:true);
            TryEquipFor(CombatCharacterManager.Instance?.CurrentEnemy, enemyInventory, enemyEquipKeysCsv, isPlayer:false);
        }

        private void TryEquipFor(Character combatant, CharacterInventory inventory, string csv, bool isPlayer)
        {
            if (combatant == null || inventory == null) return;

            int beforeAtk = StatsCalculationManager.Instance != null
                ? StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;

            if (!string.IsNullOrEmpty(csv))
            {
                var keys = csv.Split(',');
                foreach (var raw in keys)
                {
                    var key = raw.Trim();
                    if (string.IsNullOrEmpty(key)) continue;

                    // 우선 인벤토리에 추가 후 장착 슬롯에 맞춰 장착 시도
                    inventory.AddItem(key, 1);

                    // 간단 매핑: 무기/갑옷/장신구 우선순으로 슬롯 시도
                    var itemData = ItemDatabase.GetItemSafe(key);
                    if (itemData == null) continue;

                    EquipmentSlotType slot = EquipmentSlotType.None;
                    switch (itemData.itemType)
                    {
                        case ItemType.Weapon: slot = EquipmentSlotType.Weapon; break;
                        case ItemType.Armor: slot = EquipmentSlotType.Armor; break;
                        case ItemType.Accessory: slot = EquipmentSlotType.Accessory; break;
                        case ItemType.SwordArtStyle: slot = EquipmentSlotType.SwordArtStyle; break;
                    }

                    if (slot != EquipmentSlotType.None)
                        inventory.EquipItem(key, slot);
                }
            }

            int afterAtk = StatsCalculationManager.Instance != null
                ? StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;

            Debug.Log($"[TemporaryEquipmentApplier] {(isPlayer ? "Player" : "Enemy")} ATK: {beforeAtk} -> {afterAtk}");
        }
    }
}


