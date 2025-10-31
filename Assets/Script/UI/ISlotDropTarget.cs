using UnityEngine;

namespace BladeAction.UI
{
    /// <summary>
    /// 드롭을 받을 수 있는 슬롯 인터페이스
    /// </summary>
    public interface ISlotDropTarget
    {
        /// <summary>
        /// 드롭 가능 여부 확인
        /// </summary>
        bool CanAcceptDrop(object dragData);
        
        /// <summary>
        /// 드래그가 슬롯 위에 있을 때 (하이라이트 시작)
        /// </summary>
        void OnDropHover(object dragData);
        
        /// <summary>
        /// 드래그가 슬롯을 벗어났을 때 (하이라이트 종료)
        /// </summary>
        void OnDropExit();
        
        /// <summary>
        /// 실제 드롭 처리
        /// </summary>
        void OnDropReceived(object dragData, ISlotDragSource source);
    }
}

