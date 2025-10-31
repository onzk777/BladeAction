using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BladeAction.UI
{
    /// <summary>
    /// 그리드 영역 전체를 드롭 영역으로 만들기 위한 컴포넌트
    /// 장착 슬롯에서 그리드로 드롭 시 장착 해제 처리
    /// 
    /// ※ 중요: 
    /// 1. 별도 GameObject로 생성 (검술 그리드 전체 영역 크기)
    /// 2. Image 컴포넌트 자동 추가 (투명 배경)
    /// 3. Raycast는 드래그 시에만 활성화 (평소엔 비활성화)
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GridAreaDropZone : MonoBehaviour, ISlotDropTarget
    {
        private ActionCommandEquipUI actionEquipUI;
        private Image backgroundImage;
        
        private void Awake()
        {
            // 부모에서 ActionCommandEquipUI 찾기
            actionEquipUI = GetComponentInParent<ActionCommandEquipUI>();
            if (actionEquipUI == null)
            {
                Debug.LogError($"[GridAreaDropZone] ActionCommandEquipUI를 찾을 수 없습니다: {gameObject.name}");
            }
            
            // Image 컴포넌트 설정
            backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                // 투명 배경 설정 (보이지 않음)
                backgroundImage.color = new Color(1f, 1f, 1f, 0f); // 완전 투명
                backgroundImage.raycastTarget = false; // 초기 상태: Raycast 비활성화
                
                Debug.Log($"[GridAreaDropZone] 초기화 완료: {gameObject.name} → Raycast 비활성화");
            }
        }
        
        /// <summary>
        /// 드롭 영역 활성화/비활성화 (외부에서 호출)
        /// </summary>
        public void SetDropEnabled(bool enabled)
        {
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = enabled;
                Debug.Log($"[GridAreaDropZone] Raycast Target = {enabled}");
            }
        }
        
        /// <summary>
        /// 드롭 가능 여부 확인 (장착 슬롯에서 온 검술만 받음)
        /// </summary>
        public bool CanAcceptDrop(object dragData)
        {
            // ActionCommandData만 받음
            return dragData is ActionCommandData;
        }
        
        /// <summary>
        /// 드래그가 영역 위에 있을 때 (하이라이트)
        /// </summary>
        public void OnDropHover(object dragData)
        {
            // DraggableSlotUI와 ActionCommandEquipUI가 처리
        }
        
        /// <summary>
        /// 드래그가 영역을 벗어났을 때
        /// </summary>
        public void OnDropExit()
        {
            // DraggableSlotUI와 ActionCommandEquipUI가 처리
        }
        
        /// <summary>
        /// 실제 드롭 처리 (장착 해제)
        /// </summary>
        public void OnDropReceived(object dragData, ISlotDragSource source)
        {
            Debug.Log($"[GridAreaDropZone] 드롭 받음: dragData={dragData}, source={source}");
            
            if (!(dragData is ActionCommandData droppedAction))
            {
                Debug.LogWarning($"[GridAreaDropZone] ActionCommandData가 아님");
                return;
            }
            
            if (actionEquipUI == null)
            {
                Debug.LogWarning($"[GridAreaDropZone] actionEquipUI가 null");
                return;
            }
            
            // 드래그 소스가 장착 슬롯인지 확인
            var sourceSlot = source as ActionCommandSlotUI;
            
            if (sourceSlot != null && sourceSlot.IsEquippedSlot)
            {
                // 장착 슬롯 → 그리드: 장착 해제
                Debug.Log($"[GridAreaDropZone] 검술 해제: {droppedAction.commandName} (슬롯 {sourceSlot.SlotIndex})");
                actionEquipUI.OnUnequipAction(sourceSlot.SlotIndex);
            }
            else
            {
                Debug.Log($"[GridAreaDropZone] 그리드 슬롯에서 드래그함 - 무시 (IsEquippedSlot={sourceSlot?.IsEquippedSlot})");
            }
        }
    }
}

