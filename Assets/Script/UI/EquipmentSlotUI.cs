using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using BladeAction.Item;

namespace BladeAction.UI
{
    /// <summary>
    /// 장비 슬롯 UI 컴포넌트 (무기, 갑옷, 장신구, 검술 유파)
    /// EquipmentSlot 데이터를 시각적으로 표시하고 클릭 이벤트를 처리합니다.
    /// </summary>
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, ISlotDragSource, ISlotDropTarget
    {
        [Header("UI 컴포넌트 참조")]
        [Tooltip("장착된 아이템 아이콘")]
        [SerializeField] private Image iconImage;
        
        [Tooltip("장착된 아이템 이름 텍스트 (장신구는 null 가능)")]
        [SerializeField] private TextMeshProUGUI nameText;
        
        [Tooltip("슬롯 이름 텍스트 (예: '무기', '갑옷')")]
        [SerializeField] private TextMeshProUGUI slotNameText;
        
        [Tooltip("장착 상태 테두리 이미지 (아이템 장착 시 표시)")]
        [SerializeField] private Image frameImage_equipped;
        
        
        [Header("기본 아이콘 설정")]
        [Tooltip("빈 슬롯일 때 표시할 아이콘")]
        [SerializeField] private Sprite emptySlotIcon;
        
        [Header("슬롯 타입 설정")]
        [Tooltip("이 UI가 표시할 슬롯 타입")]
        [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.None;
        
        [Tooltip("장신구 슬롯인 경우 텍스트 숨김")]
        [SerializeField] private bool hideTextForAccessory = true;
        
        [Header("디버그")]
        [Tooltip("클릭 테스트용")]
        [SerializeField] private bool enableClickTest = true;
        
        // 컬러 상수
        private static readonly Color DISABLED_COLOR = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 회색 반투명 (비활성화)
        
        // 슬롯 데이터
        private EquipmentSlot equipmentSlot;
        
        // 현재 표시 중인 아이템 (드래그 앤 드롭용)
        private OwnedItem currentItem;
        
        // 선택 상태
        private bool isSelected = false;
        
        // 선택 상태 관리 컴포넌트
        private SelectableSlotUI selectableSlot;
        
        // 클릭 이벤트 콜백
        public System.Action<EquipmentSlotUI> OnSlotClicked;
        
        #region 초기화 및 설정
        
        private void Awake()
        {
            // SelectableSlotUI 컴포넌트 가져오기 또는 추가
            selectableSlot = GetComponent<SelectableSlotUI>();
            if (selectableSlot == null)
            {
                selectableSlot = gameObject.AddComponent<SelectableSlotUI>();
                Debug.Log($"[EquipmentSlotUI] SelectableSlotUI 컴포넌트 자동 추가: {gameObject.name}");
            }
            
            // 컴포넌트 null 체크
            ValidateComponents();
            
            // 초기 상태: 선택 해제
            isSelected = false;
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(false);
            }
            
            // 초기 상태: 장착 테두리 숨김
            if (frameImage_equipped != null)
            {
                frameImage_equipped.enabled = false;
            }
        }
        
        /// <summary>
        /// 컴포넌트 유효성 검증
        /// </summary>
        private void ValidateComponents()
        {
            if (iconImage == null)
                Debug.LogWarning($"[EquipmentSlotUI] iconImage가 할당되지 않았습니다: {gameObject.name}", this);
            
            // nameText는 장신구 슬롯에서는 선택사항
            if (nameText == null && slotType != EquipmentSlotType.Accessory)
                Debug.LogWarning($"[EquipmentSlotUI] nameText가 할당되지 않았습니다: {gameObject.name}", this);
        }
        
        #endregion
        
        #region 슬롯 설정 및 표시
        
        /// <summary>
        /// 장비 슬롯 데이터로 UI 설정
        /// </summary>
        /// <param name="slot">표시할 EquipmentSlot</param>
        public void Setup(EquipmentSlot slot, bool hideTextForAccessorySlot = false)
        {
            this.equipmentSlot = slot;
            
            if (slot == null)
            {
                Debug.LogWarning($"[EquipmentSlotUI] null 슬롯이 전달되었습니다: {gameObject.name}");
                Clear();
                return;
            }
            
            // 슬롯 타입 동기화
            this.slotType = slot.slotType;
            
            // 장신구 텍스트 숨김 플래그 설정
            this.hideTextForAccessory = hideTextForAccessorySlot;
            
            // Setup 시 선택 상태 초기화 (UI 리프레시 시 선택 해제)
            isSelected = false;
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(false);
            }
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 슬롯 표시 갱신
        /// </summary>
        public void UpdateDisplay()
        {
            if (equipmentSlot == null)
            {
                Clear();
                return;
            }
            
            // 슬롯 이름 표시
            if (slotNameText != null)
            {
                if (hideTextForAccessory && slotType == EquipmentSlotType.Accessory)
                {
                    slotNameText.gameObject.SetActive(false);
                }
                else
                {
                    slotNameText.text = equipmentSlot.slotName;
                    slotNameText.gameObject.SetActive(true);
                }
            }
            
            // 장착된 아이템 표시
            if (equipmentSlot.IsEmpty())
            {
                ShowEmptySlot();
            }
            else
            {
                ShowEquippedItem();
            }
            
            // 비활성화 상태 처리
            if (!equipmentSlot.IsAvailable())
            {
                ApplyDisabledState();
            }
        }
        
        /// <summary>
        /// 빈 슬롯 상태 표시
        /// </summary>
        private void ShowEmptySlot()
        {
            currentItem = null; // 드래그 불가능
            
            // 빈 슬롯 아이콘 표시
            if (iconImage != null)
            {
                iconImage.sprite = emptySlotIcon;
                iconImage.enabled = emptySlotIcon != null;
            }
            
            // 아이템 이름 숨김
            if (nameText != null)
            {
                if (hideTextForAccessory && slotType == EquipmentSlotType.Accessory)
                {
                    nameText.gameObject.SetActive(false);
                }
                else
                {
                    nameText.text = "없음";
                    nameText.gameObject.SetActive(true);
                    nameText.enabled = true;
                }
            }
            
            // 장착 테두리 업데이트
            UpdateEquippedFrame();
        }
        
        /// <summary>
        /// 장착된 아이템 표시
        /// </summary>
        private void ShowEquippedItem()
        {
            BladeAction.Item.Item itemData = equipmentSlot.GetEquippedItem();
            
            if (itemData == null)
            {
                Debug.LogWarning($"[EquipmentSlotUI] 아이템 데이터를 찾을 수 없습니다: {equipmentSlot.equippedItemKey}");
                ShowEmptySlot();
                currentItem = null;
                return;
            }
            
            // OwnedItem 가져오기 (드래그 앤 드롭용)
            var inventoryUI = GetComponentInParent<InventoryUI>();
            if (inventoryUI != null && inventoryUI.GetInventory() != null)
            {
                currentItem = new OwnedItem(equipmentSlot.equippedItemKey, equipmentSlot.equippedQuantity);
            }
            else
            {
                currentItem = null;
            }
            
            // 아이템 아이콘 표시
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
            }
            
            // 아이템 이름 표시
            if (nameText != null)
            {
                if (hideTextForAccessory && slotType == EquipmentSlotType.Accessory)
                {
                    nameText.gameObject.SetActive(false);
                }
                else
                {
                    nameText.text = itemData.itemName;
                    nameText.gameObject.SetActive(true);
                    nameText.enabled = true;
                }
            }
            
            // 장착 테두리 업데이트
            UpdateEquippedFrame();
        }
        
