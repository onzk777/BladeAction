using UnityEngine;
using UnityEngine.InputSystem;
using BladeAction.UI;

/// <summary>
/// 게임 전체 입력 관리자
/// PlayerInput 컴포넌트를 담당하며 씬 전환 시에도 유지됩니다.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }
    
    private PlayerInput playerInput;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLog = true;
    
    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            
            // PlayerInput 컴포넌트 가져오기
            playerInput = GetComponent<PlayerInput>();
            
            if (playerInput != null)
            {
                Log("GameInputManager 초기화 완료 - PlayerInput 연결됨");
                
                // UI ActionMap은 기본적으로 활성화
                EnableUIActionMap();
            }
            else
            {
                Debug.LogError("[GameInputManager] PlayerInput 컴포넌트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// UI ActionMap 활성화
    /// </summary>
    public void EnableUIActionMap()
    {
        if (playerInput == null) return;
        
        var uiActionMap = playerInput.actions.FindActionMap("UI");
        if (uiActionMap != null)
        {
            uiActionMap.Enable();
            Log("UI ActionMap 활성화");
        }
    }
    
    /// <summary>
    /// Combat ActionMap 활성화 (UI ActionMap은 유지)
    /// </summary>
    public void EnableCombatMap()
    {
        if (playerInput == null) return;
        
        var combatMap = playerInput.actions.FindActionMap("Combat");
        if (combatMap != null)
        {
            combatMap.Enable();
            Log("Combat ActionMap 활성화 (UI는 유지)");
        }
    }
    
    /// <summary>
    /// Combat ActionMap 비활성화 (UI ActionMap은 유지)
    /// </summary>
    public void DisableCombatMap()
    {
        if (playerInput == null) return;
        
        var combatMap = playerInput.actions.FindActionMap("Combat");
        if (combatMap != null)
        {
            combatMap.Disable();
            Log("Combat ActionMap 비활성화 (UI는 유지)");
        }
    }
    
    /// <summary>
    /// Combat ActionMap으로 전환 (호환성 유지, 비추천)
    /// </summary>
    public void SwitchToCombatMap()
    {
        if (playerInput == null) return;
        
        playerInput.SwitchCurrentActionMap("Combat");
        Log("Combat ActionMap으로 전환");
    }
    
    /// <summary>
    /// UI ActionMap으로 전환 (호환성 유지, 비추천)
    /// </summary>
    public void SwitchToUIMap()
    {
        if (playerInput == null) return;
        
        playerInput.SwitchCurrentActionMap("UI");
        Log("UI ActionMap으로 전환");
    }
    
    /// <summary>
    /// 모든 ActionMap 비활성화
    /// </summary>
    public void DisableAllInput()
    {
        if (playerInput == null) return;
        
        playerInput.DeactivateInput();
        Log("모든 입력 비활성화");
    }
    
    /// <summary>
    /// 모든 ActionMap 활성화
    /// </summary>
    public void EnableAllInput()
    {
        if (playerInput == null) return;
        
        playerInput.ActivateInput();
        Log("모든 입력 활성화");
    }
    
    /// <summary>
    /// PlayerInput 가져오기
    /// </summary>
    public PlayerInput GetPlayerInput()
    {
        return playerInput;
    }
    
    #region UI 이벤트 중재 (크로스 Scene 참조 해결)
    
    /// <summary>
    /// 인벤토리 UI 토글 (Input System에서 호출)
    /// MainMenuManager를 찾아서 인벤토리 토글
    /// </summary>
    public void OnToggleInventoryUI()
    {
        Log("OnInventoryToggle 호출됨");
        
        // MainMenuManager 찾기 (PersistentUIScene에 있음)
        var mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.ToggleInventoryTab();
            Log("인벤토리 토글 성공"); 
        }
        else
        {
            Debug.LogWarning("[GameInputManager] MainMenuManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 액션 커맨드 설정 UI 토글 (Input System에서 호출)
    /// MainMenuManager를 찾아서 액션 커맨드 UI 토글
    /// </summary>
    public void OnToggleActionCommandEquipUI()
    {
        Log("OnActionCommandToggle 호출됨");
        
        // MainMenuManager 찾기 (PersistentUIScene에 있음)
        var mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.ToggleActionCommandTab();
            Log("액션 커맨드 UI 토글 성공");
        }
        else
        {
            Debug.LogWarning("[GameInputManager] MainMenuManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// UI 취소/닫기 (Input System에서 호출 - ESC, Cancel 키용)
    /// MainMenuManager를 찾아서 메뉴 닫기
    /// </summary>
    public void OnCancelUI()
    {
        Log("OnCancel 호출됨");
        
        // MainMenuManager 찾기 (PersistentUIScene에 있음)
        var mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        if (mainMenuManager != null)
        {
            mainMenuManager.CancelMenu();
            Log("메뉴 취소 성공");
        }
        else
        {
            Debug.LogWarning("[GameInputManager] MainMenuManager를 찾을 수 없습니다!");
        }
    }
    
    #endregion
    
    #region 디버그
    
    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[GameInputManager] {message}");
        }
    }
    
    #endregion
}

