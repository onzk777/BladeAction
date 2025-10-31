using UnityEngine;

namespace BladeAction.UI
{
    /// <summary>
    /// 드래그 가능한 슬롯 인터페이스
    /// </summary>
    public interface ISlotDragSource
    {
        /// <summary>
        /// 드래그할 데이터 반환 (null이면 드래그 불가)
        /// </summary>
        object GetDragData();
        
        /// <summary>
        /// 드래그 완료 시 호출 (성공 여부 전달)
        /// </summary>
        void OnDragComplete(bool success);
        
        /// <summary>
        /// 드래그 시작 가능 여부
        /// </summary>
        bool CanStartDrag();
    }
}

