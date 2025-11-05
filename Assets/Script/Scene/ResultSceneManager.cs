using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ResultScene을 관리하는 매니저
/// 전투 결과를 표시하고 다음 행동을 선택
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    // Static 변수로 전투 결과 전달
    public static BattleResult LastBattleResult { get; set; }

    [Header("UI 참조 - 결과 표시")]
    [Tooltip("승리/패배 타이틀 텍스트")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [Tooltip("획득 골드 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [Tooltip("획득 경험치 표시 텍스트")]
    [SerializeField] private TextMeshProUGUI expRewardText;
    [Tooltip("승리 시 활성화될 패널")]
    [SerializeField] private GameObject victoryPanel;
    [Tooltip("패배 시 활성화될 패널")]
    [SerializeField] private GameObject defeatPanel;

    [Header("UI 참조 - 버튼")]
    [Tooltip("계속 버튼 (TestScene/LobbyScene으로 복귀)")]
    [SerializeField] private Button continueButton;
    [Tooltip("타이틀로 버튼")]
    [SerializeField] private Button returnToTitleButton;

    [Header("디버그")]
    [Tooltip("디버그 로그 활성화")]
    [SerializeField] private bool enableDebugLog = true;

    private void Start()
    {
        InitializeUI();
        DisplayBattleResult();
        Log("ResultScene 초기화 완료");
    }

    private void InitializeUI()
    {
        // 버튼 이벤트 연결
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        else
        {
            Debug.LogWarning("[ResultSceneManager] Continue 버튼이 할당되지 않았습니다!");
        }

        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
        }
        else
        {
            Debug.LogWarning("[ResultSceneManager] Return To Title 버튼이 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 전투 결과 표시
    /// </summary>
    private void DisplayBattleResult()
    {
        if (LastBattleResult == null)
        {
            Debug.LogWarning("[ResultSceneManager] 전투 결과 데이터가 없습니다!");
            SetDefaultResult();
            return;
        }

        bool isVictory = LastBattleResult.isVictory;
        Log($"전투 결과 표시: {(isVictory ? "승리" : "패배")}");

        // 승리/패배 패널 표시
        if (victoryPanel != null) victoryPanel.SetActive(isVictory);
        if (defeatPanel != null) defeatPanel.SetActive(!isVictory);

        // 결과 텍스트
        if (resultTitleText != null)
        {
            resultTitleText.text = isVictory ? "승리!" : "패배...";
            resultTitleText.color = isVictory ? Color.yellow : Color.red;
        }

        // 보상 표시
        if (goldRewardText != null)
        {
            goldRewardText.text = $"골드: +{LastBattleResult.goldReward}";
        }

        if (expRewardText != null)
        {
            expRewardText.text = $"경험치: +{LastBattleResult.expReward}";
        }

        // 보상 적용
        ApplyRewards();
    }

    /// <summary>
    /// 플레이어에게 보상 적용
    /// </summary>
    private void ApplyRewards()
    {
        if (LastBattleResult == null) return;

        if (PlayerCharacterManager.Instance != null)
        {
            PlayerCharacterManager.Instance.AddGold(LastBattleResult.goldReward);
            PlayerCharacterManager.Instance.AddExperience(LastBattleResult.expReward);
            Log($"보상 적용 완료: 골드 +{LastBattleResult.goldReward}, 경험치 +{LastBattleResult.expReward}");
        }
        else
        {
            Debug.LogError("[ResultSceneManager] PlayerCharacterManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 기본 결과 설정 (데이터가 없을 때)
    /// </summary>
    private void SetDefaultResult()
    {
        if (resultTitleText != null)
        {
            resultTitleText.text = "결과 없음";
            resultTitleText.color = Color.gray;
        }

        if (goldRewardText != null)
        {
            goldRewardText.text = "골드: +0";
        }

        if (expRewardText != null)
        {
            expRewardText.text = "경험치: +0";
        }

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    /// <summary>
    /// "계속" 버튼 클릭 시 (TestScene으로 복귀)
    /// </summary>
    private void OnContinueClicked()
    {
        Log("계속 버튼 클릭 - TestScene으로 복귀");

        if (SceneFlowController.Instance != null)
        {
            SceneFlowController.Instance.GoToTestScene();
        }
        else
        {
            Debug.LogError("[ResultSceneManager] SceneFlowController를 찾을 수 없습니다!");
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
            Debug.LogError("[ResultSceneManager] SceneFlowController를 찾을 수 없습니다!");
        }
    }

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ResultSceneManager] {message}");
        }
    }

    private void OnDestroy()
    {
        // Scene이 언로드될 때 결과 데이터 정리
        LastBattleResult = null;
    }
}

