using UnityEngine;
using System.Collections.Generic;

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
        // EnemyController 자동 찾기
        if (enemyController == null)
        {
            enemyController = FindFirstObjectByType<EnemyController>();
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
                DestroyImmediate(button.gameObject);
            }
        }
        actionButtons.Clear();
        
        if (enemyController == null)
        {
            Debug.LogWarning("[EnemyActionSelectUI] EnemyController가 설정되지 않았습니다.");
            return;
        }
        
        // 에너미의 실제 검술 데이터 사용
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
            // 임시로 기본 검술들 생성 (데이터가 없을 때)
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
        actionButton.SetInteractable(false); // 에너미 버튼은 비활성화
        
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
    
    public void ShowEnemyAction(int actionIndex)
    {
        if (actionIndex >= 0 && actionIndex < actionButtons.Count)
        {
            // 선택된 버튼 하이라이트
            for (int i = 0; i < actionButtons.Count; i++)
            {
                if (actionButtons[i] != null)
                {
                    actionButtons[i].SetFocused(i == actionIndex);
                }
            }
        }
    }
    
    public void ResetFocus()
    {
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                button.SetFocused(false);
            }
        }
    }
}
