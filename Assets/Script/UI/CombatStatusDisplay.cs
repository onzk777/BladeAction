using TMPro;
using UnityEngine;

public class CombatStatusDisplay : MonoBehaviour
{
    public static CombatStatusDisplay Instance;

    public TextMeshProUGUI actionProgress;

    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerActionCommandName;    
    public TextMeshProUGUI playerTimingStatusText;
    public TextMeshProUGUI playerInputResultText;
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI enemyActionCommandName;
    public TextMeshProUGUI enemyTimingStatusText;
    public TextMeshProUGUI enemyInputResultText;
    [SerializeField] private TextMeshProUGUI timingInfoText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ResetText()
    {
        
        actionProgress.text = "";
        playerTimingStatusText.text = "";
        playerInputResultText.text = "";
        enemyTimingStatusText.text = "";
        enemyInputResultText.text = "";
    }
    public void SetActionProgress(int current, int total)
    {
        if (current == total) actionProgress.text = $"[액션 {current}/{total}] 진행 중";
        else actionProgress.text = $"[액션 {current}/{total}] 진행 중";
    }
    public void SetPlayerTimingInfoText(string text)
    {
        if (timingInfoText != null)
            timingInfoText.text = text;
    }

    public void SetPlayerActionCommandName(string commandName)
    {
        playerActionCommandName.text = $"[액션] {commandName}";        
    }
    public void SetEnemyActionCommandName(string commandName)
    {
        enemyActionCommandName.text = $"[액션] {commandName}";
    }




    public void ShowPlayerTimingStart()
    {
        playerTimingStatusText.text = "입력 가능!";
    }
    public void ShowPlayerTimingPerfectStart()
    {
        playerTimingStatusText.text = "완벽한 타이밍!";
    }
    public void ShowPlayerTimingPerfectEnd()
    {
        playerTimingStatusText.text = "완벽하지 않은 타이밍...";
    }
    public void ShowPlayerTimingEnd()
    {
        playerTimingStatusText.text = "입력 끝..."; // 입력창 닫힘
    }
    public void ShowPlayerStatus(string status)
    {
        playerInputResultText.text = status;
    }


    public void ShowEnemyTimingStart()
    {
        enemyTimingStatusText.text = "입력 가능!";
    }

    public void ShowEnemyTimingPerfectStart()
    {
        enemyTimingStatusText.text = "완벽한 타이밍!";
    }
    public void ShowEnemyTimingPerfectEnd()
    {
        enemyTimingStatusText.text = "완벽하지 않은 타이밍...";
    }

    public void ShowEnemyTimingEnd()
    {
        enemyTimingStatusText.text = "입력 끝..."; // 입력창 닫힘
    }

    public void ShowEnemyStatus(string status)
    {
        enemyInputResultText.text = status;
    }
}
