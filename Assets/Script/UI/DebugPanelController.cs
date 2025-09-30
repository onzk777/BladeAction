using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 디버그 패널의 표시/숨김을 제어하는 간단한 스크립트
/// </summary>
public class DebugPanelController : MonoBehaviour
{
    [Header("디버그 패널 설정")]
    [SerializeField] private GameObject debugPanel; // 디버그 정보들을 묶는 상위 Panel
    
    private InputAction debugPanelAction;
    
    private void Awake() 
    {
        Debug.Log("[DebugPanelController] Awake 호출됨");
        debugPanel.SetActive(false); // 기본적으로 비활성화
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
                // UI 액션 맵을 명시적으로 활성화
                var uiActionMap = playerInput.actions.FindActionMap("UI");
                if (uiActionMap != null)
                {
                    uiActionMap.Enable();
                    Debug.Log("[DebugPanelController] UI 액션 맵 활성화 완료");
                }
                else
                {
                    Debug.LogError("[DebugPanelController] UI 액션 맵을 찾을 수 없습니다!");
                }
                
                debugPanelAction = playerInput.actions.FindAction("UI/DebugPanel");
                Debug.Log($"[DebugPanelController] DebugPanel 액션 찾기: {(debugPanelAction != null ? "성공" : "실패")}");
                
                if (debugPanelAction != null)
                {
                    debugPanelAction.performed += OnDebugPanelPressed;
                    Debug.Log("[DebugPanelController] F3 키 이벤트 구독 완료");
                    
                    // 액션도 개별적으로 활성화
                    debugPanelAction.Enable();
                    Debug.Log($"[DebugPanelController] debugPanelAction 활성화: {(debugPanelAction.enabled ? "성공" : "실패")}");
                }
            }
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("[DebugPanelController] OnEnable 호출됨");
        debugPanelAction?.Enable();
        Debug.Log($"[DebugPanelController] debugPanelAction 활성화: {(debugPanelAction?.enabled == true ? "성공" : "실패")}");
    }
    
    private void OnDisable()
    {
        Debug.Log("[DebugPanelController] OnDisable 호출됨");
        debugPanelAction?.Disable();
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
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeSelf);
        }
    }
}
