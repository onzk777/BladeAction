using UnityEngine;

namespace BladeAction.Item.Excel
{
    /// <summary>
    /// CSV 데이터를 Item으로 매핑
    /// </summary>
    public static class ItemMapper
    {
        /// <summary>
        /// CSV 데이터를 Item으로 변환
        /// </summary>
        public static Item MapCSVToItem(ItemCSVData csvData)
        {
            if (csvData == null)
            {
                Debug.LogWarning("CSV 데이터가 null입니다.");
                return null;
            }
            
            if (string.IsNullOrEmpty(csvData.Key))
            {
                Debug.LogWarning("itemKey가 비어있습니다.");
                return null;
            }
            
            var item = new Item
            {
                // CSV 데이터
                itemKey = csvData.Key,
                itemName = csvData.Name,
                description = csvData.Description,
                itemType = ParseItemType(csvData.Type),
                requiredLevel = csvData.RequiredLevel,
                maxStack = csvData.MaxStack,
                statTableKey = csvData.StatKey,
                weaponTypeKey = csvData.WeaponTypeKey,
                armorTypeKey = csvData.ArmorTypeKey,
                accessoryTypeKey = csvData.AccessoryTypeKey,
                swordArtStyleKey = csvData.SwordArtStyleKey,
                
                // Unity Asset 참조는 null (Inspector에서 수동 설정)
                icon = null,
                appearance = null
            };
            
            return item;
        }
        
        /// <summary>
        /// 기존 Item 업데이트 (CSV 데이터만, Asset 참조는 유지)
        /// </summary>
        public static void UpdateItem(Item existing, ItemCSVData csvData)
        {
            if (existing == null || csvData == null)
                return;
            
            // CSV 데이터만 업데이트 (빈 값은 미변경)
            if (csvData.HasName) existing.itemName = csvData.Name;
            if (csvData.HasDescription) existing.description = csvData.Description;
            if (csvData.HasType) existing.itemType = ParseItemType(csvData.Type);
            if (csvData.HasRequiredLevel) existing.requiredLevel = csvData.RequiredLevel;
            if (csvData.HasMaxStack) existing.maxStack = csvData.MaxStack;
            if (csvData.HasStatKey) existing.statTableKey = csvData.StatKey;
            if (csvData.HasWeaponTypeKey) existing.weaponTypeKey = csvData.WeaponTypeKey;
            if (csvData.HasArmorTypeKey) existing.armorTypeKey = csvData.ArmorTypeKey;
            if (csvData.HasAccessoryTypeKey) existing.accessoryTypeKey = csvData.AccessoryTypeKey;
            if (csvData.HasSwordArtStyleKey) existing.swordArtStyleKey = csvData.SwordArtStyleKey;
            
            // Unity Asset 참조는 유지! (icon, appearance)
        }
        
        /// <summary>
        /// 문자열을 ItemType으로 변환
        /// </summary>
        private static ItemType ParseItemType(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
                return ItemType.Weapon;
            
            switch (typeString.ToLower())
            {
                case "weapon":
                    return ItemType.Weapon;
                case "armor":
                    return ItemType.Armor;
                case "accessory":
                    return ItemType.Accessory;
                case "swordartstyle":
                case "swordart":
                    return ItemType.SwordArtStyle;
                default:
                    Debug.LogWarning($"알 수 없는 ItemType: {typeString} - Weapon으로 처리");
                    return ItemType.Weapon;
            }
        }
        
        /// <summary>
        /// ItemType을 문자열로 변환 (Export용)
        /// </summary>
        public static string ItemTypeToString(ItemType type)
        {
            switch (type)
            {
                case ItemType.Weapon: return "Weapon";
                case ItemType.Armor: return "Armor";
                case ItemType.Accessory: return "Accessory";
                case ItemType.SwordArtStyle: return "SwordArtStyle";
                default: return "Weapon";
            }
        }
    }
}

