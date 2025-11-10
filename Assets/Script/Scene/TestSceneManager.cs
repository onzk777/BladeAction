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
    
    [Header("TeamA 설정")]
    [Tooltip("TeamA 선두를 플레이어로 고정할지 여부")]
    [SerializeField] private Toggle teamAUsePlayerToggle;
    [Tooltip("TeamA NonPlayer 선택 Dropdown")]
    [SerializeField] private TMP_Dropdown teamASelectionDropdown;

    [Header("TeamB 선택")]
    [Tooltip("TeamB NonPlayer 선택 Dropdown")]
    [SerializeField] private TMP_Dropdown enemySelectionDropdown;
    
    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;
    
    private string selectedTeamAId;
    private List<string> teamAIds = new List<string>();
    // 선택된 Enemy ID
    private string selectedEnemyId;
    // Enemy ID 목록 (Dropdown 인덱스 매핑)
    private List<string> enemyIds = new List<string>();

    private void Start()
    {
        StartCoroutine(InitializeDropdownsCoroutine());
        InitializeButtons();
        Log("TestScene 초기화 완료");
    }

    private System.Collections.IEnumerator InitializeDropdownsCoroutine()
    {
        while (CharacterDatabaseManager.Instance == null)
        {
            yield return null;
        }

        yield return null;

        InitializeTeamAControls();
        InitializeEnemyDropdown();
    }

    private void InitializeTeamAControls()
    {
        if (teamAUsePlayerToggle != null)
        {
            teamAUsePlayerToggle.onValueChanged.AddListener(OnTeamAUsePlayerToggleChanged);
        }
        else
        {
            Debug.LogWarning("[TestSceneManager] TeamA Use Player Toggle이 할당되지 않았습니다!");
        }

        PopulateDropdownWithNonPlayers(teamASelectionDropdown, teamAIds, out selectedTeamAId);

        if (teamASelectionDropdown != null)
        {
            teamASelectionDropdown.onValueChanged.AddListener(OnTeamASelectionChanged);
        }

        OnTeamAUsePlayerToggleChanged(teamAUsePlayerToggle == null || teamAUsePlayerToggle.isOn);
    }

    private void InitializeTeamADropdown()
    {
        PopulateDropdownWithNonPlayers(teamASelectionDropdown, teamAIds, out selectedTeamAId);
    }

    /// <summary>
    /// Enemy 선택 Dropdown 초기화
    /// </summary>
    private void InitializeEnemyDropdown()
    {
        PopulateDropdownWithNonPlayers(enemySelectionDropdown, enemyIds, out selectedEnemyId);
        if (enemySelectionDropdown != null)
        {
            enemySelectionDropdown.onValueChanged.AddListener(OnEnemySelectionChanged);
        }
    }
    
    private void PopulateDropdownWithNonPlayers(TMP_Dropdown dropdown, List<string> idList, out string selectedId)
    {
        selectedId = null;

        if (dropdown == null)
        {
            Debug.LogWarning("[TestSceneManager] Dropdown이 할당되지 않았습니다!");
            return;
        }

        dropdown.ClearOptions();
        idList.Clear();

        if (CharacterDatabaseManager.Instance == null)
        {
            Debug.LogError("[TestSceneManager] CharacterDatabaseManager.Instance가 null입니다!");
            return;
        }

        var entries = CharacterDatabaseManager.Instance.GetAllEnemyEntries();

        if (entries == null || entries.Count == 0)
        {
            Debug.LogWarning("[TestSceneManager] 등록된 NonPlayer가 없습니다!");
            return;
        }

        List<string> options = new List<string>();

        foreach (var entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.instanceId))
            {
                idList.Add(entry.instanceId);
                options.Add(entry.instanceId);
            }
        }

        dropdown.AddOptions(options);

        if (idList.Count > 0)
        {
            selectedId = idList[0];
            Log($"Dropdown 초기화 완료 ({dropdown.name}): {idList.Count}개 ({selectedId} 선택됨)");
        }
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
    
    private void OnTeamASelectionChanged(int index)
    {
        if (index >= 0 && index < teamAIds.Count)
        {
            selectedTeamAId = teamAIds[index];
            Log($"TeamA NonPlayer 선택 변경: {selectedTeamAId}");
        }
    }

    private void OnTeamAUsePlayerToggleChanged(bool isOn)
    {
        if (teamASelectionDropdown != null)
        {
            teamASelectionDropdown.interactable = !isOn;
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

        bool usePlayer = teamAUsePlayerToggle == null || teamAUsePlayerToggle.isOn;
        if (!usePlayer && string.IsNullOrEmpty(selectedTeamAId))
        {
            Debug.LogWarning("[TestSceneManager] 선택된 TeamA NonPlayer가 없습니다!");
            return;
        }
        
        Log($"전투 시작 버튼 클릭: TeamA {(usePlayer ? "Player" : selectedTeamAId)} vs TeamB {selectedEnemyId}");

        // SceneFlowController에게 전투 시작 Flow 요청
        if (SceneFlowController.Instance != null)
        {
        IList<string> teamAList = BuildTeamAIds();
            IList<string> teamBList = new List<string> { selectedEnemyId };
            SceneFlowController.Instance.StartCombatFlow(teamAList, teamBList);
        }
        else
        {
            Debug.LogError("[TestSceneManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }

    private IList<string> BuildTeamAIds()
    {
        bool usePlayer = teamAUsePlayerToggle == null || teamAUsePlayerToggle.isOn;

        if (usePlayer)
        {
            string playerId = PlayerCharacterManager.Instance?.PlayerCharacter?.InstanceId ?? "Player";
            return new List<string> { playerId };
        }

        return new List<string> { selectedTeamAId };
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



