using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class EnemyActionSelectUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform actionButtonContainer;
    public GameObject actionButtonPrefab;

    [Header("Enemy Reference")]
    public EnemyController enemyController;

    private List<ActionButton> actionButtons = new List<ActionButton>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (enemyController == null)
        {
            enemyController = FindFirstObjectByType<EnemyController>();
            if (enemyController != null)
            {
                Debug.Log("[EnemyActionSelectUI] EnemyController 자동 연결 완료");
            }
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized) return;

        CreateActionButtons();
        DisableButtonInteraction();
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
        
        if (enemyController == null)
        {
            Debug.LogWarning("[EnemyActionSelectUI] EnemyController가 설정되지 않았습니다.");
            return;
        }
        
        // useTestMode가 true면 테스트용 단일 버튼 생성
        if (enemyController.UseTestMode)
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
        if (enemyController.EquippedStyle != null && 
            ((ICombatController)enemyController).TestCommandIndex < enemyController.EquippedStyle.CommandSet.Count)
        {
            commandData = enemyController.EquippedStyle.CommandSet[((ICombatController)enemyController).TestCommandIndex];
        }
        
        CreateActionButton(0, commandData);
    }

    private void CreateNormalModeButtons()
    {
        if (enemyController.EquippedStyle != null)
        {
            var commandSet = enemyController.EquippedStyle.CommandSet;
            int buttonCount = Mathf.Min(commandSet.Count, 5);
            
            for (int i = 0; i < buttonCount; i++)
            {
                CreateActionButton(i, commandSet[i]);
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                CreateActionButton(i, null);
            }
        }
    }
    
    private void CreateActionButton(int index, ActionCommandData commandData)
    {
        if (actionButtonPrefab == null)
        {
            Debug.LogError("[EnemyActionSelectUI] ActionButton 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        ActionButton actionButton = buttonObj.GetComponent<ActionButton>();
        
        if (actionButton == null)
        {
            actionButton = buttonObj.AddComponent<ActionButton>();
        }
        
        actionButton.Initialize(commandData, index);
        actionButtons.Add(actionButton);
    }

    private void DisableButtonInteraction()
    {
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                button.SetInteractable(false);
            }
        }
    }

    public void RefreshButtons()
    {
        CreateActionButtons();
        DisableButtonInteraction();
    }
}