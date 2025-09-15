// CombatStatusDisplay.cs (전체 리팩터링본)

using TMPro;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class CombatStatusDisplay : MonoBehaviour
{
    public static CombatStatusDisplay Instance { get; private set; }

    [Header("Progress & Labels")]
    [Tooltip("액션 진행도 표시 텍스트")]
    public TextMeshProUGUI actionProgress;
    public TextMeshProUGUI turnLabel;
    [SerializeField] private GameObject resultLinePrefab; // TextMeshProUGUI prefab
    public TextMeshProUGUI inputPromptText;

    [Header("Player UI")]
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI playerActionCommandName;
    public TextMeshProUGUI playerActionInputCooldown;
    [SerializeField] private Transform playerHitResultContainer;
    [SerializeField] private Transform TurnResultContainer;

    [Header("Player Status UI")]
    public TextMeshProUGUI playerHP;
    public TextMeshProUGUI playerPoise;
    public TextMeshProUGUI playerATK;
    public TextMeshProUGUI playerDR;
    public TextMeshProUGUI playerCrit;

    [Header("Enemy UI")]
    public TextMeshProUGUI enemyName;
    public TextMeshProUGUI enemyActionCommandName;
    public TextMeshProUGUI enemyActionInputCooldown;
    [SerializeField] private Transform enemyHitResultContainer;

    [Header("Enemy Status UI")]
    public TextMeshProUGUI enemyHP;
    public TextMeshProUGUI enemyPoise;
    public TextMeshProUGUI enemyATK;
    public TextMeshProUGUI enemyDR;
    public TextMeshProUGUI enemyCrit;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // CharacterManager가 초기화될 때까지 대기
        StartCoroutine(WaitForCharacterManager());
    }

    private System.Collections.IEnumerator WaitForCharacterManager()
    {
        // CharacterManager가 초기화될 때까지 대기
        while (CharacterManager.Instance == null)
        {
            yield return null;
        }

        // CharacterManager의 데이터가 준비될 때까지 대기
        while (CharacterManager.Instance.PlayerData == null || CharacterManager.Instance.EnemyData == null)
        {
            yield return null;
        }

        // UI 초기화 및 이벤트 구독
        InitializeStatusUI();
        SubscribeToStatusEvents();
    }

    private void InitializeStatusUI()
    {
        // 초기 스테이터스 표시
        UpdatePlayerStatus();
        UpdateEnemyStatus();
    }

    private void SubscribeToStatusEvents()
    {
        // 플레이어 스테이터스 이벤트 구독
        if (CharacterManager.Instance.PlayerData != null)
        {
            CharacterManager.Instance.PlayerData.OnStatsChanged += OnPlayerStatsChanged;
        }

        // 적 스테이터스 이벤트 구독
        if (CharacterManager.Instance.EnemyData != null)
        {
            CharacterManager.Instance.EnemyData.OnStatsChanged += OnEnemyStatsChanged;
        }
    }

    private void OnPlayerStatsChanged(CharacterData data)
    {
        UpdatePlayerStatus();
    }

    private void OnEnemyStatsChanged(CharacterData data)
    {
        UpdateEnemyStatus();
    }

    private void UpdatePlayerStatus()
    {
        if (CharacterManager.Instance?.PlayerData == null) return;

        var data = CharacterManager.Instance.PlayerData;
        if (playerHP != null) playerHP.text = $"HP: {data.GetHPStatus()}";
        if (playerPoise != null) playerPoise.text = $"Poise: {data.GetPoiseStatus()}";
        if (playerATK != null) playerATK.text = $"ATK: {data.ATK}";
        if (playerDR != null) playerDR.text = $"DR: {data.DR}";
        if (playerCrit != null) playerCrit.text = $"Crit: {data.Crit}%";
    }

    private void UpdateEnemyStatus()
    {
        if (CharacterManager.Instance?.EnemyData == null) return;

        var data = CharacterManager.Instance.EnemyData;
        if (enemyHP != null) enemyHP.text = $"HP: {data.GetHPStatus()}";
        if (enemyPoise != null) enemyPoise.text = $"Poise: {data.GetPoiseStatus()}";
        if (enemyATK != null) enemyATK.text = $"ATK: {data.ATK}";
        if (enemyDR != null) enemyDR.text = $"DR: {data.DR}";
        if (enemyCrit != null) enemyCrit.text = $"Crit: {data.Crit}%";
    }

    [ContextMenu("Force Update Status")]
    public void ForceUpdateStatus()
    {
        UpdatePlayerStatus();
        UpdateEnemyStatus();
    }

    [ContextMenu("Test Player Take Damage")]
    public void TestPlayerTakeDamage()
    {
        CharacterManager.Instance?.PlayerData?.TakeDamage(10);
    }

    [ContextMenu("Test Enemy Take Damage")]
    public void TestEnemyTakeDamage()
    {
        CharacterManager.Instance?.EnemyData?.TakeDamage(10);
    }

    [ContextMenu("Test Player Lose Poise")]
    public void TestPlayerLosePoise()
    {
        CharacterManager.Instance?.PlayerData?.LosePoise(25);
    }

    [ContextMenu("Test Enemy Lose Poise")]
    public void TestEnemyLosePoise()
    {
        CharacterManager.Instance?.EnemyData?.LosePoise(25);
    }

    public void whosTurnText(bool isPlayer)
    {
        actionProgress.text = isPlayer ? "플레이어 공격 턴" : "적 공격 턴";
        Image img_PlayerContainer = playerHitResultContainer.GetComponent<Image>();
        Image img_EnemyContainer = enemyHitResultContainer.GetComponent<Image>();
        if (isPlayer)
        {
            img_PlayerContainer.color = Color.green; // 플레이어 턴은 초록색
            img_EnemyContainer.color = Color.white; // 적 턴은 흰색
        }
        else
        {
            img_PlayerContainer.color = Color.white; // 플레이어 턴은 흰색
            img_EnemyContainer.color = Color.red; // 적 턴은 빨간색
        }

    }
    public void updateTurnInfo(float turnTimer)
    {
        turnLabel.text = $"턴: {turnTimer.ToString("F2")}초";
    }
    public void SetPlayerActionCommandName(string commandName)
        => playerActionCommandName.text = $"[액션] {commandName}";

    public void SetEnemyActionCommandName(string commandName)
        => enemyActionCommandName.text = $"[액션] {commandName}";

    public void SetPlayerActionInputCooldown(float cooldown)
    {
        if(cooldown <= 0f)
        {
            playerActionInputCooldown.text = "입력 가능!";            
        }
        else
        {
            playerActionInputCooldown.text = $"입력 대기: {cooldown.ToString("F2")}초";
        }
    }


    /////////////////////////////////////////////////////////////////////////////////

    public void ShowCommandStart(bool isPlayer, string name)
    {
        if(isPlayer) playerActionCommandName.text = $"[액션 시작] {name}";
        else enemyActionCommandName.text = $"[액션 시작] {name}";
    }

    public void ShowInputPrompt(string message)
    {
        inputPromptText.text = message;
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
