using UnityEngine;
using UnityEngine.UI;

namespace BladeAction.UI
{
    /// <summary>
    /// 선택 표시 방법
    /// </summary>
    public enum SelectionDisplayMode
    {
        HighlightImage,  // highlightImage만 사용
        BackgroundColor, // backgroundImage 색상만 변경
        FrameColor,      // frameImage 색상만 변경
        All              // 모두 사용
    }
    
    /// <summary>
    /// 선택 가능한 슬롯 UI의 공통 선택 상태 관리 컴포넌트
    /// 아이템 슬롯, 장비 슬롯, 검술 슬롯 등 모든 선택 가능한 UI에 사용합니다.
    /// </summary>
    public class SelectableSlotUI : MonoBehaviour
    {
        [Header("선택 표시 UI")]
        [Tooltip("선택 시 활성화할 하이라이트 이미지 (선택사항)")]
        [SerializeField] private Image highlightImage;
        
        [Tooltip("배경 이미지 (색상 변경용, 선택사항)")]
        [SerializeField] private Image backgroundImage;
        
        [Tooltip("테두리 이미지 (색상 변경용, 선택사항)")]
        [SerializeField] private Image frameImage;
        
        [Header("색상 설정")]
        [Tooltip("보통 상태 배경 색상")]
        [SerializeField] private Color normalBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        [Tooltip("선택 상태 배경 색상")]
        [SerializeField] private Color selectedBackgroundColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        
        [Tooltip("보통 상태 테두리 색상")]
        [SerializeField] private Color normalFrameColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        
        [Tooltip("선택 상태 테두리 색상")]
        [SerializeField] private Color selectedFrameColor = new Color(0f, 1f, 0f, 1f); // 녹색
        
        [Tooltip("하이라이트 색상")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.5f); // 노란색 반투명
        
        [Header("표시 모드")]
        [Tooltip("선택 상태를 어떻게 표시할지")]
        [SerializeField] private SelectionDisplayMode displayMode = SelectionDisplayMode.All;
        
        [Header("클릭 동작 모드")]
        [Tooltip("클릭 시 토글 동작 여부")]
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
        /// 시각적 표시 업데이트
        /// </summary>
        private void UpdateVisuals()
        {
            // HighlightImage 표시 (모드에 따라)
            if (displayMode == SelectionDisplayMode.HighlightImage || displayMode == SelectionDisplayMode.All)
            {
                if (highlightImage != null)
                {
                    highlightImage.enabled = isSelected;
                    if (isSelected)
                    {
                        highlightImage.color = highlightColor;
                    }
                }
            }
            
            // Background 색상 변경 (모드에 따라)
            if (displayMode == SelectionDisplayMode.BackgroundColor || displayMode == SelectionDisplayMode.All)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
                }
            }
            
            // Frame 색상 변경 (모드에 따라)
            if (displayMode == SelectionDisplayMode.FrameColor || displayMode == SelectionDisplayMode.All)
            {
                if (frameImage != null)
                {
                    frameImage.color = isSelected ? selectedFrameColor : normalFrameColor;
                }
            }
        }
        
        #endregion
        
        #region 색상 커스터마이징
        
        /// <summary>
        /// 보통 상태 색상 설정 (런타임에서 변경 가능)
        /// </summary>
        public void SetNormalColors(Color? backgroundColor = null, Color? frameColor = null)
        {
            if (backgroundColor.HasValue)
                normalBackgroundColor = backgroundColor.Value;
            
            if (frameColor.HasValue)
                normalFrameColor = frameColor.Value;
            
            if (!isSelected)
                UpdateVisuals();
        }
        
        /// <summary>
        /// 선택 상태 색상 설정 (런타임에서 변경 가능)
        /// </summary>
        public void SetSelectedColors(Color? backgroundColor = null, Color? frameColor = null, Color? highlightColor = null)
        {
            if (backgroundColor.HasValue)
                selectedBackgroundColor = backgroundColor.Value;
            
            if (frameColor.HasValue)
                selectedFrameColor = frameColor.Value;
            
            if (highlightColor.HasValue)
                this.highlightColor = highlightColor.Value;
            
            if (isSelected)
                UpdateVisuals();
        }
        
        /// <summary>
        /// 표시 모드 변경
        /// </summary>
        public void SetDisplayMode(SelectionDisplayMode mode)
        {
            displayMode = mode;
            UpdateVisuals();
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

