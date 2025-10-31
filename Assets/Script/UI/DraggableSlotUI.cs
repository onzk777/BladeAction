using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace BladeAction.UI
{
    /// <summary>
    /// 슬롯 드래그 앤 드롭 공통 컴포넌트
    /// ActionCommandSlotUI, ItemSlotUI, EquipmentSlotUI 등에서 공통 사용
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("▣ 드래그 설정")]
        [Tooltip("드래그 중 생성될 복사본의 투명도 (0~1)")]
        [SerializeField] private float dragAlpha = 0.6f;
        
        [Tooltip("드래그 중 원본 슬롯의 투명도 (0~1)")]
        [SerializeField] private float sourceAlpha = 0.3f;
        
        [Header("▣ 드롭 피드백")]
        [Tooltip("드롭 가능 시 강조할 Frame Image")]
        [SerializeField] private Image frameImage;
        
        [Tooltip("드롭 가능 시 Frame 색상")]
        [SerializeField] private Color dropHighlightColor = Color.green;
        
        [Tooltip("반짝임 애니메이션 시간 (초)")]
        [SerializeField] private float blinkDuration = 0.3f;
        
        // 컴포넌트 참조
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        // 드래그 상태
        private GameObject dragCopy;
        private Canvas dragCanvas;
        private ISlotDragSource dragSource;
        private ISlotDropTarget dropTarget;
        
        // 원본 색상 저장
        private Color originalFrameColor;
        private Tweener blinkTween;
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            
            // 인터페이스 컴포넌트 찾기
            dragSource = GetComponent<ISlotDragSource>();
            dropTarget = GetComponent<ISlotDropTarget>();
            
            // Frame 원본 색상 저장
            if (frameImage != null)
            {
                originalFrameColor = frameImage.color;
            }
        }
        
        #region 드래그 이벤트
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 드래그 소스가 없거나 드래그 불가능하면 무시
            if (dragSource == null || !dragSource.CanStartDrag())
            {
                Debug.Log($"[DraggableSlotUI] 드래그 불가: dragSource={dragSource}, CanStartDrag={dragSource?.CanStartDrag()}");
                eventData.pointerDrag = null; // 드래그 취소
                return;
            }
            
            // 드래그할 데이터 가져오기
            object dragData = dragSource.GetDragData();
            if (dragData == null)
            {
                Debug.Log($"[DraggableSlotUI] 드래그 데이터가 null");
                eventData.pointerDrag = null; // 드래그 취소
                return;
            }
            
            Debug.Log($"[DraggableSlotUI] 드래그 시작: {dragData}");
            
            // 드래그 복사본 생성
            CreateDragCopy();
            
            // 원본 슬롯 반투명 처리
            if (canvasGroup != null)
            {
                canvasGroup.alpha = sourceAlpha;
            }
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
            
            // 드롭 대상 찾기
            ISlotDropTarget target = FindDropTarget(eventData);
            
            if (target != null && dragSource != null)
            {
                object dragData = dragSource.GetDragData();
                
                if (target.CanAcceptDrop(dragData))
                {
                    // 드롭 처리
                    target.OnDropReceived(dragData, dragSource);
                    dragSource.OnDragComplete(true);
                    return;
                }
            }
            
            // 드롭 실패
            if (dragSource != null)
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
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중인 오브젝트가 있고, 이 슬롯이 드롭 대상이면 하이라이트
            if (eventData.pointerDrag != null && dropTarget != null)
            {
                var dragSource = eventData.pointerDrag.GetComponent<ISlotDragSource>();
                if (dragSource != null)
                {
                    object dragData = dragSource.GetDragData();
                    
                    if (dropTarget.CanAcceptDrop(dragData))
                    {
                        dropTarget.OnDropHover(dragData);
                        StartBlinkAnimation();
                    }
                }
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            // 드래그 중이고 드롭 대상이면 하이라이트 종료
            if (eventData.pointerDrag != null && dropTarget != null)
            {
                dropTarget.OnDropExit();
                StopBlinkAnimation();
            }
        }
        
        #endregion
        
        #region 드래그 복사본 생성
        
        private void CreateDragCopy()
        {
            // Canvas 찾기 (드래그 중인 복사본은 최상위 Canvas에 표시)
            dragCanvas = GetComponentInParent<Canvas>().rootCanvas;
            
            // 복사본 생성
            dragCopy = Instantiate(gameObject, dragCanvas.transform);
            dragCopy.name = "DragCopy_" + gameObject.name;
            
            // 복사본 설정
            RectTransform copyRect = dragCopy.GetComponent<RectTransform>();
            if (copyRect != null)
            {
                copyRect.sizeDelta = rectTransform.sizeDelta;
            }
            
            // 복사본 투명도 설정
            CanvasGroup copyCanvasGroup = dragCopy.GetComponent<CanvasGroup>();
            if (copyCanvasGroup == null)
            {
                copyCanvasGroup = dragCopy.AddComponent<CanvasGroup>();
            }
            
            copyCanvasGroup.alpha = dragAlpha;
            copyCanvasGroup.blocksRaycasts = false; // 마우스 이벤트 차단 해제
            copyCanvasGroup.interactable = false; // 상호작용 차단
            
            // **1단계: 불필요한 컴포넌트 먼저 제거 (Initialize 전)**
            var copyDraggable = dragCopy.GetComponent<DraggableSlotUI>();
            if (copyDraggable != null)
            {
                Destroy(copyDraggable);
            }
            
            var copySelectable = dragCopy.GetComponent<SelectableSlotUI>();
            if (copySelectable != null)
            {
                Destroy(copySelectable);
            }
            
            // 복사본의 모든 하이라이트 이미지 비활성화 (선택 표시 제거)
            var highlightImages = dragCopy.GetComponentsInChildren<Image>();
            foreach (var img in highlightImages)
            {
                if (img.name.Contains("Highlight") || img.name.Contains("highlight"))
                {
                    img.enabled = false;
                }
            }
            
            // **2단계: 복사본에 원본 데이터 완전 동기화 (컴포넌트 제거 후)**
            var originalActionSlot = GetComponent<ActionCommandSlotUI>();
            var copyActionSlot = dragCopy.GetComponent<ActionCommandSlotUI>();
            
            if (originalActionSlot != null && copyActionSlot != null && originalActionSlot.ActionData != null)
            {
                // 복사본에 원본과 동일한 데이터로 재초기화
                copyActionSlot.Initialize(
                    originalActionSlot.ActionData,
                    null, // parentUI는 필요 없음 (상호작용 없음)
                    originalActionSlot.SlotIndex,
                    originalActionSlot.IsEquippedSlot,
                    originalActionSlot.IsStyleAction,      // 원본의 isStyle 복사
                    originalActionSlot.IsEnhancedByStyle   // 원본의 isEnhanced 복사
                );
                
                Debug.Log($"[DraggableSlotUI] 복사본 데이터 동기화 완료: {originalActionSlot.ActionData.commandName} (Style={originalActionSlot.IsStyleAction}, Enhanced={originalActionSlot.IsEnhancedByStyle})");
            }
            else
            {
                Debug.LogWarning($"[DraggableSlotUI] 복사본 동기화 실패 - original={originalActionSlot != null}, copy={copyActionSlot != null}, data={originalActionSlot?.ActionData != null}");
            }
            
            Debug.Log($"[DraggableSlotUI] 드래그 복사본 생성 완료: {dragCopy.name}");
        }
        
        
        #endregion
        
        #region 드롭 대상 찾기
        
        private ISlotDropTarget FindDropTarget(PointerEventData eventData)
        {
            // 마우스 위치에 있는 모든 UI 오브젝트 찾기
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            // 첫 번째 유효한 드롭 대상 찾기
            foreach (var result in results)
            {
                var dropTarget = result.gameObject.GetComponent<ISlotDropTarget>();
                if (dropTarget != null)
                {
                    return dropTarget;
                }
            }
            
            return null;
        }
        
        #endregion
        
        #region 반짝임 애니메이션 (DOTween)
        
        /// <summary>
        /// Frame 반짝임 애니메이션 시작
        /// </summary>
        private void StartBlinkAnimation()
        {
            if (frameImage == null) return;
            
            // 기존 애니메이션 중지
            StopBlinkAnimation();
            
            // DOTween 반짝임: Color + Scale 펄스
            Sequence blinkSequence = DOTween.Sequence();
            
            // Color 반짝임 (흰색 → 초록색 → 흰색)
            blinkSequence.Append(
                DOTween.To(() => frameImage.color, x => frameImage.color = x, dropHighlightColor, blinkDuration)
                    .SetEase(Ease.InOutQuad)
            );
            blinkSequence.Append(
                DOTween.To(() => frameImage.color, x => frameImage.color = x, originalFrameColor, blinkDuration)
                    .SetEase(Ease.InOutQuad)
            );
            
            // 무한 반복
            blinkSequence.SetLoops(-1, LoopType.Restart);
            
            // Scale 펄스 (1.0 → 1.05 → 1.0)
            if (frameImage.transform is RectTransform frameRect)
            {
                frameRect.DOScale(1.05f, blinkDuration)
                    .SetEase(Ease.InOutQuad)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        /// <summary>
        /// Frame 반짝임 애니메이션 중지
        /// </summary>
        private void StopBlinkAnimation()
        {
            if (frameImage == null) return;
            
            // DOTween 애니메이션 중지 (안전하게)
            frameImage.DOKill(true); // complete = true로 최종 상태로 이동 후 중지
            
            if (frameImage.transform is RectTransform frameRect)
            {
                frameRect.DOKill(true);
                frameRect.localScale = Vector3.one; // 스케일 복원
            }
            
            // 색상 복원
            frameImage.color = originalFrameColor;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // DOTween 정리
            StopBlinkAnimation();
            
            if (dragCopy != null)
            {
                Destroy(dragCopy);
            }
        }
    }
}