        /// <summary>
        /// 비활성화 상태 적용
        /// </summary>
        private void ApplyDisabledState()
        {
            if (iconImage != null)
            {
                iconImage.color = DISABLED_COLOR;
            }
            
            if (nameText != null)
            {
                nameText.color = DISABLED_COLOR;
            }
            
            if (frameImage_equipped != null)
            {
                frameImage_equipped.enabled = false; // 비활성화 시 테두리 숨김
            }
        }
        
        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void Clear()
        {
            equipmentSlot = null;
            
            // 아이콘 초기화
            if (iconImage != null)
            {
                iconImage.sprite = emptySlotIcon;
                iconImage.enabled = emptySlotIcon != null;
            }
            
            // 이름 텍스트 초기화
            if (nameText != null)
            {
                nameText.text = "";
                nameText.enabled = false;
            }
            
            // 슬롯 이름 초기화
            if (slotNameText != null)
            {
                slotNameText.text = "";
            }
            
            // 선택 상태 초기화
            isSelected = false;
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(false);
            }
            
            // 장착 테두리 초기화 (숨김)
            if (frameImage_equipped != null)
            {
                frameImage_equipped.enabled = false;
            }
        }
        
        #endregion
        
        #region 이벤트 처리
        
        /// <summary>
        /// 마우스 클릭 이벤트 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (enableClickTest)
            {
                Debug.Log($"[EquipmentSlotUI] 클릭 감지: {equipmentSlot?.slotName ?? "null"}");
                Debug.Log($"[EquipmentSlotUI] GameObject: {gameObject.name}, 활성화: {gameObject.activeInHierarchy}");
                Debug.Log($"[EquipmentSlotUI] Image: {GetComponent<Image>() != null}");
                Debug.Log($"[EquipmentSlotUI] Raycast Target: {GetComponent<Image>()?.raycastTarget ?? false}");
            }
            
            // 비활성화된 슬롯은 클릭 무시
            if (equipmentSlot == null || !equipmentSlot.IsAvailable())
            {
                Debug.LogWarning($"[EquipmentSlotUI] 클릭 무시: 슬롯={equipmentSlot?.slotName ?? "null"}, 사용가능={equipmentSlot?.IsAvailable() ?? false}");
                return;
            }
            
            Debug.Log($"[EquipmentSlotUI] 이벤트 발생: {equipmentSlot.slotName}");
            // 콜백 호출
            OnSlotClicked?.Invoke(this);
        }
        
        #endregion
        
        #region 선택 상태 관리
        
        /// <summary>
        /// 슬롯 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // SelectableSlotUI에 위임 (선택 테두리는 SelectableSlotUI가 관리)
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(selected);
            }
            
            // 장착 테두리(frameImage_equipped)는 선택 상태와 독립적으로 유지
        }
        
        /// <summary>
        /// 장착 테두리 표시 업데이트 (아이템 장착 시에만 표시, 색상은 프리팹 설정 유지)
        /// </summary>
        public void UpdateEquippedFrame()
        {
            if (frameImage_equipped == null) return;
            
            // 아이템이 장착되어 있을 때만 테두리 표시 (프리팹 색상 유지)
            if (equipmentSlot != null && !equipmentSlot.IsEmpty() && equipmentSlot.IsAvailable())
            {
                frameImage_equipped.enabled = true;
            }
            else
            {
                // 빈 슬롯이거나 비활성화된 슬롯은 테두리 숨김
                frameImage_equipped.enabled = false;
            }
        }
        
        #endregion
        
        #region 데이터 접근
        
        /// <summary>
        /// 슬롯의 EquipmentSlot 데이터 반환
        /// </summary>
        public EquipmentSlot GetEquipmentSlot()
        {
            return equipmentSlot;
        }
        
        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsEmpty()
        {
            return equipmentSlot == null || equipmentSlot.IsEmpty();
        }
        
        /// <summary>
        /// 슬롯 타입 반환
        /// </summary>
        public EquipmentSlotType GetSlotType()
        {
            return slotType;
        }
        
        /// <summary>
        /// 장착 테두리 Image 반환 (드래그 앤 드롭 하이라이트용)
        /// </summary>
        public Image GetFrameImage()
        {
            return frameImage_equipped;
        }
        
        #endregion
        
        #region 드래그 앤 드롭 (ISlotDragSource, ISlotDropTarget)
        
        public object GetDragData()
        {
            return currentItem;
        }
        
        public bool CanStartDrag()
        {
            return currentItem != null && !IsEmpty();
        }
        
        public void OnDragComplete(bool success)
        {
            // 드래그 완료 후 처리 (필요시)
        }
        
        public bool CanAcceptDrop(object dragData, ISlotDragSource source = null)
        {
            if (dragData is OwnedItem item)
            {
                var itemData = item.GetItemData();
                if (itemData == null) return false;
                
                // 슬롯 타입과 아이템 타입이 일치하는지 확인
                return (slotType == EquipmentSlotType.Weapon && itemData.itemType == ItemType.Weapon) ||
                       (slotType == EquipmentSlotType.Armor && itemData.itemType == ItemType.Armor) ||
                       (slotType == EquipmentSlotType.Accessory && itemData.itemType == ItemType.Accessory) ||
                       (slotType == EquipmentSlotType.SwordArtStyle && itemData.itemType == ItemType.SwordArtStyle);
            }
            return false;
        }
        
        public void OnDropHover(object dragData)
        {
            // 드롭 가능 시 시각적 피드백 (선택 사항)
        }
        
        public void OnDropExit()
        {
            // 드롭 가능 시 시각적 피드백 해제 (선택 사항)
        }
        
        public void OnDropReceived(object dragData, ISlotDragSource source)
        {
            if (dragData is OwnedItem item)
            {
                var inventoryUI = GetComponentInParent<InventoryUI>();
                if (inventoryUI == null || inventoryUI.GetInventory() == null)
                    return;
                
                var itemData = item.GetItemData();
                if (itemData == null)
                    return;
                
                // 직접 인벤토리에 장착 요청
                bool success = inventoryUI.GetInventory().EquipItem(item.itemKey, slotType, out var equippedSlot);
                
                if (success && equippedSlot != null)
                {
                    // UI 갱신은 ItemEvents가 자동 처리
                    // 포커스를 장착된 슬롯으로 이동
                    inventoryUI.SetFocusToEquipmentSlot(equippedSlot);
                }
            }
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Print Debug Info")]
        private void PrintDebugInfo()
        {
            Debug.Log($"[EquipmentSlotUI] {gameObject.name}");
            Debug.Log($"  - SlotType: {slotType}");
            Debug.Log($"  - EquipmentSlot: {(equipmentSlot != null ? equipmentSlot.ToString() : "null")}");
            Debug.Log($"  - IsEmpty: {IsEmpty()}");
        }
        
        #endregion
    }
}

