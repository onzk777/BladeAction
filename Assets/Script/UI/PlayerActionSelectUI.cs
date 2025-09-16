using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class PlayerActionSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform actionButtonContainer;
    public GameObject actionButtonPrefab;
    public int maxButtons = 5;

    [Header("Player Reference")]
    public PlayerController playerController;

    private List<ActionButton> actionButtons = new List<ActionButton>();
    private bool isInitialized = false;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("[PlayerActionSelectUI] PlayerController 자동 연결 완료");
            }
        }
        
        // Canvas Group 가져오기 (네비게이션 그룹 제어용)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[PlayerActionSelectUI] Canvas Group 추가됨 (네비게이션 그룹 제어용)");
        }
        
        // Canvas Group 강제 활성화
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;
            Debug.Log("[PlayerActionSelectUI] Canvas Group 강제 활성화 완료");
        }
    }

    private void Start()
    {
        Initialize();
        CheckUIState(); // 디버깅용 메서드 활성화
        CheckCanvasGroupState(); // Canvas Group 상태 확인
        
        // 포커스 유지를 위한 모니터링 시작
        StartCoroutine(MonitorFocusRetention());
    }
    
    /// <summary>
    /// 포커스 유지 모니터링 (버튼 외 클릭 시 선택 유지)
    /// </summary>
    private System.Collections.IEnumerator MonitorFocusRetention()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            // 현재 선택된 버튼이 있는지 확인
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                bool hasSelectedButton = false;
                
                // 우리 버튼 중 하나가 선택되어 있는지 확인
                for (int i = 0; i < actionButtons.Count; i++)
                {
                    if (actionButtons[i] != null && actionButtons[i].gameObject == eventSystem.currentSelectedGameObject)
                    {
                        hasSelectedButton = true;
                        break;
                    }
                }
                
                // 선택된 버튼이 없으면 첫 번째 버튼 선택
                if (!hasSelectedButton && actionButtons.Count > 0)
                {
                    eventSystem.SetSelectedGameObject(actionButtons[0].gameObject);
                    Debug.Log("[PlayerActionSelectUI] 포커스 복원: 첫 번째 버튼 선택");
                }
            }
        }
    }
    
    private void CheckCanvasGroupState()
    {
        if (canvasGroup != null)
        {
            Debug.Log($"[PlayerActionSelectUI] Canvas Group 상태:");
            Debug.Log($"  - Interactable: {canvasGroup.interactable}");
            Debug.Log($"  - Blocks Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"  - Alpha: {canvasGroup.alpha}");
            Debug.Log($"  - Ignore Parent Groups: {canvasGroup.ignoreParentGroups}");
        }
        else
        {
            Debug.LogError("[PlayerActionSelectUI] Canvas Group이 null입니다!");
        }
    }

    private void CheckUIState()
    {
        // EventSystem 상태 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[PlayerActionSelectUI] EventSystem이 없습니다!");
        }
        else
        {
            Debug.Log($"[PlayerActionSelectUI] EventSystem 활성화 상태: {eventSystem.enabled}");
        }

        // Canvas 상태 확인
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError("[PlayerActionSelectUI] GraphicRaycaster가 없습니다!");
            }
            else
            {
                Debug.Log($"[PlayerActionSelectUI] GraphicRaycaster 활성화 상태: {raycaster.enabled}");
            }
        }

        // 버튼 상태 확인
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                var toggleComponent = button.GetComponent<UnityEngine.UI.Toggle>();
                var rectTransform = button.GetComponent<RectTransform>();
                var canvasGroup = button.GetComponent<CanvasGroup>();
                
                Debug.Log($"[PlayerActionSelectUI] 버튼 {button.GetButtonIndex()} 상태:");
                if (toggleComponent != null)
                {
                    Debug.Log($"  - interactable: {toggleComponent.interactable}");
                    Debug.Log($"  - enabled: {toggleComponent.enabled}");
                    Debug.Log($"  - isOn: {toggleComponent.isOn}");
                }
                else
                {
                    Debug.LogWarning($"  - Toggle 컴포넌트를 찾을 수 없습니다!");
                }
                Debug.Log($"  - gameObject.activeInHierarchy: {button.gameObject.activeInHierarchy}");
                if (rectTransform != null)
                {
                    Debug.Log($"  - rectTransform.sizeDelta: {rectTransform.sizeDelta}");
                    Debug.Log($"  - rectTransform.anchoredPosition: {rectTransform.anchoredPosition}");
                }
                if (canvasGroup != null)
                {
                    Debug.Log($"  - canvasGroup.alpha: {canvasGroup.alpha}");
                    Debug.Log($"  - canvasGroup.interactable: {canvasGroup.interactable}");
                    Debug.Log($"  - canvasGroup.blocksRaycasts: {canvasGroup.blocksRaycasts}");
                }
            }
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        CreateActionButtons();
        
        // 모든 버튼 생성 완료 후 초기화 완료
        isInitialized = true;
        
        // 첫 번째 버튼 선택
        SelectFirstButton();
        
        Debug.Log("[PlayerActionSelectUI] 모든 버튼 생성 완료");
    }
    
    /// <summary>
    /// 첫 번째 버튼 선택
    /// </summary>
    private void SelectFirstButton()
    {
        if (actionButtons.Count > 0)
        {
            // Unity EventSystem에 첫 번째 버튼 선택
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(actionButtons[0].gameObject);
                Debug.Log("[PlayerActionSelectUI] 첫 번째 버튼 선택 완료");
            }
        }
    }

    private void CreateActionButtons()
    {
        // 기존 버튼들 정리
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        actionButtons.Clear();
        
        if (playerController == null)
        {
            Debug.LogWarning("[PlayerActionSelectUI] PlayerController가 설정되지 않았습니다.");
            return;
        }
        
        // useTestMode가 true면 테스트용 단일 버튼 생성
        if (playerController.UseTestMode)
        {
            CreateTestModeButton();
        }
        else
        {
            CreateNormalModeButtons();
        }
        
        Debug.Log($"[PlayerActionSelectUI] 모든 버튼 생성 완료 - 총 {actionButtons.Count}개 버튼 생성됨");
    }

    private void CreateTestModeButton()
    {
        ActionCommandData commandData = null;
        if (playerController.EquippedStyle != null && 
            ((ICombatController)playerController).TestCommandIndex < playerController.EquippedStyle.CommandSet.Count)
        {
            commandData = playerController.EquippedStyle.CommandSet[((ICombatController)playerController).TestCommandIndex];
        }
        
        CreateActionButton(0, commandData);
    }

    private void CreateNormalModeButtons()
    {
        if (playerController.EquippedStyle != null)
        {
            var commandSet = playerController.EquippedStyle.CommandSet;
            int buttonCount = Mathf.Min(commandSet.Count, maxButtons);
            
            for (int i = 0; i < buttonCount; i++)
            {
                CreateActionButton(i, commandSet[i]);
            }
        }
        else
        {
            for (int i = 0; i < maxButtons; i++)
            {
                CreateActionButton(i, null);
            }
        }
    }
    
    private void CreateActionButton(int index, ActionCommandData commandData)
    {
        if (actionButtonPrefab == null)
        {
            Debug.LogError("[PlayerActionSelectUI] ActionButton 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        ActionButton actionButton = buttonObj.GetComponent<ActionButton>();
        
        if (actionButton == null)
        {
            actionButton = buttonObj.AddComponent<ActionButton>();
        }
        
        // Button 컴포넌트 확인
        Button button = actionButton.GetComponent<Button>();
        if (button != null)
        {
            Debug.Log($"[PlayerActionSelectUI] 버튼 {index} Button 컴포넌트 확인 완료");
        }
        else
        {
            Debug.LogError($"[PlayerActionSelectUI] 버튼 {index}에 Button 컴포넌트가 없습니다!");
        }
        
        actionButton.Initialize(commandData, index);
        actionButton.OnButtonClicked += OnButtonClicked;
        actionButtons.Add(actionButton);
        
        Debug.Log($"[PlayerActionSelectUI] 버튼 {index} 생성 완료");
    }

    public void RefreshButtons()
    {
        CreateActionButtons();
    }

    // 버튼 클릭 시 처리 (포커스만 이동)
    private void OnButtonClicked(int buttonIndex)
    {
        Debug.Log($"[PlayerActionSelectUI] 버튼 클릭됨: {buttonIndex}");
        
        // 포커스만 이동 (검술 인덱스는 검술 사용 시점에 설정)
        // Unity EventSystem이 자동으로 포커스 처리
    }
    
    /// <summary>
    /// 현재 선택된 버튼의 인덱스를 반환
    /// </summary>
    public int GetCurrentSelectedButtonIndex()
    {
        // Unity EventSystem에서 현재 선택된 버튼 찾기
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
        {
            // 현재 선택된 GameObject가 우리 버튼 중 하나인지 확인
            for (int i = 0; i < actionButtons.Count; i++)
            {
                if (actionButtons[i] != null && actionButtons[i].gameObject == eventSystem.currentSelectedGameObject)
                {
                    Debug.Log($"[PlayerActionSelectUI] 현재 선택된 버튼: {i}번");
                    return i;
                }
            }
        }
        
        // 선택된 버튼이 없으면 0번 반환 (기본값)
        Debug.Log($"[PlayerActionSelectUI] 선택된 버튼 없음, 기본값 0번 반환");
        return 0;
    }
    
    /// <summary>
    /// 네비게이션 그룹 활성화/비활성화
    /// </summary>
    public void SetNavigationGroupActive(bool active)
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
            Debug.Log($"[PlayerActionSelectUI] 네비게이션 그룹 {(active ? "활성화" : "비활성화")}");
        }
    }
}
