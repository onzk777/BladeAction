using UnityEngine;
using TMPro;
using System.Text;
using BladeAction.BT;

/// <summary>
/// Behavior Tree 실행 상태를 실시간으로 모니터링하는 UI
/// 디버그 패널 내부에 포함되어 사용됩니다.
/// </summary>
public class BTMonitorUI : MonoBehaviour
{
    [Header("UI 텍스트 참조")]
    [SerializeField] private TextMeshProUGUI enemyBTStatusText;
    [SerializeField] private TextMeshProUGUI playerBTStatusText;
    [SerializeField] private TextMeshProUGUI generalInfoText;
    [SerializeField] private TextMeshProUGUI historyText; // BT 실행 히스토리
    
    [Header("업데이트 설정")]
    [SerializeField] private float updateInterval = 0.5f; // 초 단위
    [SerializeField] private int maxHistoryDisplay = 10; // 표시할 최대 히스토리 수
    
    private float lastUpdateTime = 0f;
    
    private void Update()
    {
        // 일정 간격으로 업데이트 (성능 최적화)
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMonitorDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// 모니터 디스플레이 업데이트
    /// </summary>
    private void UpdateMonitorDisplay()
    {
        UpdateGeneralInfo();
        UpdateEnemyBTStatus();
        UpdatePlayerBTStatus();
        UpdateHistory();
    }
    
    /// <summary>
    /// 일반 정보 업데이트
    /// </summary>
    private void UpdateGeneralInfo()
    {
        if (generalInfoText == null) return;
        
        var combatManager = CombatManager.Instance;
        if (combatManager == null)
        {
            generalInfoText.text = "<color=#FF6B6B>전투 중이 아님</color>";
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#00BFFF>╔═══ BT 모니터 ═══╗</color>");
        sb.AppendLine($"<color=#00BFFF>║</color> 턴: {combatManager.CurrentTurnNumber}");
        sb.AppendLine($"<color=#00BFFF>║</color> 공격자: {(combatManager.IsPlayerAttackTurn ? "<color=#00FF00>플레이어</color>" : "<color=#FF6B6B>Enemy</color>")}");
        sb.AppendLine($"<color=#00BFFF>╚═════════════════╝</color>");
        
        generalInfoText.text = sb.ToString();
    }
    
    /// <summary>
    /// Enemy BT 상태 업데이트
    /// </summary>
    private void UpdateEnemyBTStatus()
    {
        if (enemyBTStatusText == null) return;
        
        var characterManager = CharacterManager.Instance;
        if (characterManager == null || characterManager.EnemyCharacter == null)
        {
            enemyBTStatusText.text = "<color=#888888>Enemy 없음</color>";
            return;
        }
        
        var enemy = characterManager.EnemyCharacter;
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"<color=#FF6B6B>╔═══ Enemy: {enemy.Name} ═══╗</color>");
        
        // BT 설정 상태
        int btCount = enemy.CharacterData?.behaviorTrees?.Count ?? 0;
        sb.AppendLine($"<color=#FF6B6B>║</color> BT 수: {btCount}개");
        
        // 런타임 확률 표시
        if (enemy.RuntimeProbabilities != null)
        {
            sb.AppendLine($"<color=#FF6B6B>║</color> ━━━ 확률 상태 ━━━");
            sb.AppendLine($"<color=#FF6B6B>║</color> 공격 성공률: <color=#FFA500>{enemy.RuntimeProbabilities.AttackPerfectRate:P0}</color>");
            sb.AppendLine($"<color=#FF6B6B>║</color> 쳐내기 성공률: <color=#FFA500>{enemy.RuntimeProbabilities.ParryPerfectRate:P0}</color>");
            sb.AppendLine($"<color=#FF6B6B>║</color> 막기 시도율: <color=#FFA500>{enemy.RuntimeProbabilities.GuardAttemptRate:P0}</color>");
            sb.AppendLine($"<color=#FF6B6B>║</color> 막기중 쳐내기: <color=#FFA500>{(enemy.RuntimeProbabilities.ParryWhileGuarding ? "O" : "X")}</color>");
            sb.AppendLine($"<color=#FF6B6B>║</color> 막기중 성공률: <color=#FFA500>{enemy.RuntimeProbabilities.ParryWhileGuardingRate:P0}</color>");
        }
        
        // BT Override 상태
        if (enemy.CurrentBTContext != null && enemy.CurrentBTContext.probabilityOverrides != null)
        {
            int overrideCount = enemy.CurrentBTContext.probabilityOverrides.Count;
            if (overrideCount > 0)
            {
                sb.AppendLine($"<color=#FF6B6B>║</color> ━━━ BT Override ━━━");
                sb.AppendLine($"<color=#FF6B6B>║</color> <color=#FFD700>활성화 ({overrideCount}개)</color>");
                
                foreach (var kvp in enemy.CurrentBTContext.probabilityOverrides)
                {
                    sb.AppendLine($"<color=#FF6B6B>║</color>  • {kvp.Key}: <color=#FFD700>{kvp.Value:P0}</color>");
                }
            }
            else
            {
                sb.AppendLine($"<color=#FF6B6B>║</color> BT Override: <color=#888888>없음</color>");
            }
            
            // 검술 선택 상태
            if (enemy.CurrentBTContext.selectedCommandIndex.HasValue)
            {
                sb.AppendLine($"<color=#FF6B6B>║</color> 선택 검술: <color=#FF69B4>인덱스 {enemy.CurrentBTContext.selectedCommandIndex.Value}</color>");
            }
            else if (!string.IsNullOrEmpty(enemy.CurrentBTContext.selectedCommandTag))
            {
                sb.AppendLine($"<color=#FF6B6B>║</color> 선택 검술: <color=#FF69B4>태그 '{enemy.CurrentBTContext.selectedCommandTag}'</color>");
            }
        }
        else
        {
            sb.AppendLine($"<color=#FF6B6B>║</color> BT 평가: <color=#888888>미실행</color>");
        }
        
        sb.AppendLine($"<color=#FF6B6B>╚═════════════════════════╝</color>");
        
        enemyBTStatusText.text = sb.ToString();
    }
    
    /// <summary>
    /// Player BT 상태 업데이트
    /// </summary>
    private void UpdatePlayerBTStatus()
    {
        if (playerBTStatusText == null) return;
        
        var characterManager = CharacterManager.Instance;
        if (characterManager == null || characterManager.PlayerCharacter == null)
        {
            playerBTStatusText.text = "<color=#888888>Player 없음</color>";
            return;
        }
        
        var player = characterManager.PlayerCharacter;
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine($"<color=#00FF00>╔═══ Player: {player.Name} ═══╗</color>");
        
        // BT 설정 상태
        int btCount = player.CharacterData?.behaviorTrees?.Count ?? 0;
        if (btCount > 0)
        {
            sb.AppendLine($"<color=#00FF00>║</color> BT 수: {btCount}개");
            
            // BT Override 상태
            if (player.CurrentBTContext != null && player.CurrentBTContext.probabilityOverrides != null)
            {
                int overrideCount = player.CurrentBTContext.probabilityOverrides.Count;
                if (overrideCount > 0)
                {
                    sb.AppendLine($"<color=#00FF00>║</color> ━━━ BT Override ━━━");
                    sb.AppendLine($"<color=#00FF00>║</color> <color=#FFD700>활성화 ({overrideCount}개)</color>");
                    
                    foreach (var kvp in player.CurrentBTContext.probabilityOverrides)
                    {
                        sb.AppendLine($"<color=#00FF00>║</color>  • {kvp.Key}: <color=#FFD700>{kvp.Value:P0}</color>");
                    }
                }
                else
                {
                    sb.AppendLine($"<color=#00FF00>║</color> BT Override: <color=#888888>없음</color>");
                }
                
                // 검술 선택 상태
                if (player.CurrentBTContext.selectedCommandIndex.HasValue)
                {
                    sb.AppendLine($"<color=#00FF00>║</color> 선택 검술: <color=#FF69B4>인덱스 {player.CurrentBTContext.selectedCommandIndex.Value}</color>");
                }
                else if (!string.IsNullOrEmpty(player.CurrentBTContext.selectedCommandTag))
                {
                    sb.AppendLine($"<color=#00FF00>║</color> 선택 검술: <color=#FF69B4>태그 '{player.CurrentBTContext.selectedCommandTag}'</color>");
                }
            }
            else
            {
                sb.AppendLine($"<color=#00FF00>║</color> BT 평가: <color=#888888>미실행</color>");
            }
        }
        else
        {
            sb.AppendLine($"<color=#00FF00>║</color> BT: <color=#888888>미설정 (UI 기반 플레이어)</color>");
        }
        
        sb.AppendLine($"<color=#00FF00>╚═════════════════════════╝</color>");
        
        playerBTStatusText.text = sb.ToString();
    }
    
    /// <summary>
    /// BT 실행 히스토리 업데이트
    /// </summary>
    private void UpdateHistory()
    {
        if (historyText == null) return;
        
        var history = BTLogHistory.Instance;
        var recentLogs = history.GetRecentLogs(maxHistoryDisplay);
        
        if (recentLogs == null || recentLogs.Count == 0)
        {
            historyText.text = "<color=#888888>BT 실행 히스토리 없음</color>";
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<color=#00BFFF>╔═══ BT 실행 히스토리 (최근 {recentLogs.Count}개) ═══╗</color>");
        
        for (int i = recentLogs.Count - 1; i >= 0; i--) // 최신순
        {
            var log = recentLogs[i];
            
            // 헤더
            string turnTypeIcon = log.isAttackTurn ? "⚔" : "🛡";
            string matchIcon = log.foundMatch ? "<color=#00FF00>✓</color>" : "<color=#FF6B6B>✗</color>";
            
            sb.AppendLine($"<color=#00BFFF>║</color> {turnTypeIcon} 턴 {log.turnNumber} | {log.combatantName} | {matchIcon}");
            
            // Entry 정보
            if (log.foundMatch && !string.IsNullOrEmpty(log.matchedEntryDescription))
            {
                sb.AppendLine($"<color=#00BFFF>║</color>   Entry[{log.matchedEntryIndex}]: <color=#FFD700>{log.matchedEntryDescription}</color>");
            }
            
            // 조건 결과 (간략)
            if (log.conditions.Count > 0)
            {
                int passCount = 0;
                foreach (var cond in log.conditions)
                {
                    if (cond.result) passCount++;
                }
                sb.AppendLine($"<color=#00BFFF>║</color>   조건: {passCount}/{log.conditions.Count} 통과");
            }
            
            // 액션 실행 (간략)
            if (log.actions.Count > 0)
            {
                int executedCount = 0;
                foreach (var act in log.actions)
                {
                    if (!act.skipped) executedCount++;
                }
                sb.AppendLine($"<color=#00BFFF>║</color>   액션: {executedCount}/{log.actions.Count} 실행");
            }
            
            // 확률 변경
            if (log.probabilityOverrides.Count > 0)
            {
                sb.Append($"<color=#00BFFF>║</color>   확률: ");
                bool first = true;
                foreach (var kvp in log.probabilityOverrides)
                {
                    if (!first) sb.Append(", ");
                    sb.Append($"<color=#FFA500>{kvp.Key}={kvp.Value:P0}</color>");
                    first = false;
                }
                sb.AppendLine();
            }
            
            // 구분선
            if (i > 0)
            {
                sb.AppendLine($"<color=#00BFFF>║</color> ───────────────────────");
            }
        }
        
        sb.AppendLine($"<color=#00BFFF>╚═══════════════════════════════════════════╝</color>");
        
        historyText.text = sb.ToString();
    }
    
    /// <summary>
    /// 히스토리 클리어
    /// </summary>
    public void ClearHistory()
    {
        BTLogHistory.Instance.Clear();
        if (historyText != null)
        {
            historyText.text = "<color=#888888>히스토리가 클리어되었습니다</color>";
        }
    }
    
    /// <summary>
    /// 모니터 UI 강제 업데이트
    /// </summary>
    public void ForceUpdate()
    {
        UpdateMonitorDisplay();
    }
}

