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
        
        // ActionCommandSelectionManager에 자신을 등록 (Scene 분리 대비)
        if (ActionCommandSelectionManager.Instance != null)
        {
            ActionCommandSelectionManager.Instance.RegisterEnemyActionUI(this);
        }
        else
        {
            Debug.LogWarning("[EnemyActionSelectUI] ActionCommandSelectionManager가 아직 생성되지 않았습니다. Start에서 재시도합니다.");
        }
    }

    private void Start()
    {
        // ActionCommandSelectionManager에 등록 재시도 (Awake에서 실패한 경우)
        if (ActionCommandSelectionManager.Instance != null && 
            ActionCommandSelectionManager.Instance.enemyActionSelectUI != this)
        {
            ActionCommandSelectionManager.Instance.RegisterEnemyActionUI(this);
        }
        
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
    
    /// <summary>
    /// 선택된 검술 버튼을 하이라이트합니다.
    /// 
    /// 역할:
    /// - BT 또는 테스트 모드에서 선택한 검술을 시각적으로 표시
    /// - Enemy는 클릭 불가하지만, 어떤 검술을 사용할지 플레이어에게 표시
    /// 
    /// 호출 시점:
    /// - Enemy 턴 시작 시
    /// - EnemyController.GetSelectedCommandIndex() 호출 후
    /// 
    /// 구현:
    /// - 모든 버튼의 하이라이트 해제
    /// - 선택된 버튼만 하이라이트 (색상 변경 등)
    /// </summary>
    /// <param name="index">선택된 검술 인덱스</param>
    public void SetSelectedButton(int index)
    {
        if (actionButtons.Count == 0)
        {
            Debug.LogWarning("[EnemyActionSelectUI] 버튼이 생성되지 않음 - 선택 표시 불가");
            return;
        }
        
        // 인덱스 범위 체크
        if (index < 0 || index >= actionButtons.Count)
        {
            Debug.LogWarning($"[EnemyActionSelectUI] 잘못된 인덱스: {index} (버튼 수: {actionButtons.Count})");
            return;
        }
        
        Debug.Log($"[EnemyActionSelectUI] 선택된 검술 표시: {index}번 버튼");
        
        // 모든 버튼 하이라이트 해제
        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i] != null)
            {
                HighlightButton(actionButtons[i], false);
            }
        }
        
        // 선택된 버튼만 하이라이트
        if (actionButtons[index] != null)
        {
            HighlightButton(actionButtons[index], true);
        }
    }
    
    /// <summary>
    /// 버튼 하이라이트를 설정합니다.
    /// </summary>
    /// <param name="actionButton">대상 버튼</param>
    /// <param name="highlight">하이라이트 여부</param>
    private void HighlightButton(ActionButton actionButton, bool highlight)
    {
        if (actionButton == null) return;
        
        // Button의 Image 컴포넌트 가져오기
        var button = actionButton.GetComponent<UnityEngine.UI.Button>();
        if (button == null) return;
        
        var image = button.GetComponent<UnityEngine.UI.Image>();
        if (image == null) return;
        
        // 하이라이트 색상 설정
        if (highlight)
        {
            // 선택됨: 노란색 또는 밝은 색
            image.color = new Color(1f, 1f, 0.5f, 1f); // 밝은 노란색
            Debug.Log($"[EnemyActionSelectUI] 버튼 {actionButton.GetButtonIndex()} 하이라이트 ON");
        }
        else
        {
            // 선택 안 됨: 기본 색
            image.color = Color.white;
        }
    }
    
    /// <summary>
    /// 현재 선택된 버튼 인덱스를 반환합니다.
    /// (Enemy는 자동 선택이므로 주로 디버깅용)
    /// </summary>
    public int GetCurrentSelectedButtonIndex()
    {
        // Enemy는 자동 선택이므로 EventSystem 체크 불필요
        // 대신 하이라이트된 버튼 찾기
        for (int i = 0; i < actionButtons.Count; i++)
        {
            if (actionButtons[i] != null)
            {
                var image = actionButtons[i].GetComponent<UnityEngine.UI.Image>();
                if (image != null && image.color != Color.white)
                {
                    return i;
                }
            }
        }
        
        return 0; // 기본값
    }
}