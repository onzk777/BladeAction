using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

namespace BladeAction.UI
{
    /// <summary>
    /// 검술 슬롯 UI 컴포넌트
    /// ActionCommandData를 시각적으로 표시하고 클릭/드래그 이벤트를 처리합니다.
    /// </summary>
    public class ActionCommandSlotUI : MonoBehaviour, IPointerClickHandler, ISlotDragSource, ISlotDropTarget
    {
        [Header("UI 컴포넌트 참조")]
        [Tooltip("검술 아이콘 이미지")]
        [SerializeField] private Image iconImage;
        
        [Tooltip("검술 카테고리 텍스트 (습득/유파 표시)")]
        [SerializeField] private TextMeshProUGUI commandCategoryText;
        
        [Tooltip("검술 이름 텍스트")]
        [SerializeField] private TextMeshProUGUI commandNameText;
        
        [Header("태그 동적 생성")]
        [Tooltip("태그 텍스트 프리팹")]
        [SerializeField] private GameObject tagTextPrefab;
        
        [Tooltip("태그가 생성될 컨테이너")]
        [SerializeField] private Transform tagContainer;
        
        // 동적 생성된 태그 텍스트 캐시
        private List<GameObject> spawnedTags = new List<GameObject>();
        
        [Tooltip("배경 이미지")]
        [SerializeField] private Image backgroundImage;
        
        [Tooltip("선택 하이라이트 이미지")]
        [SerializeField] private Image highlightImage;
        
        [Header("기본 아이콘 설정")]
        [Tooltip("빈 슬롯일 때 표시할 아이콘")]
        [SerializeField] private Sprite emptySlotIcon;
        
        [Header("색상 설정")]
        [Tooltip("습득 검술 배경 색상")]
        [SerializeField] private Color acquiredBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        [Tooltip("유파 검술 배경 색상")]
        [SerializeField] private Color styleBackgroundColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        
        // 슬롯 데이터
        private ActionCommandData actionData;
        private ActionCommandEquipUI parentUI;
        private int slotIndex = -1;  // 장착 슬롯인 경우 인덱스 (0~3), 그리드 슬롯이면 -1
        private bool isEquippedSlot = false;
        private bool isSelected = false;
        private bool isStyleAction = false;  // 유파 검술인지 여부
        private bool isEnhancedByStyle = false;  // 습득 검술이 유파로 강화되었는지 여부
        
        // 선택 상태 관리 컴포넌트
        private SelectableSlotUI selectableSlot;
        
        // 프로퍼티
        public ActionCommandData ActionData => actionData;
        public int SlotIndex => slotIndex;
        public bool IsEquippedSlot => isEquippedSlot;
        public bool IsStyleAction => isStyleAction;
        public bool IsEnhancedByStyle => isEnhancedByStyle;
        
        #region 초기화 및 설정
        
        private void Awake()
        {
            // SelectableSlotUI 컴포넌트 가져오기 또는 추가
            selectableSlot = GetComponent<SelectableSlotUI>();
            if (selectableSlot == null)
            {
                selectableSlot = gameObject.AddComponent<SelectableSlotUI>();
                Debug.Log($"[ActionCommandSlotUI] SelectableSlotUI 컴포넌트 자동 추가: {gameObject.name}");
            }
            
            // SelectableSlotUI가 자체 색상 설정을 사용
            
            // 컴포넌트 null 체크
            ValidateComponents();
            
            // 초기 상태 설정
            SetSelected(false);
            Clear();
            
            // 카테고리별 배경 색상 초기 적용
            UpdateCategoryColor();
        }
        
        private void OnDestroy()
        {
            // DOTween 애니메이션 정리 (Frame 반짝임 등)
            var draggable = GetComponent<DraggableSlotUI>();
            if (draggable != null)
            {
                // DraggableSlotUI의 OnDestroy에서 처리하지만 안전을 위해 추가
                DOTween.Kill(gameObject);
            }
        }
        
        /// <summary>
        /// 컴포넌트 유효성 검증
        /// </summary>
        private void ValidateComponents()
        {
            if (iconImage == null)
                Debug.LogWarning($"[ActionCommandSlotUI] iconImage가 할당되지 않았습니다: {gameObject.name}", this);
            
            if (commandNameText == null)
                Debug.LogWarning($"[ActionCommandSlotUI] commandNameText가 할당되지 않았습니다: {gameObject.name}", this);
            
            if (backgroundImage == null)
                Debug.LogWarning($"[ActionCommandSlotUI] backgroundImage가 할당되지 않았습니다: {gameObject.name}", this);
        }
        
