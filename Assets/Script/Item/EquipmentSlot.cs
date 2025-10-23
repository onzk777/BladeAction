using UnityEngine;

namespace BladeAction.Item
{
    /// <summary>
    /// 장비 슬롯 타입 (어떤 종류의 장비가 들어가는지)
    /// 총 6개 슬롯: 무기 1, 갑옷 1, 장신구 3, 검술 유파 1
    /// </summary>
    public enum EquipmentSlotType
    {
        None = 0,
        Weapon = 1,      // 무기
        Armor = 2,       // 갑옷
        Accessory = 3,   // 장신구
        SwordArtStyle = 4 // 검술 유파
    }
    
    /// <summary>
    /// 장비 슬롯 (특정 타입의 장비를 장착하는 슬롯)
    /// </summary>
    [System.Serializable]
    public class EquipmentSlot
    {
        [Header("슬롯 정보")]
        [Tooltip("슬롯 타입 (어떤 종류의 장비가 들어가는지)")]
        public EquipmentSlotType slotType = EquipmentSlotType.None;
        
        [Tooltip("슬롯 이름 (UI 표시용)")]
        public string slotName = "";
        
        [Tooltip("슬롯이 활성화되어 있는지")]
        public bool isActive = true;
        
        [Header("장착된 아이템")]
        [Tooltip("현재 장착된 아이템 키")]
        public string equippedItemKey = "";
        
        [Tooltip("장착된 아이템의 수량")]
        public int equippedQuantity = 0;
        
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public EquipmentSlot()
        {
            slotType = EquipmentSlotType.None;
            slotName = "Empty Slot";
            isActive = true;
            equippedItemKey = "";
            equippedQuantity = 0;
        }
        
        /// <summary>
        /// 타입으로 생성
        /// </summary>
        public EquipmentSlot(EquipmentSlotType slotType, string slotName = "")
        {
            this.slotType = slotType;
            this.slotName = string.IsNullOrEmpty(slotName) ? GetDefaultSlotName(slotType) : slotName;
            this.isActive = true;
            this.equippedItemKey = "";
            this.equippedQuantity = 0;
        }
        
        /// <summary>
        /// 장착된 아이템 데이터 가져오기
        /// </summary>
        public Item GetEquippedItem()
        {
            if (string.IsNullOrEmpty(equippedItemKey))
                return null;
                
            return ItemDatabase.GetItemSafe(equippedItemKey);
        }
        
        /// <summary>
        /// 아이템 장착
        /// </summary>
        public bool EquipItem(string itemKey, int quantity = 1)
        {
            if (!CanEquipItem(itemKey))
                return false;
                
            equippedItemKey = itemKey;
            equippedQuantity = quantity;
            return true;
        }
        
        /// <summary>
        /// 아이템 해제
        /// </summary>
        public string UnequipItem()
        {
            string unequippedKey = equippedItemKey;
            equippedItemKey = "";
            equippedQuantity = 0;
            return unequippedKey;
        }
        
        /// <summary>
        /// 아이템을 장착할 수 있는지 확인
        /// </summary>
        public bool CanEquipItem(string itemKey)
        {
            if (!isActive || string.IsNullOrEmpty(itemKey))
                return false;
                
            var item = ItemDatabase.GetItemSafe(itemKey);
            if (item == null)
                return false;
                
            // 아이템 타입이 슬롯 타입과 일치하는지 확인
            return GetItemTypeForSlot(slotType) == item.itemType;
        }
        
        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(equippedItemKey) || equippedQuantity <= 0;
        }
        
        /// <summary>
        /// 슬롯이 사용 가능한지 확인
        /// </summary>
        public bool IsAvailable()
        {
            return isActive && slotType != EquipmentSlotType.None;
        }
        
        /// <summary>
        /// 슬롯 타입에 따른 기본 이름 반환
        /// </summary>
        private string GetDefaultSlotName(EquipmentSlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentSlotType.Weapon:
                    return "무기";
                case EquipmentSlotType.Armor:
                    return "갑옷";
                case EquipmentSlotType.Accessory:
                    return "장신구";
                case EquipmentSlotType.SwordArtStyle:
                    return "검술 유파";
                default:
                    return "Empty Slot";
            }
        }
        
        /// <summary>
        /// 슬롯 타입에 따른 아이템 타입 반환
        /// </summary>
        private ItemType GetItemTypeForSlot(EquipmentSlotType slotType)
        {
            switch (slotType)
            {
                case EquipmentSlotType.Weapon:
                    return ItemType.Weapon;
                case EquipmentSlotType.Armor:
                    return ItemType.Armor;
                case EquipmentSlotType.Accessory:
                    return ItemType.Accessory;
                case EquipmentSlotType.SwordArtStyle:
                    return ItemType.SwordArtStyle;
                default:
                    return ItemType.Weapon; // 기본값
            }
        }
        
        /// <summary>
        /// 디버그용 문자열
        /// </summary>
        public override string ToString()
        {
            if (IsEmpty())
                return $"[{slotName}] Empty";
            else
                return $"[{slotName}] {equippedItemKey} x{equippedQuantity}";
        }
    }
}
