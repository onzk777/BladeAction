using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// TestScene을 관리하는 매니저
/// Scene 전환 테스트 및 Enemy 선택 기능
/// </summary>
public class TestSceneManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("전투 시작 버튼")]
    [SerializeField] private Button startCombatButton;
    [Tooltip("타이틀로 복귀 버튼")]
    [SerializeField] private Button returnToTitleButton;
    
    [Header("Enemy 선택")]
    [Tooltip("Enemy 선택 Dropdown")]
    [SerializeField] private TMP_Dropdown enemySelectionDropdown;
    
    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 선택된 Enemy ID
    private string selectedEnemyId;
    // Enemy ID 목록 (Dropdown 인덱스 매핑)
    private List<string> enemyIds = new List<string>();

    private void Start()
    {
        InitializeEnemyDropdown();
        InitializeButtons();
        Log("TestScene 초기화 완료");
    }

    /// <summary>
    /// Enemy 선택 Dropdown 초기화
    /// </summary>
    private void InitializeEnemyDropdown()
    {
        if (enemySelectionDropdown == null)
        {
            Debug.LogWarning("[TestSceneManager] Enemy Selection Dropdown이 할당되지 않았습니다!");
            return;
        }
        
        // CharacterDatabaseManager에서 Enemy 목록 가져오기
        if (CharacterDatabaseManager.Instance == null)
        {
            Debug.LogError("[TestSceneManager] CharacterDatabaseManager.Instance가 null입니다!");
            return;
        }
        
        var entries = CharacterDatabaseManager.Instance.GetAllEnemyEntries();
        
        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning("[TestSceneManager] 등록된 Enemy가 없습니다!");
            return;
        }
        
        // Dropdown 옵션 생성
        enemySelectionDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        foreach (var entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.instanceId))
            {
                enemyIds.Add(entry.instanceId);
                
                // 옵션 텍스트: "ID (템플릿 이름)"
                string optionText = $"{entry.instanceId}";
                options.Add(optionText);
            }
        }
        
        enemySelectionDropdown.AddOptions(options);
        
        // 첫 번째 Enemy를 기본 선택
        if (enemyIds.Count > 0)
        {
            selectedEnemyId = enemyIds[0];
            Log($"Enemy Dropdown 초기화 완료: {enemyIds.Count}개 ({selectedEnemyId} 선택됨)");
        }
        
        // Dropdown 변경 이벤트 연결
        enemySelectionDropdown.onValueChanged.AddListener(OnEnemySelectionChanged);
    }
    
    /// <summary>
    /// Enemy 선택 변경 시
    /// </summary>
    private void OnEnemySelectionChanged(int index)
    {
        if (index >= 0 && index < enemyIds.Count)
        {
            selectedEnemyId = enemyIds[index];
            Log($"Enemy 선택 변경: {selectedEnemyId}");
        }
    }
    
    private void InitializeButtons()
    {
        // 버튼 이벤트 연결
        if (startCombatButton != null)
        {
            startCombatButton.onClick.AddListener(OnStartCombatClicked);
        }
        else
        {
            Debug.LogWarning("[TestSceneManager] Start Combat 버튼이 할당되지 않았습니다!");
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
        }
        else
        {
            Debug.LogWarning("[TestSceneManager] Return To Title 버튼이 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// "전투 시작" 버튼 클릭 시
    /// 선택된 Enemy와 전투 시작
    /// </summary>
    private void OnStartCombatClicked()
    {
        if (string.IsNullOrEmpty(selectedEnemyId))
        {
            Debug.LogWarning("[TestSceneManager] 선택된 Enemy가 없습니다!");
            return;
        }
        
        Log($"전투 시작 버튼 클릭: vs {selectedEnemyId}");

        // SceneFlowController에게 전투 시작 Flow 요청
        if (SceneFlowController.Instance != null)
        {
            SceneFlowController.Instance.StartCombatFlow("Player", selectedEnemyId);
        }
        else
        {
            Debug.LogError("[TestSceneManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// "타이틀로" 버튼 클릭 시
    /// </summary>
    private void OnReturnToTitleClicked()
    {
        Log("타이틀로 버튼 클릭");

        if (SceneFlowController.Instance != null)
        {
            SceneFlowController.Instance.GoToTitle();
        }
        else
        {
            Debug.LogError("[TestSceneManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[TestSceneManager] {message}");
        }
    }
}



