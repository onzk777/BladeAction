using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
//using UnityEngine.UIElements;


public class CombatManager : MonoBehaviour
{
    private PlayerCombatant player;
    private Combatant enemy;
    public TimingInputHandler timingInputHandler;
    [SerializeField] private GameObject playerActor;  // 플레이어 액터 (항상 고정)
    [SerializeField] private GameObject currentEnemyActor; // 전투 시 적 액터를 이걸로 설정
    private PlayerController playerController;
    private EnemyController enemyController;
    [SerializeField] private SwordArtStyleData defaultSwordArt;
    [SerializeField] private GlobalConfig config;
    [SerializeField] private Button resetButton;

    private int currentHitIndex = 0;  // 현재 히트 번호

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


    public struct CommandPair // 플레이어와 적의 커맨드 쌍
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


        // 플레이어 캐릭터 생성 및 기본 검술 장비
        player = playerController.combatant; // 기존 PlayerController에서 가져옴
        enemy = enemyController.combatant; // 기존 EnemyController에서 가져옴


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
        playerActionDone = false;
        enemyActionDone = false;

        var playerResults = playerController.combatant.SelectedCommandResults;
        var enemyResults = enemyController.combatant.SelectedCommandResults;

        for (currentCommandIndex = 0; currentCommandIndex < 5; currentCommandIndex++)
        {
            CombatStatusDisplay.Instance?.ResetText();

            var playerResult = playerResults[currentCommandIndex];
            var enemyResult = enemyResults[currentCommandIndex];

            // UI 표시
            CombatStatusDisplay.Instance?.SetActionProgress(currentCommandIndex + 1, 5);
            CombatStatusDisplay.Instance?.SetPlayerActionCommandName(playerResult.command.commandName);
            CombatStatusDisplay.Instance?.SetEnemyActionCommandName(enemyResult.command.commandName);

            // ⏱ 동시에 실행
            StartCoroutine(ExecutePlayerAction(playerResult, enemyResult));
            StartCoroutine(ExecuteEnemyAction(enemyResult, playerResult));
            // 두 코루틴이 끝날 때까지 딱 한 번만 대기
            yield return new WaitUntil(() => playerActionDone && enemyActionDone);



            // 💥 중단 판정: 서로가 서로를 끊을 수 있는지 확인
            TryInterrupt(playerResult, enemyResult); // 플레이어가 적을 중단시킬 수 있는가?
            TryInterrupt(enemyResult, playerResult); // 적이 플레이어를 중단시킬 수 있는가?

            float actionStartTime = Time.time; // ★ 액션 시작 시각 기록
            // ▶️ 'GlobalConfig.InputWindowSeconds'만큼 최소 보장
            float actionElapsed = Time.time - actionStartTime;
            float minActionDuration = config.InputWindowSeconds; // 3초
            if (actionElapsed < minActionDuration)
            yield return new WaitForSeconds(minActionDuration - actionElapsed);


            //yield return new WaitForSeconds(1f); // Optional: 다음 액션 직전 짧은 숨고르기

            // 💡 이후 대결 결과 판정은 CombatJudge.Resolve() 등에서 처리 예정
        }

