using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 게임의 Scene Flow를 제어하는 컨트롤러 (CoreSystemScene 소속)
/// 
/// 역할:
/// - Scene 전환 관리
/// - Scene 진입 시 필요한 데이터 전달
/// - Scene 로드 후 초기화 트리거
/// 
/// 컨텐츠별 Flow 메서드 제공:
/// - GoToTitle(): 타이틀 화면
/// - GoToTestScene(): 테스트/Lobby
/// - StartCombatFlow(playerId, enemyId): 전투 시작
/// - ShowResultFlow(result): 전투 결과 표시
/// </summary>
public class SceneFlowController : MonoBehaviour
{
    public static SceneFlowController Instance { get; private set; }

    [Header("Scene 참조")]
    [Tooltip("전투 Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset combatSceneAsset;
#endif
    [Tooltip("타이틀 화면 Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset titleSceneAsset;
#endif
    [Tooltip("테스트/Lobby Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset testSceneAsset;
#endif
    [Tooltip("전투 결과 Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset resultSceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string combatSceneName = "";
    [HideInInspector]
    [SerializeField] private string titleSceneName = "";
    [HideInInspector]
    [SerializeField] private string testSceneName = "";
    [HideInInspector]
    [SerializeField] private string resultSceneName = "";

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // SceneAsset이 변경되면 자동으로 Scene 이름 업데이트
        if (combatSceneAsset != null)
            combatSceneName = combatSceneAsset.name;
        if (titleSceneAsset != null)
            titleSceneName = titleSceneAsset.name;
        if (testSceneAsset != null)
            testSceneName = testSceneAsset.name;
        if (resultSceneAsset != null)
            resultSceneName = resultSceneAsset.name;
    }
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Log("SceneFlowController 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[SceneFlowController] 중복된 인스턴스 발견, 제거합니다.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전투 시작 (Scene 전환 + 전투 트리거)
    /// </summary>
    /// <param name="playerId">플레이어 Character Instance ID</param>
    /// <param name="enemyId">적 Character Instance ID</param>
    public void StartCombatFlow(string playerId, string enemyId)
    {
        Log($"전투 시작 Flow: {playerId} vs {enemyId}");
        
        // 1. CombatManager에 전투 참가자 설정 (static)
        CombatManager.SetupNextBattle(playerId, enemyId);
        
        // 2. CombatScene 전환 완료 시 콜백 등록
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.OnSceneTransitionComplete += OnCombatSceneLoaded;
            SceneTransitionManager.Instance.TransitionToScene(combatSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// CombatScene 로드 완료 시 전투 시작 트리거
    /// </summary>
    private void OnCombatSceneLoaded(string sceneName)
    {
        if (sceneName == combatSceneName)
        {
            // 이벤트 구독 해제
            SceneTransitionManager.Instance.OnSceneTransitionComplete -= OnCombatSceneLoaded;
            
            // 3. CombatManager에게 전투 시작 명령
            if (CombatManager.Instance != null)
            {
                Log($"CombatScene 로드 완료 - 전투 시작 트리거");
                CombatManager.Instance.StartBattle(); // SetupNextBattle()로 설정된 참가자로 시작
            }
            else
            {
                Debug.LogError("[SceneFlowController] CombatManager를 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 타이틀 화면으로 이동
    /// </summary>
    public void GoToTitle()
    {
        Log("타이틀 화면으로 이동");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(titleSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// TestScene(Lobby)으로 이동
    /// </summary>
    public void GoToTestScene()
    {
        Log("TestScene으로 이동");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(testSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 전투 결과 표시 Flow (데이터 전달 + Scene 전환)
    /// </summary>
    /// <param name="result">전투 결과 데이터</param>
    public void ShowResultFlow(BattleResult result)
    {
        Log($"전투 결과 표시 Flow: {(result.isVictory ? "승리" : "패배")}");
        
        // 1. ResultSceneManager에 결과 데이터 전달 (static)
        ResultSceneManager.LastBattleResult = result;
        
        // 2. ResultScene으로 전환
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(resultSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
        
        // ResultSceneManager.Start()에서 자동으로 LastBattleResult 표시
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (안전장치)
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.OnSceneTransitionComplete -= OnCombatSceneLoaded;
        }
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SceneFlowController] {message}");
        }
    }
}

