using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 게임의 Scene 흐름을 제어하는 컨트롤러 (CoreSystemScene 소속)
/// 전투 시작, 인벤토리 접근, Scene 전환 등 게임 컨텐츠 진입을 관리
/// TestScene, LobbyScene 등에서 호출하여 사용
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
    [HideInInspector]
    [SerializeField] private string combatSceneName = "03.CombatScene";
    [HideInInspector]
    [SerializeField] private string titleSceneName = "05.TitleScene";
    [HideInInspector]
    [SerializeField] private string testSceneName = "00.TestScene";

    [Header("전투 설정")]
    [Tooltip("전투 시작 시 기본으로 사용할 플레이어 ID (CharacterDatabase에 등록된 ID)")]
    [SerializeField] private string defaultPlayerId = "Player";
    [Tooltip("전투 시작 시 기본으로 사용할 적 ID (테스트용, 커스텀 ID로 호출 가능)")]
    [SerializeField] private string defaultEnemyId = "Test_Enemy1";

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
    /// 전투 시작 (CombatScene으로 전환 후 자동 시작)
    /// </summary>
    public void StartCombat(string playerId = null, string enemyId = null)
    {
        string actualPlayerId = string.IsNullOrEmpty(playerId) ? defaultPlayerId : playerId;
        string actualEnemyId = string.IsNullOrEmpty(enemyId) ? defaultEnemyId : enemyId;

        Log($"전투 시작: {actualPlayerId} vs {actualEnemyId}");

        if (SceneTransitionManager.Instance != null)
        {
            // Scene 전환 완료 후 전투 시작
            SceneTransitionManager.Instance.OnSceneTransitionComplete += OnCombatSceneLoaded;
            SceneTransitionManager.Instance.TransitionToScene(combatSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// CombatScene 로드 완료 시 전투 시작
    /// </summary>
    private void OnCombatSceneLoaded(string sceneName)
    {
        if (sceneName == combatSceneName)
        {
            // 이벤트 구독 해제
            SceneTransitionManager.Instance.OnSceneTransitionComplete -= OnCombatSceneLoaded;

            // CombatManager 찾아서 전투 시작
            if (CombatManager.Instance != null)
            {
                Log($"전투 시작 명령 전달: {defaultPlayerId} vs {defaultEnemyId}");
                CombatManager.Instance.StartBattle(defaultPlayerId, defaultEnemyId);
            }
            else
            {
                Debug.LogError("[SceneFlowController] CombatManager를 찾을 수 없습니다!");
            }
        }
    }

    /// <summary>
    /// 타이틀 화면으로 복귀
    /// </summary>
    public void ReturnToTitle()
    {
        Log("타이틀 화면으로 복귀");

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
    /// TestScene으로 전환
    /// </summary>
    public void GoToTestScene()
    {
        Log("TestScene으로 전환");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(testSceneName);
        }
        else
        {
            Debug.LogError("[SceneFlowController] SceneTransitionManager를 찾을 수 없습니다!");
        }
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

