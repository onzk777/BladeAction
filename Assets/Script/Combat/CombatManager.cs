using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [SerializeField] private PlayerController playerController;
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private TimingInputHandler timingInputHandler;

    [Header("전역 설정")]
    [SerializeField] private GlobalConfig globalConfig;

    private PlayerCombatant playerCombatant;
    private EnemyCombatant enemyCombatant;

    // 현재 히트 컨텍스트 전역화
    public int CurrentHit { get; private set; }
    public bool CurrentHitResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    public bool windowPrompted { get; private set; } = false;
    public ICombatController CurrentController { get; private set; }
    public CombatantCommandResult CurrentResult { get; private set; }
    public static float CombatStartTime { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // CombatManager는 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성 방지
        }
    }

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
        Debug.Log($"[RunCombat] CombatStartTime 세팅됨: {CombatStartTime}");
        Debug.Log($"[RunCombat] HandlerInstance: {timingInputHandler.GetInstanceID()}");
        Debug.Log($"[RunCombat] timingInputHandler InstanceID: {timingInputHandler.GetInstanceID()}");
        bool isCombatOver = false; // 테스트용, 전투 완료 여부
        while (!isCombatOver)
        {
            yield return new WaitForSeconds(0.1139f); // 첫 턴 시작 전에 살짝 대기
            CombatStartTime = Time.time;            
            yield return StartCoroutine(PerformTurn(playerController));
            yield return StartCoroutine(PerformTurn(enemyController));
            // CombatStatusDisplay.Instance.ClearResults();    // 결과 표시 초기화
        }

        Debug.Log("전투 종료!");
    }

    private IEnumerator PerformTurn(ICombatController controller)
    {
        Debug.Log($"[PerformTurn] 시작 - CombatStartTime: {CombatStartTime}");
        // 초기화        
        Combatant actor = controller.Combatant;
        int selectedIndex = controller.GetSelectedIndex();
        ActionCommandData command = actor.AvailableCommands[selectedIndex];
        CombatantCommandResult result = new CombatantCommandResult(command);
        timingInputHandler.BeginListening();

        // 턴 시간 및 히트 카운터 초기화
        float elapsed = 0f;
        float turnDuration = globalConfig.TurnDurationSeconds;
        int hitCount = command.hitCount;              

        // 히트 컨텍스트 초기화
        CurrentHit = 0;
        CurrentController = controller;
        CurrentResult = result;

        CombatStatusDisplay.Instance.ClearResults(); // 결과 표시 초기화
        
        bool isPlayer = (controller.Combatant == playerController.Combatant) ? true : false;

        CombatStatusDisplay.Instance.whosTurnText(isPlayer);

        // 1.1. 커맨드 유효성 확인
        if (selectedIndex < 0 || selectedIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[{actor.Name}] 선택 인덱스가 유효하지 않습니다: {selectedIndex}");
            yield break;  // 잘못된 인덱스면 턴 건너뜀
        }

        CombatStatusDisplay.Instance.ShowCommandStart(isPlayer, command.commandName);
        

        // 2. 타이밍 윈도우 등록 및 입력 수신 시작
        timingInputHandler.LoadTimingWindows(command.perfectTimings);
        CombatStatusDisplay.Instance.ShowInputPrompt(isPlayer, "입력 대기"); // 5. 입력 프롬프트 표시
        
        
        // 4. 메인 루프: 시간이 남아있고, 처리할 히트가 남았으면 반복
        windowPrompted = false; // 히트 윈도우가 열렸는지 여부

        // 5. 메인 루프 시작
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            CombatStatusDisplay.Instance?.updateTurnInfo(turnDuration - elapsed);
            
            if (CheckInterruptCondition())
            {
                Debug.Log("턴이 중단되었습니다.");
                break;
            }

            if (CurrentHit < hitCount)  // 현재 히트가 유효한 경우
            {   
                // 1) 이번 히트 윈도우 정의
                var perfectWindow = command.perfectTimings[CurrentHit];
                float perfectWindowStart = perfectWindow.start;
                float perfectWindowEnd = perfectWindow.start + perfectWindow.duration;

                Debug.Log($"[{actor.Name}] 현재 히트: {CurrentHit + 1}/{hitCount}, 윈도우: {perfectWindowStart} ~ {perfectWindowEnd}");
                // 윈도우 오픈: prompt 한 번만 띄우기
                if (!windowPrompted && elapsed >= perfectWindowStart)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} 오픈");
                    windowPrompted = true;
                    CurrentHitResultShown = false; // 히트 결과가 아직 표시되지 않았음
                    CombatStatusDisplay.Instance.ShowInputPrompt(isPlayer, "지금이닷!");
                    //timingInputHandler.BeginListening();  // ✅ 히트마다 리스너 시작!
                    CurrentController = controller;
                    CurrentResult = result;
                    timingInputHandler.RegisterHitTiming(perfectWindow);
                    
                }

                // AI 처리
                if (windowPrompted && !CurrentHitResultShown && !isPlayer)
                {
                    // 예시: AI 확률 기반 완벽 입력
                    bool isPerfect = Random.value < globalConfig.NpcActionPerfectRate;
                    ResolveInput(isPlayer, isPerfect, CurrentHit, controller, result);                                           
                }

                // 실패 fallback 처리
                if (windowPrompted && elapsed >= perfectWindowEnd && !CurrentHitResultShown)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} fallback");
                    if (isPlayer)
                        timingInputHandler.NotifyWindowClosed(isPlayer);
                    else
                        ResolveInput(isPlayer, false, CurrentHit, controller, result);
                }

                // 히트 전환
                if (CurrentHitResultShown && windowPrompted)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} 완료 → 전환");
                    CurrentHit++;
                    windowPrompted = false;                    
                }
            }
            
            yield return null;
        }
          
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");  // 최종 결과 로그
        controller.ReceiveCommandResult(result);    // 커맨드 결과를 컨트롤러에 전달
        timingInputHandler.EndListening();
    }

    // 플레이어 또는 적이 입력을 성공적으로 처리했을 때 호출 (TimingInputHandler에서 호출됨)
    public void ResolveInput(
        bool isPlayer,
        bool isPerfect,
        int CurrentHit,
        ICombatController controller,
        CombatantCommandResult result
    )
    {
        if (CurrentHitResultShown) return;
        result.SetHitResult(CurrentHit, isPerfect);
        controller.ReceiveCommandResult(result);
        controller.OnHitResult(CurrentHit, isPerfect);
        
        CombatStatusDisplay.Instance.ShowInputPrompt(isPlayer, isPerfect ? "성공!" : "실패!");

        CurrentHitResultShown = true; // 💡 이 변수 필요: 전역으로 선언!
        
        Debug.Log($"[ResolveInput] 히트 {CurrentHit} 결과 처리됨 | 성공여부: {isPerfect} → CurrentHit: {CurrentHit + 1}");
    }


    private bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();        
    }

    public static void MarkCurrentHitResultShown()
    {
        CombatManager.Instance.CurrentHitResultShown = true;
    }




}
