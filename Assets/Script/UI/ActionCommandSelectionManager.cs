using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Action Command UI들을 관리하는 싱글톤 매니저
/// 
/// 사용 방법:
/// 1. Inspector 할당 (기존 방식): SerializeField로 직접 할당
/// 2. 등록 방식 (Scene 분리 대비): UI가 자신을 등록
/// </summary>
public class ActionCommandSelectionManager : MonoBehaviour
{
    public static ActionCommandSelectionManager Instance { get; private set; }
    
    [SerializeField] private TeamActionSelectUI teamAUiReference;
    [SerializeField] private TeamActionSelectUI teamBUiReference;

    private readonly Dictionary<CombatCharacterManager.CombatTeam, TeamActionSelectUI> teamUiLookup = new();

    public TeamActionSelectUI GetTeamActionUI(CombatCharacterManager.CombatTeam team)
    {
        if (teamUiLookup.TryGetValue(team, out var ui) && ui != null)
        {
            return ui;
        }

        return team == CombatCharacterManager.CombatTeam.TeamA ? teamAUiReference : teamBUiReference;
    }

    public TeamActionSelectUI teamAActionSelectUI => GetTeamActionUI(CombatCharacterManager.CombatTeam.TeamA);
    public TeamActionSelectUI teamBActionSelectUI => GetTeamActionUI(CombatCharacterManager.CombatTeam.TeamB);
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // CombatScene 전용이므로 DontDestroyOnLoad 적용 안함
            Debug.Log("[ActionCommandSelectionManager] 싱글톤 인스턴스 생성");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Inspector에서 할당되지 않은 경우 자동 찾기 (하위 호환성)
        AutoDiscoverTeamUI(CombatCharacterManager.CombatTeam.TeamA);
        AutoDiscoverTeamUI(CombatCharacterManager.CombatTeam.TeamB);
    }
    
    public void RegisterTeamActionUI(CombatCharacterManager.CombatTeam team, TeamActionSelectUI ui)
    {
        if (ui == null)
        {
            Debug.LogWarning($"[ActionCommandSelectionManager] RegisterTeamActionUI: {team} UI가 null입니다.");
            return;
        }

        if (teamUiLookup.TryGetValue(team, out var existing) && existing != null && existing != ui)
        {
            Debug.LogWarning($"[ActionCommandSelectionManager] {team} UI 재등록: 기존 {existing.name} → 새로 {ui.name}");
        }

        if (!IsSceneInstance(ui))
        {
            Debug.LogWarning($"[ActionCommandSelectionManager] {team} UI 등록 무시 - Scene 인스턴스가 아닙니다. ({ui.name})");
            return;
        }

        teamUiLookup[team] = ui;

        if (team == CombatCharacterManager.CombatTeam.TeamA)
        {
            teamAUiReference = ui;
        }
        else
        {
            teamBUiReference = ui;
        }

        Debug.Log($"[ActionCommandSelectionManager] {team} UI 등록 완료: {ui.name}");
    }
    
    /// <summary>
    /// 인스턴스가 없으면 생성합니다
    /// </summary>
    public static ActionCommandSelectionManager EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject managerObject = new GameObject("ActionCommandSelectionManager");
            Instance = managerObject.AddComponent<ActionCommandSelectionManager>();
        }
        return Instance;
    }

    private void AutoDiscoverTeamUI(CombatCharacterManager.CombatTeam team)
    {
        if (team == CombatCharacterManager.CombatTeam.TeamA && !IsSceneInstance(teamAUiReference))
        {
            teamAUiReference = null;
        }

        if (team == CombatCharacterManager.CombatTeam.TeamB && !IsSceneInstance(teamBUiReference))
        {
            teamBUiReference = null;
        }

        if (team == CombatCharacterManager.CombatTeam.TeamA && teamAUiReference != null)
        {
            RegisterTeamActionUI(team, teamAUiReference);
            return;
        }

        if (team == CombatCharacterManager.CombatTeam.TeamB && teamBUiReference != null)
        {
            RegisterTeamActionUI(team, teamBUiReference);
            return;
        }

        var discovered = FindTeamUiInScene(team);
        if (discovered != null)
        {
            RegisterTeamActionUI(team, discovered);
        }
    }

    private TeamActionSelectUI FindTeamUiInScene(CombatCharacterManager.CombatTeam team)
    {
        var candidates = FindObjectsByType<TeamActionSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var candidate in candidates)
        {
            if (candidate != null && candidate.Team == team && IsSceneInstance(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsSceneInstance(Component component)
    {
        return component != null && component.gameObject.scene.IsValid();
    }
}
