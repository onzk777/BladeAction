using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CombatManager : MonoBehaviour
{
    private PlayerCombatant player;
    private Combatant enemy;
    public TimingInputHandler timingInputHandler;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SwordArtStyleData defaultSwordArt;
    [SerializeField] private GlobalConfig config;

    private int currentCommandIndex = 0;
    private bool inputAccepted = false;
    public bool IsTimingWindowOpen { get; private set; } = false;

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


    void Start()
    {
        // 플레이어 캐릭터 생성 및 기본 검술 장비
        player = new PlayerCombatant("Player");

        var style = playerController.equippedStyle;
        if(style == null)
        {
            Debug.LogWarning("PlayerController에 SwordArtStyle이 장착되어있지 않음");
        }
        else
        {
            player.EquipSwordArtStyle(style);
        }

        //SwordArtStyleData equippedSwordArtStyle = player.EquippedStyle;
        //if (!equippedSwordArtStyle) player.EquipSwordArtStyle(defaultSwordArt);

        player.SelectedCommands = playerController.combatant.SelectedCommands;

        // 적 캐릭터 생성 및 기본 검술 장비
        enemy = new Combatant("Enemy");
        
        
        StartCoroutine(ExecuteTurn());
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
        for (currentCommandIndex = 0; currentCommandIndex < 5; currentCommandIndex++)
        {
            inputAccepted = false;

            ActionCommandData playerAction = player.SelectedCommands[currentCommandIndex];
            //ActionCommandData enemyAction = enemy.SelectedCommands[currentCommandIndex];

            if (player.SelectedCommands[currentCommandIndex] == null)
            {
                Debug.LogError($"[Error] 플레이어의 커맨드 {currentCommandIndex}가 null입니다.");
            }

            Debug.Log($"[턴 {currentCommandIndex + 1}] {player.Name}: {playerAction.commandName} vs enemy.Name: enemyAction.commandName");

            // 타이밍 입력 판정 (플레이어만)
            yield return StartCoroutine(HandleActionWithTimingInput(player, player.SelectedCommands[currentCommandIndex], true));

            // 적은 AI라서 무조건 false (이후 개선)
            enemy.IsPerfectTiming[currentCommandIndex] = false;

            yield return new WaitForSeconds(1.0f);
        }

        Debug.Log("이번 턴 종료.");
    }





    private IEnumerator HandleActionWithTimingInput(Combatant character, ActionCommandData action, bool isPlayer)
    {
        float inputWindow = config.InputWindowSeconds;
        inputAccepted = false;
        float elapsed = 0f;

        // 💡 완벽 타이밍 범위: 예시로 0.4 ~ 0.6초 설정 (비율 또는 절대값 조정 가능)
        float perfectStart = action.perfectTimeStart;
        float perfectRange = action.perfectTimeRange;
        float perfectEnd = perfectStart + perfectRange;
        bool perfectStartLogged = false;
        bool perfectEndLogged = false;

        // 🔁 inputWindow 시간 동안 대기하며 입력 감지
        OpenTimingWindow();
        /* 버그 확인용
        Debug.Log($"perfectStart: {perfectStart}");
        Debug.Log($"perfectEnd: {perfectEnd}");
        */
        
        while (elapsed < inputWindow)
        {
            if (!perfectStartLogged && elapsed >= perfectStart)
            {
                Debug.Log($"[Timing] 타이밍 입력 시작 (elapsed = {elapsed:F2}s)");
                perfectStartLogged = true;
            }

            if (!perfectEndLogged && elapsed >= perfectEnd)
            {
                Debug.Log($"[Timing] 타이밍 입력 종료 (elapsed = {elapsed:F2}s)");
                perfectEndLogged = true;
            }
            if (inputAccepted)
            {
                
                if (elapsed >= perfectStart && elapsed <= perfectEnd)
                {
                    // 🎯 완벽 타이밍 판정
                    Debug.Log("[Perfect] 타이밍 입력 성공!");
                    character.IsPerfectTiming[currentCommandIndex] = true;
                }
                else
                {
                    // ❌ 실패 타이밍
                    Debug.Log("[Fail] 타이밍 입력 실패.");
                    character.IsPerfectTiming[currentCommandIndex] = false;
                }

                break; // ✅ 어쨌든 입력은 한 번만
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!inputAccepted)
        {
            Debug.Log("[Miss] 입력 없음 (실패)");
            character.IsPerfectTiming[currentCommandIndex] = false;
        }
        CloseTimingWindow();
        Debug.Log($"[{(isPlayer ? "Player" : "Enemy")}] 타이밍 입력 종료");
    }


}