        /// <summary>
        /// 슬롯 초기화 (그리드용)
        /// </summary>
        public void Initialize(ActionCommandData data, ActionCommandEquipUI parent, int index = -1, bool isEquipped = false, bool isStyle = false, bool isEnhanced = false)
        {
            actionData = data;
            parentUI = parent;
            slotIndex = index;
            isEquippedSlot = isEquipped;
            isStyleAction = isStyle;
            isEnhancedByStyle = isEnhanced;
            
            UpdateDisplay();
        }
        
        /// <summary>
        /// 슬롯 비우기
        /// </summary>
        public void Clear()
        {
            actionData = null;
            UpdateDisplay();
        }
        
        #endregion
        
        #region 표시 업데이트
        
        /// <summary>
        /// 슬롯 표시 업데이트
        /// </summary>
        private void UpdateDisplay()
        {
            if (actionData != null)
            {
                // 검술 데이터가 있는 경우
                UpdateIcon(actionData.icon);
                
                // 카테고리 표시 (DisplayActionInfo 규칙 적용)
                string categoryText;
                if (isEnhancedByStyle)
                {
                    categoryText = "강화"; // 강화된 검술
                }
                else if (isStyleAction)
                {
                    categoryText = "유파"; // 유파 전용
                }
                else
                {
                    categoryText = "습득"; // 습득 검술
                }
                
                UpdateCategory(categoryText);
                UpdateName(actionData.commandName);
                UpdateTags(actionData.tags);
                UpdateBackgroundColor(isStyleAction);
            }
            else
            {
                // 빈 슬롯인 경우
                UpdateIcon(emptySlotIcon);
                UpdateCategory("");
                UpdateName("빈 슬롯");
                ClearTags();
                UpdateBackgroundColor(false);
            }
        }
        
        /// <summary>
        /// 아이콘 업데이트
        /// </summary>
        private void UpdateIcon(Sprite sprite)
        {
            if (iconImage == null) return;
            
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        /// <summary>
        /// 이름 업데이트 (강화 표시 포함)
        /// </summary>
        private void UpdateName(string text)
        {
            if (commandNameText == null) return;
            
            // 유파로 강화된 검술은 별표 표시
            if (isEnhancedByStyle)
            {
                commandNameText.text = "★ " + text;
                Debug.Log($"[ActionCommandSlotUI] 별표 표시: ★ {text} (isEnhanced={isEnhancedByStyle}, isStyle={isStyleAction})");
            }
            else
            {
                commandNameText.text = text;
            }
        }
        
        /// <summary>
        /// 카테고리 업데이트 (습득/유파)
        /// </summary>
        private void UpdateCategory(string text)
        {
            if (commandCategoryText == null) return;
            
            commandCategoryText.text = text;
        }
        
        /// <summary>
        /// 태그 업데이트 (동적 생성)
        /// </summary>
        private void UpdateTags(List<string> tags)
        {
            // 기존 태그 제거
            ClearTags();
            
            // 태그가 없으면 종료
            if (tags == null || tags.Count == 0 || tagTextPrefab == null || tagContainer == null)
                return;
            
            // 각 태그 생성
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag)) continue;
                
                GameObject tagObj = Instantiate(tagTextPrefab, tagContainer);
                TextMeshProUGUI tagText = tagObj.GetComponent<TextMeshProUGUI>();
                
                // 직접 없으면 자식에서 찾기
                if (tagText == null)
                {
                    tagText = tagObj.GetComponentInChildren<TextMeshProUGUI>();
                }
                
