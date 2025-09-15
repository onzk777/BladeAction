using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerActionSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform actionButtonContainer;
    public GameObject actionButtonPrefab;
    public int maxButtons = 5;
    
    [Header("Player Reference")]
    public PlayerController playerController;
    
    [Header("Input Settings")]
    public InputActionReference actionSelectInput;
    
    private List<ActionButton> actionButtons = new List<ActionButton>();
    private int focusedIndex = 0; // 현재 포커스된 버튼 인덱스
    private bool isInitialized = false;
    
    private void Awake()
    {
        // PlayerController 자동 찾기
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }
    
    private void Start()
    {
        Initialize();
        SetupInput();
    }
    
    private void OnDestroy()
    {
        if (actionSelectInput != null && actionSelectInput.action != null)
        {
            actionSelectInput.action.performed -= OnActionSelectPerformed;
            actionSelectInput.action.Disable();
        }
    }
    
    public void Initialize()
    {
        if (isInitialized) return;
        
        CreateActionButtons();
        SetInitialFocus();
        isInitialized = true;
    }
    
    private void CreateActionButtons()
    {
        // 기존 버튼들 정리
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        actionButtons.Clear();
        
        if (playerController == null)
        {
            Debug.LogWarning("[PlayerActionSelectUI] PlayerController가 설정되지 않았습니다.");
            return;
        }
        
        // 플레이어의 실제 검술 데이터 사용
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
            // 임시로 기본 검술들 생성 (데이터가 없을 때)
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
        
        actionButton.Initialize(commandData, index);
        actionButton.OnButtonClicked += OnButtonClicked;
        
        actionButtons.Add(actionButton);
    }
    
    private void SetupInput()
    {
        if (actionSelectInput != null && actionSelectInput.action != null)
        {
            actionSelectInput.action.performed += OnActionSelectPerformed;
            actionSelectInput.action.Enable();
        }
    }
    
    private void OnActionSelectPerformed(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        Debug.Log($"[PlayerActionSelectUI] 키보드 입력 감지: {input}");
        
        if (input.y > 0.5f) // 위로
        {
            Debug.Log("[PlayerActionSelectUI] 위로 이동");
            MoveFocus(-1);
        }
        else if (input.y < -0.5f) // 아래로
        {
            Debug.Log("[PlayerActionSelectUI] 아래로 이동");
            MoveFocus(1);
        }
    }
    
    private void MoveFocus(int direction)
    {
        if (actionButtons.Count == 0) return;
        
        focusedIndex = (focusedIndex + direction + actionButtons.Count) % actionButtons.Count;
        UpdateFocus();
        
        // Unity EventSystem에도 동기화
        if (actionButtons[focusedIndex] != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(actionButtons[focusedIndex].gameObject);
        }
        
        // 포커스 이동 시 즉시 PlayerController에 반영
        UpdatePlayerController();
    }
    
    private void UpdateFocus()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i] != null)
            {
                actionButtons[i].SetFocused(i == focusedIndex);
            }
        }
    }
    
    private void SetInitialFocus()
    {
        focusedIndex = 0;
        UpdateFocus();
        
        // Unity EventSystem에 첫 번째 버튼을 선택된 상태로 설정
        if (actionButtons.Count > 0 && actionButtons[0] != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(actionButtons[0].gameObject);
        }
        
        // 초기 포커스도 PlayerController에 반영
        UpdatePlayerController();
    }
    
    private void OnButtonClicked(int buttonIndex)
    {
        // 마우스 클릭으로 포커스 이동
        focusedIndex = buttonIndex;
        UpdateFocus();
        
        // Unity EventSystem에도 동기화
        if (actionButtons[buttonIndex] != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(actionButtons[buttonIndex].gameObject);
        }
        
        // 포커스 이동 시 즉시 PlayerController에 반영
        UpdatePlayerController();
    }
    
    public int GetFocusedIndex()
    {
        return focusedIndex;
    }
    
    public void ResetFocus()
    {
        SetInitialFocus();
    }
    
    public void SetInteractable(bool interactable)
    {
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                button.SetInteractable(interactable);
            }
        }
    }
    
    private void UpdatePlayerController()
    {
        // PlayerController에 현재 포커스된 검술 인덱스 전달
        if (playerController != null)
        {
            playerController.SetSelectedCommandIndex(focusedIndex);
            Debug.Log($"[PlayerActionSelectUI] PlayerController에 검술 인덱스 {focusedIndex} 전달");
        }
        else
        {
            Debug.LogWarning("[PlayerActionSelectUI] PlayerController가 null입니다!");
        }
    }
}
