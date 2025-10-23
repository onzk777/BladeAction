using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BladeAction.UI
{
    /// <summary>
    /// 검술 리스트의 개별 아이템 UI
    /// 검술 이름과 태그 정보를 간단하게 표시합니다.
    /// </summary>
    public class ActionCommandItemUI : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [Tooltip("검술 이름 텍스트")]
        [SerializeField] private TextMeshProUGUI commandNameText;
        
        [Tooltip("검술 태그 텍스트 (선택사항)")]
        [SerializeField] private TextMeshProUGUI commandTagText;
        
        [Tooltip("배경 이미지")]
        [SerializeField] private Image backgroundImage;
        
        [Header("색상 설정")]
        [Tooltip("기본 배경 색상")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        // 현재 표시 중인 검술 데이터
        private ActionCommandData commandData;
        
        #region 초기화 및 표시
        
        /// <summary>
        /// 검술 데이터로 UI 설정
        /// </summary>
        public void Setup(ActionCommandData data)
        {
            commandData = data;
            
            if (data == null)
            {
                Clear();
                return;
            }
            
            // 검술 이름 표시
            if (commandNameText != null)
            {
                commandNameText.text = data.commandName;
            }
            
            // 검술 태그 표시 (있으면)
            if (commandTagText != null)
            {
                if (data.tags != null && data.tags.Count > 0)
                {
                    // 첫 번째 태그만 표시 (또는 여러 개를 조합)
                    commandTagText.text = $"[{data.tags[0]}]";
                    commandTagText.enabled = true;
                }
                else
                {
                    commandTagText.enabled = false;
                }
            }
            
            // 배경 색상 설정
            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }
        
        /// <summary>
        /// UI 비우기
        /// </summary>
        public void Clear()
        {
            commandData = null;
            
            if (commandNameText != null)
            {
                commandNameText.text = "";
            }
            
            if (commandTagText != null)
            {
                commandTagText.enabled = false;
            }
        }
        
        #endregion
        
        #region 데이터 접근
        
        /// <summary>
        /// 현재 검술 데이터 반환
        /// </summary>
        public ActionCommandData GetCommandData()
        {
            return commandData;
        }
        
        #endregion
    }
}

