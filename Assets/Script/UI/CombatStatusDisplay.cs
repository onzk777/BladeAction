// CombatStatusDisplay.cs (전체 리팩터링본)

using TMPro;
using UnityEngine;

public class CombatStatusDisplay : MonoBehaviour
{
    public static CombatStatusDisplay Instance { get; private set; }

    [Header("Progress & Labels")]
    [Tooltip("액션 진행도 표시 텍스트")]
    public TextMeshProUGUI actionProgress;
    public TextMeshProUGUI turnLabel;
    [SerializeField] private GameObject resultLinePrefab; // TextMeshProUGUI prefab


    [Header("Player UI")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerPromptText;
    public TextMeshProUGUI playerActionCommandName;
    [SerializeField] private Transform playerHitResultContainer;
    [SerializeField] private Transform TurnResultContainer;


    [Header("Enemy UI")]
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI enemyPromptText;
    public TextMeshProUGUI enemyActionCommandName;
    [SerializeField] private Transform enemyHitResultContainer;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void whosTurnText(bool isPlayer)
    {
        actionProgress.text = isPlayer ? "플레이어의 턴" : "적의 턴";
    }
    public void updateTurnInfo(float turnTimer)
    {
        turnLabel.text = $"턴: {turnTimer.ToString("F2")}초";
    }
    public void SetPlayerActionCommandName(string commandName)
        => playerActionCommandName.text = $"[액션] {commandName}";

    /////////////////////////////////////////////////////////////////////////////////
    
    public void ShowCommandStart(bool isPlayer, string name)
    {
        if(isPlayer) playerActionCommandName.text = $"[액션 시작] {name}";
        else enemyActionCommandName.text = $"[액션 시작] {name}";
    }

    public void ShowInputPrompt(bool isPlayer, string message)
    {
        if (isPlayer) playerPromptText.text = message;
        else enemyPromptText.text = message;
    }

    public void ShowPlayerHitResult(int hitIndex, string msg)
    {
        var go = Instantiate(resultLinePrefab, playerHitResultContainer);
        go.GetComponent<TextMeshProUGUI>().text =
            $"히트 {hitIndex+1}: {msg}";
    }
    /// 적의 히트 판정 결과를 (필요하다면) 화면에 보여 줍니다.
    public void ShowEnemyHitResult(int hitIndex, string msg)
    {
        var go = Instantiate(resultLinePrefab, enemyHitResultContainer);
        go.GetComponent<TextMeshProUGUI>().text =
            $"히트 {hitIndex + 1}: {msg}";
    }
    public void ShowHitVersusResult(int hitIndex, string msg)
    {
        Debug.Log($"[CombatStatusDisplay] ShowHitVersusResult 호출됨: 히트 {hitIndex + 1} → {msg}");
        // 히트 대결 결과를 화면에 보여 줍니다.
        var go = Instantiate(resultLinePrefab, TurnResultContainer);
        go.GetComponent<TextMeshProUGUI>().text =
            $"히트 대결 {hitIndex + 1}: {msg}";
    }

    public void ClearResults()
    {
        foreach (Transform child in playerHitResultContainer) Destroy(child.gameObject);
        foreach (Transform child in enemyHitResultContainer) Destroy(child.gameObject);
        foreach (Transform child in TurnResultContainer) Destroy(child.gameObject);

    }


}
