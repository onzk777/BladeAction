using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// TitleScene을 관리하는 매니저
/// 게임 시작 버튼, 종료 버튼 등을 처리
/// </summary>
public class TitleSceneManager : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("게임 시작 버튼")]
    [SerializeField] private Button startGameButton;
    [Tooltip("게임 종료 버튼")]
    [SerializeField] private Button exitGameButton;
    [Tooltip("버전 정보 텍스트")]
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("게임 정보")]
    [Tooltip("게임 버전 정보 (Version Text에 표시됨)")]
    [SerializeField] private string gameVersion = "v0.1.0";

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

    private void Start()
    {
        InitializeUI();
        Log("TitleScene 초기화 완료");
    }

    private void InitializeUI()
    {
        // 버튼 이벤트 연결
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            Debug.LogWarning("[TitleSceneManager] Start Game 버튼이 할당되지 않았습니다!");
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.AddListener(OnExitGameClicked);
        }
        else
        {
            Debug.LogWarning("[TitleSceneManager] Exit Game 버튼이 할당되지 않았습니다!");
        }

        // 버전 정보 표시
        if (versionText != null)
        {
            versionText.text = gameVersion;
        }
    }

    /// <summary>
    /// "게임 시작" 버튼 클릭 시 (TestScene으로 전환)
    /// </summary>
    private void OnStartGameClicked()
    {
        Log("게임 시작 버튼 클릭 - TestScene으로 전환");

        if (SceneFlowController.Instance != null)
        {
            SceneFlowController.Instance.GoToTestScene();
        }
        else
        {
            Debug.LogError("[TitleSceneManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// "종료" 버튼 클릭 시
    /// </summary>
    private void OnExitGameClicked()
    {
        Log("게임 종료 버튼 클릭");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[TitleSceneManager] {message}");
        }
    }
}

