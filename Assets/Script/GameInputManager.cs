using UnityEngine;
using UnityEngine.InputSystem;

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
            
            // root GameObject일 때만 DontDestroyOnLoad 적용
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("[GameInputManager] DontDestroyOnLoad는 root GameObject에만 적용됩니다. 부모에서 분리하거나 root로 이동하세요.");
            }
            
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
    /// Combat ActionMap으로 전환
    /// </summary>
    public void SwitchToCombatMap()
    {
        if (playerInput == null) return;
        
        playerInput.SwitchCurrentActionMap("Combat");
        Log("Combat ActionMap으로 전환");
    }
    
    /// <summary>
    /// UI ActionMap으로 전환
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

