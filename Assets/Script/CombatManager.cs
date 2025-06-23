using System.Collections;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private Combatant player;
    private Combatant enemy;

    void Start()
    {
        player = new Combatant("Player");
        enemy = new Combatant("Enemy");

        SetupCommands();
        StartCoroutine(ExecuteTurn());
    }

    void SetupCommands()
    {
        // 테스트용 하드코딩
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

    IEnumerator ExecuteTurn()
    {
        for (int i = 0; i < 5; i++)
        {
            ActionCommand playerAction = player.Commands[i];
            ActionCommand enemyAction = enemy.Commands[i];

            Debug.Log($"[턴 {i + 1}] {player.Name}: {playerAction} vs {enemy.Name}: {enemyAction}");

            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("이번 턴 종료.");
    }
}
