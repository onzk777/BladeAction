namespace BladeAction.Item.Excel
{
    /// <summary>
    /// CSV에서 읽은 Item 한 행의 데이터
    /// Unity Asset 참조는 제외하고 텍스트/수치 데이터만 포함
    /// </summary>
    [System.Serializable]
    public class ItemCSVData
    {
        public string Key;              // itemKey
        public string Name;             // itemName
        public string Description;      // 설명
        public string Type;             // ItemType (문자열: "Weapon", "Armor", etc)
        public int RequiredLevel;       // 필요 레벨
        public int MaxStack;            // 최대 중첩 수
        public string StatKey;          // statTableKey
        public string WeaponTypeKey;    // weaponTypeKey (Type=Weapon인 경우)
        public string ArmorTypeKey;     // armorTypeKey (Type=Armor인 경우)
        public string AccessoryTypeKey; // accessoryTypeKey (Type=Accessory인 경우)
        public string SwordArtStyleKey; // swordArtStyleKey (Type=SwordArtStyle인 경우)

        // 원본 CSV에서 해당 필드가 비어있지 않았는지 표시(머지 시 빈 값=미변경 처리용)
        public bool HasName;
        public bool HasDescription;
        public bool HasType;
        public bool HasRequiredLevel;
        public bool HasMaxStack;
        public bool HasStatKey;
        public bool HasWeaponTypeKey;
        public bool HasArmorTypeKey;
        public bool HasAccessoryTypeKey;
        public bool HasSwordArtStyleKey;
    }
}

