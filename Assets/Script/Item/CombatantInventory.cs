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
        /// 총 6개 슬롯: 무기 1, 갑옷 1, 장신구 3, 검술 유파 1
        /// </summary>
        private void InitializeDefaultEquipmentSlots()
        {
            equipmentSlots.Clear();
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Weapon, "주무기"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Armor, "갑옷"));
            
            // 장신구 슬롯 3개
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구1"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구2"));
            equipmentSlots.Add(new EquipmentSlot(EquipmentSlotType.Accessory, "장신구3"));
            
            // 검술 유파 슬롯
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
                
            var itemData = ItemDatabase.GetItemSafe(itemKey);
            if (itemData == null)
                return false;
            
            // 기존 아이템이 있는지 확인
            var existingItem = items.FirstOrDefault(i => i.itemKey == itemKey);
            if (existingItem != null)
            {
                // 기존 아이템에 수량 추가 시도
                existingItem.UpdateMaxQuantity();
                int addedQuantity = Mathf.Min(quantity, existingItem.maxQuantity - existingItem.quantity);
                
                if (addedQuantity > 0)
                {
                    existingItem.quantity += addedQuantity;
                    SafeTriggerEvent(events => events.TriggerItemQuantityChanged(itemKey, existingItem.quantity, inventoryName));
                    quantity -= addedQuantity; // 남은 수량
                }
                
                // 남은 수량이 있으면 새로운 슬롯 생성
                if (quantity > 0)
                {
                    return AddItemToNewSlot(itemKey, quantity);
                }
                
                return true;
            }
            else
            {
                // 새 아이템 추가
                return AddItemToNewSlot(itemKey, quantity);
            }
        }
        
        /// <summary>
        /// 새로운 슬롯에 아이템 추가 (다른 같은 아이템 슬롯들도 고려)
        /// </summary>
        private bool AddItemToNewSlot(string itemKey, int quantity)
        {
            if (items.Count >= maxItemSlots)
            {
                SafeTriggerEvent(events => events.TriggerInventoryFull(itemKey, inventoryName));
                return false; // 인벤토리 가득참
            }
            
            var itemData = ItemDatabase.GetItemSafe(itemKey);
            if (itemData == null)
                return false;
            
            // 다른 같은 아이템 슬롯들 중에서 스택 가능한 슬롯 찾기
            var sameItems = items.Where(i => i.itemKey == itemKey && i.quantity < i.maxQuantity).ToList();
            
            foreach (var existingItem in sameItems)
            {
                if (quantity <= 0) break;
                
                existingItem.UpdateMaxQuantity();
                int canAdd = existingItem.maxQuantity - existingItem.quantity;
                int addAmount = Mathf.Min(quantity, canAdd);
                
                if (addAmount > 0)
                {
                    existingItem.quantity += addAmount;
                    quantity -= addAmount;
                    SafeTriggerEvent(events => events.TriggerItemQuantityChanged(itemKey, existingItem.quantity, inventoryName));
                }
            }
            
            // 남은 수량이 있으면 새로운 슬롯 생성
            if (quantity > 0)
            {
                var newItem = new OwnedItem(itemKey, quantity);
                newItem.UpdateMaxQuantity();
                
                // maxQuantity 업데이트 후 수량이 초과되었는지 확인 및 조정
                if (newItem.quantity > newItem.maxQuantity)
                {
                    newItem.quantity = newItem.maxQuantity;
                }
                
                items.Add(newItem);
                SafeTriggerEvent(events => events.TriggerItemAdded(itemKey, newItem.quantity, inventoryName));
                
                // 남은 수량이 있으면 재귀적으로 추가
                int remainingQuantity = quantity - newItem.quantity;
                if (remainingQuantity > 0)
                {
                    return AddItemToNewSlot(itemKey, remainingQuantity);
                }
            }
            
            return true;
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
                    SafeTriggerEvent(events => events.TriggerItemRemoved(itemKey, quantity, inventoryName));
                }
                else
                {
                    SafeTriggerEvent(events => events.TriggerItemQuantityChanged(itemKey, item.quantity, inventoryName));
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
                SafeTriggerEvent(events => events.TriggerItemUnequipped(unequippedKey, slotType, inventoryName));
            }
            
            // 새 아이템 장착
            if (slot.EquipItem(itemKey, 1))
            {
                // 인벤토리에서 아이템 제거 (이벤트는 RemoveItem에서 발생)
                if (RemoveItem(itemKey, 1))
                {
                    SafeTriggerEvent(events => events.TriggerItemEquipped(itemKey, slotType, inventoryName));
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
                    SafeTriggerEvent(events => events.TriggerItemUnequipped(unequippedKey, slotType, inventoryName));
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
        /// 안전한 이벤트 발생 (에디터 모드 고려)
        /// </summary>
        private void SafeTriggerEvent(System.Action<ItemEvents> eventAction)
        {
            try
            {
                // OnDestroy 등에서 호출 시 새 GameObject 생성 방지
                if (!ItemEvents.HasInstance)
                    return;
                
                var events = ItemEvents.Instance;
                if (events != null)
                {
                    eventAction?.Invoke(events);
                }
            }
            catch (System.Exception ex)
            {
                // 에디터 모드나 테스트에서 발생할 수 있는 오류를 조용히 처리
                if (Application.isEditor && !Application.isPlaying)
                {
                    // 테스트 중이므로 조용히 무시
                    return;
                }
                Debug.LogWarning($"[CombatantInventory] 이벤트 발생 실패: {ex.Message}");
            }
        }
        
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
            
            SafeTriggerEvent(events => events.TriggerInventoryCleared(inventoryName));
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
