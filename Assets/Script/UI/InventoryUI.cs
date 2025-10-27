using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;

namespace BladeAction.UI
{
    /// <summary>
    /// 인벤토리 UI 메인 패널 컨트롤러
    /// 전체 인벤토리 UI를 통합 관리하고 ItemEvents와 연동합니다.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("인벤토리 참조")]
        [Tooltip("표시할 인벤토리 (런타임에 설정)")]
        [SerializeField] private CombatantInventory inventory;
        
        [Header("UI 컨테이너 참조")]
        [Tooltip("메인 패널 GameObject")]
        [SerializeField] private GameObject panel;
        
        [Tooltip("아이템 그리드 컨테이너 (ItemSlotUI들이 생성될 부모)")]
        [SerializeField] private Transform itemGridContainer;
        
        [Tooltip("아이템 그리드가 들어있는 ScrollRect")]
        [SerializeField] private ScrollRect itemScrollRect;
        
        [Tooltip("장비 슬롯 컨테이너 (EquipmentSlotUI들이 생성될 부모)")]
        [SerializeField] private Transform equipmentSlotContainer;
        
        [Tooltip("장신구 슬롯 컨테이너 (가로 배치)")]
        [SerializeField] private Transform accessoryPanel;
        
        [Tooltip("검술 유파 슬롯 컨테이너")]
        [SerializeField] private Transform swordArtStylePanel;
        
        [Header("Prefab 참조")]
        [Tooltip("아이템 슬롯 프리팹")]
        [SerializeField] private GameObject itemSlotPrefab;
        
        [Tooltip("장비 슬롯 프리팹")]
        [SerializeField] private GameObject equipmentSlotPrefab;
        
        [Header("패널 참조")]
        [Tooltip("아이템 상세 정보 패널")]
        [SerializeField] private ItemDetailPanel itemDetailPanel;
        
        [Tooltip("검술 유파 표시 패널")]
        [SerializeField] private EquippedSwordArtStyleUI EquippedSwordArtStyleUI;
        
        [Header("UI 설정")]
        [Tooltip("자동으로 ItemEvents 구독")]
        [SerializeField] private bool autoSubscribeEvents = true;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // UI 슬롯 리스트
        private List<ItemSlotUI> itemSlots = new List<ItemSlotUI>();
        private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
        
