using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using BladeAction.BT;

/// <summary>
/// BT 디버그 패널 - BTMonitorUI의 확장판
/// 히스토리, 상세 로그, 필터링, 제어 기능 포함
/// </summary>
public class BTDebugPanel : MonoBehaviour
{
    [Header("UI 텍스트 참조")]
    [SerializeField] private TextMeshProUGUI summaryText;      // 요약 정보
    [SerializeField] private TextMeshProUGUI historyText;      // 실행 히스토리
    [SerializeField] private TextMeshProUGUI detailText;       // 상세 로그 (선택된 항목)
    
    [Header("컨트롤 버튼")]
    [SerializeField] private Button clearHistoryButton;        // 히스토리 클리어
    [SerializeField] private Button pauseLoggingButton;        // 로그 일시정지
    [SerializeField] private Button exportButton;              // 로그 내보내기
    
    [Header("필터 토글")]
    [SerializeField] private Toggle showEnemyToggle;           // Enemy 로그 표시
    [SerializeField] private Toggle showPlayerToggle;          // Player 로그 표시
    [SerializeField] private Toggle showMatchedOnlyToggle;     // 매칭된 로그만
    
    [Header("설정")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private int maxHistoryDisplay = 10;
    [SerializeField] private bool verboseMode = false;         // 상세 모드
    
    private float lastUpdateTime = 0f;
    private bool isPaused = false;
    private int selectedLogIndex = -1;
    
    // 필터 설정
    private bool filterEnemy = true;
    private bool filterPlayer = true;
    private bool filterMatchedOnly = false;
    
    private void Start()
    {
        // 버튼 이벤트 연결
        if (clearHistoryButton != null)
            clearHistoryButton.onClick.AddListener(OnClearHistory);
        
        if (pauseLoggingButton != null)
            pauseLoggingButton.onClick.AddListener(OnTogglePause);
        
        if (exportButton != null)
            exportButton.onClick.AddListener(OnExportLogs);
        
        // 토글 이벤트 연결
        if (showEnemyToggle != null)
            showEnemyToggle.onValueChanged.AddListener(OnEnemyFilterChanged);
        
        if (showPlayerToggle != null)
            showPlayerToggle.onValueChanged.AddListener(OnPlayerFilterChanged);
        
        if (showMatchedOnlyToggle != null)
            showMatchedOnlyToggle.onValueChanged.AddListener(OnMatchedFilterChanged);
        
        // 초기 상태 설정
        UpdatePauseButtonText();
    }
    
    private void Update()
    {
        if (isPaused) return;
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateAllPanels();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// 모든 패널 업데이트
    /// </summary>
    private void UpdateAllPanels()
    {
        UpdateSummary();
        UpdateHistory();
        if (selectedLogIndex >= 0)
            UpdateDetail();
    }
    
    /// <summary>
    /// 요약 정보 업데이트
    /// </summary>
    private void UpdateSummary()
    {
        if (summaryText == null) return;
        
        var combatManager = CombatManager.Instance;
        var history = BTLogHistory.Instance;
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#0066CC>[=== BT 디버그 패널 ===]</color>");
        
        if (combatManager != null)
        {
            sb.AppendLine($"<color=#0066CC>|</color> 턴: {combatManager.CurrentTurnNumber}");
            sb.AppendLine($"<color=#0066CC>|</color> 공격자: {(combatManager.IsPlayerAttackTurn ? "<color=#008800>Player</color>" : "<color=#CC0000>Enemy</color>")}");
        }
        
        sb.AppendLine($"<color=#0066CC>|</color> --- 로그 상태 ---");
        sb.AppendLine($"<color=#0066CC>|</color> 총 기록: {history.EvaluationLogs.Count}개");
        sb.AppendLine($"<color=#0066CC>|</color> 표시 중: {GetFilteredLogs().Count}개");
        sb.AppendLine($"<color=#0066CC>|</color> 로깅: {(isPaused ? "<color=#CC0000>일시정지</color>" : "<color=#008800>활성</color>")}");
        sb.AppendLine($"<color=#0066CC>|</color> 상세 모드: {(verboseMode ? "ON" : "OFF")}");
        
        sb.AppendLine($"<color=#0066CC>[=======================]</color>");
        
        summaryText.text = sb.ToString();
    }
    
    /// <summary>
    /// 실행 히스토리 업데이트
    /// </summary>
    private void UpdateHistory()
    {
        if (historyText == null) return;
        
        var filteredLogs = GetFilteredLogs();
        
        if (filteredLogs.Count == 0)
        {
            historyText.text = "<color=#666666>필터링된 로그가 없습니다</color>";
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#0066CC>[=== 실행 히스토리 (최근 {Mathf.Min(maxHistoryDisplay, filteredLogs.Count)}개) ===]</color>");
        
        int startIdx = Mathf.Max(0, filteredLogs.Count - maxHistoryDisplay);
        
        for (int i = filteredLogs.Count - 1; i >= startIdx; i--)
        {
            var log = filteredLogs[i];
            
            // 선택 표시
            string selectMarker = (i == selectedLogIndex) ? "<color=#CC8800>></color> " : "  ";
            
            // 헤더
            string turnTypeIcon = log.isAttackTurn ? "[ATK]" : "[DEF]";
            string matchIcon = log.foundMatch ? "<color=#008800>O</color>" : "<color=#CC0000>X</color>";
            
            sb.AppendLine($"<color=#0066CC>|</color> {selectMarker}{turnTypeIcon} T{log.turnNumber} | {log.combatantName} | {matchIcon}");
            
            // Entry 정보
            if (log.foundMatch && !string.IsNullOrEmpty(log.matchedEntryDescription))
            {
                sb.AppendLine($"<color=#0066CC>|</color>    <color=#CC8800>{log.matchedEntryDescription}</color>");
            }
            
            // 간략 통계
            int conditionPass = 0;
            foreach (var c in log.conditions)
                if (c.result) conditionPass++;
            
            int actionExecuted = 0;
            foreach (var a in log.actions)
                if (!a.skipped) actionExecuted++;
            
            if (verboseMode)
            {
                sb.AppendLine($"<color=#0066CC>|</color>    조건: {conditionPass}/{log.conditions.Count} | 액션: {actionExecuted}/{log.actions.Count}");
            }
            
            // 확률 요약
            if (log.probabilityOverrides.Count > 0 && verboseMode)
            {
                sb.Append($"<color=#0066CC>|</color>    확률: ");
                int count = 0;
                foreach (var kvp in log.probabilityOverrides)
                {
                    if (count > 0) sb.Append(", ");
                    sb.Append($"<color=#CC6600>{GetShortName(kvp.Key)}={kvp.Value:P0}</color>");
                    count++;
                    if (count >= 2) break; // 최대 2개만 표시
                }
                if (log.probabilityOverrides.Count > 2)
                    sb.Append($" +{log.probabilityOverrides.Count - 2}");
                sb.AppendLine();
            }
            
            // 구분선
            if (i > startIdx)
            {
                sb.AppendLine($"<color=#0066CC>|</color> ---------------------");
            }
        }
        
        sb.AppendLine($"<color=#0066CC>[================================]</color>");
        sb.AppendLine("\n<color=#666666>클릭으로 선택 -> 상세 보기</color>");
        
        historyText.text = sb.ToString();
    }
    
    /// <summary>
    /// 상세 로그 업데이트 (선택된 항목)
    /// </summary>
    private void UpdateDetail()
    {
        if (detailText == null || selectedLogIndex < 0) return;
        
        var filteredLogs = GetFilteredLogs();
        if (selectedLogIndex >= filteredLogs.Count)
        {
            selectedLogIndex = -1;
            detailText.text = "";
            return;
        }
        
        var log = filteredLogs[selectedLogIndex];
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#CC8800>[=== 상세 로그: 턴 {log.turnNumber} | {log.combatantName} ===]</color>");
        sb.AppendLine($"<color=#CC8800>|</color> BT: {log.treeName}");
        sb.AppendLine($"<color=#CC8800>|</color> 타입: {(log.isAttackTurn ? "공격 턴" : "방어 턴")}");
        sb.AppendLine($"<color=#CC8800>|</color> 결과: {(log.foundMatch ? "O 매칭 성공" : "X 매칭 실패")}");
        
        if (log.foundMatch)
        {
            sb.AppendLine($"<color=#CC8800>|</color> Entry[{log.matchedEntryIndex}]: {log.matchedEntryDescription}");
        }
        
        // 조건 상세
        if (log.conditions.Count > 0)
        {
            sb.AppendLine($"<color=#CC8800>|</color> --- 조건 평가 ---");
            foreach (var cond in log.conditions)
            {
                string icon = cond.result ? "<color=#008800>O</color>" : "<color=#CC0000>X</color>";
                string invertMark = cond.inverted ? " <color=#CC6600>[반전]</color>" : "";
                sb.AppendLine($"<color=#CC8800>|</color>  {icon} {cond.conditionName}{invertMark}");
                
                if (!string.IsNullOrEmpty(cond.details))
                {
                    sb.AppendLine($"<color=#CC8800>|</color>     - {cond.details}");
                }
            }
        }
        
        // 액션 상세
        if (log.actions.Count > 0)
        {
            sb.AppendLine($"<color=#CC8800>|</color> --- 액션 실행 ---");
            foreach (var act in log.actions)
            {
                if (act.skipped)
                {
                    sb.AppendLine($"<color=#CC8800>|</color>  <color=#666666>X {act.actionName}</color>");
                    sb.AppendLine($"<color=#CC8800>|</color>     - 건너뜀: {act.skipReason}");
                }
                else
                {
                    sb.AppendLine($"<color=#CC8800>|</color>  <color=#CC0088>*</color> {act.actionName} <color=#666666>[P:{act.priority}]</color>");
                    
                    if (!string.IsNullOrEmpty(act.details))
                    {
                        sb.AppendLine($"<color=#CC8800>|</color>     - {act.details}");
                    }
                }
            }
        }
        
        // 확률 변경 상세
        if (log.probabilityOverrides.Count > 0)
        {
            sb.AppendLine($"<color=#CC8800>|</color> --- 확률 변경 ---");
            foreach (var kvp in log.probabilityOverrides)
            {
                sb.AppendLine($"<color=#CC8800>|</color>  + {kvp.Key}: <color=#CC6600>{kvp.Value:P0}</color>");
            }
        }
        
        // 검술 선택
        if (log.selectedCommandIndex.HasValue)
        {
            sb.AppendLine($"<color=#CC8800>|</color> --- 검술 선택 ---");
            sb.AppendLine($"<color=#CC8800>|</color>  인덱스: {log.selectedCommandIndex.Value}");
        }
        else if (!string.IsNullOrEmpty(log.selectedCommandTag))
        {
            sb.AppendLine($"<color=#CC8800>|</color> --- 검술 선택 ---");
            sb.AppendLine($"<color=#CC8800>|</color>  태그: {log.selectedCommandTag}");
        }
        
        sb.AppendLine($"<color=#CC8800>|</color> ---------------");
        sb.AppendLine($"<color=#CC8800>|</color> 시각: {log.timestamp:HH:mm:ss}");
        sb.AppendLine($"<color=#CC8800>[================================]</color>");
        
        detailText.text = sb.ToString();
    }
    
    /// <summary>
    /// 필터링된 로그 가져오기
    /// </summary>
    private System.Collections.Generic.List<BTLogHistory.BTEvaluationLog> GetFilteredLogs()
    {
        var allLogs = BTLogHistory.Instance.EvaluationLogs;
        var filtered = new System.Collections.Generic.List<BTLogHistory.BTEvaluationLog>();
        
        foreach (var log in allLogs)
        {
            // Combatant 필터
            bool isEnemy = !log.combatantName.Contains("Player");
            if (isEnemy && !filterEnemy) continue;
            if (!isEnemy && !filterPlayer) continue;
            
            // 매칭 필터
            if (filterMatchedOnly && !log.foundMatch) continue;
            
            filtered.Add(log);
        }
        
        return filtered;
    }
    
    /// <summary>
    /// 확률 이름 축약
    /// </summary>
    private string GetShortName(string fullName)
    {
        switch (fullName)
        {
            case "AttackPerfectRate": return "공격";
            case "ParryPerfectRate": return "쳐내기";
            case "GuardAttemptRate": return "막기";
            case "ParryWhileGuardingRate": return "막중쳐";
            case "DoParryWhileGuarding": return "쳐내기시도";
            default: return fullName;
        }
    }
    
    // ========================================
    // 버튼 이벤트 핸들러
    // ========================================
    
    private void OnClearHistory()
    {
        BTLogHistory.Instance.Clear();
        selectedLogIndex = -1;
        
        if (detailText != null)
            detailText.text = "<color=#666666>히스토리가 클리어되었습니다</color>";
        
        Debug.Log("[BTDebugPanel] 히스토리 클리어됨");
    }
    
    private void OnTogglePause()
    {
        isPaused = !isPaused;
        UpdatePauseButtonText();
        
        Debug.Log($"[BTDebugPanel] 로깅 {(isPaused ? "일시정지" : "재개")}");
    }
    
    private void OnExportLogs()
    {
        string filename = $"BTLogs_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        
        try
        {
            var logs = BTLogHistory.Instance.EvaluationLogs;
            StringBuilder export = new StringBuilder();
            
            export.AppendLine("=== BT 로그 내보내기 ===");
            export.AppendLine($"생성 시각: {System.DateTime.Now}");
            export.AppendLine($"총 로그 수: {logs.Count}");
            export.AppendLine();
            
            foreach (var log in logs)
            {
                export.AppendLine($"--- 턴 {log.turnNumber} | {log.combatantName} | {(log.isAttackTurn ? "공격" : "방어")} ---");
                export.AppendLine($"BT: {log.treeName}");
                export.AppendLine($"결과: {(log.foundMatch ? "매칭 성공" : "매칭 실패")}");
                
                if (log.foundMatch)
                    export.AppendLine($"Entry[{log.matchedEntryIndex}]: {log.matchedEntryDescription}");
                
                export.AppendLine($"조건: {log.conditions.Count}개");
                export.AppendLine($"액션: {log.actions.Count}개");
                export.AppendLine($"확률 변경: {log.probabilityOverrides.Count}개");
                export.AppendLine($"시각: {log.timestamp:yyyy-MM-dd HH:mm:ss}");
                export.AppendLine();
            }
            
            System.IO.File.WriteAllText(path, export.ToString());
            Debug.Log($"[BTDebugPanel] 로그 내보내기 완료: {path}");
            
            // UI에 알림 (옵션)
            if (summaryText != null)
            {
                string originalText = summaryText.text;
                summaryText.text = $"<color=#008800>✓ 로그 저장됨:\n{filename}</color>";
                StartCoroutine(RestoreTextAfterDelay(summaryText, originalText, 3f));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BTDebugPanel] 로그 내보내기 실패: {e.Message}");
        }
    }
    
    private System.Collections.IEnumerator RestoreTextAfterDelay(TextMeshProUGUI text, string originalText, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (text != null)
            text.text = originalText;
    }
    
    private void UpdatePauseButtonText()
    {
        if (pauseLoggingButton != null)
        {
            var buttonText = pauseLoggingButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isPaused ? "▶ 재개" : "⏸ 일시정지";
            }
        }
    }
    
    // ========================================
    // 필터 이벤트 핸들러
    // ========================================
    
    private void OnEnemyFilterChanged(bool value)
    {
        filterEnemy = value;
        Debug.Log($"[BTDebugPanel] Enemy 필터: {(value ? "ON" : "OFF")}");
    }
    
    private void OnPlayerFilterChanged(bool value)
    {
        filterPlayer = value;
        Debug.Log($"[BTDebugPanel] Player 필터: {(value ? "ON" : "OFF")}");
    }
    
    private void OnMatchedFilterChanged(bool value)
    {
        filterMatchedOnly = value;
        Debug.Log($"[BTDebugPanel] 매칭만 표시: {(value ? "ON" : "OFF")}");
    }
    
    // ========================================
    // Public 메서드
    // ========================================
    
    /// <summary>
    /// 로그 선택 (외부에서 호출 가능, 예: UI 클릭)
    /// </summary>
    public void SelectLog(int index)
    {
        selectedLogIndex = index;
        UpdateDetail();
    }
    
    /// <summary>
    /// 상세 모드 토글
    /// </summary>
    public void ToggleVerboseMode()
    {
        verboseMode = !verboseMode;
        Debug.Log($"[BTDebugPanel] 상세 모드: {(verboseMode ? "ON" : "OFF")}");
    }
    
    /// <summary>
    /// 강제 업데이트
    /// </summary>
    public void ForceUpdate()
    {
        UpdateAllPanels();
    }
}

