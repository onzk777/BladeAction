using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    private PlayerCombatant player;
    private Combatant enemy;
    public TimingInputHandler timingInputHandler;
    [SerializeField] private GameObject playerActor;
    [SerializeField] private GameObject currentEnemyActor;
    private PlayerController playerController;
    private EnemyController enemyController;
    [SerializeField] private SwordArtStyleData defaultSwordArt;
    [SerializeField] private GlobalConfig config;
    [SerializeField] private Button resetButton;

    private int currentHitIndex = 0;
    private int currentCommandIndex = 0;
    private bool inputAccepted = false;
    public bool IsTimingWindowOpen { get; private set; } = false;

    private bool playerActionDone = false;
    private bool enemyActionDone = false;

    public void OpenTimingWindow()
    {
        IsTimingWindowOpen = true;
    }

    public void CloseTimingWindow()
    {
        IsTimingWindowOpen = false;
    }

    public struct CommandPair
    {
        public ActionCommand playerCommand;
        public ActionCommand enemyCommand;
        public bool playerPerfect;
        public bool enemyPerfect;
    }

    void RestartAction()
    {
        StartCoroutine(ExecuteTurn());
        resetButton.gameObject.SetActive(false);
    }

    void Start()
    {
        playerController = playerActor.GetComponent<PlayerController>();
        enemyController = currentEnemyActor.GetComponent<EnemyController>();

        if (playerController == null || enemyController == null)
        {
            Debug.LogError("⚠️ PlayerController 또는 EnemyController가 해당 액터에 없습니다.");
            return;
        }

        player = playerController.combatant;
        enemy = enemyController.combatant;

        player = new PlayerCombatant("Player");
        player.EquipSwordArtStyle(playerController.equippedStyle);
        player.SelectedCommandResults = playerController.combatant.SelectedCommandResults;

        enemy = new Combatant("Enemy");
        enemy.EquipSwordArtStyle(enemyController.equippedStyle);
        enemy.SelectedCommandResults = enemyController.combatant.SelectedCommandResults;

        resetButton.onClick.AddListener(RestartAction);
        resetButton.gameObject.SetActive(false);
        StartCoroutine(ExecuteTurn());
    }

    public void RegisterPerfectInput()
    {
        if (!IsTimingWindowOpen || inputAccepted) return;
        inputAccepted = true;
        Debug.Log(">> 입력 감지 완료 (inputAccepted = true)");
    }

    private IEnumerator ExecuteTurn()
    {
        var playerResults = playerController.combatant.SelectedCommandResults;
        var enemyResults = enemyController.combatant.SelectedCommandResults;

        for (currentCommandIndex = 0; currentCommandIndex < 5; currentCommandIndex++)
        {
            // 🔥 각 액션마다 플래그 초기화
            playerActionDone = false;
            enemyActionDone = false;

            CombatStatusDisplay.Instance?.ResetText();

            var playerResult = playerResults[currentCommandIndex];
            var enemyResult = enemyResults[currentCommandIndex];

            // UI 표시
            CombatStatusDisplay.Instance?.SetActionProgress(currentCommandIndex + 1, 5);
            CombatStatusDisplay.Instance?.SetPlayerActionCommandName(playerResult.command.commandName);
            CombatStatusDisplay.Instance?.SetEnemyActionCommandName(enemyResult.command.commandName);

            // 🔥 액션 시작 시간 기록
            float actionStartTime = Time.time;

            // ⏱ 동시에 실행
            StartCoroutine(ExecutePlayerAction(playerResult, enemyResult));
            StartCoroutine(ExecuteEnemyAction(enemyResult, playerResult));

            // 두 코루틴이 끝날 때까지 대기
            yield return new WaitUntil(() => playerActionDone && enemyActionDone);

            // 💥 중단 판정
            TryInterrupt(playerResult, enemyResult);
            TryInterrupt(enemyResult, playerResult);

            // 🔥 최소 액션 시간 보장 (수정됨)
            float actionElapsed = Time.time - actionStartTime;
            float minActionDuration = config.InputWindowSeconds;
            if (actionElapsed < minActionDuration)
            {
                yield return new WaitForSeconds(minActionDuration - actionElapsed);
            }

            // 다음 액션 전 잠깐 대기
            yield return new WaitForSeconds(0.5f);
        }

        resetButton.gameObject.SetActive(true);
        Debug.Log("전투 종료");
    }

    private IEnumerator ExecutePlayerAction(CombatantCommandResult playerResult, CombatantCommandResult enemyResult)
    {
        var action = playerResult.command;
        if (action.hitCount == 0)
        {
            Debug.Log($"[Player] 히트 타이밍 없음 - 입력 스킵");
            playerActionDone = true; // 🔥 플래그 설정 추가
            yield break;
        }
        if (playerResult.wasInterrupted)
        {
            Debug.Log("⛔ 액션 중단: 이 커맨드는 더 이상 진행되지 않습니다.");
            playerActionDone = true; // 🔥 플래그 설정 추가
            yield break;
        }

        for (int i = 0; i < action.hitCount; i++)
        {
            yield return StartCoroutine(HandleHitTimingInput(true, playerResult, i));
        }
        playerActionDone = true;
    }

    private IEnumerator ExecuteEnemyAction(CombatantCommandResult enemyResult, CombatantCommandResult playerResult)
    {
        var action = enemyResult.command;
        if (action.hitCount == 0)
        {
            Debug.Log($"[Enemy] 히트 타이밍 없음 - 입력 스킵");
            enemyActionDone = true; // 🔥 플래그 설정 추가
            yield break;
        }

        if (enemyResult.wasInterrupted)
        {
            Debug.Log("⛔ 액션 중단: 이 커맨드는 더 이상 진행되지 않습니다.");
            enemyActionDone = true; // 🔥 플래그 설정 추가
            yield break;
        }

        for (int i = 0; i < action.hitCount; i++)
        {
            yield return StartCoroutine(HandleHitTimingInput(false, enemyResult, i));
        }
        enemyActionDone = true;
    }

    private IEnumerator HandleHitTimingInput(bool isPlayer, CombatantCommandResult commandResult, int hitIndex)
    {
        var action = commandResult.command;        

        // 히트가 없으면 스킵
        if (action.hitCount == 0)
        {
            Debug.Log($"[{(isPlayer ? "Player" : "Enemy")}] 히트 수가 0이므로 타이밍 입력 스킵");
            yield break;
        }

        // 타이밍 윈도우 준비
        var timing = action.perfectTimings[hitIndex];        
        float startTime = Time.time;
        float perfectStart = timing.start;
        float perfectEnd = timing.start + timing.duration;
        float window = config.InputWindowSeconds;

        inputAccepted = false;
        float elapsed = 0f;

        // UI 표시
        if (isPlayer)
        {
            CombatStatusDisplay.Instance?.ShowPlayerStatus("");
            CombatStatusDisplay.Instance?.ShowPlayerTimingStart();
        }

        else
        {
            CombatStatusDisplay.Instance?.ShowEnemyStatus("");
            CombatStatusDisplay.Instance?.ShowEnemyTimingStart();            
        }

        // 플레이어 처리
        OpenTimingWindow();
        bool startShown = false;
        bool endShown = false;

        while (elapsed < window && IsTimingWindowOpen)
        {
            CombatStatusDisplay.Instance?.ShowPlayerStatus("");
            CombatStatusDisplay.Instance?.ShowEnemyStatus("");
            // 입력 처리
            
            CombatStatusDisplay.Instance?.SetPlayerTimingInfoText(
                $"[Hit {hitIndex + 1}/{action.hitCount}] " +
                $"Perfect: 0.00 ~ {timing.duration:F2}s | " +
                $"Elapsed: {elapsed:F2}s"
            );

            if (!startShown && elapsed >= perfectStart)
            {
                if(isPlayer)CombatStatusDisplay.Instance?.ShowPlayerTimingPerfectStart();
                else CombatStatusDisplay.Instance?.ShowEnemyTimingPerfectStart();
                startShown = true;
            }

            if (!endShown && elapsed >= perfectEnd)
            {
                if(isPlayer)CombatStatusDisplay.Instance?.ShowPlayerTimingPerfectEnd();
                else CombatStatusDisplay.Instance?.ShowEnemyTimingPerfectEnd();
                endShown = true;
            }

            if (isPlayer && inputAccepted)
            {
                // 버튼을 눌렀을 때 즉시 성공/실패 처리
                bool success = elapsed >= perfectStart && elapsed <= perfectEnd;
                commandResult.hitResults[hitIndex].isPerfect = success;

                if (success)
                {
                    Debug.Log($"[Player Perfect] 히트 {hitIndex} 성공");
                    CombatStatusDisplay.Instance?.ShowPlayerStatus("완벽 타이밍 성공!");
                }
                else
                {
                    Debug.Log($"[Player Fail] 히트 {hitIndex} 실패");
                    CombatStatusDisplay.Instance?.ShowPlayerStatus("완벽 타이밍 실패...");
                }

                CloseTimingWindow();
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        

        // 입력이 없었을 경우
        if (!inputAccepted)
        {
            commandResult.hitResults[hitIndex].isPerfect = false;
            CombatStatusDisplay.Instance?.ShowPlayerStatus("입력 없음 (실패)");
        }
        CloseTimingWindow();
        CombatStatusDisplay.Instance?.ShowPlayerTimingEnd();

        


        // 적(AI) 처리
        if (!isPlayer)
        {
            CombatStatusDisplay.Instance?.SetEnemyTimingInfoText(
                $"[Hit {hitIndex + 1}/{action.hitCount}] " +
                $"Perfect: {perfectStart:F2} ~ {perfectEnd:F2}s"
            );

            // 타이밍 중심에서 판정
            yield return new WaitForSeconds(timing.duration * 0.5f);

            bool aiPerfect = Random.value < config.npcActionPerfectRate;
            commandResult.hitResults[hitIndex].isPerfect = aiPerfect;

            if (aiPerfect)
            {
                CombatStatusDisplay.Instance?.ShowEnemyStatus("완벽 타이밍 성공!");
                Debug.Log($"[Enemy Perfect] 히트 {hitIndex} 성공");
            }
            else
            {
                CombatStatusDisplay.Instance?.ShowEnemyStatus("완벽 타이밍 실패...");
                Debug.Log($"[Enemy Fail] 히트 {hitIndex} 실패");
            }

            CombatStatusDisplay.Instance?.ShowEnemyTimingEnd();

            // 나머지 타이밍 대기
            yield return new WaitForSeconds(timing.duration * 0.5f);

            

            yield break;
        }

        // 다음 히트까지 기다리기
        if (hitIndex + 1 < action.perfectTimings.Length)
        {
            float nextStart = action.perfectTimings[hitIndex + 1].start;
            float waitTime = Mathf.Max(0f, nextStart - elapsed);
            yield return new WaitForSeconds(waitTime);
        }

        // 🔥 타이밍 창 시작까지 대기
        if (perfectStart > 0)
        {
            yield return new WaitForSeconds(perfectStart);
        }

        float timingStartTime = Time.time; // 실제 타이밍 윈도우 시작 시간        
    }

    private void TryInterrupt(CombatantCommandResult attacker, CombatantCommandResult defender)
    {
        if (!attacker.command || !defender.command) return;

        bool attackerPerfect = attacker.hitResults[currentHitIndex].isPerfect;
        if (!attackerPerfect) return;

        if (ShouldInterrupt(attacker.command, defender.command))
        {
            defender.wasInterrupted = true;
            Debug.Log($"[Interrupt] {defender.command.commandName} 커맨드가 중단되었습니다.");
        }
    }

    private bool ShouldInterrupt(ActionCommandData attacker, ActionCommandData defender)
    {
        return attacker.canInterruptTarget && defender.canBeInterrupted;
    }
}