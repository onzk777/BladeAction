namespace BladeAction.Item
{
    /// <summary>
    /// 아이템 타입 (장비 슬롯과 1:1 대응)
    /// 총 5가지: 무기, 갑옷, 장신구, 검술 유파, 소모품
    /// </summary>
    public enum ItemType
    {
        Weapon = 1,         // 무기
        Armor = 2,          // 갑옷
        Accessory = 3,      // 장신구
        SwordArtStyle = 4,  // 검술 유파
        Consumable = 6      // 소모품 (향후 확장)
    }
}

