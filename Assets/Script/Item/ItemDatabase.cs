using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BladeAction.Item
{
    /// <summary>
    /// 아이템 데이터베이스 (순수 아이템 콘텐츠 데이터)
    /// 이 ScriptableObject 하나만 열면 모든 아이템 편집 가능
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Item/Item Database", order = 10)]
    public class ItemDatabase : ScriptableObject
    {
        [Header("데이터베이스 참조")]
        [Tooltip("아이템 타입 정의 (게임 룰)")]
        public ItemTypeDatabase typeDatabase;
        
        [Tooltip("스탯 테이블 프리셋")]
        public StatDatabase statDatabase;
        
        [Header("모든 아이템 (인라인 편집)")]
        [Tooltip("게임 내 모든 아이템 - 여기서 한번에 관리!")]
        public List<Item> items = new List<Item>();
        
        #region 아이템 조회
        
        /// <summary>
        /// itemKey로 아이템 검색
        /// </summary>
        public Item GetItem(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey))
                return null;
                
            return items.Find(item => item.itemKey == itemKey);
        }
        
        /// <summary>
        /// 타입별 아이템 필터링
        /// </summary>
        public List<Item> GetItemsByType(ItemType type)
        {
            return items.Where(item => item.itemType == type).ToList();
        }
        
        /// <summary>
        /// 무기 타입별 필터링 (typeKey로)
        /// </summary>
        public List<Item> GetWeaponsByTypeKey(string weaponTypeKey)
        {
            if (string.IsNullOrEmpty(weaponTypeKey))
                return new List<Item>();
                
            return items.Where(item => 
                item.itemType == ItemType.Weapon && 
                item.weaponTypeKey == weaponTypeKey
            ).ToList();
        }
        
        /// <summary>
        /// 방어구 타입별 필터링 (typeKey로)
        /// </summary>
        public List<Item> GetArmorsByTypeKey(string armorTypeKey)
        {
            if (string.IsNullOrEmpty(armorTypeKey))
                return new List<Item>();
                
            return items.Where(item => 
                item.itemType == ItemType.Armor && 
                item.armorTypeKey == armorTypeKey
            ).ToList();
        }
        
        /// <summary>
        /// 아이템이 존재하는지 확인
        /// </summary>
        public bool HasItem(string itemKey)
        {
            return GetItem(itemKey) != null;
        }
        
        #endregion
        
        #region 에디터 헬퍼 메서드
        
        /// <summary>
        /// 새 아이템 추가
        /// </summary>
        public void AddItem(Item item)
        {
            if (item == null || string.IsNullOrEmpty(item.itemKey))
            {
                Debug.LogWarning("Invalid Item");
                return;
            }
            
            // 중복 체크
            if (HasItem(item.itemKey))
            {
                Debug.LogWarning($"Item with key '{item.itemKey}' already exists");
                return;
            }
            
            items.Add(item);
        }
        
        /// <summary>
        /// 아이템 제거
        /// </summary>
        public bool RemoveItem(string itemKey)
        {
            var item = GetItem(itemKey);
            if (item == null)
                return false;
            
            return items.Remove(item);
        }
        
        #endregion
    }
}

