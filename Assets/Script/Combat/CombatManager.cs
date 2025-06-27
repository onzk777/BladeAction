using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private TimingInputHandler timingInputHandler;

    [Header("전역 설정")]
    [SerializeField] private GlobalConfig globalConfig;

    private PlayerCombatant playerCombatant;
    private EnemyCombatant enemyCombatant;

    private void Start()
    {
        InitializeCombatants();
        StartCoroutine(RunCombat());
    }

    private void InitializeCombatants()
    {
        // 플레이어 및 적 Combatant 인스턴스 생성
        playerCombatant = new PlayerCombatant("Player");
        enemyCombatant = new EnemyCombatant("Enemy");
    }

    private IEnumerator RunCombat()
    {
        bool isCombatOver = false; // 테스트용, 전투 완료 여부
        while (isCombatOver)
        {
            yield return StartCoroutine(PerformTurn(playerController));
            yield return StartCoroutine(PerformTurn(enemyController));
        }

        Debug.Log("전투 종료!");
    }

    private IEnumerator PerformTurn(ICombatController controller)
    {
        // 0. 초기화
        Combatant actor = controller.Combatant;
        InterruptManager.Reset();

        // 1. 커맨드 선택
        // CommandSelection selection = actor.ChooseCommand();

        // 1.1. 커맨드 유효성 확인
        int selectedIndex = controller.GetSelectedIndex();
        if (selectedIndex < 0 || selectedIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[{actor.Name}] 선택 인덱스가 유효하지 않습니다: {selectedIndex}");
            yield break;  // 잘못된 인덱스면 턴 건너뜀
        }
        ActionCommandData command = actor.AvailableCommands[selectedIndex];
        CombatantCommandResult result = new CombatantCommandResult(command);

        // 2. 타이밍 윈도우 등록 및 입력 수신 시작
        timingInputHandler.LoadTimingWindows(command.perfectTimings);
        timingInputHandler.BeginListening();

        // 3. 턴 시간 및 히트 카운터 초기화
        float elapsed = 0f;
        float turnDuration = globalConfig.TurnDurationSeconds;
        int hitCount = command.hitCount;
        int currentHit = 0;
        float nextHitTime = (hitCount > 0)
            ? command.perfectTimings[0].start
            : float.MaxValue;

        // 4. 메인 루프: 시간이 남아있고, 처리할 히트가 남았으면 반복

        while (elapsed < turnDuration && currentHit < hitCount)
        {
            elapsed += Time.deltaTime;

            if (CheckInterruptCondition())
            {
                Debug.Log("턴이 중단되었습니다.");
                break;
            }

            if (elapsed >= nextHitTime)
            {
                var timing = command.perfectTimings[currentHit];
                timingInputHandler.RegisterHitTiming(timing);

                bool isPerfect = timingInputHandler.EvaluateInput(timing);
                result.SetHitResult(currentHit, isPerfect);

                currentHit++;

                nextHitTime = (currentHit < command.perfectTimings.Count)
                    ? command.perfectTimings[currentHit].start
                    : float.MaxValue;
            }

            yield return null;
        }

        timingInputHandler.EndListening();     
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");     // 6. 최종 결과 로그

        // 7. 커맨드 결과 처리
        controller.ReceiveCommandResult(result);
    }

    private bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();
    }

}
