using UnityEngine;

namespace BladeAction.UI
{
    /// <summary>
    /// 메인 메뉴 UI들의 상위 관리자
    /// 하위 Canvas들을 활성화하고 탭 전환 등을 담당합니다.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("메뉴 Canvas 참조")]
        [Tooltip("인벤토리 Canvas")]
        [SerializeField] private GameObject inventoryCanvas;
        
        [Tooltip("검술 장착 Canvas")]
        [SerializeField] private GameObject actionCommandEquipCanvas;
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 하위 Canvas들 활성화
            ActivateAllCanvases();
        }
        
        #endregion
        
        #region Canvas 관리
        
        /// <summary>
        /// 모든 하위 Canvas 활성화
        /// </summary>
        private void ActivateAllCanvases()
        {
            int activatedCount = 0;
            
            if (inventoryCanvas != null)
            {
                inventoryCanvas.SetActive(true);
                activatedCount++;
                Log("InventoryCanvas 활성화");
            }
            
            if (actionCommandEquipCanvas != null)
            {
                actionCommandEquipCanvas.SetActive(true);
                activatedCount++;
                Log("ActionCommandEquipCanvas 활성화");
            }
            
            Log($"총 {activatedCount}개 Canvas 활성화 완료");
        }
        
        #endregion
        
        #region 탭 전환 (향후 확장)
        
        /// <summary>
        /// 소지품 탭으로 전환 (향후 구현)
        /// </summary>
        public void ShowInventoryTab()
        {
            // TODO: 상위 네비게이션 탭 구현 시 추가
            Log("소지품 탭 전환 요청 (미구현)");
        }
        
        /// <summary>
        /// 검술 탭으로 전환 (향후 구현)
        /// </summary>
        public void ShowActionCommandTab()
        {
            // TODO: 상위 네비게이션 탭 구현 시 추가
            Log("검술 탭 전환 요청 (미구현)");
        }
        
        #endregion
        
        #region 디버그
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[MainMenuManager] {message}");
            }
        }
        
        #endregion
    }
}

