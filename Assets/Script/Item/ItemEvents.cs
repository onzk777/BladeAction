using UnityEngine;
using UnityEngine.Events;

namespace BladeAction.Item
{
    /// <summary>
    /// 아이템 관련 이벤트 데이터
    /// </summary>
    [System.Serializable]
    public class ItemEventData
    {
        public string itemKey;
        public int quantity;
        public ItemType itemType;
        public EquipmentSlotType? slotType;
        public string inventoryName;
        
        public ItemEventData(string itemKey, int quantity = 1, ItemType itemType = ItemType.Weapon, 
                           EquipmentSlotType? slotType = null, string inventoryName = "")
        {
            this.itemKey = itemKey;
            this.quantity = quantity;
            this.itemType = itemType;
            this.slotType = slotType;
            this.inventoryName = inventoryName;
        }
    }
    
    /// <summary>
    /// 아이템 이벤트 타입
    /// </summary>
    public enum ItemEventType
    {
        ItemAdded,           // 아이템 추가
        ItemRemoved,         // 아이템 제거
        ItemEquipped,        // 아이템 장착
        ItemUnequipped,      // 아이템 해제
        ItemQuantityChanged, // 수량 변경
        InventoryFull,       // 인벤토리 가득참
        InventoryCleared     // 인벤토리 비움
    }
    
    /// <summary>
    /// 아이템 이벤트 시스템 (싱글톤)
    /// 인벤토리 관련 이벤트를 중앙에서 관리
    /// </summary>
    public class ItemEvents : MonoBehaviour
    {
        private static ItemEvents _instance;
        
        /// <summary>
        /// 인스턴스 존재 여부 (GameObject 생성 없이 안전하게 체크)
        /// </summary>
        public static bool HasInstance => _instance != null;
        
