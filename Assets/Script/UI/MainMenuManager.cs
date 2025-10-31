using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BladeAction.UI
{
    /// <summary>
    /// 메인 메뉴 탭 항목 (패널과 버튼의 명시적 매핑)
    /// </summary>
    [System.Serializable]
    public class MenuTab
    {
        [Tooltip("탭 이름 (구분용)")]
        public string tabName = "새 탭";
        
        [Tooltip("표시할 UI 패널 GameObject")]
        public GameObject panelObject;
        
        [Tooltip("이 탭의 네비게이션 버튼")]
        public Button tabButton;
    }
    
    /// <summary>
    /// 메인 메뉴 UI들의 상위 관리자
    /// TopNavigationBar를 통해 여러 패널을 전환합니다.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("메뉴 탭 목록")]
        [Tooltip("각 탭의 패널과 버튼을 명시적으로 연결")]
        [SerializeField] private List<MenuTab> menuTabs = new List<MenuTab>();
        
        [Header("디버그")]
        [Tooltip("디버그 로그 출력")]
        [SerializeField] private bool enableDebugLog = true;
        
        // 현재 활성화된 탭 인덱스
        private int currentTabIndex = 0;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            // 유효성 검증
            if (menuTabs == null || menuTabs.Count == 0)
            {
                Debug.LogError("[MainMenuManager] menuTabs 리스트가 비어있습니다!");
                return;
            }
            
            // 각 탭 검증 및 버튼 이벤트 연결
            for (int i = 0; i < menuTabs.Count; i++)
            {
                var tab = menuTabs[i];
                
                // 패널 검증
                if (tab.panelObject == null)
                {
                    Debug.LogWarning($"[MainMenuManager] 탭 '{tab.tabName}'의 panelObject가 null입니다!");
                    continue;
                }
                
                // 버튼 검증 및 이벤트 연결
                if (tab.tabButton != null)
                {
                    int index = i; // 클로저 캡처 문제 방지
                    tab.tabButton.onClick.AddListener(() => ShowTab(index));
                    Log($"탭 버튼 연결: '{tab.tabName}' (인덱스 {index})");
                }
                else
                {
                    Debug.LogWarning($"[MainMenuManager] 탭 '{tab.tabName}'의 tabButton이 null입니다!");
                }
            }
        }
        
        private void Start()
        {
            // 초기 상태: 메뉴 닫힌 상태로 시작
            gameObject.SetActive(false);
            Log("메인 메뉴 초기화 완료 (닫힌 상태)");
        }
        
        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            foreach (var tab in menuTabs)
            {
                if (tab != null && tab.tabButton != null)
                {
                    tab.tabButton.onClick.RemoveAllListeners();
                }
            }
        }
        
        #endregion
        
        #region 탭 전환
        
        /// <summary>
        /// 특정 인덱스의 탭을 활성화하고 나머지는 비활성화
        /// </summary>
        /// <param name="tabIndex">활성화할 탭 인덱스</param>
        public void ShowTab(int tabIndex)
        {
            // 범위 체크
            if (tabIndex < 0 || tabIndex >= menuTabs.Count)
            {
                Debug.LogError($"[MainMenuManager] 잘못된 탭 인덱스: {tabIndex} (범위: 0~{menuTabs.Count - 1})");
                return;
            }
            
            // 이미 활성화된 탭이면 스킵
            if (currentTabIndex == tabIndex)
            {
                Log($"'{menuTabs[tabIndex].tabName}' 탭이 이미 활성화되어 있습니다.");
                return;
            }
            
            // 모든 탭 순회하며 제어
            for (int i = 0; i < menuTabs.Count; i++)
            {
                var tab = menuTabs[i];
                if (tab.panelObject != null)
                {
                    if (i == tabIndex)
                    {
                        // 선택된 탭의 패널 활성화
                        tab.panelObject.SetActive(true);
                        Log($"'{tab.tabName}' 탭 활성화");
                    }
                    else
                    {
                        // 나머지 탭의 패널 비활성화
                        tab.panelObject.SetActive(false);
                    }
                }
            }
            
            // 탭 버튼 상태 업데이트
            UpdateTabButtons(tabIndex);
            
            currentTabIndex = tabIndex;
            Log($"탭 전환 완료: '{menuTabs[tabIndex].tabName}'");
        }
        
        /// <summary>
        /// 탭 버튼 상태 업데이트 (선택된 탭은 비활성화)
        /// </summary>
        private void UpdateTabButtons(int activeIndex)
        {
            for (int i = 0; i < menuTabs.Count; i++)
            {
                if (menuTabs[i].tabButton != null)
                {
                    // 활성화된 탭의 버튼은 비활성화 (이미 선택됨)
                    menuTabs[i].tabButton.interactable = (i != activeIndex);
                }
            }
        }
        
        /// <summary>
        /// 탭 이름으로 찾기
        /// </summary>
        private int FindTabIndexByName(string tabName)
        {
            for (int i = 0; i < menuTabs.Count; i++)
            {
                if (menuTabs[i].tabName == tabName)
                    return i;
            }
            return -1;
        }
        
        #endregion
        
        #region 명시적 탭 전환 메서드
        
        /// <summary>
        /// 소지품 탭으로 전환
        /// </summary>
        public void ShowInventoryTab()
        {
            int index = FindTabIndexByName("소지품");
            if (index >= 0)
            {
                ShowTab(index);
            }
            else
            {
                // 이름으로 못 찾으면 첫 번째 탭 (하위 호환)
                ShowTab(0);
            }
        }
        
        /// <summary>
        /// 검술 탭으로 전환
        /// </summary>
        public void ShowActionCommandTab()
        {
            int index = FindTabIndexByName("검술");
            if (index >= 0)
            {
                ShowTab(index);
            }
            else
            {
                // 이름으로 못 찾으면 두 번째 탭 (하위 호환)
                ShowTab(1);
            }
        }
        
        #endregion
        
        #region 메뉴 제어
        
        /// <summary>
        /// 메인 메뉴 전체 열기
        /// </summary>
        public void OpenMainMenu()
        {
            gameObject.SetActive(true);
            ShowTab(0); // 첫 번째 탭으로 시작
            Log("메인 메뉴 열림");
        }
        
        /// <summary>
        /// 메인 메뉴 전체 닫기
        /// </summary>
        public void CloseMainMenu()
        {
            gameObject.SetActive(false);
            Log("메인 메뉴 닫힘");
        }
        
        /// <summary>
        /// 인벤토리 탭 토글 (B키용)
        /// 닫혀있으면 인벤토리 탭으로 열고, 인벤토리 탭이 열려있으면 닫기
        /// </summary>
        public void ToggleInventoryTab()
        {
            int inventoryIndex = FindTabIndexByName("소지품");
            if (inventoryIndex < 0) inventoryIndex = 0; // fallback
            
            if (!gameObject.activeSelf)
            {
                // 메뉴가 닫혀있으면 인벤토리 탭으로 열기
                gameObject.SetActive(true);
                ShowTab(inventoryIndex);
                Log("메인 메뉴 열림 (소지품 탭)");
            }
            else if (currentTabIndex == inventoryIndex)
            {
                // 인벤토리 탭이 열려있으면 메뉴 닫기
                gameObject.SetActive(false);
                Log("메인 메뉴 닫힘 (소지품 탭에서)");
            }
            else
            {
                // 다른 탭이 열려있으면 인벤토리 탭으로 전환
                ShowTab(inventoryIndex);
                Log("소지품 탭으로 전환");
            }
        }
        
        /// <summary>
        /// 검술 장착 탭 토글 (X키용)
        /// 닫혀있으면 검술 탭으로 열고, 검술 탭이 열려있으면 닫기
        /// </summary>
        public void ToggleActionCommandTab()
        {
            int actionIndex = FindTabIndexByName("검술");
            if (actionIndex < 0) actionIndex = 1; // fallback
            
            if (!gameObject.activeSelf)
            {
                // 메뉴가 닫혀있으면 검술 탭으로 열기
                gameObject.SetActive(true);
                ShowTab(actionIndex);
                Log("메인 메뉴 열림 (검술 탭)");
            }
            else if (currentTabIndex == actionIndex)
            {
                // 검술 탭이 열려있으면 메뉴 닫기
                gameObject.SetActive(false);
                Log("메인 메뉴 닫힘 (검술 탭에서)");
            }
            else
            {
                // 다른 탭이 열려있으면 검술 탭으로 전환
                ShowTab(actionIndex);
                Log("검술 탭으로 전환");
            }
        }
        
        #endregion
        
        #region 디버그
        
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

