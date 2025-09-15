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
    public ToggleGroup toggleGroup; // Toggle Group 컴포넌트

    [Header("Player Reference")]
    public PlayerController playerController;

    private List<ActionButton> actionButtons = new List<ActionButton>();
    private bool isInitialized = false;

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
    }

    private void Start()
    {
        Initialize();
        // CheckUIState(); // 디버깅용 메서드, 임시 비활성화
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
        
        // 초기 포커스 설정
        if (actionButtons.Count > 0)
        {
            Debug.Log("[PlayerActionSelectUI] Initialize에서 초기 포커스 설정");
            SetFocusToFirstButton();
        }
        
        isInitialized = true;
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
        
        // Toggle Group에 연결
        Toggle toggle = actionButton.GetComponent<Toggle>();
        if (toggle != null && toggleGroup != null)
        {
            toggle.group = toggleGroup;
        }
        
        actionButton.Initialize(commandData, index);
        actionButton.OnButtonClicked += OnButtonClicked;
        actionButtons.Add(actionButton);
        
        Debug.Log($"[PlayerActionSelectUI] 버튼 {index} 생성 완료, Toggle Group 연결됨");
    }

    public void RefreshButtons()
    {
        CreateActionButtons();
    }

    // Toggle Group 기반 선택 시스템
    private void OnButtonClicked(int buttonIndex)
    {
        Debug.Log($"[PlayerActionSelectUI] Toggle 선택됨: {buttonIndex}");
        
        // PlayerController에 검술 인덱스 전달
        if (playerController != null)
        {
            playerController.SetSelectedCommandIndex(buttonIndex);
            Debug.Log($"[PlayerActionSelectUI] PlayerController에 검술 인덱스 {buttonIndex} 전달 완료");
        }
    }

    public void SetFocusToFirstButton()
    {
        Debug.Log($"[PlayerActionSelectUI] SetFocusToFirstButton 호출됨 - 버튼 개수: {actionButtons.Count}");
        
        if (actionButtons.Count > 0)
        {
            // 모든 버튼의 선택 상태를 먼저 해제
            foreach (var button in actionButtons)
            {
                if (button != null)
                {
                    button.SetSelected(false);
                }
            }
            
            // 첫 번째 버튼 선택
            SelectButton(0);
            Debug.Log("[PlayerActionSelectUI] 첫 번째 버튼 선택 완료");
        }
        else
        {
            Debug.LogWarning("[PlayerActionSelectUI] 액션 버튼이 없어서 포커스를 설정할 수 없습니다!");
        }
    }
    
    public void MoveFocus(int direction)
    {
        if (actionButtons.Count == 0) return;
        
        // 현재 선택된 인덱스 찾기
        int currentIndex = GetCurrentSelectedIndex();
        
        // 선택된 것이 없으면 첫 번째 버튼으로 설정
        if (currentIndex == -1)
        {
            currentIndex = 0;
        }
        
        // 새로운 인덱스 계산 (순환 구조)
        int newIndex = (currentIndex + direction + actionButtons.Count) % actionButtons.Count;
        
        Debug.Log($"[PlayerActionSelectUI] MoveFocus: 현재={currentIndex}, 방향={direction}, 새로운={newIndex}");
        
        // 키보드 입력으로 선택 이동
        SelectButton(newIndex);
    }
    
    private int GetCurrentSelectedIndex()
    {
        // 현재 선택된 Toggle 찾기
        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i] != null && actionButtons[i].IsSelected())
            {
                return i;
            }
        }
        return -1; // 선택된 것이 없으면 -1 반환
    }
    
    /// <summary>
    /// Toggle Group을 사용한 안전한 버튼 선택
    /// </summary>
    private void SelectButton(int targetIndex)
    {
        Debug.Log($"[PlayerActionSelectUI] SelectButton 호출됨 - 목표: {targetIndex}, 버튼 개수: {actionButtons.Count}");
        
        if (actionButtons.Count == 0)
        {
            Debug.LogWarning("[PlayerActionSelectUI] 액션 버튼이 없습니다!");
            return;
        }
        
        // 인덱스 범위 검증
        if (targetIndex < 0 || targetIndex >= actionButtons.Count)
        {
            Debug.LogWarning($"[PlayerActionSelectUI] 유효하지 않은 인덱스: {targetIndex} (범위: 0-{actionButtons.Count - 1})");
            return;
        }
        
        // Toggle Group이 자동으로 하나만 선택되도록 처리
        if (actionButtons[targetIndex] != null)
        {
            actionButtons[targetIndex].SetSelected(true);
            Debug.Log($"[PlayerActionSelectUI] 버튼 {targetIndex} 선택 완료");
        }
        else
        {
            Debug.LogError($"[PlayerActionSelectUI] 버튼 {targetIndex}이 null입니다!");
        }
    }
}
