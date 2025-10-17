using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BladeAction.Item
{
    /// <summary>
    /// 전투원의 인벤토리 시스템
    /// 아이템 소유, 장비 관리, 이벤트 통신
    /// </summary>
    [System.Serializable]
    public class CombatantInventory
    {
        [Header("인벤토리 설정")]
        [Tooltip("최대 아이템 슬롯 수")]
        public int maxItemSlots = 5000;
        
        [Tooltip("인벤토리 이름")]
        public string inventoryName = "Player Inventory";
        
        [Header("아이템 저장소")]
        [Tooltip("소유한 모든 아이템")]
        public List<OwnedItem> items = new List<OwnedItem>();
        
        [Header("장비 슬롯")]
        [Tooltip("장비 슬롯들")]
        public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();
        
        [Header("상태")]
        [Tooltip("인벤토리가 잠겨있는지")]
        public bool isLocked = false;
        
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public CombatantInventory()
        {
            InitializeDefaultEquipmentSlots();
        }
        
        /// <summary>
        /// 기본 장비 슬롯 초기화
        /// </summary>
        private void InitializeDefaultEquipmentSlots()
        {
            equipmentSlots.Clear();
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Weapon, "주무기"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Armor, "갑옷"));
            
            // 장신구 슬롯 5개
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구1"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구2"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구3"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구4"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구5"));
            
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.SwordArtStyle, "검술 유파"));
        }
        
        #region 아이템 관리
        
        /// <summary>
        /// 아이템 추가
        /// </summary>
        public bool AddItem(string itemKey, int quantity = 1)
        {
            if (isLocked || string.IsNullOrEmpty(itemKey) || quantity <= 0)
                return false;
                
            var itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
            var itemData = itemDatabase?.GetItem(itemKey);
            if (itemData == null)
                return false;
            
            // 기존 아이템이 있는지 확인
            var existingItem = items.FirstOrDefault(i => i.itemKey == itemKey);
            if (existingItem != null)
            {
                // 기존 아이템에 수량 추가
                existingItem.UpdateMaxQuantity();
                bool success = existingItem.AddQuantity(quantity);
                if (success)
                {
                    ItemEvents.Instance?.TriggerItemQuantityChanged(itemKey, existingItem.quantity, inventoryName);
                }
                return success;
            }
            else
            {
                // 새 아이템 추가
                if (items.Count >= maxItemSlots)
                {
                    ItemEvents.Instance?.TriggerInventoryFull(itemKey, inventoryName);
                    return false; // 인벤토리 가득참
                }
                    
                var newItem = new OwnedItem(itemKey, quantity);
                newItem.UpdateMaxQuantity();
                items.Add(newItem);
                ItemEvents.Instance?.TriggerItemAdded(itemKey, quantity, inventoryName);
                return true;
            }
        }
        
        /// <summary>
        /// 아이템 제거
        /// </summary>
        public bool RemoveItem(string itemKey, int quantity = 1)
        {
            if (isLocked || string.IsNullOrEmpty(itemKey) || quantity <= 0)
                return false;
                
            var item = items.FirstOrDefault(i => i.itemKey == itemKey);
            if (item == null)
                return false;
                
            if (item.RemoveQuantity(quantity))
            {
                if (item.IsEmpty())
                {
                    items.Remove(item);
                    ItemEvents.Instance?.TriggerItemRemoved(itemKey, quantity, inventoryName);
                }
                else
                {
                    ItemEvents.Instance?.TriggerItemQuantityChanged(itemKey, item.quantity, inventoryName);
                }
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 아이템 수량 확인
        /// </summary>
        public int GetItemQuantity(string itemKey)
        {
            var item = items.FirstOrDefault(i => i.itemKey == itemKey);
            return item?.quantity ?? 0;
        }
        
        /// <summary>
        /// 아이템 보유 여부 확인
        /// </summary>
        public bool HasItem(string itemKey, int quantity = 1)
        {
            return GetItemQuantity(itemKey) >= quantity;
        }
        
        /// <summary>
        /// 아이템 검색
        /// </summary>
        public List<OwnedItem> FindItems(System.Func<OwnedItem, bool> predicate)
        {
            return items.Where(predicate).ToList();
        }
        
        /// <summary>
        /// 아이템 타입별 검색
        /// </summary>
        public List<OwnedItem> GetItemsByType(ItemType itemType)
        {
            return items.Where(item => 
            {
                var itemData = item.GetItemData();
                return itemData?.itemType == itemType;
            }).ToList();
        }
        
        #endregion
        
        #region 장비 관리
        
        /// <summary>
        /// 아이템 장착
        /// </summary>
        public bool EquipItem(string itemKey, EquipmentSlotType slotType)
        {
            if (isLocked || string.IsNullOrEmpty(itemKey))
                return false;
                
            var slot = equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
            if (slot == null || !slot.CanEquipItem(itemKey))
                return false;
                
            // 기존 장착된 아이템 해제
            if (!slot.IsEmpty())
            {
                string unequippedKey = slot.UnequipItem();
                // 해제된 아이템을 인벤토리에 다시 추가
                AddItem(unequippedKey, 1);
                ItemEvents.Instance?.TriggerItemUnequipped(unequippedKey, slotType, inventoryName);
            }
            
            // 새 아이템 장착
            if (slot.EquipItem(itemKey, 1))
            {
                // 인벤토리에서 아이템 제거 (이벤트는 RemoveItem에서 발생)
                if (RemoveItem(itemKey, 1))
                {
                    ItemEvents.Instance?.TriggerItemEquipped(itemKey, slotType, inventoryName);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 아이템 해제
        /// </summary>
        public bool UnequipItem(EquipmentSlotType slotType)
        {
            if (isLocked)
                return false;
                
            var slot = equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
            if (slot == null || slot.IsEmpty())
                return false;
                
            string unequippedKey = slot.UnequipItem();
            if (!string.IsNullOrEmpty(unequippedKey))
            {
                // 해제된 아이템을 인벤토리에 다시 추가
                bool success = AddItem(unequippedKey, 1);
                if (success)
                {
                    ItemEvents.Instance?.TriggerItemUnequipped(unequippedKey, slotType, inventoryName);
                }
                return success;
            }
            
            return false;
        }
        
        /// <summary>
        /// 장착된 아이템 가져오기
        /// </summary>
        public Item GetEquippedItem(EquipmentSlotType slotType)
        {
            var slot = equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
            return slot?.GetEquippedItem();
        }
        
        /// <summary>
        /// 모든 장착된 아이템 가져오기
        /// </summary>
        public List<Item> GetAllEquippedItems()
        {
            return equipmentSlots
                .Where(slot => !slot.IsEmpty())
                .Select(slot => slot.GetEquippedItem())
                .Where(item => item != null)
                .ToList();
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 인벤토리 정리 (빈 슬롯 제거)
        /// </summary>
        public void CleanupInventory()
        {
            items.RemoveAll(item => item.IsEmpty());
        }
        
        /// <summary>
        /// 인벤토리 비우기
        /// </summary>
        public void ClearInventory()
        {
            if (isLocked) return;
            
            items.Clear();
            foreach (var slot in equipmentSlots)
            {
                slot.UnequipItem();
            }
            
            ItemEvents.Instance?.TriggerInventoryCleared(inventoryName);
        }
        
        /// <summary>
        /// 인벤토리 상태 확인
        /// </summary>
        public bool IsFull()
        {
            return items.Count >= maxItemSlots;
        }
        
        /// <summary>
        /// 사용 가능한 슬롯 수
        /// </summary>
        public int GetAvailableSlots()
        {
            return maxItemSlots - items.Count;
        }
        
        /// <summary>
        /// 디버그용 정보
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Inventory: {items.Count}/{maxItemSlots} slots used\n" +
                   $"Equipment: {equipmentSlots.Count(s => !s.IsEmpty())}/{equipmentSlots.Count} slots equipped";
        }
        
        #endregion
    }
}
