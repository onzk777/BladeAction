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
    
    [Header("Turn Timer Progress Bar")]
    [Tooltip("턴 타이머 진행률 바 (Image 컴포넌트, Type=Filled 권장)")]
    [SerializeField] private Image turnTimerProgressBar;
    
    [Tooltip("턴 타이머 진행률 바 배경 (선택 사항)")]
    [SerializeField] private Image turnTimerProgressBarBackground;
    
    [Header("Perfect Timing Guide")]
    [Tooltip("Perfect Timing 가이드 Prefab")]
    [SerializeField] private GameObject perfectTimingGuidePrefab;
    
    [Tooltip("가이드를 배치할 부모 Transform (turnTimerProgressBar의 부모가 적절)")]
    [SerializeField] private RectTransform guideContainer;
    
    // 생성된 가이드들을 추적
    private System.Collections.Generic.List<PerfectTimingGuide> activeGuides = new System.Collections.Generic.List<PerfectTimingGuide>();

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
        while (CharacterManager.Instance.PlayerCombatant == null || CharacterManager.Instance.EnemyCombatant == null)
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
        if (CharacterManager.Instance.PlayerCombatant != null)
        {
            CharacterManager.Instance.PlayerCombatant.OnStatsChanged += OnPlayerStatsChanged;
        }

        // 적 스테이터스 이벤트 구독
        if (CharacterManager.Instance.EnemyCombatant != null)
        {
            CharacterManager.Instance.EnemyCombatant.OnStatsChanged += OnEnemyStatsChanged;
        }
    }

    private void OnPlayerStatsChanged(Combatant combatant)
    {
        UpdatePlayerStatus();
    }

    private void OnEnemyStatsChanged(Combatant combatant)
    {
        UpdateEnemyStatus();
    }

    private void UpdatePlayerStatus()
    {
        if (CharacterManager.Instance?.PlayerCombatant == null) return;

        var combatant = CharacterManager.Instance.PlayerCombatant;
        if (playerHP != null) playerHP.text = $"HP: {combatant.GetHPStatus()}";
        if (playerPoise != null) playerPoise.text = $"Poise: {combatant.GetPoiseStatus()}";
        if (playerATK != null) playerATK.text = GetEffectiveAttackText(combatant, true);
        if (playerDR != null) 
        {
            int effectiveDR = combatant.GetEffectiveDR();
            if (combatant.tempDRBonus > 0)
            {
                playerDR.text = $"DR: {effectiveDR} ({combatant.DR} + {combatant.tempDRBonus})";
            }
            else
            {
                playerDR.text = $"DR: {effectiveDR}";
            }
        }
        if (playerCrit != null) playerCrit.text = $"Crit: {combatant.Crit}%";
    }

    private void UpdateEnemyStatus()
    {
        if (CharacterManager.Instance?.EnemyCombatant == null) return;

        var combatant = CharacterManager.Instance.EnemyCombatant;
        if (enemyHP != null) enemyHP.text = $"HP: {combatant.GetHPStatus()}";
        if (enemyPoise != null) enemyPoise.text = $"Poise: {combatant.GetPoiseStatus()}";
        if (enemyATK != null) enemyATK.text = GetEffectiveAttackText(combatant, false);
        if (enemyDR != null) 
        {
            int effectiveDR = combatant.GetEffectiveDR();
            if (combatant.tempDRBonus > 0)
            {
                enemyDR.text = $"DR: {effectiveDR} ({combatant.DR} + {combatant.tempDRBonus})";
            }
            else
            {
                enemyDR.text = $"DR: {effectiveDR}";
            }
        }
        if (enemyCrit != null) enemyCrit.text = $"Crit: {combatant.Crit}%";
    }

    private string GetEffectiveAttackText(Combatant combatant, bool isPlayer)
    {
        if (CombatManager.Instance == null) return $"ATK: {combatant.ATK}";
        
        ICombatController controller = isPlayer ? CombatManager.Instance.PlayerController : CombatManager.Instance.EnemyController;
        if (controller?.Combatant?.AvailableCommands == null || controller.Combatant.AvailableCommands.Count == 0)
        {
            return $"ATK: {combatant.ATK}";
        }
        
        int selectedIndex = controller.GetSelectedCommandIndex();
        if (selectedIndex < 0 || selectedIndex >= controller.Combatant.AvailableCommands.Count)
        {
            return $"ATK: {combatant.ATK}";
        }
        
        var command = controller.Combatant.AvailableCommands[selectedIndex];
        
        // 다중 히트 공격의 경우 모든 히트의 damageRatio 표시
        if (command.hitCount > 1)
        {
            return GenerateMultiHitATKText(combatant.ATK, controller, selectedIndex);
        }
        else
        {
            // 단일 히트 공격의 경우 기존 방식 사용
            float damageRatio = command.GetDamageRatio(0);
            int effectiveATK = Mathf.RoundToInt(combatant.ATK * damageRatio);
            return $"ATK: {effectiveATK}({combatant.ATK} * {damageRatio * 100:F0}%)";
        }
    }
    
    /// <summary>
    /// 다중 히트 공격의 ATK 텍스트를 생성합니다
    /// </summary>
    private string GenerateMultiHitATKText(int baseATK, ICombatController controller, int commandIndex)
    {
        if (controller?.Combatant?.AvailableCommands == null || commandIndex >= controller.Combatant.AvailableCommands.Count)
        {
            return $"ATK: {baseATK}";
        }
        
        var command = controller.Combatant.AvailableCommands[commandIndex];
        string result = $"ATK: {baseATK}\n";
        
        for (int i = 0; i < command.hitCount; i++)
        {
            float damageRatio = command.GetDamageRatio(i);
            int effectiveATK = Mathf.RoundToInt(baseATK * damageRatio);
            result += $"  히트 {i + 1}: {effectiveATK} ({damageRatio * 100:F0}%)\n";
        }
        
        return result.TrimEnd('\n');
    }

    [ContextMenu("Force Update Status")]
    public void ForceUpdateStatus()
    {
        UpdatePlayerStatus();
        UpdateEnemyStatus();
    }
    
    /// <summary>
    /// UI를 강제로 업데이트합니다 (외부에서 호출용)
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdatePlayerStatus();
        UpdateEnemyStatus();
        Debug.Log("[CombatStatusDisplay] UI 강제 업데이트 완료");
    }

    [ContextMenu("Test Player Take Damage")]
    public void TestPlayerTakeDamage()
    {
        CharacterManager.Instance?.PlayerCombatant?.TakeDamage(10);
    }

    [ContextMenu("Test Enemy Take Damage")]
    public void TestEnemyTakeDamage()
    {
        CharacterManager.Instance?.EnemyCombatant?.TakeDamage(10);
    }

    [ContextMenu("Test Player Lose Poise")]
    public void TestPlayerLosePoise()
    {
        CharacterManager.Instance?.PlayerCombatant?.LosePoise(25);
    }

    [ContextMenu("Test Enemy Lose Poise")]
    public void TestEnemyLosePoise()
    {
        CharacterManager.Instance?.EnemyCombatant?.LosePoise(25);
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
    /// <summary>
    /// 턴 정보를 업데이트합니다.
    /// </summary>
    /// <param name="remainingTime">잔여 시간</param>
    /// <param name="totalTime">전체 턴 시간 (옵션)</param>
    public void updateTurnInfo(float remainingTime, float totalTime = 0f)
    {
        if (totalTime > 0f)
        {
            // 전체 시간이 제공된 경우: 잔여/전체 + 진행률 표시
            float elapsedTime = totalTime - remainingTime;
            float progressPercent = (elapsedTime / totalTime) * 100f;
            float progressNormalized = Mathf.Clamp01(elapsedTime / totalTime); // 0~1 범위
            
            // 텍스트 업데이트
            if (turnLabel != null)
            {
                turnLabel.text = $"턴 타이머: {remainingTime:F2} / {totalTime:F2}초 ({progressPercent:F0}%)";
            }
            
            // 프로그레스 바 업데이트
            UpdateProgressBar(progressNormalized);
        }
        else
        {
            // 하위 호환성: 잔여 시간만 표시
            if (turnLabel != null)
            {
                turnLabel.text = $"턴: {remainingTime:F2}초";
            }
            
            // 프로그레스 바는 업데이트하지 않음 (또는 0으로 설정)
            UpdateProgressBar(0f);
        }
    }
    
    /// <summary>
    /// 프로그레스 바를 업데이트합니다.
    /// </summary>
    /// <param name="normalizedProgress">진행률 (0~1)</param>
    private void UpdateProgressBar(float normalizedProgress)
    {
        if (turnTimerProgressBar == null) return;
        
        // Image의 타입에 따라 다르게 처리
        // Type=Filled인 경우: fillAmount 사용 (권장)
        if (turnTimerProgressBar.type == Image.Type.Filled)
        {
            turnTimerProgressBar.fillAmount = normalizedProgress;
        }
        // Type=Simple 등 다른 타입: Scale 조정 (0~1)
        else
        {
            RectTransform rectTransform = turnTimerProgressBar.rectTransform;
            if (rectTransform != null)
            {
                // Scale의 x값만 변경 (y, z는 유지)
                // normalizedProgress: 0 = 0%, 1 = 100%
                Vector3 scale = rectTransform.localScale;
                scale.x = normalizedProgress;
                rectTransform.localScale = scale;
            }
        }
        
        // 색상 변경 (선택 사항): 진행률에 따라 색상 그라데이션
        // UpdateProgressBarColor(normalizedProgress);
    }
    
    /// <summary>
    /// 진행률에 따라 프로그레스 바 색상을 변경합니다. (선택 사항)
    /// </summary>
    /// <param name="normalizedProgress">진행률 (0~1)</param>
    private void UpdateProgressBarColor(float normalizedProgress)
    {
        if (turnTimerProgressBar == null) return;
        
        // 0%: 초록색, 50%: 노란색, 100%: 빨간색
        Color barColor;
        if (normalizedProgress < 0.5f)
        {
            // 초록 → 노랑 (0 ~ 0.5)
            barColor = Color.Lerp(Color.green, Color.yellow, normalizedProgress * 2f);
        }
        else
        {
            // 노랑 → 빨강 (0.5 ~ 1.0)
            barColor = Color.Lerp(Color.yellow, Color.red, (normalizedProgress - 0.5f) * 2f);
        }
        
        turnTimerProgressBar.color = barColor;
    }
    public void SetPlayerActionCommandName(string commandName)
        => playerActionCommandName.text = $"[액션] {commandName}";

    public void SetEnemyActionCommandName(string commandName)
        => enemyActionCommandName.text = $"[액션] {commandName}";

    public void SetPlayerActionInputCooldown(float cooldown)
    {
        if (playerActionInputCooldown == null) return;
        
        if(cooldown <= 0f)
        {
            playerActionInputCooldown.text = "입력 가능!";            
        }
        else
        {
            playerActionInputCooldown.text = $"입력 대기: {cooldown.ToString("F2")}초";
        }
    }

    public void ShowBattleEndResult(string winnerName, string resultMessage)
    {
        if (actionProgress != null)
        {
            actionProgress.text = $"전투 종료! {winnerName} {resultMessage}";
        }
        if (inputPromptText != null)
        {
            inputPromptText.text = "Restart 버튼을 눌러 다시 시작하세요";
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
        
        // Perfect Timing 가이드도 함께 제거
        ClearPerfectTimingGuides();
    }

    /// <summary>
    /// Perfect Timing 가이드들을 생성합니다
    /// </summary>
    /// <param name="actionData">현재 공격자가 사용한 검술 데이터</param>
    /// <param name="totalTurnTime">전체 턴 시간</param>
    public void ShowPerfectTimingGuides(ActionCommandData actionData, float totalTurnTime)
    {
        // 이전 가이드들 제거
        ClearPerfectTimingGuides();
        
        if (actionData == null || actionData.perfectTimings == null || actionData.perfectTimings.Count == 0)
        {
            Debug.Log("[CombatStatusDisplay] Perfect Timing 데이터가 없습니다.");
            return;
        }
        
        if (perfectTimingGuidePrefab == null)
        {
            Debug.LogWarning("[CombatStatusDisplay] perfectTimingGuidePrefab이 할당되지 않았습니다!");
            return;
        }
        
        if (guideContainer == null)
        {
            Debug.LogWarning("[CombatStatusDisplay] guideContainer가 할당되지 않았습니다!");
            return;
        }
        
        if (totalTurnTime <= 0f)
        {
            Debug.LogWarning("[CombatStatusDisplay] totalTurnTime이 유효하지 않습니다!");
            return;
        }
        
        // 게이지 바의 실제 픽셀 width 가져오기
        float gaugeWidth = GetGaugeWidth();
        if (gaugeWidth <= 0f)
        {
            Debug.LogWarning("[CombatStatusDisplay] 게이지 바의 width를 가져올 수 없습니다!");
            return;
        }
        
        // 각 Hit의 Perfect Timing에 대해 가이드 생성
        for (int i = 0; i < actionData.perfectTimings.Count; i++)
        {
            PerfectTimingWindow timing = actionData.perfectTimings[i];
            CreatePerfectTimingGuide(timing, totalTurnTime, gaugeWidth, i);
        }
        
        Debug.Log($"[CombatStatusDisplay] {actionData.perfectTimings.Count}개의 Perfect Timing 가이드 생성 완료");
    }
    
    /// <summary>
    /// 개별 Perfect Timing 가이드를 생성합니다
    /// </summary>
    private void CreatePerfectTimingGuide(PerfectTimingWindow timing, float totalTurnTime, float gaugeWidth, int hitIndex)
    {
        // Prefab 인스턴스화
        GameObject guideObj = Instantiate(perfectTimingGuidePrefab, guideContainer);
        PerfectTimingGuide guide = guideObj.GetComponent<PerfectTimingGuide>();
        
        if (guide == null)
        {
            Debug.LogError("[CombatStatusDisplay] Prefab에 PerfectTimingGuide 컴포넌트가 없습니다!");
            Destroy(guideObj);
            return;
        }
        
        // Start 시간 기준으로 상대적 위치 계산 (Debug.Log에서도 사용하기 위해 밖에서 선언)
        float startRatio = timing.start / totalTurnTime;
        float startPositionX = gaugeWidth * startRatio;
        
        // RectTransform 설정 (Anchor를 Left로)
        RectTransform guideRect = guideObj.GetComponent<RectTransform>();
        if (guideRect != null)
        {
            // Anchor를 Left-Center로 설정
            guideRect.anchorMin = new Vector2(0f, 0.5f);
            guideRect.anchorMax = new Vector2(0f, 0.5f);
            guideRect.pivot = new Vector2(0f, 0.5f);
            
            // 위치 설정
            guideRect.anchoredPosition = new Vector2(startPositionX, 0f);
        }
        
        // Perfect 구간의 width 계산
        float durationRatio = timing.duration / totalTurnTime;
        float guideWidth = gaugeWidth * durationRatio;
        
        // 가이드 width 설정 (색상, 크기 등은 Prefab에서 설정된 값 사용)
        guide.SetGuideWidth(guideWidth);
        
        // 리스트에 추가
        activeGuides.Add(guide);
        
        Debug.Log($"[CombatStatusDisplay] Hit {hitIndex + 1} 가이드 생성: Start={timing.start:F3}초, Duration={timing.duration:F3}초, X={startPositionX:F1}px, Width={guideWidth:F1}px");
    }
    
    /// <summary>
    /// 게이지 바의 실제 픽셀 width를 반환합니다
    /// </summary>
    private float GetGaugeWidth()
    {
        if (turnTimerProgressBar == null) return 0f;
        
        RectTransform gaugeRect = turnTimerProgressBar.rectTransform;
        if (gaugeRect == null) return 0f;
        
        return gaugeRect.rect.width;
    }
    
    /// <summary>
    /// 모든 Perfect Timing 가이드를 제거합니다
    /// </summary>
    public void ClearPerfectTimingGuides()
    {
        foreach (var guide in activeGuides)
        {
            if (guide != null)
            {
                guide.Cleanup();
            }
        }
        activeGuides.Clear();
    }
    
    /// <summary>
    /// 특정 Hit의 가이드를 완료 상태로 전환합니다 (완벽 입력 성공 시)
    /// </summary>
    /// <param name="hitIndex">Hit 인덱스 (0부터 시작)</param>
    public void MarkGuideAsCompleted(int hitIndex)
    {
        if (activeGuides == null || hitIndex < 0 || hitIndex >= activeGuides.Count)
        {
            Debug.LogWarning($"[CombatStatusDisplay] 유효하지 않은 hitIndex: {hitIndex} (activeGuides.Count: {activeGuides?.Count ?? 0})");
            return;
        }
        
        PerfectTimingGuide guide = activeGuides[hitIndex];
        if (guide != null)
        {
            guide.MarkAsCompleted();
            Debug.Log($"[CombatStatusDisplay] Hit {hitIndex + 1} 가이드 완료 상태로 전환");
        }
        else
        {
            Debug.LogWarning($"[CombatStatusDisplay] Hit {hitIndex + 1} 가이드가 null입니다");
        }
    }

    /// <summary>
    /// Scene 전환 시 이벤트 구독 해제 및 리소스 정리
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (CharacterManager.Instance?.PlayerCombatant != null)
        {
            CharacterManager.Instance.PlayerCombatant.OnStatsChanged -= OnPlayerStatsChanged;
        }

        if (CharacterManager.Instance?.EnemyCombatant != null)
        {
            CharacterManager.Instance.EnemyCombatant.OnStatsChanged -= OnEnemyStatsChanged;
        }

        // 가이드 정리
        ClearPerfectTimingGuides();
    }

}
