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
        // �׽�Ʈ�� �ϵ��ڵ� Ŀ�ǵ� ����
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

        Debug.Log(">> �Ϻ� �Է� ��ϵ�!");
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

            Debug.Log($"[�� {currentCommandIndex + 1}] {player.Name}: {playerAction} vs {enemy.Name}: {enemyAction}");

            // Ÿ�̹� �Է� ���� (�÷��̾)
            yield return StartCoroutine(HandleActionWithTimingInput(player, currentCommandIndex));

            // ���� AI�� ������ false (���� ����)
            enemy.IsPerfectTiming[currentCommandIndex] = false;

            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("�̹� �� ����.");
    }





    IEnumerator HandleActionWithTimingInput(Combatant actor, int commandIndex)
    {
        float inputWindow = 1.0f; // 1�� ���� �Է� ����
        float timer = 0f;

        
        OpenTimingWindow();
        Debug.Log($"[{actor.Name}] Ÿ�̹� �Է� ���� (Ŀ�ǵ�: {actor.Commands[commandIndex]})");

        // Ÿ�̹� �Է� ��� �ð�
        yield return new WaitForSeconds(inputWindow);

        while (timer < inputWindow)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CloseTimingWindow();
        Debug.Log("[Player] Ÿ�̹� ������ ����");
    }


}
