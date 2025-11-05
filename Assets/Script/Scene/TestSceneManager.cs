using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TestScene을 관리하는 매니저
/// Scene 전환 테스트를 위한 버튼 이벤트 처리
/// </summary>
public class TestSceneManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("전투 시작 버튼")]
    [SerializeField] private Button startCombatButton;
    [Tooltip("타이틀로 복귀 버튼")]
    [SerializeField] private Button returnToTitleButton;

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

    private void Start()
    {
        InitializeButtons();
        Log("TestScene 초기화 완료");
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
    /// </summary>
    private void OnStartCombatClicked()
    {
        Log("전투 시작 버튼 클릭");

        if (SceneFlowController.Instance != null)
        {
            // 기본 ID로 전투 시작
            SceneFlowController.Instance.StartCombat();
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
            SceneFlowController.Instance.ReturnToTitle();
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

