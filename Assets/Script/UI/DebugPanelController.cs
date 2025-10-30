using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 디버그 패널의 표시/숨김을 제어하는 간단한 스크립트
/// </summary>
public class DebugPanelController : MonoBehaviour
{
    [Header("디버그 패널 설정")]
    [SerializeField] private GameObject debugPanel; // 디버그 정보들을 묶는 상위 Panel
    
    [Header("정보 패널 설정")]
    [Tooltip("전투 정보를 표시하는 패널 (CombatStatusDisplay 등)")]
    [SerializeField] private GameObject combatInfoPanel;
    
    [Tooltip("BT 정보를 표시하는 패널 (BTMonitorUI 등)")]
    [SerializeField] private GameObject btInfoPanel;
    
    // 향후 추가될 패널들을 위한 주석
    // [SerializeField] private GameObject otherInfoPanel;
    
    private InputAction debugPanelAction;
    private UnityEngine.InputSystem.InputActionMap debugActionMap;
    
    private void Awake() 
    {
        Debug.Log("[DebugPanelController] Awake 호출됨");
        debugPanel.SetActive(false); // 기본적으로 비활성화
        
        // 기본적으로 전투 정보 패널만 활성화 (디버그 패널이 열릴 때)
        if (combatInfoPanel != null)
            combatInfoPanel.SetActive(true);
        if (btInfoPanel != null)
            btInfoPanel.SetActive(false);
    }
    
    private void Start()
    {
        Debug.Log("[DebugPanelController] Start 호출됨");
        InitializeInput();
    }
    
    private void InitializeInput()
    {
        Debug.Log("[DebugPanelController] InitializeInput 시작");
        
        // PlayerController에서 사용하는 Input Action Asset 가져오기
        var playerController = FindFirstObjectByType<PlayerController>();
        Debug.Log($"[DebugPanelController] PlayerController 찾기: {(playerController != null ? "성공" : "실패")}");
        
        if (playerController != null)
        {
            var playerInput = playerController.GetComponent<PlayerInput>();
            Debug.Log($"[DebugPanelController] PlayerInput 찾기: {(playerInput != null ? "성공" : "실패")}");
            
            if (playerInput != null && playerInput.actions != null)
            {
                // Debug 액션 맵을 찾아서 항상 활성화
                debugActionMap = playerInput.actions.FindActionMap("Debug");
                if (debugActionMap != null)
                {
                    debugActionMap.Enable();
                    Debug.Log($"[DebugPanelController] Debug 액션 맵 활성화 완료: enabled={debugActionMap.enabled}");
                }
                else
                {
                    Debug.LogError("[DebugPanelController] Debug 액션 맵을 찾을 수 없습니다! Input Actions Asset에 Debug 맵이 있는지 확인하세요.");
                    return;
                }
                
                // Debug 액션 맵의 DebugPanel 액션 찾기
                debugPanelAction = playerInput.actions.FindAction("Debug/DebugPanel");
                Debug.Log($"[DebugPanelController] DebugPanel 액션 찾기: {(debugPanelAction != null ? "성공" : "실패")}");
                
                if (debugPanelAction != null)
                {
                    debugPanelAction.performed += OnDebugPanelPressed;
                    Debug.Log("[DebugPanelController] F3 키 이벤트 구독 완료");
                    
                    // Debug 액션 맵은 항상 활성화 상태 유지 (개별 액션도 활성화)
                    debugPanelAction.Enable();
                    Debug.Log($"[DebugPanelController] debugPanelAction 활성화 완료: enabled={debugPanelAction.enabled}");
                }
                else
                {
                    Debug.LogError("[DebugPanelController] Debug/DebugPanel 액션을 찾지 못했습니다! Input Actions Asset을 확인하세요.");
                }
            }
        }
    }
    
    private void OnEnable()
    {
        // OnEnable은 Start 전에 호출되므로 초기화 전 상태일 수 있음
        // Start()에서 InitializeInput()이 호출되면 자동으로 활성화되므로 여기서는 아무것도 하지 않음
    }
    
    private void OnDisable()
    {
        Debug.Log("[DebugPanelController] OnDisable 호출됨");
        // Debug 액션은 항상 켜져있어야 하므로 Disable하지 않음
        // 이벤트 구독만 해제됨 (OnDestroy에서 처리)
    }
    
    private void OnDestroy()
    {
        if (debugPanelAction != null)
        {
            debugPanelAction.performed -= OnDebugPanelPressed;
        }
    }
    
    /// <summary>
    /// F3 키 입력 시 호출되는 메서드
    /// </summary>
    private void OnDebugPanelPressed(InputAction.CallbackContext context)
    {
        Debug.Log("[DebugPanelController] F3 키 입력 감지됨!");
        ToggleDebugPanel();
    }
    
    /// <summary>
    /// 디버그 패널 토글 (버튼 또는 F3 키에서 호출)
    /// </summary>
    public void ToggleDebugPanel()
    {
        Debug.Log($"[DebugPanelController] ToggleDebugPanel 호출됨");
        
        if (debugPanel == null)
        {
            Debug.LogError("[DebugPanelController] debugPanel이 null입니다!");
            return;
        }
        
        bool currentState = debugPanel.activeSelf;
        bool newState = !currentState;
        debugPanel.SetActive(newState);
        Debug.Log($"[DebugPanelController] 패널 토글 완료: {debugPanel.name} ({currentState} → {newState})");
    }
    
    // ========================================
    // 정보 패널 전환 메서드 (버튼 연결용)
    // ========================================
    
    /// <summary>
    /// 전투 정보 패널 활성화 (다른 정보 패널은 비활성화)
    /// UI 버튼에 연결하여 사용합니다.
    /// </summary>
    public void ShowCombatInfoPanel()
    {
        Debug.Log("[DebugPanelController] 전투 정보 패널 활성화");
        
        // 모든 정보 패널 비활성화
        DeactivateAllInfoPanels();
        
        // 전투 정보 패널만 활성화
        if (combatInfoPanel != null)
        {
            combatInfoPanel.SetActive(true);
            Debug.Log($"[DebugPanelController] {combatInfoPanel.name} 활성화됨");
        }
        else
        {
            Debug.LogWarning("[DebugPanelController] combatInfoPanel이 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// BT 정보 패널 활성화 (다른 정보 패널은 비활성화)
    /// UI 버튼에 연결하여 사용합니다.
    /// </summary>
    public void ShowBTInfoPanel()
    {
        Debug.Log("[DebugPanelController] BT 정보 패널 활성화");
        
        // 모든 정보 패널 비활성화
        DeactivateAllInfoPanels();
        
        // BT 정보 패널만 활성화
        if (btInfoPanel != null)
        {
            btInfoPanel.SetActive(true);
            Debug.Log($"[DebugPanelController] {btInfoPanel.name} 활성화됨");
        }
        else
        {
            Debug.LogWarning("[DebugPanelController] btInfoPanel이 할당되지 않았습니다.");
        }
    }
    
    /// <summary>
    /// 모든 정보 패널 비활성화
    /// 새로운 정보 패널을 추가할 때 이 메서드에도 추가해야 합니다.
    /// </summary>
    private void DeactivateAllInfoPanels()
    {
        if (combatInfoPanel != null)
            combatInfoPanel.SetActive(false);
        
        if (btInfoPanel != null)
            btInfoPanel.SetActive(false);
        
        // 향후 추가될 패널들
        // if (otherInfoPanel != null)
        //     otherInfoPanel.SetActive(false);
    }
}
