// CombatDebugDisplay.cs
// 전투 디버그 정보 표시 (개발자용)
// 배치 위치: PersistentUIScene > Canvus_Debug

using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// 전투 중 디버그 정보를 표시하는 UI 컴포넌트
/// PersistentUIScene의 Canvus_Debug에 배치됨
/// </summary>
public class CombatDebugDisplay : MonoBehaviour
{
    public static CombatDebugDisplay Instance { get; private set; }
    
    private CombatCharacterManager.CombatantSlot trackedTeamASlot;
    private Character trackedTeamACharacter;
    private CombatCharacterManager.CombatantSlot trackedTeamBSlot;
    private Character trackedTeamBCharacter;

    [Header("Player Debug UI")]
    [Tooltip("플레이어 이름")]
    public TextMeshProUGUI playerName;
    
    [Tooltip("플레이어 HP 상태")]
    public TextMeshProUGUI playerHP;
    
    [Tooltip("플레이어 Poise 상태")]
    public TextMeshProUGUI playerPoise;
    
    [Tooltip("플레이어 ATK")]
    public TextMeshProUGUI playerATK;
    
    [Tooltip("플레이어 DR")]
    public TextMeshProUGUI playerDR;
    
    [Tooltip("플레이어 Crit")]
    public TextMeshProUGUI playerCrit;
    
    [Tooltip("플레이어가 선택한 액션 커맨드 이름")]
    public TextMeshProUGUI playerActionCommandName;
    
    [Tooltip("플레이어 액션 입력 쿨다운")]
    public TextMeshProUGUI playerActionInputCooldown;
    
    [Tooltip("플레이어 히트 결과를 표시할 컨테이너")]
    [SerializeField] private Transform playerHitResultContainer;

    [Header("Enemy Debug UI")]
    [Tooltip("적 이름")]
    public TextMeshProUGUI enemyName;
    
    [Tooltip("적 HP 상태")]
    public TextMeshProUGUI enemyHP;
    
    [Tooltip("적 Poise 상태")]
    public TextMeshProUGUI enemyPoise;
    
    [Tooltip("적 ATK")]
    public TextMeshProUGUI enemyATK;
    
    [Tooltip("적 DR")]
    public TextMeshProUGUI enemyDR;
    
    [Tooltip("적 Crit")]
    public TextMeshProUGUI enemyCrit;
    
    [Tooltip("적이 선택한 액션 커맨드 이름")]
    public TextMeshProUGUI enemyActionCommandName;
    
    [Tooltip("적 액션 입력 쿨다운")]
    public TextMeshProUGUI enemyActionInputCooldown;
    
    [Tooltip("적 히트 결과를 표시할 컨테이너")]
    [SerializeField] private Transform enemyHitResultContainer;

    [Header("Combat Log")]
    [Tooltip("액션 진행도 표시 텍스트")]
    public TextMeshProUGUI actionProgress;
    
    [Tooltip("턴 대결 결과를 표시할 컨테이너")]
    [SerializeField] private Transform turnResultContainer;
    
    [Tooltip("결과 라인을 생성하기 위한 Prefab")]
    [SerializeField] private GameObject resultLinePrefab;

    [Header("Turn Info")]
    [Tooltip("턴 타이머 시간 표시 (Debug)")]
    public TextMeshProUGUI turnLabel;
    
    [Tooltip("입력 프롬프트 표시 (Debug)")]
    public TextMeshProUGUI inputPromptText;

    [Header("Restart Button")]
    [Tooltip("전투 재시작 버튼 (자동으로 찾거나 수동 할당)")]
    public UnityEngine.UI.Button restartButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[CombatDebugDisplay] 중복 인스턴스 감지! 기존 인스턴스 유지");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 재시작 버튼 찾기 및 연결
        InitializeRestartButton();
        
