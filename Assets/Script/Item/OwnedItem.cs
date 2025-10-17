using UnityEngine;

namespace BladeAction.Item
{
    /// <summary>
    /// 소유한 아이템 정보 (인벤토리에서 관리)
    /// 아이템 데이터 + 소유 수량 + 추가 상태 정보
    /// </summary>
    [System.Serializable]
    public class OwnedItem
    {
        [Header("아이템 참조")]
        [Tooltip("아이템 데이터베이스에서의 키")]
        public string itemKey;
        
        [Header("소유 정보")]
        [Tooltip("현재 소유 수량")]
        [Range(1, 999)]
        public int quantity = 1;
        
        [Tooltip("최대 소유 수량 (아이템의 maxStack과 동일)")]
        [Range(1, 999)]
        public int maxQuantity = 1;
        
        [Header("상태 정보")]
        [Tooltip("아이템이 장착되어 있는지")]
        public bool isEquipped = false;
        
        [Tooltip("아이템이 잠겨있는지 (판매/버리기 불가)")]
        public bool isLocked = false;
        
        [Tooltip("아이템 획득 시간 (정렬용)")]
        public System.DateTime acquiredTime;
        
        /// <summary>
        /// 기본 생성자
        /// </summary>
        public OwnedItem()
        {
            acquiredTime = System.DateTime.Now;
        }
        
        /// <summary>
        /// 아이템 키로 생성
        /// </summary>
        public OwnedItem(string itemKey, int quantity = 1)
        {
            this.itemKey = itemKey;
            this.quantity = quantity;
            this.acquiredTime = System.DateTime.Now;
        }
        
        /// <summary>
        /// 아이템 데이터 가져오기 (ItemDatabase에서)
        /// </summary>
        public Item GetItemData()
        {
            if (string.IsNullOrEmpty(itemKey))
                return null;
                
            var itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
            return itemDatabase?.GetItem(itemKey);
        }
        
        /// <summary>
        /// 수량 추가
        /// </summary>
        public bool AddQuantity(int amount)
        {
            if (amount <= 0) return false;
            
            int newQuantity = quantity + amount;
            if (newQuantity > maxQuantity)
            {
                quantity = maxQuantity;
                return false; // 최대 수량 초과
            }
            
            quantity = newQuantity;
            return true;
        }
        
        /// <summary>
        /// 수량 제거
        /// </summary>
        public bool RemoveQuantity(int amount)
        {
            if (amount <= 0) return false;
            
            int newQuantity = quantity - amount;
            if (newQuantity < 0)
            {
                return false; // 수량 부족
            }
            
            quantity = newQuantity;
            return true;
        }
        
        /// <summary>
        /// 수량 설정
        /// </summary>
        public void SetQuantity(int amount)
        {
            quantity = Mathf.Clamp(amount, 0, maxQuantity);
        }
        
        /// <summary>
        /// 최대 수량 설정 (아이템 데이터 기반)
        /// </summary>
        public void UpdateMaxQuantity()
        {
            var itemData = GetItemData();
            if (itemData != null)
            {
                maxQuantity = itemData.maxStack;
            }
        }
        
        /// <summary>
        /// 아이템이 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(itemKey) && quantity > 0;
        }
        
        /// <summary>
        /// 아이템이 비어있는지 확인
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(itemKey) || quantity <= 0;
        }
        
        /// <summary>
        /// 아이템이 최대 수량인지 확인
        /// </summary>
        public bool IsFull()
        {
            return quantity >= maxQuantity;
        }
        
        /// <summary>
        /// 디버그용 문자열
        /// </summary>
        public override string ToString()
        {
            var itemData = GetItemData();
            string itemName = itemData?.itemName ?? "Unknown";
            return $"[{itemKey}] {itemName} x{quantity}";
        }
    }
}
