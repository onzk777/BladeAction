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
        // 캐싱된 인스턴스 (메모리 최적화)
        private static ItemDatabase _cachedInstance;
        private static bool _hasSearchedForInstance = false;
        
        /// <summary>
        /// 캐싱된 ItemDatabase 인스턴스 가져오기 (메모리 최적화)
        /// 파일명에 의존하지 않고 Resources 폴더에서 자동 검색
        /// </summary>
        public static ItemDatabase Instance
        {
            get
            {
                if (_cachedInstance == null && !_hasSearchedForInstance)
                {
                    FindAndCacheInstance();
                }
                return _cachedInstance;
            }
        }
        
        /// <summary>
        /// Resources 폴더에서 ItemDatabase 찾기 (파일명 무관)
        /// </summary>
        private static void FindAndCacheInstance()
        {
            _hasSearchedForInstance = true;
            
            try
            {
                // Resources 폴더에서 모든 ItemDatabase 검색 (파일명 의존성 제거)
                ItemDatabase[] foundDatabases = Resources.LoadAll<ItemDatabase>("");
                
                if (foundDatabases != null && foundDatabases.Length > 0)
                {
                    if (foundDatabases.Length == 1)
                    {
                        _cachedInstance = foundDatabases[0];
                        Debug.Log($"[ItemDatabase] 인스턴스 발견: '{_cachedInstance.name}' ({_cachedInstance.items?.Count ?? 0}개 아이템)");
                    }
                    else
                    {
                        // 여러 개 발견 시 가장 많은 아이템을 가진 것 선택
                        _cachedInstance = foundDatabases.OrderByDescending(db => db.items?.Count ?? 0).First();
                        Debug.LogWarning($"[ItemDatabase] {foundDatabases.Length}개 발견. 가장 많은 아이템을 가진 '{_cachedInstance.name}' 선택 ({_cachedInstance.items?.Count ?? 0}개 아이템)");
                    }
                }
                else
                {
                    Debug.LogError("[ItemDatabase] Resources 폴더에서 ItemDatabase를 찾을 수 없습니다! Create > Item > Item Database로 생성 후 Resources 폴더에 저장하세요.");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemDatabase] 인스턴스 검색 중 오류 발생: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 캐시 초기화 (에디터에서 필요시 사용)
        /// </summary>
        public static void RefreshInstance()
        {
            _cachedInstance = null;
            _hasSearchedForInstance = false;
        }
        
        /// <summary>
        /// ItemDatabase가 사용 가능한지 확인
        /// </summary>
        public static bool IsAvailable()
        {
            return Instance != null && Instance.items != null;
        }
        
        /// <summary>
        /// 안전한 아이템 조회 (null 체크 포함)
        /// </summary>
        public static Item GetItemSafe(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey))
                return null;
                
            var instance = Instance;
            if (instance?.items == null)
                return null;
                
            return instance.GetItem(itemKey);
        }

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