        // 현재 선택된 슬롯
        private ItemSlotUI selectedItemSlot;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 컴포넌트 유효성 검증
            ValidateComponents();
        }
        
        private void Start()
        {
            // 이벤트 구독
            if (autoSubscribeEvents)
            {
                SubscribeToEvents();
            }
            
            // 패널 초기 상태 (비활성화)
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region 초기화 및 검증
        
        /// <summary>
        /// 컴포넌트 유효성 검증
        /// </summary>
        private void ValidateComponents()
        {
            if (panel == null)
                Debug.LogWarning("[InventoryUI] panel이 할당되지 않았습니다!", this);
            
            if (itemGridContainer == null)
                Debug.LogWarning("[InventoryUI] itemGridContainer가 할당되지 않았습니다!", this);
            
            if (equipmentSlotContainer == null)
                Debug.LogWarning("[InventoryUI] equipmentSlotContainer가 할당되지 않았습니다!", this);
            
            if (itemSlotPrefab == null)
                Debug.LogWarning("[InventoryUI] itemSlotPrefab이 할당되지 않았습니다!", this);
            
            if (equipmentSlotPrefab == null)
                Debug.LogWarning("[InventoryUI] equipmentSlotPrefab이 할당되지 않았습니다!", this);
        }
        
        /// <summary>
        /// 인벤토리 및 UI 초기화
        /// </summary>
        /// <param name="inventory">표시할 인벤토리</param>
        public void Initialize(CombatantInventory inventory)
        {
            if (inventory == null)
            {
                Debug.LogError("[InventoryUI] null 인벤토리가 전달되었습니다!");
                return;
            }
            
            this.inventory = inventory;
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 인벤토리 초기화: {inventory.inventoryName}");
            
            // ItemDetailPanel 초기화
            if (itemDetailPanel != null)
            {
                itemDetailPanel.Initialize(inventory);
            }
            
            // SwordArtDisplayUI 초기화
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.Initialize(inventory);
            }
            
            // UI 생성
            CreateEquipmentSlots();
            CreateItemSlots();
            
            // UI 갱신
            RefreshAll();
        }
        
        #endregion
        
        #region 장비 슬롯 생성 및 관리
        
        /// <summary>
        /// 장비 슬롯 UI 생성
        /// </summary>
        private void CreateEquipmentSlots()
        {
            if (inventory == null || equipmentSlotPrefab == null)
                return;
            
            // 기존 슬롯 제거
            ClearEquipmentSlots();
            
            // 무기, 갑옷 슬롯만 메인 장비 패널에 생성
            var mainSlots = inventory.equipmentSlots
                .Where(slot => slot.slotType != EquipmentSlotType.Accessory && 
                              slot.slotType != EquipmentSlotType.SwordArtStyle)
                .ToList();
            
            foreach (var slot in mainSlots)
            {
                GameObject slotObj = Instantiate(equipmentSlotPrefab, equipmentSlotContainer);
                EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Setup(slot);
                    slotUI.OnSlotClicked += OnEquipmentSlotClicked;
                    equipmentSlots.Add(slotUI);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] equipmentSlotPrefab에 EquipmentSlotUI 컴포넌트가 없습니다!");
                }
            }
            
            // 장신구 슬롯 생성
            CreateAccessorySlots();
            
            // 검술 유파 슬롯 생성
            CreateSwordArtStyleSlot();
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 장비 슬롯 {equipmentSlots.Count}개 생성 완료");
        }
        
        /// <summary>
        /// 장비 슬롯 UI 제거
        /// </summary>
        private void ClearEquipmentSlots()
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnEquipmentSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            equipmentSlots.Clear();
        }
        
        /// <summary>
        /// 장비 슬롯 UI 갱신
        /// </summary>
        private void RefreshEquipmentSlots()
        {
            if (inventory == null)
                return;
            
            for (int i = 0; i < equipmentSlots.Count && i < inventory.equipmentSlots.Count; i++)
            {
                var slot = inventory.equipmentSlots[i];
                // 장신구 슬롯은 텍스트 숨김
                bool hideText = slot.slotType == EquipmentSlotType.Accessory;
                equipmentSlots[i].Setup(slot, hideText);
            }
        }
        
        /// <summary>
        /// 장신구 슬롯 생성 (가로 배치)
        /// </summary>
        private void CreateAccessorySlots()
        {
            if (accessoryPanel == null || equipmentSlotPrefab == null)
                return;
            
            // 기존 장신구 슬롯들 제거
            foreach (Transform child in accessoryPanel)
            {
                Destroy(child.gameObject);
            }
            
            // 장신구 슬롯들 생성
            var accessorySlots = inventory.equipmentSlots
                .Where(slot => slot.slotType == EquipmentSlotType.Accessory)
                .ToList();
            
            foreach (var slot in accessorySlots)
            {
                GameObject slotObj = Instantiate(equipmentSlotPrefab, accessoryPanel);
                EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();
                
                if (slotUI != null)
                {
                    // 장신구 슬롯은 정사각형으로 설정 (아이콘만 표시)
                    RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = new Vector2(100, 100); // 100x100 정사각형
                    }
                    
                    // 장신구는 텍스트 숨김
                    slotUI.Setup(slot, hideTextForAccessorySlot: true);
                    slotUI.OnSlotClicked += OnEquipmentSlotClicked;
                    equipmentSlots.Add(slotUI);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] equipmentSlotPrefab에 EquipmentSlotUI 컴포넌트가 없습니다!");
                }
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 장신구 슬롯 {accessorySlots.Count}개 생성 완료");
        }
        
        /// <summary>
        /// 검술 유파 슬롯 생성
        /// </summary>
        private void CreateSwordArtStyleSlot()
        {
            if (swordArtStylePanel == null || equipmentSlotPrefab == null)
                return;
            
            // 기존 검술 유파 슬롯 제거
            foreach (Transform child in swordArtStylePanel)
            {
                Destroy(child.gameObject);
            }
            
            // 검술 유파 슬롯 찾기
            var styleSlot = inventory.equipmentSlots
                .FirstOrDefault(slot => slot.slotType == EquipmentSlotType.SwordArtStyle);
            
            if (styleSlot != null)
            {
                GameObject slotObj = Instantiate(equipmentSlotPrefab, swordArtStylePanel);
                EquipmentSlotUI slotUI = slotObj.GetComponent<EquipmentSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.Setup(styleSlot);
                    slotUI.OnSlotClicked += OnEquipmentSlotClicked;
                    equipmentSlots.Add(slotUI);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] equipmentSlotPrefab에 EquipmentSlotUI 컴포넌트가 없습니다!");
                }
            }
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] 검술 유파 슬롯 생성 완료");
        }
        
        #endregion
        
        #region 아이템 슬롯 생성 및 관리
        
        /// <summary>
        /// 아이템 슬롯 UI 동적 생성
        /// </summary>
        private void CreateItemSlots()
        {
            if (itemGridContainer == null || itemSlotPrefab == null || inventory == null)
                return;
            
            // 기존 슬롯 제거
            ClearItemSlots();
            
            // 보유 아이템 수만큼 슬롯 생성
            int itemCount = inventory.items.Count;
            int slotsToCreate = Mathf.Max(itemCount, 1); // 최소 1개는 생성
            
            for (int i = 0; i < slotsToCreate; i++)
            {
                GameObject slotObj = Instantiate(itemSlotPrefab, itemGridContainer);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                
                if (slotUI != null)
                {
                    slotUI.OnSlotClicked += OnItemSlotClicked;
                    itemSlots.Add(slotUI);
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] itemSlotPrefab에 ItemSlotUI 컴포넌트가 없습니다!");
                }
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 아이템 슬롯 {itemSlots.Count}개 동적 생성 완료 (보유 아이템: {itemCount}개)");
        }
        
        /// <summary>
        /// 아이템 슬롯 UI 제거
        /// </summary>
        private void ClearItemSlots()
        {
            foreach (var slot in itemSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnItemSlotClicked;
                    Destroy(slot.gameObject);
                }
            }
            itemSlots.Clear();
        }
        
        /// <summary>
        /// 아이템 그리드 갱신 (동적 슬롯 관리)
        /// </summary>
        private void RefreshItemGrid()
        {
            if (inventory == null)
                return;
            
            int currentItemCount = inventory.items.Count;
            int currentSlotCount = itemSlots.Count;
            
            // 슬롯 수가 아이템 수와 다르면 재생성
            if (currentSlotCount != currentItemCount)
            {
                CreateItemSlots();
            }
            
            // 모든 슬롯 초기화
            foreach (var slot in itemSlots)
            {
                slot.Clear();
            }
            
            // 인벤토리 아이템을 슬롯에 할당
            for (int i = 0; i < inventory.items.Count && i < itemSlots.Count; i++)
            {
                itemSlots[i].Setup(inventory.items[i]);
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 아이템 그리드 동적 갱신 완료: {inventory.items.Count}개 아이템, {itemSlots.Count}개 슬롯");
        }
        
        #endregion
        
        #region 이벤트 처리
        
        /// <summary>
        /// ItemEvents 구독
        /// </summary>
        private void SubscribeToEvents()
        {
            if (ItemEvents.Instance == null)
            {
                Debug.LogWarning("[InventoryUI] ItemEvents 인스턴스를 찾을 수 없습니다!");
                return;
            }
            
            ItemEvents.Instance.OnItemAdded.AddListener(OnItemAddedEvent);
            ItemEvents.Instance.OnItemRemoved.AddListener(OnItemRemovedEvent);
            ItemEvents.Instance.OnItemEquipped.AddListener(OnItemEquippedEvent);
            ItemEvents.Instance.OnItemUnequipped.AddListener(OnItemUnequippedEvent);
            ItemEvents.Instance.OnItemQuantityChanged.AddListener(OnItemQuantityChangedEvent);
            ItemEvents.Instance.OnInventoryFull.AddListener(OnInventoryFullEvent);
            ItemEvents.Instance.OnInventoryCleared.AddListener(OnInventoryClearedEvent);
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] ItemEvents 구독 완료");
        }
        
        /// <summary>
        /// ItemEvents 구독 해제
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            // OnDestroy에서 호출 시 새 GameObject 생성 방지
            if (!ItemEvents.HasInstance)
                return;
            
            var events = ItemEvents.Instance;
            events.OnItemAdded.RemoveListener(OnItemAddedEvent);
            events.OnItemRemoved.RemoveListener(OnItemRemovedEvent);
            events.OnItemEquipped.RemoveListener(OnItemEquippedEvent);
            events.OnItemUnequipped.RemoveListener(OnItemUnequippedEvent);
            events.OnItemQuantityChanged.RemoveListener(OnItemQuantityChangedEvent);
            events.OnInventoryFull.RemoveListener(OnInventoryFullEvent);
            events.OnInventoryCleared.RemoveListener(OnInventoryClearedEvent);
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] ItemEvents 구독 해제 완료");
        }
        
        // 이벤트 콜백 메서드들
        private void OnItemAddedEvent(ItemEventData data) => RefreshItemGrid();
        private void OnItemRemovedEvent(ItemEventData data) => RefreshItemGrid();
        private void OnItemEquippedEvent(ItemEventData data) => RefreshAll();
        private void OnItemUnequippedEvent(ItemEventData data) => RefreshAll();
        private void OnItemQuantityChangedEvent(ItemEventData data) => RefreshItemGrid();
        private void OnInventoryFullEvent(ItemEventData data)
        {
            if (enableDebugLog)
                Debug.LogWarning($"[InventoryUI] 인벤토리가 가득 찼습니다: {data.itemKey}");
        }
        private void OnInventoryClearedEvent(ItemEventData data) => RefreshAll();
        
        /// <summary>
        /// 아이템 슬롯 클릭 처리
        /// </summary>
        private void OnItemSlotClicked(ItemSlotUI clickedSlot)
        {
            Debug.Log($"[InventoryUI] 아이템 슬롯 클릭 감지: {clickedSlot?.name ?? "null"}");
            
            if (clickedSlot == null || clickedSlot.IsEmpty())
            {
                Debug.LogWarning("[InventoryUI] 클릭된 슬롯이 null이거나 비어있습니다!");
                return;
            }
            
            // 같은 슬롯을 다시 클릭하면 선택 해제
            if (selectedItemSlot == clickedSlot)
            {
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
                
                // 상세 정보 패널 숨기기
                if (itemDetailPanel != null)
                {
                    itemDetailPanel.HidePanel();
                }
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 아이템 선택 해제");
                return;
            }
            
            // 이전 선택 해제
            if (selectedItemSlot != null)
            {
                selectedItemSlot.SetSelected(false);
            }
            
            // 새로운 슬롯 선택
            selectedItemSlot = clickedSlot;
            selectedItemSlot.SetSelected(true);
            
            // 아이템 상세 정보 패널 업데이트
            if (itemDetailPanel != null)
            {
                var item = selectedItemSlot.GetOwnedItem();
                itemDetailPanel.ShowItem(item);
                
                if (enableDebugLog)
                    Debug.Log($"[InventoryUI] 아이템 선택: {item}");
            }

            // 선택 슬롯을 뷰포트 최상단으로 자동 스크롤
            ScrollToItemSlotTop(selectedItemSlot);
        }

        /// <summary>
        /// 선택된 아이템 슬롯이 ScrollRect의 뷰포트 최상단에 오도록 스크롤 조정
        /// </summary>
        private void ScrollToItemSlotTop(ItemSlotUI targetSlot)
        {
            if (itemScrollRect == null || targetSlot == null)
                return;

            var content = itemScrollRect.content;
            var viewport = itemScrollRect.viewport != null ? itemScrollRect.viewport : (RectTransform)itemScrollRect.transform;
            var target = targetSlot.GetComponent<RectTransform>();
            if (content == null || viewport == null || target == null)
                return;

            // 레이아웃 최신화 후 위치 계산
            Canvas.ForceUpdateCanvases();

            // content 기준 target의 로컬 위치를 얻고, 그 값을 content의 앵커 위치로 반영
            Vector2 localPointInContent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, target.position),
                null,
                out localPointInContent
            );

            // target의 상단이 viewport 상단에 맞닿도록 content의 anchoredPosition 조정
            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            float targetTopY = localPointInContent.y + (target.rect.height * (1f - target.pivot.y));

            // ScrollRect는 보통 위로 갈수록 양수/음수 방향이 반대이므로 보정
            Vector2 anchored = content.anchoredPosition;
            anchored.y = -(targetTopY) ;

            // 클램핑: content가 viewport를 벗어나지 않도록 제한
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
            anchored.y = Mathf.Clamp(anchored.y, 0f, maxY);

            content.anchoredPosition = anchored;
        }
        
        /// <summary>
        /// 장비 슬롯 클릭 처리
        /// </summary>
        private void OnEquipmentSlotClicked(EquipmentSlotUI clickedSlot)
        {
            Debug.Log($"[InventoryUI] 장비 슬롯 클릭 이벤트 수신: {clickedSlot?.name ?? "null"}");
            
            if (clickedSlot == null || inventory == null)
            {
                Debug.LogWarning($"[InventoryUI] 클릭 이벤트 무시: clickedSlot={clickedSlot != null}, inventory={inventory != null}");
                return;
            }
            
            var equipSlot = clickedSlot.GetEquipmentSlot();
            if (equipSlot == null)
            {
                Debug.LogWarning("[InventoryUI] EquipmentSlot 데이터가 null입니다!");
                return;
            }
            
            // 장착된 아이템이 있으면 해당 아이템의 상세 정보 표시
            if (!equipSlot.IsEmpty())
            {
                // 장착된 아이템의 데이터를 직접 가져오기
                var itemData = equipSlot.GetEquippedItem();
                
                if (itemData != null && itemDetailPanel != null)
                {
                    // 장착된 아이템을 위한 임시 OwnedItem 생성
                    var tempOwnedItem = new OwnedItem(equipSlot.equippedItemKey, 1);
                    tempOwnedItem.isEquipped = true;
                    itemDetailPanel.ShowItem(tempOwnedItem);
                    
                    if (enableDebugLog)
                        Debug.Log($"[InventoryUI] 장비 슬롯 클릭: {equipSlot.slotName} ({equipSlot.equippedItemKey})");
                }
                else
                {
                    Debug.LogWarning($"[InventoryUI] 장착된 아이템 데이터를 찾을 수 없습니다: {equipSlot.equippedItemKey}");
                }
            }
        }
        
        #endregion
        
        #region UI 갱신 및 제어
        
        /// <summary>
        /// 전체 UI 갱신
        /// </summary>
        public void RefreshAll()
        {
            RefreshItemGrid();
            RefreshEquipmentSlots();
            RefreshSwordArtDisplay();
        }
        
        /// <summary>
        /// 검술 유파 표시 갱신
        /// </summary>
        private void RefreshSwordArtDisplay()
        {
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.Refresh();
            }
        }
        
        /// <summary>
        /// 패널 열기/닫기 토글
        /// </summary>
        public void TogglePanel()
        {
            if (panel == null)
                return;
            
            bool isActive = !panel.activeSelf;
            panel.SetActive(isActive);
            
            if (isActive)
            {
                RefreshAll();
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 패널 {(isActive ? "열기" : "닫기")}");
        }
        
        /// <summary>
        /// 패널 열기
        /// </summary>
        public void OpenPanel()
        {
            if (panel == null)
                return;
            
            panel.SetActive(true);
            RefreshAll();
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] 패널 열기");
        }
        
        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void ClosePanel()
        {
            if (panel == null)
                return;
            
            panel.SetActive(false);
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] 패널 닫기");
        }
        
        /// <summary>
        /// 패널 열기 (기존 메서드)
        /// </summary>
        public void ShowPanel()
        {
            if (panel != null)
            {
                panel.SetActive(true);
                RefreshAll();
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 패널 열기");
            }
        }
        
        /// <summary>
        /// 패널 닫기
        /// </summary>
        public void HidePanel()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 패널 닫기");
            }
        }
        
        #endregion
        
        #region 데이터 접근
        
        /// <summary>
        /// 현재 인벤토리 반환
        /// </summary>
        public CombatantInventory GetInventory()
        {
            return inventory;
        }
        
        /// <summary>
        /// 선택된 아이템 반환
        /// </summary>
        public OwnedItem GetSelectedItem()
        {
            return selectedItemSlot?.GetOwnedItem();
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Print Debug Info")]
        private void PrintDebugInfo()
        {
            Debug.Log("[InventoryUI] 디버그 정보:");
            Debug.Log($"  - Inventory: {(inventory != null ? inventory.inventoryName : "null")}");
            Debug.Log($"  - Item Slots: {itemSlots.Count}");
            Debug.Log($"  - Equipment Slots: {equipmentSlots.Count}");
            Debug.Log($"  - Panel Active: {(panel != null ? panel.activeSelf : false)}");
        }
        
        /// <summary>
        /// 강제로 UI 갱신 (디버그용)
        /// </summary>
        [ContextMenu("Force Refresh")]
        private void ForceRefresh()
        {
            RefreshAll();
            Debug.Log("[InventoryUI] 강제 갱신 완료");
        }
        
        #endregion
    }
}

