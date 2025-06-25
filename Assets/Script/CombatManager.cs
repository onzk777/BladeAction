using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private EnemyController enemyController;

    [Header("Config")]
    [SerializeField] private GlobalConfig config;
    [Tooltip("테스트할 총 턴 수")]
    [SerializeField] private int testTurnCount = 3;

    [Header("Test Commands")]
    [Tooltip("테스트용 플레이어 커맨드 리스트")]
    [SerializeField] private List<ActionCommandData> playerTestCommands;
    [Tooltip("테스트용 적 커맨드 리스트")]
    [SerializeField] private List<ActionCommandData> enemyTestCommands;

    [Header("Input Handler")]
    [SerializeField] private TimingInputHandler timingInputHandler;

    // 마지막 입력 시각
    private float lastInputTime = float.MinValue;

    private void Start()
    {
        timingInputHandler.OnPerfectInput += OnPlayerInput;
        StartCoroutine(RunCombat());
    }

    private void OnDestroy()
    {
        timingInputHandler.OnPerfectInput -= OnPlayerInput;
    }

    private void OnPlayerInput()
    {
        lastInputTime = Time.time;
    }

    private IEnumerator RunCombat()
    {
        for (int turn = 0; turn < testTurnCount; turn++)
        {
            // 플레이어 턴
            var pList = (playerTestCommands != null && playerTestCommands.Count > 0)
                        ? playerTestCommands
                        : playerController.Combatant.AvailableCommands;
            Debug.Log($"=== 턴 {turn + 1} 시작: 플레이어 ===");
            yield return StartCoroutine(PerformTurn(
                pList, turn, true
            ));

            // 적 턴
            var eList = (enemyTestCommands != null && enemyTestCommands.Count > 0)
                        ? enemyTestCommands
                        : enemyController.Combatant.AvailableCommands;
            Debug.Log($"=== 턴 {turn + 1} 시작: 적 ===");
            yield return StartCoroutine(PerformTurn(
                eList, turn, false
            ));
        }

        Debug.Log("=== 전투 종료 ===");
    }

    private IEnumerator PerformTurn(
        List<ActionCommandData> commands,
        int turnIndex,
        bool isPlayer)
    {
        // 커맨드 선택
        var action = commands[turnIndex % commands.Count];
        var perfects = action.perfectTimings;
        int hitCount = perfects.Count;

        float windowStart = Time.time;
        float window = config.InputWindowSeconds;
        float cooldown = config.ActionInputCooldown;

        int nextHitIndex = 0;
        float nextAllowedInputTime = windowStart;
        bool[] results = new bool[hitCount];

        Debug.Log($"{(isPlayer ? "[P]" : "[E]")} 액션: {action.commandName}, 히트 수: {hitCount}");

        // 윈도우 루프
        while (Time.time - windowStart < window)
        {
            float elapsed = Time.time - windowStart;

            if (!isPlayer)
            {
                // AI 판정
                if (nextHitIndex < hitCount && elapsed >= perfects[nextHitIndex].start)
                {
                    bool success = Random.value < config.NpcActionPerfectRate;
                    results[nextHitIndex] = success;
                    Debug.Log($"[E Hit {nextHitIndex + 1}] {(success ? "완벽 성공" : "완벽 실패")} ({elapsed:F2}s)");
                    nextHitIndex++;
                }
            }
            else
            {
                // 플레이어 입력 판정
                if (lastInputTime >= nextAllowedInputTime
                    && lastInputTime - windowStart < window
                    && nextHitIndex < hitCount)
                {
                    float t = lastInputTime - windowStart;
                    var pt = perfects[nextHitIndex];
                    bool success = t >= pt.start && t <= pt.start + pt.duration;
                    results[nextHitIndex] = success;

                    if (success)
                    {
                        Debug.Log($"[P Hit {nextHitIndex + 1}] 완벽 성공 ({t:F2}s)");
                        nextAllowedInputTime = lastInputTime;
                    }
                    else
                    {
                        Debug.Log($"[P Hit {nextHitIndex + 1}] 타이밍 실패 ({t:F2}s)");
                        nextAllowedInputTime = lastInputTime + cooldown;
                    }

                    nextHitIndex++;
                }
            }

            yield return null;
        }

        // 윈도우 종료 후 결과 정리
        /*
        for (int i = 0; i < hitCount; i++)
        {
            if (!results[i])
                Debug.Log($"[{(isPlayer ? "P" : "E")} Hit {i + 1}] 입력 없음 또는 실패");
        }
        */

        yield return null;
    }
}
