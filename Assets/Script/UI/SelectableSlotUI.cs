using UnityEngine;
using UnityEngine.UI;

namespace BladeAction.UI
{
    /// <summary>
    /// 선택 가능한 슬롯 UI의 공통 선택 상태 관리 컴포넌트
    /// 하이라이트 이미지 on/off만 담당 (단순화)
    /// </summary>
    public class SelectableSlotUI : MonoBehaviour
    {
        [Header("선택 표시")]
        [Tooltip("선택 시 활성화할 하이라이트 이미지 (배경 강조)")]
        [SerializeField] private Image highlightImage;
        
        [Tooltip("선택 시 활성화할 테두리 이미지 (선택 상태 표시)")]
        [SerializeField] private Image frameImage;
        
        [Header("클릭 동작")]
        [Tooltip("클릭 시 토글 동작 여부 (재클릭 시 선택 해제)")]
        [SerializeField] private bool enableClickToggle = true;
        
        // 선택 상태
        private bool isSelected = false;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 초기 상태를 Normal로 설정
            SetSelected(false);
        }
        
        #endregion
        
        #region 선택 상태 관리
        
        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 클릭 시 호출 (토글 동작 지원)
        /// </summary>
        /// <param name="currentlySelected">현재 이 슬롯이 선택되어 있는지</param>
        /// <returns>새로운 선택 상태 (true: 선택됨, false: 선택 해제됨)</returns>
        public bool HandleClick(bool currentlySelected)
        {
            if (enableClickToggle && currentlySelected)
            {
                // 토글 모드: 이미 선택되어 있으면 해제
                SetSelected(false);
                return false;
            }
            else
            {
                // 비토글 모드 또는 선택되지 않은 상태: 선택
                SetSelected(true);
                return true;
            }
        }
        
        /// <summary>
        /// 토글 모드 설정
        /// </summary>
        public void SetToggleMode(bool enableToggle)
        {
            enableClickToggle = enableToggle;
        }
        
        /// <summary>
        /// 현재 선택 상태 반환
        /// </summary>
        public bool IsSelected => isSelected;
        
        /// <summary>
        /// 시각적 표시 업데이트 (하이라이트 배경 + 선택 테두리 on/off)
        /// </summary>
        private void UpdateVisuals()
        {
            if (highlightImage != null)
            {
                highlightImage.enabled = isSelected;
            }
            
            if (frameImage != null)
            {
                frameImage.enabled = isSelected;
            }
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 강제 상태 갱신 (디버그용)
        /// </summary>
        [ContextMenu("Refresh Visuals")]
        public void RefreshVisuals()
        {
            UpdateVisuals();
            Debug.Log($"[SelectableSlotUI] 시각 상태 갱신: {(isSelected ? "선택됨" : "보통")}");
        }
        
        #endregion
    }
}

