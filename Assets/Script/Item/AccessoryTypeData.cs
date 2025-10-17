using UnityEngine;

namespace BladeAction.Item
{
    public enum AccessoryCategory
    {
        Ring,       // 반지
        Necklace,   // 목걸이
        Bracelet,   // 팔찌
        Special     // 특수 보조장비
    }
    
    [CreateAssetMenu(fileName = "AccessoryType", menuName = "Item/Accessory Type", order = 3)]
    public class AccessoryTypeData : ScriptableObject
    {
        [Header("기본 정보")]
        public string typeName;
        public string typeKey;
        public Sprite typeIcon;
        
        [TextArea(3, 5)]
        public string description;
        
        [Header("보조장비 특성")]
        public AccessoryCategory category = AccessoryCategory.Ring;
        
        [Tooltip("최대 장착 개수")]
        public int maxEquipCount = 1;
        
        [Tooltip("필요 레벨")]
        public int requiredLevel = 1;
    }
}

