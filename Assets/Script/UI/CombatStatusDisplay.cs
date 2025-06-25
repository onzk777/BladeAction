// CombatStatusDisplay.cs (전체 리팩터링본)

using TMPro;
using UnityEngine;

public class CombatStatusDisplay : MonoBehaviour
{
    public static CombatStatusDisplay Instance;

    [Header("Progress & Labels")]
    [Tooltip("액션 진행도 표시 텍스트")]
    public TextMeshProUGUI actionProgress;
    public TextMeshProUGUI turnLabel;

    [Header("Player UI")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerActionCommandName;
    [SerializeField] private TextMeshProUGUI playerTimingInfoText;
    public TextMeshProUGUI playerTimingStatusText;
    public TextMeshProUGUI playerInputResultText;

    [Header("Enemy UI")]
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI enemyActionCommandName;
    [SerializeField] private TextMeshProUGUI enemyTimingInfoText;
    public TextMeshProUGUI enemyTimingStatusText;
    public TextMeshProUGUI enemyInputResultText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>전투 UI를 초기화합니다.</summary>
    public void ResetText()
    {
        actionProgress.text = string.Empty;
        turnLabel.text = string.Empty;
        playerTimingInfoText.text = string.Empty;
        enemyTimingInfoText.text = string.Empty;
        playerTimingStatusText.text = string.Empty;
        playerInputResultText.text = string.Empty;
        enemyTimingStatusText.text = string.Empty;
        enemyInputResultText.text = string.Empty;
    }

    /// <summary>전체 턴 중 현재 턴 진행도를 "액션 X/Y 진행 중" 형태로 표시합니다.</summary>
    public void SetActionProgress(int current, int total)
    {
        actionProgress.text = $"[턴 {current}/{total}] 진행 중";
    }

    /// <summary>현재 턴 레이블("Player Turn (2.3s)" 등)을 설정합니다.</summary>
    public void ShowTurnLabel(string turnText)
        => turnLabel.text = turnText;

    /// <summary>플레이어 타이밍 디버그 정보([Hit X/Y] 텍스트)를 설정합니다.</summary>
    public void SetPlayerTimingInfoText(string text)
        => playerTimingInfoText.text = text;

    /// <summary>적 타이밍 디버그 정보([Hit X/Y] 텍스트)를 빨간색으로 설정합니다.</summary>
    public void SetEnemyTimingInfoText(string text)
        => enemyTimingInfoText.text = $"<color=red>{text}</color>";

    public void SetPlayerActionCommandName(string commandName)
        => playerActionCommandName.text = $"[액션] {commandName}";

    public void SetEnemyActionCommandName(string commandName)
        => enemyActionCommandName.text = $"[액션] {commandName}";

    public void ShowPlayerTimingStart()
        => playerTimingStatusText.text = "입력 가능!";

    public void ShowPlayerTimingPerfectStart()
        => playerTimingStatusText.text = "완벽한 타이밍!";

    public void ShowPlayerTimingPerfectEnd()
        => playerTimingStatusText.text = "완벽하지 않은 타이밍...";

    public void ShowPlayerTimingEnd()
        => playerTimingStatusText.text = "입력 끝...";

    public void ShowPlayerPerfectInputResult(string status)
        => playerInputResultText.text = status;

    public void ShowEnemyTimingStart()
        => enemyTimingStatusText.text = "입력 가능!";

    public void ShowEnemyTimingPerfectStart()
        => enemyTimingStatusText.text = "완벽한 타이밍!";

    public void ShowEnemyTimingPerfectEnd()
        => enemyTimingStatusText.text = "완벽하지 않은 타이밍...";

    public void ShowEnemyTimingEnd()
        => enemyTimingStatusText.text = "입력 끝...";

    public void ShowEnemyPerfectInputResult(string result)
        => enemyInputResultText.text = result;
}