                if (tagText != null)
                {
                    tagText.text = $"[{tag}]";
                    spawnedTags.Add(tagObj);
                }
                else
                {
                    Debug.LogWarning("[ActionCommandSlotUI] tagTextPrefab에 TextMeshProUGUI 컴포넌트가 없습니다!");
                    Destroy(tagObj);
                }
            }
        }
        
        /// <summary>
        /// 태그 제거
        /// </summary>
        private void ClearTags()
        {
            foreach (var tag in spawnedTags)
            {
                if (tag != null)
                    Destroy(tag);
            }
            spawnedTags.Clear();
        }
        
        /// <summary>
        /// 배경 색상 업데이트 (습득/유파에 따라)
        /// </summary>
        private void UpdateBackgroundColor(bool isStyle)
        {
            if (backgroundImage == null) return;
            
            Color categoryColor = isStyle ? styleBackgroundColor : acquiredBackgroundColor;
            backgroundImage.color = categoryColor;
        }
        
        /// <summary>
        /// 카테고리 색상 강제 업데이트
        /// </summary>
        private void UpdateCategoryColor()
        {
            if (backgroundImage != null)
            {
                Color categoryColor = isStyleAction ? styleBackgroundColor : acquiredBackgroundColor;
                backgroundImage.color = categoryColor;
            }
        }
        
        #endregion
        
        #region 선택 상태
        
        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // SelectableSlotUI에 위임
            if (selectableSlot != null)
            {
                selectableSlot.SetSelected(selected);
                
                // 선택 해제 시 카테고리 색상 복원
                if (!selected)
                {
                    UpdateCategoryColor();
                }
            }
        }
        
        #endregion
        
        #region 이벤트 처리
        
        /// <summary>
        /// 클릭 이벤트 처리
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentUI == null) return;
            
            // 부모 UI에 선택 이벤트 전달
            parentUI.OnSlotSelected(this);
        }
        
        #endregion
        
        #region 드래그 앤 드롭 인터페이스 (ISlotDragSource)
        
        /// <summary>
        /// 드래그할 데이터 반환
        /// </summary>
        public object GetDragData()
        {
            return actionData;
        }
        
        /// <summary>
        /// 드래그 완료 시 호출
        /// </summary>
        public void OnDragComplete(bool success)
        {
            if (success)
            {
                Debug.Log($"[ActionCommandSlotUI] 드래그 성공: {actionData?.commandName}");
            }
        }
        
        /// <summary>
        /// 드래그 시작 가능 여부 (빈 슬롯은 드래그 불가)
        /// </summary>
        public bool CanStartDrag()
        {
            return actionData != null;
        }
        
        #endregion
        
        #region 드래그 앤 드롭 인터페이스 (ISlotDropTarget)
        
        /// <summary>
        /// 드롭 가능 여부 확인
        /// </summary>
        public bool CanAcceptDrop(object dragData)
        {
            // 장착 슬롯만 드롭 받을 수 있음
            if (!isEquippedSlot) return false;
            
            // ActionCommandData만 받을 수 있음
            if (!(dragData is ActionCommandData)) return false;
            
            return true;
        }
        
        /// <summary>
        /// 드래그가 슬롯 위에 있을 때 (하이라이트)
        /// </summary>
        public void OnDropHover(object dragData)
        {
            // DraggableSlotUI가 Frame 애니메이션 처리
        }
        
        /// <summary>
        /// 드래그가 슬롯을 벗어났을 때
        /// </summary>
        public void OnDropExit()
        {
            // DraggableSlotUI가 Frame 애니메이션 중지
        }
        
        /// <summary>
        /// 실제 드롭 처리
        /// </summary>
        public void OnDropReceived(object dragData, ISlotDragSource source)
        {
            if (!(dragData is ActionCommandData droppedAction)) return;
            if (parentUI == null) return;
            
            Debug.Log($"[ActionCommandSlotUI] 드롭 받음: {droppedAction.commandName} → 슬롯 {slotIndex}");
            
            // 드래그 소스가 장착 슬롯인지 확인
            var sourceSlot = source as ActionCommandSlotUI;
            
            if (sourceSlot != null && sourceSlot.IsEquippedSlot)
            {
                // 장착 슬롯 ↔ 장착 슬롯: 위치 교체
                SwapEquippedSlots(sourceSlot.SlotIndex, this.slotIndex);
            }
            else
            {
                // 그리드 → 장착 슬롯: 장착 (또는 교체)
                parentUI.OnEquipAction(droppedAction, slotIndex);
            }
        }
        
        /// <summary>
        /// 장착 슬롯 위치 교체
        /// </summary>
        private void SwapEquippedSlots(int fromIndex, int toIndex)
        {
            if (parentUI == null) return;
            
            Debug.Log($"[ActionCommandSlotUI] 슬롯 교체 요청: {fromIndex} ↔ {toIndex}");
            
            // ActionCommandEquipUI의 공개 메서드 호출
            parentUI.SwapEquippedSlots(fromIndex, toIndex);
        }
        
        #endregion
    }
}