        resetButton.gameObject.SetActive(true);
        Debug.Log("전투 종료");
    }

    private IEnumerator ExecutePlayerAction(CombatantCommandResult playerResult, CombatantCommandResult enemyResult)
    {
        float actionTime0 = Time.time;
        var action = playerResult.command;
        if (action.hitCount ==0)
        {
            Debug.Log($"[Player] 히트 타이밍 없음 - 입력 스킵");
            yield break;
        }
        if (playerResult.wasInterrupted)
        {
            Debug.Log("⛔ 액션 중단: 이 커맨드는 더 이상 진행되지 않습니다.");
            yield break;
        }
        // 💡 방어일 경우, 적의 타이밍을 가져와서 복사
        /*
        if (action.commandType == CommandType.Defend)
        {
            var source = enemyResult.command.perfectTimings;
            if (source != null)
            {
                action.perfectTimings = new PerfectTimingWindow[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    action.perfectTimings[i] = new PerfectTimingWindow
                    {
                        start = source[i].start,
                        duration = source[i].duration
                    };
                }
            }
        }
        */

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
            yield break;
        }
        Debug.Log($"[검사] Enemy HitCount: {enemyResult.command.hitCount}, perfectTimings.Length: {enemyResult.command.perfectTimings?.Length}, hitResults.Length: {enemyResult.hitResults?.Length}");

        if (enemyResult.wasInterrupted)
        {
            Debug.Log("⛔ 액션 중단: 이 커맨드는 더 이상 진행되지 않습니다.");
            yield break;
        }
        // 💡 방어일 경우, 플레이어의 타이밍 복사
        /*
        if (action.commandType == CommandType.Defend)
        {
            var source = playerResult.command.perfectTimings;
            if (source != null)
            {
                action.perfectTimings = new PerfectTimingWindow[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    action.perfectTimings[i] = new PerfectTimingWindow
                    {
                        start = source[i].start,
                        duration = source[i].duration
                    };
                }
            }
        }
        */

        for (int i = 0; i < action.hitCount; i++)
        {
            yield return StartCoroutine(HandleHitTimingInput(false, enemyResult, i));
        }
        enemyActionDone = true;
    }





    private IEnumerator HandleHitTimingInput(bool isPlayer, CombatantCommandResult commandResult, int hitIndex)
    {
        var action = commandResult.command;
        inputAccepted = false;
        /////////////////히트가 없는 액션////////////////
        if (action.hitCount == 0)
        {
            Debug.Log($"[{(isPlayer ? "Player" : "Enemy")}] 히트 수가 0이므로 타이밍 입력 스킵");
            yield break;
        }
        ///////////////////////////////////////////////

        /////////////////////Debug/////////////////////
        if (action == null)
        {
            Debug.LogError("[HandleHitTimingInput] action is null");
            yield break;
        }
        if (action.perfectTimings == null || hitIndex >= action.perfectTimings.Length)
        {
            Debug.LogError($"[Error] perfectTimings 배열이 null이거나 hitIndex({hitIndex})가 길이({action.perfectTimings?.Length})를 초과했습니다. Command: {action.commandName}");
            yield break;
        }
        if (action.perfectTimings == null || action.perfectTimings.Length == 0)
        {
            Debug.LogWarning($"[{(isPlayer ? "Player" : "Enemy")}] perfectTimings가 null이거나 비어 있음 → 스킵");
            yield break;
        }
        if (hitIndex >= action.perfectTimings.Length)
        {
            Debug.LogError($"[Error] hitIndex({hitIndex})가 perfectTimings.Length({action.perfectTimings.Length})보다 큼");
            yield break;
        }
        ////////////////////////////////////////////////

        var timing = action.perfectTimings[hitIndex];


        float window = config.InputWindowSeconds;
        float startTime = Time.time;                 // ← (A) 타이밍 시작 순간 저장
        float elapsed = 0f;

        if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerStatus(""); // 플레이어 입력 상태 표시 초기화
        else CombatStatusDisplay.Instance?.ShowEnemyStatus(""); // 적 입력 상태 표시 초기화
        

        // ✅ UI 표시 (공통)
        if (isPlayer)
            CombatStatusDisplay.Instance?.ShowPlayerTimingStart();
        else
            CombatStatusDisplay.Instance?.ShowEnemyTimingStart();

        float perfectStart = timing.start;
        float perfectEnd = timing.start + timing.duration;


        // ✅ 적(AI) 처리: 랜덤으로 성공 여부 판단
        if (!isPlayer)
        {
            ////////////////타이밍 정보 표시/////////////////////
            CombatStatusDisplay.Instance?.SetEnemyTimingInfoText(
            $"[Hit {hitIndex + 1}/{action.hitCount}] " +
            $"Perfect: {perfectStart:F2} ~ {perfectEnd:F2}s | " +
            $"Elapsed: {elapsed:F2}s"
            );
            ////////////////////////////////////////////////////

            yield return new WaitForSeconds(perfectStart + (timing.duration * 0.5f)); // 타이밍 중심에서 판정

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
            yield return new WaitForSeconds(window - (perfectStart + timing.duration * 0.5f)); // 남은 시간 대기
            yield break;
        }



        OpenTimingWindow();
        bool startShown = false;
        bool endShown = false;



        while ((Time.time - startTime) < window && IsTimingWindowOpen)
        {
            elapsed = Time.time - startTime;
            // ⏱️ 타이밍 정보 표시
            CombatStatusDisplay.Instance?.SetPlayerTimingInfoText(
            $"[Hit {hitIndex + 1}/{action.hitCount}] " +
            $"Perfect: {perfectStart:F2} ~ {perfectEnd:F2}s | " +
            $"Elapsed: {elapsed:F2}s"
            );            

            if (!startShown && elapsed >= perfectStart)
            {
                if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerTimingPerfectStart();
                else CombatStatusDisplay.Instance?.ShowEnemyTimingPerfectStart();
                startShown = true;
            }

            if (!endShown && elapsed >= perfectEnd)
            {
                if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerTimingPerfectEnd();
                else CombatStatusDisplay.Instance?.ShowEnemyTimingPerfectEnd();
                endShown = true;
            }

            // ❗ 입력 처리: 입력이 발생했다면 판정
            if (inputAccepted)
            {
                bool success = elapsed >= perfectStart && elapsed <= perfectEnd;
                commandResult.hitResults[hitIndex].isPerfect = success;

                if (success)
                {
                    if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerStatus("완벽 타이밍 성공!");
                    //else CombatStatusDisplay.Instance?.ShowEnemyStatus("완벽 타이밍 성공!");
                }
                else
                {
                    if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerStatus("완벽 타이밍 실패...");
                    //else CombatStatusDisplay.Instance?.ShowEnemyStatus("완벽 타이밍 실패...");
                }

                // ✅ 입력 후에는 윈도우 닫고 더 이상 입력을 받지 않음
                CloseTimingWindow();
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ⛔ 입력이 없었을 경우 처리
        if (!inputAccepted)
        {
            commandResult.hitResults[hitIndex].isPerfect = false;
            if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerStatus("입력 없음 (실패)");
            else CombatStatusDisplay.Instance?.ShowEnemyStatus("입력 없음 (실패)");
        }

        CloseTimingWindow();
        if (isPlayer) CombatStatusDisplay.Instance?.ShowPlayerTimingEnd();
        else CombatStatusDisplay.Instance?.ShowEnemyTimingEnd();

        
        /*
        if (hitIndex + 1 < action.perfectTimings.Length)
        {
            float nextHitStart = action.perfectTimings[hitIndex + 1].start;
            float actualElapsed = Time.time - startTime;                  // ← (D) 실제 경과시간
            float waitTime = Mathf.Max(0f, nextHitStart - actualElapsed);
            yield return new WaitForSeconds(waitTime);
        }

        // ⏱️ 액션 전체 시간(3초) 보장
        float actionDuration = config.InputWindowSeconds;  // 3초 (예정)
        if (elapsed < actionDuration)
        {
            yield return new WaitForSeconds(actionDuration - elapsed);
        }
        */
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
