using UnityEngine;

namespace BladeAction.Item
{
    /// <summary>
    /// 개별 아이템 데이터 (일반 클래스 - ScriptableObject 아님!)
    /// ItemDatabase에 인라인으로 포함되어 한 곳에서 모든 아이템 편집 가능
    /// </summary>
    [System.Serializable]
    public class Item
    {
        [Header("아이템 고유 키")]
        [Tooltip("아이템 고유 키")]
        public string itemKey;
        
        [Tooltip("아이템 이름")]
        public string itemName;
        
        [Tooltip("아이템 설명")]
        [TextArea(3, 5)]
        public string description;
        
        [Tooltip("아이템 타입")]
        public ItemType itemType;
        
        [Tooltip("필요 레벨")]
        public int requiredLevel = 1;
        
        [Tooltip("최대 중첩 수 (1이면 중첩 불가)")]
        public int maxStack = 1;
        
        [Header("Unity Asset 참조 (Inspector에서 설정)")]
        [Tooltip("아이템 아이콘")]
        public Sprite icon;
        
        [Tooltip("전투 중 표시될 외형")]
        public Sprite appearance;
        
        [Header("타입 참조 (ItemTypeDatabase)")]
        [Tooltip("무기 타입 키 (무기인 경우)")]
        [DatabaseKey(typeof(ItemTypeDatabase), "weaponTypes", "typeKey", "typeName")]
        public string weaponTypeKey;
        
        [Tooltip("방어구 타입 키 (방어구인 경우)")]
        [DatabaseKey(typeof(ItemTypeDatabase), "armorTypes", "typeKey", "typeName")]
        public string armorTypeKey;
        
        [Tooltip("보조장비 타입 키 (보조장비인 경우)")]
        [DatabaseKey(typeof(ItemTypeDatabase), "accessoryTypes", "typeKey", "typeName")]
        public string accessoryTypeKey;
        
        [Tooltip("검술 유파 키 (유파 아이템인 경우)")]
        [DatabaseKey(typeof(SwordArtStyleDatabase), "styles", "key", "displayName")]
        public string swordArtStyleKey;
        
        [Header("스탯 (재사용 방식)")]
        [Tooltip("스탯 테이블 사용 여부 (체크하면 스탯 테이블 연결, 체크 해제하면 스탯 없는 아이템)")]
        public bool useStatTable = false;
        
        [Tooltip("스탯 테이블 키 (StatTable 참조)")]
        [DatabaseKey(typeof(StatDatabase), "statTables", "tableKey", "description")]
        public string statTableKey;
        
        /// <summary>
        /// 스탯 가져오기 (StatDatabase에서 조회)
        /// </summary>
        public EquipmentStats GetStats(StatDatabase statDatabase)
        {
            // 스탯 테이블을 사용하지 않는 아이템이면 빈 스탯 반환
            if (!useStatTable)
                return new EquipmentStats();
            
            if (statDatabase == null || string.IsNullOrEmpty(statTableKey))
                return new EquipmentStats();
            
            var table = statDatabase.GetStatTable(statTableKey);
            return table != null ? table.stats : new EquipmentStats();
        }

        /// <summary>
        /// 검술 유파 SO 가져오기 (SwordArtStyleDatabase에서 조회)
        /// </summary>
        public SwordArtStyleData GetSwordArtStyle(SwordArtStyleDatabase styleDatabase)
        {
            if (styleDatabase == null || string.IsNullOrEmpty(swordArtStyleKey))
                return null;
            return styleDatabase.GetStyle(swordArtStyleKey);
        }
        
        /// <summary>
        /// 무기 타입 가져오기 (ItemTypeDatabase에서 조회)
        /// </summary]
        public WeaponTypeEntry GetWeaponType(ItemTypeDatabase typeDatabase)
        {
            if (typeDatabase == null || string.IsNullOrEmpty(weaponTypeKey))
                return null;
            
            return typeDatabase.GetWeaponType(weaponTypeKey);
        }
        
        /// <summary>
        /// 방어구 타입 가져오기 (ItemTypeDatabase에서 조회)
        /// </summary]
        public ArmorTypeEntry GetArmorType(ItemTypeDatabase typeDatabase)
        {
            if (typeDatabase == null || string.IsNullOrEmpty(armorTypeKey))
                return null;
            
            return typeDatabase.GetArmorType(armorTypeKey);
        }
        
        /// <summary>
        /// 보조장비 타입 가져오기 (ItemTypeDatabase에서 조회)
        /// </summary]
        public AccessoryTypeEntry GetAccessoryType(ItemTypeDatabase typeDatabase)
        {
            if (typeDatabase == null || string.IsNullOrEmpty(accessoryTypeKey))
                return null;
            
            return typeDatabase.GetAccessoryType(accessoryTypeKey);
        }
    }
}

