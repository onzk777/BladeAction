using UnityEngine;
using System.Collections.Generic;

namespace BladeAction.Item
{
    /// <summary>
    /// 아이템 타입 데이터베이스 (게임 룰/시스템 데이터)
    /// 게임에 존재하는 모든 타입 정의를 중앙 관리
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTypeDatabase", menuName = "Item/Item Type Database", order = 8)]
    public class ItemTypeDatabase : ScriptableObject
    {
        [Header("무기 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 무기 타입")]
        public List<WeaponTypeData> weaponTypes = new List<WeaponTypeData>();
        
        [Header("방어구 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 방어구 타입")]
        public List<ArmorTypeData> armorTypes = new List<ArmorTypeData>();
        
        [Header("보조장비 타입 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 보조장비 타입")]
        public List<AccessoryTypeData> accessoryTypes = new List<AccessoryTypeData>();
        
        [Header("검술 유파 (게임 룰)")]
        [Tooltip("게임에서 사용 가능한 모든 검술 유파")]
        public List<SwordArtStyleData> swordArtStyles = new List<SwordArtStyleData>();
        
        #region 타입 조회 메서드
        
        /// <summary>
        /// typeKey로 무기 타입 검색
        /// </summary>
        public WeaponTypeData GetWeaponType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return weaponTypes.Find(t => t.typeKey == typeKey);
        }
        
        /// <summary>
        /// typeKey로 방어구 타입 검색
        /// </summary>
        public ArmorTypeData GetArmorType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return armorTypes.Find(t => t.typeKey == typeKey);
        }
        
        /// <summary>
        /// typeKey로 보조장비 타입 검색
        /// </summary>
        public AccessoryTypeData GetAccessoryType(string typeKey)
        {
            if (string.IsNullOrEmpty(typeKey))
                return null;
            
            return accessoryTypes.Find(t => t.typeKey == typeKey);
        }
        
        /// <summary>
        /// styleKey로 검술 유파 검색
        /// </summary>
        public SwordArtStyleData GetSwordArtStyle(string styleKey)
        {
            if (string.IsNullOrEmpty(styleKey))
                return null;
            
            return swordArtStyles.Find(s => s.styleName == styleKey);
        }
        
        #endregion
    }
}

