using UnityEngine;

namespace BladeAction.Item
{
    public enum ArmorCategory
    {
        Light,      // 경갑
        Medium,     // 중갑
        Heavy,      // 중갑
        Special     // 특수 방어구
    }
    
    [CreateAssetMenu(fileName = "ArmorType", menuName = "Item/Armor Type", order = 2)]
    public class ArmorTypeData : ScriptableObject
    {
        [Header("기본 정보")]
        public string typeName;
        public string typeKey;
        public Sprite typeIcon;
        
        [TextArea(3, 5)]
        public string description;
        
        [Header("방어구 특성")]
        public ArmorCategory category = ArmorCategory.Light;
        
        [Tooltip("기본 무게")]
        public float baseWeight = 1.0f;
        
        [Tooltip("기본 기동성 (0~1, 높을수록 좋음)")]
        [Range(0f, 1f)]
        public float baseMobility = 1.0f;
        
        [Tooltip("필요 레벨")]
        public int requiredLevel = 1;
    }
}