        public static ItemEvents Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ItemEvents>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemEvents");
                        _instance = go.AddComponent<ItemEvents>();
                        // PersistentUIScene 전용이므로 DontDestroyOnLoad 적용 안함
                    }
                }
                return _instance;
            }
        }
        
        [Header("이벤트 리스너")]
        [Tooltip("아이템 추가 이벤트")]
        public UnityEvent<ItemEventData> OnItemAdded = new UnityEvent<ItemEventData>();
        
        [Tooltip("아이템 제거 이벤트")]
        public UnityEvent<ItemEventData> OnItemRemoved = new UnityEvent<ItemEventData>();
        
        [Tooltip("아이템 장착 이벤트")]
        public UnityEvent<ItemEventData> OnItemEquipped = new UnityEvent<ItemEventData>();
        
        [Tooltip("아이템 해제 이벤트")]
        public UnityEvent<ItemEventData> OnItemUnequipped = new UnityEvent<ItemEventData>();
        
        [Tooltip("수량 변경 이벤트")]
        public UnityEvent<ItemEventData> OnItemQuantityChanged = new UnityEvent<ItemEventData>();
        
        [Tooltip("인벤토리 가득참 이벤트")]
        public UnityEvent<ItemEventData> OnInventoryFull = new UnityEvent<ItemEventData>();
        
        [Tooltip("인벤토리 비움 이벤트")]
        public UnityEvent<ItemEventData> OnInventoryCleared = new UnityEvent<ItemEventData>();
        
        [Header("디버그")]
        [Tooltip("이벤트 로그 출력")]
        public bool enableDebugLog = true;
        
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                // PersistentUIScene 전용이므로 DontDestroyOnLoad 적용 안함
            }
            else if (_instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // 에디터 모드에서는 즉시 파괴
                    DestroyImmediate(gameObject);
                }
            }
        }
        
        void OnDestroy()
        {
            // 이 인스턴스가 싱글톤 인스턴스인 경우에만 정리
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        void OnApplicationQuit()
        {
            // 애플리케이션 종료 시 명시적으로 정리
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #region 이벤트 발생 메서드
        
        /// <summary>
        /// 아이템 추가 이벤트 발생
        /// </summary>
        public void TriggerItemAdded(string itemKey, int quantity = 1, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, quantity, 
                itemData?.itemType ?? ItemType.Weapon, null, inventoryName);
            
            OnItemAdded?.Invoke(eventData);
            LogEvent(ItemEventType.ItemAdded, eventData);
        }
        
        /// <summary>
        /// 아이템 제거 이벤트 발생
        /// </summary>
        public void TriggerItemRemoved(string itemKey, int quantity = 1, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, quantity, 
                itemData?.itemType ?? ItemType.Weapon, null, inventoryName);
            
            OnItemRemoved?.Invoke(eventData);
            LogEvent(ItemEventType.ItemRemoved, eventData);
        }
        
        /// <summary>
        /// 아이템 장착 이벤트 발생
        /// </summary>
        public void TriggerItemEquipped(string itemKey, EquipmentSlotType slotType, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, 1, 
                itemData?.itemType ?? ItemType.Weapon, slotType, inventoryName);
            
            OnItemEquipped?.Invoke(eventData);
            LogEvent(ItemEventType.ItemEquipped, eventData);
        }
        
        /// <summary>
        /// 아이템 해제 이벤트 발생
        /// </summary>
        public void TriggerItemUnequipped(string itemKey, EquipmentSlotType slotType, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, 1, 
                itemData?.itemType ?? ItemType.Weapon, slotType, inventoryName);
            
            OnItemUnequipped?.Invoke(eventData);
            LogEvent(ItemEventType.ItemUnequipped, eventData);
        }
        
        /// <summary>
        /// 수량 변경 이벤트 발생
        /// </summary>
        public void TriggerItemQuantityChanged(string itemKey, int quantity, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, quantity, 
                itemData?.itemType ?? ItemType.Weapon, null, inventoryName);
            
            OnItemQuantityChanged?.Invoke(eventData);
            LogEvent(ItemEventType.ItemQuantityChanged, eventData);
        }
        
        /// <summary>
        /// 인벤토리 가득참 이벤트 발생
        /// </summary>
        public void TriggerInventoryFull(string itemKey, string inventoryName = "")
        {
            var itemData = GetItemData(itemKey);
            var eventData = new ItemEventData(itemKey, 1, 
                itemData?.itemType ?? ItemType.Weapon, null, inventoryName);
            
            OnInventoryFull?.Invoke(eventData);
            LogEvent(ItemEventType.InventoryFull, eventData);
        }
        
        /// <summary>
        /// 인벤토리 비움 이벤트 발생
        /// </summary>
        public void TriggerInventoryCleared(string inventoryName = "")
        {
            var eventData = new ItemEventData("", 0, ItemType.Weapon, null, inventoryName);
            
            OnInventoryCleared?.Invoke(eventData);
            LogEvent(ItemEventType.InventoryCleared, eventData);
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 안전한 이벤트 발생 (에디터 모드 고려)
        /// </summary>
        private static void SafeTriggerEvent(System.Action triggerAction)
        {
            try
            {
                triggerAction?.Invoke();
            }
            catch (System.Exception ex)
            {
                // 에디터 모드에서 발생할 수 있는 오류를 조용히 처리
                if (Application.isEditor && !Application.isPlaying)
                {
                    Debug.LogWarning($"[ItemEvents] 에디터 모드에서 이벤트 발생 감지: {ex.Message}");
                }
                else
                {
                    Debug.LogError($"[ItemEvents] 이벤트 발생 오류: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 아이템 데이터 가져오기
        /// </summary>
        private Item GetItemData(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey))
                return null;
                
            return ItemDatabase.GetItemSafe(itemKey);
        }
        
        /// <summary>
        /// 이벤트 로그 출력
        /// </summary>
        private void LogEvent(ItemEventType eventType, ItemEventData eventData)
        {
            if (!enableDebugLog) return;
            
            string slotInfo = eventData.slotType.HasValue ? $" (Slot: {eventData.slotType})" : "";
            string inventoryInfo = !string.IsNullOrEmpty(eventData.inventoryName) ? $" [{eventData.inventoryName}]" : "";
            
            Debug.Log($"[ItemEvents] {eventType}: {eventData.itemKey} x{eventData.quantity}{slotInfo}{inventoryInfo}");
        }
        
        #endregion
    }
}
