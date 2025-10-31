using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using BladeAction.Item;
using System.Collections;

namespace BladeAction.UI
{
    /// <summary>
    /// 인벤토리 UI 메인 패널 컨트롤러
    /// 전체 인벤토리 UI를 통합 관리하고 ItemEvents와 연동합니다.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        // Character 참조 (런타임에 자동 연결됨, Inspector 설정 불필요)
        private Character targetCharacter;
        
        [Header("▣ 메인 패널 (사용 안 함)")]
        [Tooltip("※ 더 이상 사용하지 않음 - MainMenuManager가 관리")]
        [SerializeField] private GameObject panel;
        
        [Header("▣ 아이템 그리드 (소지품 목록)")]
        [Tooltip("아이템 슬롯들이 동적으로 생성될 부모 Transform (보통 Content)")]
        [SerializeField] private Transform itemGridContainer;
        
        [Tooltip("아이템 그리드 프리팹 (ItemSlot.prefab)")]
        [SerializeField] private GameObject itemSlotPrefab;
        
        [Tooltip("아이템 그리드의 스크롤 영역 (ScrollRect 컴포넌트)")]
        [SerializeField] private ScrollRect itemScrollRect;
        
        [Header("▣ 장비 슬롯 (착용 중인 장비)")]
        [Tooltip("장비 슬롯들이 동적으로 생성될 부모 Transform (보통 EquipmentSlots)")]
        [SerializeField] private Transform equipmentSlotContainer;
        
        [Tooltip("장비 슬롯 프리팹 (EquipmentSlot.prefab)")]
        [SerializeField] private GameObject equipmentSlotPrefab;
        
        [Tooltip("장신구 슬롯 영역 (가로 배치, AccessoryPanel)")]
        [SerializeField] private Transform accessoryPanel;
        
        [Header("▣ 상세 정보 패널")]
        [Tooltip("선택한 아이템의 상세 정보를 표시하는 패널 (ItemDetailPanel 컴포넌트)")]
        [SerializeField] private ItemDetailPanel itemDetailPanel;
        
        /// <summary>
        /// ItemDetailPanel 접근자 (외부 UI 연동용)
        /// </summary>
        public ItemDetailPanel ItemDetailPanel => itemDetailPanel;
        
        [Tooltip("착용 중인 검술 유파를 표시하는 슬롯 (EquippedSwordArtStyleUI 컴포넌트)")]
        [SerializeField] private EquippedSwordArtStyleUI EquippedSwordArtStyleUI;
        
        [Header("▣ 설정")]
        [Tooltip("아이템 변경 이벤트를 자동으로 구독할지 여부 (보통 true)")]
        [SerializeField] private bool autoSubscribeEvents = true;
        
        [Header("▣ 디버그")]
        [Tooltip("Console에 상세 로그를 출력할지 여부")]
        [SerializeField] private bool enableDebugLog = true;
        
        // UI 슬롯 리스트
        private List<ItemSlotUI> itemSlots = new List<ItemSlotUI>();
        private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
        
        // 현재 선택된 슬롯
        private ItemSlotUI selectedItemSlot;
        
        #region Unity 생명주기
        
        /// <summary>
        /// 현재 표시 중인 Inventory (targetCharacter.Inventory의 간편 접근자)
        /// </summary>
        private CharacterInventory Inventory => targetCharacter?.Inventory;
        
        /// <summary>
        /// Character 연결 상태 확인 및 자동 재연결
        /// </summary>
        /// <returns>Character가 유효하면 true, 아니면 false</returns>
        private bool EnsureCharacterConnection()
        {
            // 이미 연결되어 있으면 OK
            if (targetCharacter != null && targetCharacter.Inventory != null)
                return true;
            
            // 연결이 끊어졌으면 자동 재연결 시도
            if (enableDebugLog)
                Debug.LogWarning("[InventoryUI] Character 연결이 끊어졌습니다. 자동 재연결 시도...");
            
            AutoConnectToPlayerInventory();
            
            // 재연결 결과 확인
            if (targetCharacter != null && targetCharacter.Inventory != null)
            {
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] Character 자동 재연결 성공");
                return true;
            }
            
