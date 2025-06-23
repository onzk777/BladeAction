using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private Combatant player;
    private Combatant enemy;
    public TimingInputHandler timingInputHandler;

    private int currentCommandIndex = 0;
    private bool inputAccepted = false;
    public void OpenTimingWindow()
    {
        IsTimingWindowOpen = true;
    }

    public void CloseTimingWindow()
    {
        IsTimingWindowOpen = false;
    }

    public bool IsTimingWindowOpen { get; private set; } = false;

    public struct CommandPair
    {
        public ActionCommand playerCommand;
        public ActionCommand enemyCommand;
        public bool playerPerfect;
        public bool enemyPerfect;
    }


    void Start()
    {
        player = new Combatant("Player");
        enemy = new Combatant("Enemy");

        SetupCommands();
        StartCoroutine(ExecuteTurn());
    }


    void SetupCommands()
    {
        // 테스트용 하드코딩 커맨드 예약
        player.Commands = new ActionCommand[]
        {
            ActionCommand.Slash_Horizontal,
            ActionCommand.Slash_Horizontal,
            ActionCommand.Defend,
            ActionCommand.Thrust,
            ActionCommand.Defend
        };

        enemy.Commands = new ActionCommand[]
        {
            ActionCommand.Slash_Horizontal,
            ActionCommand.Slash_Vertical,
            ActionCommand.SecretArt,
            ActionCommand.Defend,
            ActionCommand.Thrust
        };
    }

    public void RegisterPerfectInput()
    {
        if (inputAccepted) return;

        Debug.Log(">> 완벽 입력 등록됨!");
        player.IsPerfectTiming[currentCommandIndex] = true;
        inputAccepted = true;
    }


    IEnumerator ExecuteTurn()
    {
        for (int currentCommandIndex = 0; currentCommandIndex < 5; currentCommandIndex++)
        {
            inputAccepted = false;

            ActionCommand playerAction = player.Commands[currentCommandIndex];
            ActionCommand enemyAction = enemy.Commands[currentCommandIndex];

            Debug.Log($"[턴 {currentCommandIndex + 1}] {player.Name}: {playerAction} vs {enemy.Name}: {enemyAction}");

            // 타이밍 입력 판정 (플레이어만)
            yield return StartCoroutine(HandleActionWithTimingInput(player, currentCommandIndex));

            // 적은 AI라서 무조건 false (이후 개선)
            enemy.IsPerfectTiming[currentCommandIndex] = false;

            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("이번 턴 종료.");
    }





    IEnumerator HandleActionWithTimingInput(Combatant actor, int commandIndex)
    {
        float inputWindow = 1.0f; // 1초 동안 입력 가능
        float timer = 0f;

        
        OpenTimingWindow();
        Debug.Log($"[{actor.Name}] 타이밍 입력 시작 (커맨드: {actor.Commands[commandIndex]})");

        // 타이밍 입력 대기 시간
        yield return new WaitForSeconds(inputWindow);

        while (timer < inputWindow)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CloseTimingWindow();
        Debug.Log("[Player] 타이밍 윈도우 닫힘");
    }


}
