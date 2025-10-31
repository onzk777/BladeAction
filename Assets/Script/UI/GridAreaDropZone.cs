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
    /// 1. 별도 GameObject로 생성 (검술/아이템 그리드 전체 영역 크기)
    /// 2. Image 컴포넌트 자동 추가 (투명 배경)
    /// 3. Raycast는 드래그 시에만 활성화 (평소엔 비활성화)
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GridAreaDropZone : MonoBehaviour, ISlotDropTarget
    {
        private ActionCommandEquipUI actionEquipUI;
        private InventoryUI inventoryUI;
        private Image backgroundImage;
        
        private void Awake()
        {
            // 부모에서 ActionCommandEquipUI 또는 InventoryUI 찾기
            actionEquipUI = GetComponentInParent<ActionCommandEquipUI>();
            inventoryUI = GetComponentInParent<InventoryUI>();
            
            if (actionEquipUI == null && inventoryUI == null)
            {
                Debug.LogError($"[GridAreaDropZone] ActionCommandEquipUI 또는 InventoryUI를 찾을 수 없습니다: {gameObject.name}");
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
            }
        }
        
        /// <summary>
        /// 드롭 가능 여부 확인 (장착 슬롯에서 온 드래그만 받음)
        /// </summary>
        public bool CanAcceptDrop(object dragData, ISlotDragSource source = null)
        {
            // 소스가 없으면 거부
            if (source == null) return false;
            
            // ActionCommandData: 장착 슬롯에서만 받음
            if (dragData is ActionCommandData)
            {
                var sourceSlot = source as ActionCommandSlotUI;
                return sourceSlot != null && sourceSlot.IsEquippedSlot;
            }
            
            // OwnedItem: 장착 슬롯에서만 받음
            if (dragData is BladeAction.Item.OwnedItem)
            {
                return source is EquipmentSlotUI;
            }
            
            return false;
        }
        
        /// <summary>
        /// 드래그가 영역 위에 있을 때 (하이라이트)
        /// </summary>
        public void OnDropHover(object dragData)
        {
            // DraggableSlotUI와 부모 UI가 처리
        }
        
        /// <summary>
        /// 드래그가 영역을 벗어났을 때
        /// </summary>
        public void OnDropExit()
        {
            // DraggableSlotUI와 부모 UI가 처리
        }
        
        /// <summary>
        /// 실제 드롭 처리 (장착 해제)
        /// </summary>
        public void OnDropReceived(object dragData, ISlotDragSource source)
        {
            // ActionCommandEquipUI 처리
            if (dragData is ActionCommandData droppedAction && actionEquipUI != null)
            {
                var sourceSlot = source as ActionCommandSlotUI;
                
                if (sourceSlot != null && sourceSlot.IsEquippedSlot)
                {
                    // 드래그 전 선택 해제
                    sourceSlot.SetSelected(false);
                    
                    // 장착 슬롯 → 그리드: 장착 해제
                    actionEquipUI.OnUnequipAction(sourceSlot.SlotIndex);
                }
                return;
            }
            
            // InventoryUI 처리
            if (dragData is BladeAction.Item.OwnedItem droppedItem && inventoryUI != null)
            {
                var sourceSlot = source as EquipmentSlotUI;
                
                if (sourceSlot != null && !sourceSlot.IsEmpty())
                {
                    var inventory = inventoryUI.GetInventory();
                    if (inventory != null)
                    {
                        // 장착된 슬롯 찾기
                        var equippedSlot = inventory.FindEquippedSlot(droppedItem.itemKey);
                        if (equippedSlot != null)
                        {
                            // 드래그 전 선택 해제
                            sourceSlot.SetSelected(false);
                            
                            // 직접 인벤토리에 해제 요청
                            bool success = inventory.UnequipItem(equippedSlot);
                            
                            if (success)
                            {
                                // UI 갱신은 ItemEvents가 자동 처리
                                // 포커스를 해제된 아이템으로 이동
                                inventoryUI.SetFocusToItem(droppedItem.itemKey);
                            }
                        }
                    }
                }
                return;
            }
        }
    }
}