        // CombatCharacterManager가 초기화될 때까지 대기 후 구독
        StartCoroutine(WaitForCombatCharacterManager());
    }

    /// <summary>
    /// 재시작 버튼을 찾아서 CombatManager.RestartBattle에 연결
    /// </summary>
    private void InitializeRestartButton()
    {
        // 버튼이 수동으로 할당되지 않았다면 자동으로 찾기
        if (restartButton == null)
        {
            // Button_Restart 이름으로 찾기
            GameObject buttonObj = GameObject.Find("Button_Restart");
            if (buttonObj != null)
            {
                restartButton = buttonObj.GetComponent<UnityEngine.UI.Button>();
                Debug.Log("[CombatDebugDisplay] 재시작 버튼 자동 찾기 성공");
            }
            else
            {
                Debug.LogWarning("[CombatDebugDisplay] 재시작 버튼을 찾을 수 없습니다. Button_Restart 이름을 가진 GameObject가 있는지 확인하세요.");
                return;
            }
        }

        // 버튼이 있으면 이벤트 연결
        if (restartButton != null)
        {
            // 기존 리스너 제거 후 새로 추가
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
            Debug.Log("[CombatDebugDisplay] 재시작 버튼 이벤트 연결 완료");
        }
    }

    /// <summary>
    /// 재시작 버튼 클릭 시 호출되는 메서드
    /// </summary>
    private void OnRestartButtonClicked()
    {
        Debug.Log("[CombatDebugDisplay] 재시작 버튼 클릭됨");
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.RestartBattle();
        }
        else
        {
            Debug.LogError("[CombatDebugDisplay] CombatManager.Instance가 null입니다!");
        }
    }

    private IEnumerator WaitForCombatCharacterManager()
    {
        // CombatCharacterManager가 초기화될 때까지 대기
        while (CombatCharacterManager.Instance == null)
        {
            yield return null;
        }

        // CharacterManager의 데이터가 준비될 때까지 대기
        while (CombatCharacterManager.Instance.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA)?.Character == null ||
               CombatCharacterManager.Instance.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB)?.Character == null)
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
        UnsubscribeFromStatusEvents();

        var manager = CombatCharacterManager.Instance;
        if (manager != null)
        {
            var leaderA = manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA);
            var leaderB = manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB);

            SubscribeToLeaderCharacter(CombatCharacterManager.CombatTeam.TeamA, null, leaderA);
            SubscribeToLeaderCharacter(CombatCharacterManager.CombatTeam.TeamB, null, leaderB);
        }

        CombatCharacterManager.OnLeaderSlotChanged += HandleLeaderSlotChanged;
    }

    private void UnsubscribeFromStatusEvents()
    {
        CombatCharacterManager.OnLeaderSlotChanged -= HandleLeaderSlotChanged;
        UnsubscribeFromLeaderCharacter(CombatCharacterManager.CombatTeam.TeamA);
        UnsubscribeFromLeaderCharacter(CombatCharacterManager.CombatTeam.TeamB);
    }

    private void HandleLeaderSlotChanged(CombatCharacterManager.CombatTeam team, CombatCharacterManager.CombatantSlot previousSlot, CombatCharacterManager.CombatantSlot newSlot)
    {
        UnsubscribeFromLeaderCharacter(team);
        SubscribeToLeaderCharacter(team, previousSlot, newSlot);
    }

    private void SubscribeToLeaderCharacter(CombatCharacterManager.CombatTeam team, CombatCharacterManager.CombatantSlot previousSlot, CombatCharacterManager.CombatantSlot slot)
    {
        Character target = slot?.Character;

        if (team == CombatCharacterManager.CombatTeam.TeamA)
        {
            if (previousSlot != null && trackedTeamASlot == previousSlot && trackedTeamACharacter != null)
            {
                trackedTeamACharacter.OnStatsChanged -= OnPlayerStatsChanged;
            }

            trackedTeamASlot = slot;
            trackedTeamACharacter = target;
            if (trackedTeamACharacter != null)
            {
                trackedTeamACharacter.OnStatsChanged += OnPlayerStatsChanged;
            }

            UpdatePlayerStatus();
        }
        else if (team == CombatCharacterManager.CombatTeam.TeamB)
        {
            if (previousSlot != null && trackedTeamBSlot == previousSlot && trackedTeamBCharacter != null)
            {
                trackedTeamBCharacter.OnStatsChanged -= OnEnemyStatsChanged;
            }

            trackedTeamBSlot = slot;
            trackedTeamBCharacter = target;
            if (trackedTeamBCharacter != null)
            {
                trackedTeamBCharacter.OnStatsChanged += OnEnemyStatsChanged;
            }

            UpdateEnemyStatus();
        }
    }

    private void UnsubscribeFromLeaderCharacter(CombatCharacterManager.CombatTeam team)
    {
        if (team == CombatCharacterManager.CombatTeam.TeamA)
        {
            if (trackedTeamACharacter != null)
            {
                trackedTeamACharacter.OnStatsChanged -= OnPlayerStatsChanged;
                trackedTeamACharacter = null;
            }
            trackedTeamASlot = null;
        }
        else if (team == CombatCharacterManager.CombatTeam.TeamB)
        {
            if (trackedTeamBCharacter != null)
            {
                trackedTeamBCharacter.OnStatsChanged -= OnEnemyStatsChanged;
                trackedTeamBCharacter = null;
            }
            trackedTeamBSlot = null;
        }
    }

    private void OnPlayerStatsChanged(Character combatant)
    {
        UpdatePlayerStatus();
    }

    private void OnEnemyStatsChanged(Character combatant)
    {
        UpdateEnemyStatus();
    }

    /// <summary>
    /// 플레이어 상태 UI 업데이트
    /// </summary>
    public void UpdatePlayerStatus()
    {
        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            return;
        }

        var slot = manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA);
        var combatant = slot?.Character ?? manager.PlayerCharacter;
        if (combatant == null)
        {
            return;
        }
        
        // 이름 업데이트
        if (playerName != null) playerName.text = combatant.Name;
        
        if (playerHP != null) playerHP.text = $"HP: {combatant.GetHPStatus()}";
        if (playerPoise != null) playerPoise.text = $"Poise: {combatant.GetPoiseStatus()}";
        if (playerATK != null) playerATK.text = GetEffectiveAttackText(combatant, true);
        if (playerDR != null) 
        {
            int effectiveDR = combatant.GetFinalDR();
            if (combatant.tempDRBonus > 0)
            {
                playerDR.text = $"DR: {effectiveDR} ({combatant.DR} + {combatant.tempDRBonus})";
            }
            else
            {
                playerDR.text = $"DR: {effectiveDR}";
            }
        }
        if (playerCrit != null) playerCrit.text = $"Crit: {combatant.CritChance * 100f:F1}%";
    }

    /// <summary>
    /// 적 상태 UI 업데이트
    /// </summary>
    public void UpdateEnemyStatus()
    {
        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            return;
        }

        var slot = manager.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB);
        var combatant = slot?.Character ?? manager.CurrentEnemy;
        if (combatant == null)
        {
            return;
        }
        
        // 이름 업데이트
        if (enemyName != null) enemyName.text = combatant.Name;
        
        if (enemyHP != null) enemyHP.text = $"HP: {combatant.GetHPStatus()}";
        if (enemyPoise != null) enemyPoise.text = $"Poise: {combatant.GetPoiseStatus()}";
        if (enemyATK != null) enemyATK.text = GetEffectiveAttackText(combatant, false);
        if (enemyDR != null) 
        {
            int effectiveDR = combatant.GetFinalDR();
            if (combatant.tempDRBonus > 0)
            {
                enemyDR.text = $"DR: {effectiveDR} ({combatant.DR} + {combatant.tempDRBonus})";
            }
            else
            {
                enemyDR.text = $"DR: {effectiveDR}";
            }
        }
        if (enemyCrit != null) enemyCrit.text = $"Crit: {combatant.CritChance * 100f:F1}%";
    }

    /// <summary>
    /// 효과적인 공격력 텍스트를 생성합니다
    /// </summary>
    private string GetEffectiveAttackText(Character combatant, bool isPlayer)
    {
        if (CombatManager.Instance == null) return $"ATK: {combatant.ATK}";
        
        ICombatController controller = isPlayer ? CombatManager.Instance.PlayerController : CombatManager.Instance.NonPlayerController;
        if (controller?.Character?.AvailableCommands == null || controller.Character.AvailableCommands.Count == 0)
        {
            int baseAtkOnly = BladeAction.Combat.StatsCalculationManager.Instance != null 
                ? BladeAction.Combat.StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;
            return $"ATK: {baseAtkOnly}";
        }
        
        int selectedIndex = controller.GetSelectedCommandIndex();
        if (selectedIndex < 0 || selectedIndex >= controller.Character.AvailableCommands.Count)
        {
            int baseAtkOnly = BladeAction.Combat.StatsCalculationManager.Instance != null 
                ? BladeAction.Combat.StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;
            return $"ATK: {baseAtkOnly}";
        }
        
        var command = controller.Character.AvailableCommands[selectedIndex];
        
        // 다중 히트 공격의 경우 모든 히트의 damageRatio 표시
        if (command.hitCount > 1)
        {
            int baseAtk = BladeAction.Combat.StatsCalculationManager.Instance != null 
                ? BladeAction.Combat.StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;
            return GenerateMultiHitATKText(baseAtk, controller, selectedIndex);
        }
        else
        {
            // 단일 히트 공격의 경우 기존 방식 사용
            float damageRatio = command.GetDamageRatio(0);
            int baseAtk = BladeAction.Combat.StatsCalculationManager.Instance != null 
                ? BladeAction.Combat.StatsCalculationManager.Instance.GetFinalATK(combatant)
                : combatant.ATK;
            int effectiveATK = Mathf.RoundToInt(baseAtk * damageRatio);
            return $"ATK: {effectiveATK}({baseAtk} * {damageRatio * 100:F0}%)";
        }
    }
    
    /// <summary>
    /// 다중 히트 공격의 ATK 텍스트를 생성합니다
    /// </summary>
    private string GenerateMultiHitATKText(int baseATK, ICombatController controller, int commandIndex)
    {
        if (controller?.Character?.AvailableCommands == null || commandIndex >= controller.Character.AvailableCommands.Count)
        {
            return $"ATK: {baseATK}";
        }
        
        var command = controller.Character.AvailableCommands[commandIndex];
        string result = $"ATK: {baseATK}\n";
        
        for (int i = 0; i < command.hitCount; i++)
        {
            float damageRatio = command.GetDamageRatio(i);
            int effectiveATK = Mathf.RoundToInt(baseATK * damageRatio);
            result += $"  히트 {i + 1}: {effectiveATK} ({damageRatio * 100:F0}%)\n";
        }
        
        return result.TrimEnd('\n');
    }

    /// <summary>
    /// 플레이어 히트 결과를 표시합니다
    /// </summary>
    public void ShowPlayerHitResult(int hitIndex, string msg)
    {
        if (playerHitResultContainer == null || resultLinePrefab == null) return;
        
        var go = Instantiate(resultLinePrefab, playerHitResultContainer);
        go.GetComponent<TextMeshProUGUI>().text = $"히트 {hitIndex + 1}: {msg}";
    }

    /// <summary>
    /// 적 히트 결과를 표시합니다
    /// </summary>
    public void ShowEnemyHitResult(int hitIndex, string msg)
    {
        if (enemyHitResultContainer == null || resultLinePrefab == null) return;
        
        var go = Instantiate(resultLinePrefab, enemyHitResultContainer);
        go.GetComponent<TextMeshProUGUI>().text = $"히트 {hitIndex + 1}: {msg}";
    }

    /// <summary>
    /// 히트 대결 결과를 표시합니다
    /// </summary>
    public void ShowHitVersusResult(int hitIndex, string msg)
    {
        if (turnResultContainer == null || resultLinePrefab == null) return;
        
        Debug.Log($"[CombatDebugDisplay] ShowHitVersusResult 호출됨: 히트 {hitIndex + 1} → {msg}");
        var go = Instantiate(resultLinePrefab, turnResultContainer);
        go.GetComponent<TextMeshProUGUI>().text = $"히트 대결 {hitIndex + 1}: {msg}";
    }

    /// <summary>
    /// 커맨드 시작을 표시합니다
    /// </summary>
    public void ShowCommandStart(bool isPlayer, string name)
    {
        if (isPlayer)
        {
            if (playerActionCommandName != null)
                playerActionCommandName.text = $"[액션 시작] {name}";
        }
        else
        {
            if (enemyActionCommandName != null)
                enemyActionCommandName.text = $"[액션 시작] {name}";
        }
    }

    /// <summary>
    /// 플레이어 액션 커맨드 이름 설정
    /// </summary>
    public void SetPlayerActionCommandName(string commandName)
    {
        if (playerActionCommandName != null)
            playerActionCommandName.text = $"[액션] {commandName}";
    }

    /// <summary>
    /// 적 액션 커맨드 이름 설정
    /// </summary>
    public void SetEnemyActionCommandName(string commandName)
    {
        if (enemyActionCommandName != null)
            enemyActionCommandName.text = $"[액션] {commandName}";
    }

    /// <summary>
    /// 플레이어 액션 입력 쿨다운 설정
    /// </summary>
    public void SetPlayerActionInputCooldown(float cooldown)
    {
        if (playerActionInputCooldown == null) return;
        
        if (cooldown <= 0f)
        {
            playerActionInputCooldown.text = "입력 가능!";
        }
        else
        {
            playerActionInputCooldown.text = $"입력 대기: {cooldown:F2}초";
        }
    }

    /// <summary>
    /// 디버그 결과 표시 초기화
    /// </summary>
    public void ClearDebugResults()
    {
        if (playerHitResultContainer != null)
        {
            foreach (Transform child in playerHitResultContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (enemyHitResultContainer != null)
        {
            foreach (Transform child in enemyHitResultContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (turnResultContainer != null)
        {
            foreach (Transform child in turnResultContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// UI를 강제로 업데이트합니다 (외부에서 호출용)
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdatePlayerStatus();
        UpdateEnemyStatus();
        Debug.Log("[CombatDebugDisplay] UI 강제 업데이트 완료");
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
        CombatCharacterManager.Instance?.PlayerCharacter?.TakeDamage(10);
    }

    [ContextMenu("Test Enemy Take Damage")]
    public void TestEnemyTakeDamage()
    {
        CombatCharacterManager.Instance?.CurrentEnemy?.TakeDamage(10);
    }

    [ContextMenu("Test Player Lose Poise")]
    public void TestPlayerLosePoise()
    {
        CombatCharacterManager.Instance?.PlayerCharacter?.LosePoise(25);
    }

    [ContextMenu("Test Enemy Lose Poise")]
    public void TestEnemyLosePoise()
    {
        CombatCharacterManager.Instance?.CurrentEnemy?.LosePoise(25);
    }

    /// <summary>
    /// 턴 타이머 정보 업데이트 (Debug)
    /// </summary>
    public void UpdateTurnInfo(float remainingTime, float totalTime = 0f)
    {
        if (turnLabel == null) return;
        
        if (totalTime > 0f)
        {
            float elapsedTime = totalTime - remainingTime;
            float progressPercent = (elapsedTime / totalTime) * 100f;
            turnLabel.text = $"턴 타이머: {remainingTime:F2} / {totalTime:F2}초 ({progressPercent:F0}%)";
        }
        else
        {
            turnLabel.text = $"턴: {remainingTime:F2}초";
        }
    }

    /// <summary>
    /// 입력 프롬프트 표시 (Debug)
    /// </summary>
    public void ShowInputPrompt(string message)
    {
        if (inputPromptText != null)
        {
            inputPromptText.text = message;
        }
    }

    /// <summary>
    /// Scene 전환 시 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeFromStatusEvents();
    }
}

