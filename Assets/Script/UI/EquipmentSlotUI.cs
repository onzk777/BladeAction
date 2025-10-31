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
    public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 컴포넌트 참조")]
        [Tooltip("장착된 아이템 아이콘")]
        [SerializeField] private Image iconImage;
        
        [Tooltip("장착된 아이템 이름 텍스트 (장신구는 null 가능)")]
        [SerializeField] private TextMeshProUGUI nameText;
        
        [Tooltip("슬롯 이름 텍스트 (예: '무기', '갑옷')")]
        [SerializeField] private TextMeshProUGUI slotNameText;
        
        [Tooltip("테두리 이미지")]
        [SerializeField] private Image frameImage;
        
        
        [Header("기본 아이콘 설정")]
        [Tooltip("빈 슬롯일 때 표시할 아이콘")]
        [SerializeField] private Sprite emptySlotIcon;
        
        [Header("슬롯 타입 설정")]
        [Tooltip("이 UI가 표시할 슬롯 타입")]
        [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.None;
        
        [Tooltip("장신구 슬롯인 경우 텍스트 숨김")]
        [SerializeField] private bool hideTextForAccessory = true;
        
        [Header("색상 설정")]
        [Tooltip("장착된 아이템이 있을 때 테두리 색상")]
        [SerializeField] private Color equippedFrameColor = new Color(1f, 0.843f, 0f, 1f); // 황금색
        
        [Tooltip("비활성화 색상")]
        [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
        [Header("디버그")]
        [Tooltip("클릭 테스트용")]
        [SerializeField] private bool enableClickTest = true;
        
        // 슬롯 데이터
        private EquipmentSlot equipmentSlot;
        
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
                // FrameColor 모드 사용 (EquipmentSlotUI는 frameImage만 사용)
                selectableSlot.SetDisplayMode(SelectionDisplayMode.FrameColor);
                Debug.Log($"[EquipmentSlotUI] SelectableSlotUI 컴포넌트 자동 추가: {gameObject.name}");
            }
            
            // SelectableSlotUI가 자체 색상 설정을 사용
            // EquipmentSlotUI는 장착 상태 색상(equippedFrameColor)만 별도 관리
            
            // 컴포넌트 null 체크
            ValidateComponents();
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
            // 빈 슬롯 아이콘 표시
            if (iconImage != null)
            {
                iconImage.sprite = emptySlotIcon;
                iconImage.enabled = emptySlotIcon != null;
                iconImage.color = Color.white;
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
            
            // 테두리 색상 업데이트
            UpdateFrameColor();
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
                return;
            }
            
            // 아이템 아이콘 표시
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
                iconImage.color = Color.white;
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
            
            // 테두리 색상 업데이트
            UpdateFrameColor();
        }
        
        /// <summary>
        /// 비활성화 상태 적용
        /// </summary>
        private void ApplyDisabledState()
        {
            if (iconImage != null)
            {
                iconImage.color = disabledColor;
            }
            
            if (nameText != null)
            {
                nameText.color = disabledColor;
            }
            
            if (frameImage != null)
            {
                frameImage.color = disabledColor;
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
                iconImage.color = Color.white;
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
            
            // 테두리 색상 초기화
            UpdateFrameColor();
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
            
            // SelectableSlotUI에 위임
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(selected);
                
                // 선택 해제 시 장착 상태 색상 복원
                if (!selected)
                {
                    UpdateFrameColor();
                }
            }
            else
            {
                // Fallback: 직접 색상 변경
                UpdateFrameColor();
            }
        }
        
        /// <summary>
        /// 테두리 색상 업데이트 (장착 상태 우선, 선택 상태는 SelectableSlotUI가 처리)
        /// </summary>
        private void UpdateFrameColor()
        {
            if (frameImage == null) return;
            
            // 장착 상태와 비활성화 상태는 EquipmentSlotUI가 직접 관리
            // 선택 상태는 SelectableSlotUI가 처리하므로 여기서는 장착/비활성화만 처리
            
            if (equipmentSlot != null && !equipmentSlot.IsAvailable())
            {
                // 비활성화 상태 (최우선)
                frameImage.color = disabledColor;
                return;
            }
            
            // 선택 상태는 SelectableSlotUI가 이미 처리했으므로 스킵
            // 단, 선택되지 않았을 때만 장착/기본 색상 적용
            if (!isSelected)
            {
                if (equipmentSlot != null && !equipmentSlot.IsEmpty())
                {
                    // 장착됨
                    frameImage.color = equippedFrameColor;
                }
                else
                {
                    // 기본 (빈 슬롯) - SelectableSlotUI의 normalFrameColor 사용
                    // 직접 설정하지 않으면 SelectableSlotUI가 이미 설정한 색상 유지
                }
            }
            // 선택됨 상태는 SelectableSlotUI가 이미 색상 변경했음
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

