using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using BladeAction.Item;

namespace BladeAction.UI
{
    /// <summary>
    /// 인벤토리 그리드의 개별 아이템 슬롯 UI 컴포넌트
    /// OwnedItem 데이터를 시각적으로 표시하고 클릭 이벤트를 처리합니다.
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 컴포넌트 참조")]
        [Tooltip("아이템 아이콘 이미지")]
        [SerializeField] private Image iconImage;
        
        [Tooltip("아이템 수량 텍스트")]
        [SerializeField] private TextMeshProUGUI quantityText;
        
        [Tooltip("배경 이미지")]
        [SerializeField] private Image backgroundImage;
        
        [Tooltip("선택 하이라이트 이미지")]
        [SerializeField] private Image highlightImage;
        
        [Header("기본 아이콘 설정")]
        [Tooltip("빈 슬롯일 때 표시할 아이콘 (null이면 비활성화)")]
        [SerializeField] private Sprite emptySlotIcon;
        
        // 슬롯 데이터
        private OwnedItem ownedItem;
        private bool isSelected = false;
        
        // 선택 상태 관리 컴포넌트
        private SelectableSlotUI selectableSlot;
        
        // 클릭 이벤트 콜백
        public System.Action<ItemSlotUI> OnSlotClicked;
        
        #region 초기화 및 설정
        
        private void Awake()
        {
            // SelectableSlotUI 컴포넌트 가져오기 또는 추가
            selectableSlot = GetComponent<SelectableSlotUI>();
            if (selectableSlot == null)
            {
                selectableSlot = gameObject.AddComponent<SelectableSlotUI>();
                Debug.Log($"[ItemSlotUI] SelectableSlotUI 컴포넌트 자동 추가: {gameObject.name}");
            }
            
            // SelectableSlotUI가 자체 색상 설정을 사용
            
            // 컴포넌트 null 체크
            ValidateComponents();
            
            // 초기 상태 설정
            SetSelected(false);
            Clear();
        }
        
        /// <summary>
        /// 컴포넌트 유효성 검증
        /// </summary>
        private void ValidateComponents()
        {
            if (iconImage == null)
                Debug.LogWarning($"[ItemSlotUI] iconImage가 할당되지 않았습니다: {gameObject.name}", this);
            
            if (quantityText == null)
                Debug.LogWarning($"[ItemSlotUI] quantityText가 할당되지 않았습니다: {gameObject.name}", this);
            
            if (backgroundImage == null)
                Debug.LogWarning($"[ItemSlotUI] backgroundImage가 할당되지 않았습니다: {gameObject.name}", this);
            
            if (highlightImage == null)
                Debug.LogWarning($"[ItemSlotUI] highlightImage가 할당되지 않았습니다: {gameObject.name}", this);
        }
        
        #endregion
        
        #region 슬롯 설정 및 표시
        
        /// <summary>
        /// 아이템 데이터로 슬롯 설정
        /// </summary>
        /// <param name="item">표시할 OwnedItem (null이면 빈 슬롯)</param>
        public void Setup(OwnedItem item)
        {
            this.ownedItem = item;
            
            if (item == null || item.IsEmpty())
            {
                Clear();
                return;
            }
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 슬롯 표시 갱신
        /// </summary>
        private void UpdateDisplay()
        {
            if (ownedItem == null || ownedItem.IsEmpty())
            {
                Clear();
                return;
            }
            
            // 아이템 데이터 가져오기
            BladeAction.Item.Item itemData = ownedItem.GetItemData();
            
            if (itemData == null)
            {
                Debug.LogWarning($"[ItemSlotUI] 아이템 데이터를 찾을 수 없습니다: {ownedItem.itemKey}");
                Clear();
                return;
            }
            
            // 아이콘 설정
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
            }
            
            // 수량 텍스트 설정
            if (quantityText != null)
            {
                // 수량이 1보다 크면 표시, 1이면 숨김
                if (ownedItem.quantity > 1)
                {
                    quantityText.text = ownedItem.quantity.ToString();
                    quantityText.enabled = true;
                }
                else
                {
                    quantityText.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// 슬롯 비우기 (빈 슬롯 상태로 전환)
        /// </summary>
        public void Clear()
        {
            ownedItem = null;
            
            // 아이콘 초기화
            if (iconImage != null)
            {
                if (emptySlotIcon != null)
                {
                    iconImage.sprite = emptySlotIcon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }
            }
            
            // 수량 텍스트 숨김
            if (quantityText != null)
            {
                quantityText.enabled = false;
            }
        }
        
        #endregion
        
        #region 선택 상태 관리
        
        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        /// <param name="selected">선택 여부</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // SelectableSlotUI에 위임
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(selected);
            }
        }
        
        /// <summary>
        /// 현재 선택 상태 반환
        /// </summary>
        public bool IsSelected()
        {
            return isSelected;
        }
        
        #endregion
        
        #region 이벤트 처리
        
        /// <summary>
        /// 마우스 클릭 이벤트 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 빈 슬롯은 클릭 무시
            if (ownedItem == null || ownedItem.IsEmpty())
                return;
            
            // 콜백 호출
            OnSlotClicked?.Invoke(this);
        }
        
        #endregion
        
        #region 데이터 접근
        
        /// <summary>
        /// 슬롯의 OwnedItem 데이터 반환
        /// </summary>
        public OwnedItem GetOwnedItem()
        {
            return ownedItem;
        }
        
        /// <summary>
        /// 슬롯이 비어있는지 확인
        /// </summary>
        public bool IsEmpty()
        {
            return ownedItem == null || ownedItem.IsEmpty();
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        [ContextMenu("Print Debug Info")]
        private void PrintDebugInfo()
        {
            Debug.Log($"[ItemSlotUI] {gameObject.name}");
            Debug.Log($"  - OwnedItem: {(ownedItem != null ? ownedItem.ToString() : "null")}");
            Debug.Log($"  - IsSelected: {isSelected}");
            Debug.Log($"  - IsEmpty: {IsEmpty()}");
        }
        
        #endregion
    }
}

