using UnityEngine;

namespace BladeAction.Item
{
    public enum WeaponCategory
    {
        Melee,      // 근접 무기
        Ranged,     // 원거리 무기
        Magic       // 마법 무기
    }
    
    [CreateAssetMenu(fileName = "WeaponType", menuName = "Item/Weapon Type", order = 1)]
    public class WeaponTypeData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("표시될 타입 이름 (예: 일본도, 직검, 레이피어)")]
        public string typeName;
        
        [Tooltip("내부 식별자 (예: katana, straight_sword)")]
        public string typeKey;
        
        [Tooltip("타입 아이콘")]
        public Sprite typeIcon;
        
        [Tooltip("타입 설명")]
        [TextArea(3, 5)]
        public string description;
        
        [Header("무기 특성")]
        [Tooltip("무기 카테고리")]
        public WeaponCategory category = WeaponCategory.Melee;
        
        [Tooltip("기본 공격 속도")]
        public float baseAttackSpeed = 1.0f;
        
        [Tooltip("기본 사거리")]
        public float baseRange = 1.0f;
    }
}

