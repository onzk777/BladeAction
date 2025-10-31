using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using BladeAction.Item;

namespace BladeAction.UI
{
    /// <summary>
    /// 슬롯 드래그 앤 드롭 공통 컴포넌트
    /// ActionCommandSlotUI, ItemSlotUI, EquipmentSlotUI 등에서 공통 사용
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [Header("▣ 드래그 설정")]
        [Tooltip("드래그 중 생성될 복사본의 투명도 (0~1)")]
        [SerializeField] private float dragAlpha = 0.6f;
        
        [Tooltip("드래그 중 원본 슬롯의 투명도 (0~1)")]
        [SerializeField] private float sourceAlpha = 0.3f;
        
        [Tooltip("드래그 복사본의 고정 가로 크기")]
        [SerializeField] private float dragCopyWidth = 280f;
        
        // 컴포넌트 참조
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        // 드래그 상태
        private GameObject dragCopy;
        private Canvas dragCanvas;
        private ISlotDragSource dragSource;
        private ISlotDropTarget dropTarget;
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            
            // 인터페이스 컴포넌트 찾기
            dragSource = GetComponent<ISlotDragSource>();
            dropTarget = GetComponent<ISlotDropTarget>();
        }
        
        #region 드래그 이벤트
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 드래그 소스가 없거나 드래그 불가능하면 무시
            if (dragSource == null || !dragSource.CanStartDrag())
            {
                eventData.pointerDrag = null; // 드래그 취소
                return;
            }
            
            // 드래그할 데이터 가져오기
            object dragData = dragSource.GetDragData();
            if (dragData == null)
            {
                eventData.pointerDrag = null; // 드래그 취소
                return;
            }
            
            // 드래그 복사본 생성
            CreateDragCopy();
            
            if (dragCopy == null)
            {
                eventData.pointerDrag = null;
                return;
            }
            
            // 원본 슬롯 반투명 처리
            if (canvasGroup != null)
            {
                canvasGroup.alpha = sourceAlpha;
            }
            
            // 드롭 가능 영역 하이라이트 시작
            NotifyDragStart(dragData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (dragCopy != null)
            {
                // 드래그 복사본을 마우스 위치로 이동
                dragCopy.transform.position = eventData.position;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            // 드래그 복사본 제거
            if (dragCopy != null)
            {
                Destroy(dragCopy);
                dragCopy = null;
            }
            
            // 원본 슬롯 투명도 복원
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            
            // **중요: 드롭 대상을 먼저 찾고 처리 (비활성화 전에)**
            ISlotDropTarget target = FindDropTarget(eventData);
            
            bool dropSuccess = false;
            
            if (target != null && dragSource != null)
            {
                object dragData = dragSource.GetDragData();
                
                if (target.CanAcceptDrop(dragData, dragSource))
                {
                    // 드롭 처리
                    target.OnDropReceived(dragData, dragSource);
                    dragSource.OnDragComplete(true);
                    dropSuccess = true;
                }
            }
            
            // **드롭 처리 완료 후 하이라이트 종료 및 드롭존 비활성화**
            NotifyDragEnd();
            
            // 드롭 실패
            if (!dropSuccess && dragSource != null)
            {
                dragSource.OnDragComplete(false);
            }
        }
        
        #endregion
        
        #region 드롭 이벤트
        
        public void OnDrop(PointerEventData eventData)
        {
            // OnEndDrag에서 처리하므로 여기서는 비워둠
            // (혹시 필요하면 추가 검증 가능)
        }
        
        #endregion
        
        #region 드래그 복사본 생성
        
        private void CreateDragCopy()
        {
            // Canvas 찾기 (드래그 중인 복사본은 최상위 Canvas에 표시)
            dragCanvas = GetComponentInParent<Canvas>().rootCanvas;
            
            // **1단계: 원본 크기 측정**
            float originalHeight = rectTransform.rect.height;
            
            // **2단계: Wrapper 오브젝트 생성 (크기 제한 역할)**
            GameObject wrapperObject = new GameObject("DragCopyWrapper");
            wrapperObject.transform.SetParent(dragCanvas.transform, false);
            
            RectTransform wrapperRect = wrapperObject.AddComponent<RectTransform>();
            wrapperRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 고정
            wrapperRect.anchorMax = new Vector2(0.5f, 0.5f);
            wrapperRect.pivot = new Vector2(0.5f, 0.5f);
            wrapperRect.sizeDelta = new Vector2(dragCopyWidth, originalHeight); // 가로는 고정, 세로는 원본
            
            // **중요: Wrapper에 Canvas 추가 (부모 Layout Group 영향 차단)**
            Canvas wrapperCanvas = wrapperObject.AddComponent<Canvas>();
            wrapperCanvas.overrideSorting = true; // 독립적인 렌더링 순서
            wrapperCanvas.sortingOrder = 1000; // 최상위 표시
            
            // **Layout Element 추가 (부모 Layout Group이 크기 조정하지 못하도록)**
            UnityEngine.UI.LayoutElement layoutElement = wrapperObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.ignoreLayout = true; // 부모 레이아웃 무시
            
            // **RectMask2D 추가 (자식 오브젝트가 Wrapper 밖으로 튀어나가지 않도록)**
            UnityEngine.UI.RectMask2D rectMask = wrapperObject.AddComponent<UnityEngine.UI.RectMask2D>();
            
            // **3단계: Wrapper 하위로 원본 복제**
            dragCopy = Instantiate(gameObject, wrapperObject.transform);
            dragCopy.name = "DragCopy_" + gameObject.name;
            
            // Wrapper의 자식이므로 전체 크기에 맞춤 (stretch)
            RectTransform copyRect = dragCopy.GetComponent<RectTransform>();
            if (copyRect != null)
            {
                copyRect.anchorMin = Vector2.zero;
                copyRect.anchorMax = Vector2.one;
                copyRect.offsetMin = Vector2.zero;
                copyRect.offsetMax = Vector2.zero;
            }
            
            // **4단계: 복제본(자식)에 투명도 및 상호작용 설정**
            GameObject actualCopy = dragCopy; // 실제 복제된 슬롯 (Wrapper의 자식)
            
            // 복사본 투명도 설정
            CanvasGroup copyCanvasGroup = actualCopy.GetComponent<CanvasGroup>();
            if (copyCanvasGroup == null)
            {
                copyCanvasGroup = actualCopy.AddComponent<CanvasGroup>();
            }
            
            copyCanvasGroup.alpha = dragAlpha;
            copyCanvasGroup.blocksRaycasts = false; // 마우스 이벤트 차단 해제
            copyCanvasGroup.interactable = false; // 상호작용 차단
            
            // 불필요한 컴포넌트 제거 (상호작용 방지)
            var copyDraggable = actualCopy.GetComponent<DraggableSlotUI>();
            if (copyDraggable != null)
            {
                Destroy(copyDraggable);
            }
            
            var copySelectable = actualCopy.GetComponent<SelectableSlotUI>();
            if (copySelectable != null)
            {
                Destroy(copySelectable);
            }
            
            // 하이라이트 이미지 비활성화 (선택 표시 제거)
            var highlightImages = actualCopy.GetComponentsInChildren<Image>(true);
            foreach (var img in highlightImages)
            {
                if (img.name.Contains("Highlight") || img.name.Contains("highlight") || 
                    img.name.Contains("Frame") || img.name.Contains("frame"))
                {
                    img.enabled = false;
                }
                
                // **중요: 모든 Image의 raycastTarget을 끔 (드래그 복사본이 마우스 이벤트를 가로채지 않도록)**
                img.raycastTarget = false;
            }
            
            // 복사본에 원본 데이터 동기화 (빈 슬롯 방지)
            var originalActionSlot = GetComponent<ActionCommandSlotUI>();
            var copyActionSlot = actualCopy.GetComponent<ActionCommandSlotUI>();
            
            if (originalActionSlot != null && copyActionSlot != null && originalActionSlot.ActionData != null)
            {
                copyActionSlot.Initialize(
                    originalActionSlot.ActionData,
                    null,
                    originalActionSlot.SlotIndex,
                    originalActionSlot.IsEquippedSlot,
                    originalActionSlot.IsStyleAction,
                    originalActionSlot.IsEnhancedByStyle
                );
            }
            
            // ItemSlotUI 데이터 동기화
            var originalItemSlot = GetComponent<ItemSlotUI>();
            var copyItemSlot = actualCopy.GetComponent<ItemSlotUI>();
            
            if (originalItemSlot != null && copyItemSlot != null && originalItemSlot.GetOwnedItem() != null)
            {
                copyItemSlot.Setup(originalItemSlot.GetOwnedItem());
            }
            
            // EquipmentSlotUI 데이터 동기화
            var originalEquipmentSlot = GetComponent<EquipmentSlotUI>();
            var copyEquipmentSlot = actualCopy.GetComponent<EquipmentSlotUI>();
            
            if (originalEquipmentSlot != null && copyEquipmentSlot != null && originalEquipmentSlot.GetEquipmentSlot() != null)
            {
                copyEquipmentSlot.Setup(originalEquipmentSlot.GetEquipmentSlot());
            }
            
            // **dragCopy는 Wrapper를 가리킴 (OnDrag에서 이동시킬 대상)**
            dragCopy = wrapperObject;
        }
        
        
        #endregion
        
        #region 드롭 대상 찾기
        
        private ISlotDropTarget FindDropTarget(PointerEventData eventData)
        {
            // 마우스 위치에 있는 모든 UI 오브젝트 찾기
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            // 드래그 데이터 가져오기
            object dragData = dragSource?.GetDragData();
            
            // 모든 결과를 순회하여 ISlotDropTarget 찾기 (우선순위 + CanAcceptDrop 확인)
            ISlotDropTarget firstValidSlot = null;
            ISlotDropTarget gridAreaDropZone = null;
            
            foreach (var result in results)
            {
                // **중요: 자식 오브젝트에서 Raycast를 받을 수 있으므로 부모까지 찾음**
                var dropTarget = result.gameObject.GetComponentInParent<ISlotDropTarget>();
                
                if (dropTarget != null)
                {
                    // 슬롯은 우선순위 1 (ActionCommandSlotUI, EquipmentSlotUI)
                    if ((dropTarget is ActionCommandSlotUI || dropTarget is EquipmentSlotUI) && firstValidSlot == null)
                    {
                        // CanAcceptDrop으로 실제 받을 수 있는지 확인
                        if (dragData != null && dropTarget.CanAcceptDrop(dragData, dragSource))
                        {
                            firstValidSlot = dropTarget;
                        }
                    }
                    // GridAreaDropZone은 우선순위 2 (백업)
                    else if (dropTarget is GridAreaDropZone && gridAreaDropZone == null)
                    {
                        gridAreaDropZone = dropTarget;
                    }
                }
            }
            
            // 우선순위: 실제 받을 수 있는 슬롯 > GridAreaDropZone
            if (firstValidSlot != null)
            {
                return firstValidSlot;
            }
            else if (gridAreaDropZone != null)
            {
                return gridAreaDropZone;
            }
            
            return null;
        }
        
        #endregion
        
        #region 드롭 영역 하이라이트 알림
        
        /// <summary>
        /// 드래그 시작 시 부모 UI에 알림 (드롭 가능 영역 하이라이트)
        /// </summary>
        private void NotifyDragStart(object dragData)
        {
            // ActionCommandEquipUI 찾기
            var actionEquipUI = GetComponentInParent<ActionCommandEquipUI>();
            if (actionEquipUI != null)
            {
                // 드래그 소스가 장착 슬롯인지 확인
                var actionSlot = GetComponent<ActionCommandSlotUI>();
                bool isDraggingFromEquipped = actionSlot != null && actionSlot.IsEquippedSlot;
                
                actionEquipUI.OnDragStart(isDraggingFromEquipped);
                return;
            }
            
            // InventoryUI 찾기
            var inventoryUI = GetComponentInParent<InventoryUI>();
            if (inventoryUI != null)
            {
                // 드래그 소스가 장착 슬롯인지 확인
                var equipmentSlot = GetComponent<EquipmentSlotUI>();
                bool isDraggingFromEquipped = equipmentSlot != null;
                
                // 드래그 중인 아이템 타입 확인
                ItemType itemType = ItemType.Weapon;
                if (dragData is OwnedItem ownedItem)
                {
                    var itemData = ownedItem.GetItemData();
                    if (itemData != null)
                    {
                        itemType = itemData.itemType;
                    }
                }
                
                inventoryUI.OnDragStart(isDraggingFromEquipped, itemType);
                return;
            }
        }
        
        /// <summary>
        /// 드래그 종료 시 부모 UI에 알림 (하이라이트 종료)
        /// </summary>
        private void NotifyDragEnd()
        {
            // ActionCommandEquipUI 찾기
            var actionEquipUI = GetComponentInParent<ActionCommandEquipUI>();
            if (actionEquipUI != null)
            {
                actionEquipUI.OnDragEnd();
                return;
            }
            
            // InventoryUI 찾기
            var inventoryUI = GetComponentInParent<InventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.OnDragEnd();
                return;
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // 드래그 복사본 정리
            if (dragCopy != null)
            {
                Destroy(dragCopy); 
            }
        }
    }
}