            // 재연결 실패
            Debug.LogError("[InventoryUI] Character 연결 실패! UI를 표시할 수 없습니다.");
            return false;
        }
        
        private void Awake()
        {
            // Canvas는 MainMenuManager에서 활성화됨
            // 컴포넌트 유효성 검증
            ValidateComponents();
        }
        
        private void Start()
        {
            // UI Action Map 활성화 (인벤토리 토글 키 활성화)
            EnableUIActionMap();
            
            // 이벤트 구독
            if (autoSubscribeEvents)
            {
                SubscribeToEvents();
            }
            
            // 지연 초기화 (CharacterManager보다 늦게 실행될 수 있으므로)
            StartCoroutine(DelayedAutoConnect());
        }
        
        private void OnEnable()
        {
            // GameObject가 활성화될 때 Character 연결 및 UI 갱신
            // Character가 아직 연결되지 않았으면 자동 연결 시도
            if (targetCharacter == null)
            {
                AutoConnectToPlayerInventory();
            }
            
            // UI 갱신
            if (targetCharacter != null && Inventory != null)
            {
                RefreshAll();
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] InventoryUI 활성화 - UI 갱신");
            }
        }
        
        private void OnDisable()
        {
            // GameObject가 비활성화될 때 정리 작업
            // 아이템 슬롯 선택 상태 초기화
            if (selectedItemSlot != null)
            {
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
            }
            
            // 장비 슬롯 선택 상태 초기화
            if (selectedEquipmentSlot != null)
            {
                selectedEquipmentSlot.SetSelected(false);
                selectedEquipmentSlot = null;
            }
            
            // 상세 패널 닫기
            if (itemDetailPanel != null)
            {
                itemDetailPanel.HidePanel();
            }
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] InventoryUI 비활성화 - 상태 정리");
        }
        
        /// <summary>
        /// 지연된 자동 연결 (CharacterManager 초기화 대기)
        /// </summary>
        private System.Collections.IEnumerator DelayedAutoConnect()
        {
            // 한 프레임 대기 (모든 Awake/Start 완료 대기)
            yield return null;
            
            // 자동으로 PlayerCharacter의 Inventory 연결
            AutoConnectToPlayerInventory();
            
            // 연결 실패 시 재시도 (최대 5프레임)
            int retryCount = 0;
            while (targetCharacter == null && retryCount < 5)
            {
                yield return null;
                Debug.LogWarning($"[InventoryUI] PlayerCharacter 연결 재시도 ({retryCount + 1}/5)");
                AutoConnectToPlayerInventory();
                retryCount++;
            }
            
            if (targetCharacter == null)
            {
                Debug.LogError("[InventoryUI] PlayerCharacter 연결 실패! CharacterManager가 초기화되지 않았을 수 있습니다.");
            }
        }
        
        /// <summary>
        /// PlayerCharacter의 Inventory에 자동 연결
        /// 외부에서 이미 Initialize()가 호출되었으면 스킵합니다.
        /// </summary>
        private void AutoConnectToPlayerInventory()
        {
            Debug.Log("[InventoryUI-AutoConnect] ========== 자동 연결 시작 ==========");
            
            // 이미 다른 곳에서 연결되었으면 스킵
            if (targetCharacter != null)
            {
                Debug.Log($"[InventoryUI-AutoConnect] ❌ Character가 이미 할당되어 있습니다. (Name: {targetCharacter.Name})");
                return;
            }
            
            Debug.Log("[InventoryUI-AutoConnect] ✓ targetCharacter는 null, 연결 시도 중...");
            
            // CharacterManager 확인
            if (CharacterManager.Instance == null)
            {
                Debug.LogWarning("[InventoryUI-AutoConnect] ❌ CharacterManager.Instance가 null입니다!");
                return;
            }
            
            Debug.Log("[InventoryUI-AutoConnect] ✓ CharacterManager.Instance 존재");
            
            // PlayerCharacter 확인
            if (CharacterManager.Instance.PlayerCharacter == null)
            {
                Debug.LogWarning("[InventoryUI-AutoConnect] ❌ PlayerCharacter가 null입니다!");
                return;
            }
            
            Debug.Log($"[InventoryUI-AutoConnect] ✓ PlayerCharacter 존재: {CharacterManager.Instance.PlayerCharacter.Name}");
            
            // PlayerCharacter.Inventory 확인
            if (CharacterManager.Instance.PlayerCharacter.Inventory == null)
            {
                Debug.LogWarning("[InventoryUI-AutoConnect] ❌ PlayerCharacter.Inventory가 null입니다!");
                return;
            }
            
            Debug.Log($"[InventoryUI-AutoConnect] ✓ PlayerCharacter.Inventory 존재: {CharacterManager.Instance.PlayerCharacter.Inventory.GetDebugInfo()}");
            
            // Character 연결
            ConnectToCharacter(CharacterManager.Instance.PlayerCharacter);
            
            Debug.Log("[InventoryUI-AutoConnect] ✅✅✅ PlayerCharacter 자동 연결 완료 ✅✅✅");
        }
        
        /// <summary>
        /// 특정 Character의 Inventory를 이 UI에 연결합니다.
        /// 확장성: 플레이어가 아닌 다른 Character의 Inventory도 표시 가능
        /// </summary>
        /// <param name="character">표시할 Character</param>
        public void ConnectToCharacter(Character character)
        {
            if (character == null)
            {
                Debug.LogError("[InventoryUI] Character가 null입니다!");
                return;
            }
            
            if (character.Inventory == null)
            {
                Debug.LogError($"[InventoryUI] {character.Name}의 Inventory가 null입니다!");
                return;
            }
            
            // Character 연결
            targetCharacter = character;
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] {character.Name}의 Inventory 연결 완료");
            
            // UI 초기화
            InitializeUI();
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Input System 설정
        
        /// <summary>
        /// UI Action Map 활성화 (B 키 등 UI 입력 활성화)
        /// </summary>
        private void EnableUIActionMap()
        {
            var playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                var uiActionMap = playerInput.actions.FindActionMap("UI");
                if (uiActionMap != null)
                {
                    uiActionMap.Enable();
                    Debug.Log("[InventoryUI] UI Action Map 활성화됨 (인벤토리 토글 키 사용 가능)");
                }
                else
                {
                    Debug.LogWarning("[InventoryUI] UI Action Map을 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogWarning("[InventoryUI] PlayerInput 컴포넌트를 찾을 수 없습니다. 입력이 작동하지 않을 수 있습니다.");
            }
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
        /// <summary>
        /// [Deprecated] 하위 호환성을 위해 남겨둠. ConnectToCharacter()를 사용하세요.
        /// </summary>
        [System.Obsolete("Initialize(CharacterInventory)는 deprecated입니다. ConnectToCharacter(Character)를 사용하세요.")]
        public void Initialize(CharacterInventory inventory)
        {
            if (inventory == null)
            {
                Debug.LogError("[InventoryUI] null 인벤토리가 전달되었습니다!");
                return;
            }
            
            // Inventory의 Owner(Character)를 찾아서 ConnectToCharacter 호출
            if (inventory.Owner != null)
            {
                ConnectToCharacter(inventory.Owner);
            }
            else
            {
                Debug.LogWarning("[InventoryUI] Inventory.Owner가 null입니다! UI 초기화가 제한됩니다.");
            }
        }
        
        /// <summary>
        /// UI 초기화 (내부용)
        /// </summary>
        private void InitializeUI()
        {
            if (Inventory == null)
            {
                Debug.LogError("[InventoryUI] Inventory가 null입니다!");
                return;
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] UI 초기화: {Inventory.inventoryName}");
            
            // ItemDetailPanel 초기화 (InventoryUI 참조 전달)
            if (itemDetailPanel != null)
            {
                itemDetailPanel.Initialize(Inventory, this);
            }
            
            // SwordArtDisplayUI 초기화
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.Initialize(Inventory, this);
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
            if (Inventory == null || equipmentSlotPrefab == null)
                return;
            
            // 기존 슬롯 제거
            ClearEquipmentSlots();
            
            // 무기, 갑옷 슬롯만 메인 장비 패널에 생성
            var mainSlots = Inventory.equipmentSlots
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
            // Character 연결 상태 재확인
            if (!EnsureCharacterConnection())
                return;
            
            for (int i = 0; i < equipmentSlots.Count && i < Inventory.equipmentSlots.Count; i++)
            {
                var slot = Inventory.equipmentSlots[i];
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
            var accessorySlots = Inventory.equipmentSlots
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
        /// 검술 유파 UI 갱신 (EquippedSwordArtStyleUI 사용)
        /// </summary>
        private void CreateSwordArtStyleSlot()
        {
            // EquipmentSlotUI Prefab 대신 EquippedSwordArtStyleUI를 사용
            // 유파는 특별한 슬롯이므로 전용 UI에 직접 표시
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.Refresh();
            }
            
            if (enableDebugLog)
                Debug.Log("[InventoryUI] 검술 유파 UI 갱신 완료");
        }
        
        #endregion
        
        #region 아이템 슬롯 생성 및 관리
        
        /// <summary>
        /// 아이템 슬롯 UI 동적 생성
        /// </summary>
        private void CreateItemSlots()
        {
            if (itemGridContainer == null || itemSlotPrefab == null || Inventory == null)
                return;
            
            // 기존 슬롯 제거
            ClearItemSlots();
            
            // 보유 아이템 수만큼 슬롯 생성
            int itemCount = Inventory.items.Count;
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
            // Character 연결 상태 재확인 (중첩 호출 대비)
            if (!EnsureCharacterConnection())
                return;
            
            int currentItemCount = Inventory.items.Count;
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
            for (int i = 0; i < Inventory.items.Count && i < itemSlots.Count; i++)
            {
                itemSlots[i].Setup(Inventory.items[i]);
            }
            
            if (enableDebugLog)
                Debug.Log($"[InventoryUI] 아이템 그리드 동적 갱신 완료: {Inventory.items.Count}개 아이템, {itemSlots.Count}개 슬롯");
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
        private void OnItemUnequippedEvent(ItemEventData data)
        {
            // Unequip만 단독으로 발생하면 갱신 필요하지만,
            // 아이템 교체 장착 시에는 Unequip → Equip 순서로 발생하므로
            // Unequip에서 RefreshAll() 호출 시 중간 상태(빈 슬롯)가 보임
            // Equip 이벤트에서만 RefreshAll() 호출하도록 변경
            // 단, 장비 해제만 하는 경우를 위해 장비 슬롯만 갱신
            RefreshEquipmentSlots();
            
            // 유파 슬롯 해제 시 유파 UI도 갱신
            if (data.slotType == EquipmentSlotType.SwordArtStyle)
            {
                RefreshSwordArtDisplay();
            }
        }
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
        private Coroutine scrollRoutine;

        private void OnItemSlotClicked(ItemSlotUI clickedSlot)
        {
            Debug.Log($"[InventoryUI] 아이템 슬롯 클릭 감지: {clickedSlot?.name ?? "null"}");
            
            if (clickedSlot == null || clickedSlot.IsEmpty())
            {
                Debug.LogWarning("[InventoryUI] 클릭된 슬롯이 null이거나 비어있습니다!");
                return;
            }
            
            // 기존 장비 슬롯 선택 해제
            if (selectedEquipmentSlot != null)
            {
                selectedEquipmentSlot.SetSelected(false);
                selectedEquipmentSlot = null;
            }
            
            // 유파 슬롯 선택 해제
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.ClearSelection();
            }
            
            // 같은 슬롯 재클릭 시 토글 처리
            bool alreadySelected = (selectedItemSlot == clickedSlot);
            
            if (alreadySelected)
            {
                // 선택 해제
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
                
                // 상세 정보 패널 숨기기
                if (itemDetailPanel != null)
                {
                    itemDetailPanel.HidePanel();
                }
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 아이템 선택 해제 (토글)");
                return;
            }
            
            // 기존 아이템 슬롯 선택 해제
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

            // 선택 슬롯이 뷰포트 밖일 때만 자동 스크롤 (레이아웃 확정 후)
            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
                scrollRoutine = null;
            }
            scrollRoutine = StartCoroutine(ScrollToItemIfOutOfViewDelayed(selectedItemSlot));
        }

        /// <summary>
        /// 선택 슬롯이 뷰포트 밖에 있을 때만 자동 스크롤한다.
        /// 위로 벗어나면 첫 행(상단 정렬), 아래로 벗어나면 마지막 행(하단 정렬)에 맞춘다.
        /// </summary>
        private IEnumerator ScrollToItemIfOutOfViewDelayed(ItemSlotUI targetSlot)
        {
            if (itemScrollRect == null || targetSlot == null)
                yield break;

            var content = itemScrollRect.content;
            var viewport = itemScrollRect.viewport != null ? itemScrollRect.viewport : (RectTransform)itemScrollRect.transform;
            var target = targetSlot.GetComponent<RectTransform>();
            if (content == null || viewport == null || target == null)
                yield break;

            // 레이아웃이 수축/확장된 후 계산되도록 프레임 종료까지 대기
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();

            // 최소 이동으로 가시화: content 로컬 좌표에서 델타 계산
            Vector3[] viewWC = new Vector3[4];
            Vector3[] targetWC = new Vector3[4];
            viewport.GetWorldCorners(viewWC);
            target.GetWorldCorners(targetWC);

            // 뷰포트 상/하단을 content 로컬로 변환
            Vector2 viewTopLocal, viewBottomLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, viewWC[1]), // Top-Left
                null,
                out viewTopLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, viewWC[0]), // Bottom-Left
                null,
                out viewBottomLocal
            );

            // 타겟 상/하단을 content 로컬로 변환
            Vector2 targetTopLocal, targetBottomLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, targetWC[1]),
                null,
                out targetTopLocal
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content,
                RectTransformUtility.WorldToScreenPoint(null, targetWC[0]),
                null,
                out targetBottomLocal
            );

            const float tol = 2f;
            bool above = targetTopLocal.y > viewTopLocal.y - tol;
            bool below = targetBottomLocal.y < viewBottomLocal.y + tol;
            if (!above && !below)
                yield break;

            float viewportHeight = viewport.rect.height;
            float contentHeight = content.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
            Vector2 anchored = content.anchoredPosition;

            if (above)
            {
                // 타겟 상단이 뷰포트 상단으로 살짝 내려오도록 최소 이동
                float delta = targetTopLocal.y - viewTopLocal.y; // >0
                anchored.y = Mathf.Clamp(anchored.y - delta, 0f, maxY);
            }
            else // below
            {
                // 타겟 하단이 뷰포트 하단으로 살짝 올라오도록 최소 이동
                float delta = viewBottomLocal.y - targetBottomLocal.y; // >0
                anchored.y = Mathf.Clamp(anchored.y + delta, 0f, maxY);
            }

            content.anchoredPosition = anchored;
        }

        private Vector2 ClampContentY(RectTransform content, RectTransform viewport, Vector2 anchored)
        {
            float contentHeight = content.rect.height;
            float viewportHeight = viewport.rect.height;
            float maxY = Mathf.Max(0f, contentHeight - viewportHeight);
            anchored.y = Mathf.Clamp(anchored.y, 0f, maxY);
            return anchored;
        }
        
        /// <summary>
        /// 장비 슬롯 클릭 처리
        /// </summary>
        private void OnEquipmentSlotClicked(EquipmentSlotUI clickedSlot)
        {
            Debug.Log($"[InventoryUI] 장비 슬롯 클릭 이벤트 수신: {clickedSlot?.name ?? "null"}");
            
            if (clickedSlot == null || Inventory == null)
            {
                Debug.LogWarning($"[InventoryUI] 클릭 이벤트 무시: clickedSlot={clickedSlot != null}, Inventory={Inventory != null}");
                return;
            }
            
            var equipSlot = clickedSlot.GetEquipmentSlot();
            if (equipSlot == null)
            {
                Debug.LogWarning("[InventoryUI] EquipmentSlot 데이터가 null입니다!");
                return;
            }
            
            // 빈 슬롯은 선택하지 않음
            if (equipSlot.IsEmpty())
            {
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 빈 장비 슬롯은 선택할 수 없습니다.");
                return;
            }
            
            // 기존 아이템 슬롯 선택 해제
            if (selectedItemSlot != null)
            {
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
            }
            
            // 유파 슬롯 선택 해제
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.ClearSelection();
            }
            
            // 같은 슬롯 재클릭 시 토글 처리
            bool alreadySelected = (selectedEquipmentSlot == clickedSlot);
            
            if (alreadySelected)
            {
                // 선택 해제
                selectedEquipmentSlot.SetSelected(false);
                selectedEquipmentSlot = null;
                
                // 상세 정보 패널 숨기기
                if (itemDetailPanel != null)
                {
                    itemDetailPanel.HidePanel();
                }
                
                if (enableDebugLog)
                    Debug.Log("[InventoryUI] 장비 슬롯 선택 해제 (토글)");
                return;
            }
            
            // 기존 장비 슬롯 선택 해제
            if (selectedEquipmentSlot != null)
            {
                selectedEquipmentSlot.SetSelected(false);
            }
            
            // 새 장비 슬롯 선택
            selectedEquipmentSlot = clickedSlot;
            selectedEquipmentSlot.SetSelected(true);
            
            // 장착된 아이템의 상세 정보 표시
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
            // Character 연결 상태 확인
            if (!EnsureCharacterConnection())
            {
                Debug.LogWarning("[InventoryUI] RefreshAll: Character가 연결되지 않아 UI를 갱신할 수 없습니다.");
                return;
            }
            
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
        /// 패널 활성화 상태 확인
        /// </summary>
        public bool IsPanelActive => panel != null && panel.activeSelf;
        
        #endregion
        
        #region 포커스 관리
        
        // 현재 선택된 장비 슬롯
        private EquipmentSlotUI selectedEquipmentSlot;
        
        /// <summary>
        /// 특정 장비 슬롯으로 포커스 이동 (EquipmentSlot 인스턴스 기반)
        /// </summary>
        /// <param name="equipSlot">포커스를 이동할 장비 슬롯</param>
        public void SetFocusToEquipmentSlot(EquipmentSlot equipSlot)
        {
            if (equipSlot == null)
                return;
            
            // 유파 슬롯인 경우 특수 처리
            if (equipSlot.slotType == EquipmentSlotType.SwordArtStyle)
            {
                SetFocusToSwordArtStyleSlot(equipSlot);
                return;
            }
            
            if (equipmentSlots == null || equipmentSlots.Count == 0)
                return;
            
            // 해당 EquipmentSlot에 대응하는 UI 찾기
            var targetSlot = equipmentSlots.FirstOrDefault(slotUI => 
                slotUI.GetEquipmentSlot() == equipSlot);
            
            if (targetSlot != null)
            {
                // 기존 아이템 선택 해제
                if (selectedItemSlot != null)
                {
                    selectedItemSlot.SetSelected(false);
                    selectedItemSlot = null;
                }
                
                // 기존 장비 슬롯 선택 해제
                if (selectedEquipmentSlot != null)
                {
                    selectedEquipmentSlot.SetSelected(false);
                }
                
                // 새 장비 슬롯 선택
                selectedEquipmentSlot = targetSlot;
                selectedEquipmentSlot.SetSelected(true);
                
                // Unity EventSystem으로 선택 (파란색 하이라이트)
                var selectable = targetSlot.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null)
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(targetSlot.gameObject);
                }
                
                // 상세 패널 업데이트 (장착된 아이템 표시)
                if (!targetSlot.IsEmpty() && itemDetailPanel != null)
                {
                    var tempOwnedItem = new OwnedItem(equipSlot.equippedItemKey, 1);
                    tempOwnedItem.isEquipped = true;
                    itemDetailPanel.ShowItem(tempOwnedItem);
                }
                
                if (enableDebugLog)
                    Debug.Log($"[InventoryUI] 포커스 이동: {equipSlot.slotName} 슬롯");
            }
            else
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[InventoryUI] 슬롯 UI를 찾을 수 없음: {equipSlot.slotName}");
            }
        }
        
        /// <summary>
        /// 특정 장비 슬롯 타입으로 포커스 이동 (하위 호환용)
        /// 주의: 장신구처럼 같은 타입이 여러 개 있으면 첫 번째만 선택됨
        /// </summary>
        public void SetFocusToEquipmentSlot(EquipmentSlotType slotType)
        {
            if (Inventory == null || Inventory.equipmentSlots == null)
                return;
            
            // CharacterInventory의 EquipmentSlot 찾기
            var equipSlot = Inventory.equipmentSlots.FirstOrDefault(s => s.slotType == slotType);
            if (equipSlot != null)
            {
                SetFocusToEquipmentSlot(equipSlot);
            }
        }
        
        /// <summary>
        /// 유파 슬롯으로 포커스 이동
        /// </summary>
        private void SetFocusToSwordArtStyleSlot(EquipmentSlot equipSlot)
        {
            if (EquippedSwordArtStyleUI == null || equipSlot.IsEmpty())
                return;
            
            // 기존 아이템/장비 슬롯 선택 해제
            if (selectedItemSlot != null)
            {
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
            }
            
            if (selectedEquipmentSlot != null)
            {
                selectedEquipmentSlot.SetSelected(false);
                selectedEquipmentSlot = null;
            }
            
            // 유파 슬롯 선택 (EquippedSwordArtStyleUI를 통해)
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.SetSelected(true);
            }
            
            // ItemDetailPanel에 유파 아이템 표시
            if (itemDetailPanel != null)
            {
                var tempOwnedItem = new OwnedItem(equipSlot.equippedItemKey, 1);
                tempOwnedItem.isEquipped = true;
                itemDetailPanel.ShowItem(tempOwnedItem);
                
                if (enableDebugLog)
                    Debug.Log($"[InventoryUI] 유파 슬롯 포커스: {equipSlot.slotName}");
            }
        }
        
        /// <summary>
        /// 모든 선택 상태 해제 (외부에서 호출용)
        /// </summary>
        public void ClearAllSelections()
        {
            if (selectedItemSlot != null)
            {
                selectedItemSlot.SetSelected(false);
                selectedItemSlot = null;
            }
            
            if (selectedEquipmentSlot != null)
            {
                selectedEquipmentSlot.SetSelected(false);
                selectedEquipmentSlot = null;
            }
            
            if (EquippedSwordArtStyleUI != null)
            {
                EquippedSwordArtStyleUI.ClearSelection();
            }
        }
        
        /// <summary>
        /// 특정 아이템으로 포커스 이동 및 스크롤
        /// </summary>
        /// <param name="itemKey">포커스를 이동할 아이템 키</param>
        public void SetFocusToItem(string itemKey)
        {
            if (itemSlots == null || itemSlots.Count == 0 || string.IsNullOrEmpty(itemKey))
                return;
            
            // 해당 아이템 슬롯 찾기 (장착되지 않은 아이템만)
            ItemSlotUI targetSlot = null;
            foreach (var slot in itemSlots)
            {
                if (slot != null && !slot.IsEmpty())
                {
                    var ownedItem = slot.GetOwnedItem();
                    if (ownedItem != null && ownedItem.itemKey == itemKey && !ownedItem.isEquipped)
                    {
                        targetSlot = slot;
                        break;
                    }
                }
            }
            
            if (targetSlot != null)
            {
                // 기존 아이템 선택 해제
                if (selectedItemSlot != null)
                {
                    selectedItemSlot.SetSelected(false);
                }
                
                // 기존 장비 슬롯 선택 해제
                if (selectedEquipmentSlot != null)
                {
                    selectedEquipmentSlot.SetSelected(false);
                    selectedEquipmentSlot = null;
                }
                
                // 새 아이템 선택
                selectedItemSlot = targetSlot;
                selectedItemSlot.SetSelected(true);
                
                // Unity EventSystem으로 선택 (파란색 하이라이트)
                var selectable = targetSlot.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null)
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(targetSlot.gameObject);
                }
                
                // 상세 패널 업데이트
                if (itemDetailPanel != null)
                {
                    var item = selectedItemSlot.GetOwnedItem();
                    itemDetailPanel.ShowItem(item);
                }
                
                // 자동 스크롤
                if (scrollRoutine != null)
                {
                    StopCoroutine(scrollRoutine);
                    scrollRoutine = null;
                }
                scrollRoutine = StartCoroutine(ScrollToItemIfOutOfViewDelayed(selectedItemSlot));
                
                if (enableDebugLog)
                    Debug.Log($"[InventoryUI] 포커스 이동: {itemKey} (파란색 하이라이트)");
            }
            else
            {
                // 아이템을 찾을 수 없으면 (모두 장착되었거나 없음)
                if (enableDebugLog)
                    Debug.LogWarning($"[InventoryUI] 아이템 '{itemKey}'를 찾을 수 없습니다 (장착되었거나 제거됨)");
            }
        }
        
        #endregion
        
        #region 데이터 접근
        
        /// <summary>
        /// 현재 인벤토리 반환
        /// </summary>
        public CharacterInventory GetInventory()
        {
            return Inventory;
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
            Debug.Log($"  - Inventory: {(Inventory != null ? Inventory.inventoryName : "null")}");
            Debug.Log($"  - Target Character: {(targetCharacter != null ? targetCharacter.Name : "null")}");
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

