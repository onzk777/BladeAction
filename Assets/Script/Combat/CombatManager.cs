using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; } // CombatManager의 싱글톤 인스턴스

    [SerializeField] private PlayerController playerController; // 플레이어 컨트롤러
    [SerializeField] private EnemyController enemyController; // 적 컨트롤러
    [SerializeField] private TimingInputHandler timingInputHandler; // 타이밍 입력 핸들러
    private bool currentIsPlayer; // 현재 턴이 플레이어인지 여부
    private bool currentIsDefenseMode; // 현재 방어 모드인지 여부


    [Header("전역 설정")]
    [SerializeField] private GlobalConfig globalConfig;

    private PlayerCombatant playerCombatant; // 플레이어 Combatant 인스턴스
    private EnemyCombatant enemyCombatant; // 적 Combatant 인스턴스

    // 현재 히트 컨텍스트 전역화
    public int CurrentHit { get; private set; } // 현재 히트 인덱스. (연타 공격일 경우 체크용)
    public bool CurrentHitResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    public bool windowPrompted { get; private set; } = false; // 히트 윈도우가 열렸는지 여부
    public ICombatController CurrentController { get; private set; } // player/enemy 컨트롤러의 인터페이스
    public CombatantCommandResult CurrentResult { get; private set; } // 현재 커맨드 결과
    public static float CombatStartTime { get; private set; } // 전투 시작 시간 (초 단위 f.)
    public float GetInputDeadline() // 입력 마감 시간 계산
    {
        return CombatStartTime + GlobalConfig.Instance.TurnDurationSeconds - GlobalConfig.Instance.InputBufferEndSeconds;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // CombatManager의 싱글톤 인스턴스 설정
            DontDestroyOnLoad(gameObject); // CombatManager는 씬 전환 시에도 유지
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 중복 생성 방지
        }
    }

    private void Start()
    {
        InitializeCombatants(); // Combatant 인스턴스 초기화
        StartCoroutine(RunCombat()); // 전투 시작 코루틴 실행
    }

    private void InitializeCombatants() // Combatant 인스턴스 초기화
    {
        // 플레이어 및 적 Combatant 인스턴스 생성
        playerCombatant = new PlayerCombatant("Player", playerController);
        enemyCombatant = new EnemyCombatant("Enemy", enemyController);
    }


    private IEnumerator RunCombat()
    {
        /////////////////////// 점검용 디버그 로그 ///////////////////////
        Debug.Log($"[RunCombat] CombatStartTime 세팅됨: {CombatStartTime}");
        Debug.Log($"[RunCombat] HandlerInstance: {timingInputHandler.GetInstanceID()}");
        Debug.Log($"[RunCombat] timingInputHandler InstanceID: {timingInputHandler.GetInstanceID()}");
        ////////////////////////////////////////////////////////////////

        bool isCombatOver = false; // 테스트용, 전투 완료 여부
        while (!isCombatOver)
        {
            yield return new WaitForSeconds(0.2f); // 첫 턴 시작 전에 살짝 대기
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
        Combatant actor = controller.Combatant; // 현재 턴을 수행하는 Combatant
        int selectedCommandIndex = controller.GetSelectedCommandIndex(); // 선택된 커맨드 인덱스
        ActionCommandData command = actor.AvailableCommands[selectedCommandIndex];
        currentIsPlayer = (controller.Combatant == playerController.Combatant) ? true : false; // 플레이어 여부
        currentIsDefenseMode = false; // 방어 모드 여부 (필요 시 설정)        
        CombatantCommandResult result = new CombatantCommandResult(command); // 커맨드 결과 객체 생성
        timingInputHandler.BeginListening(); // 입력 리스닝 시작

        // 턴 시간 및 히트 카운터 초기화
        float elapsed = 0f; // 경과 시간 초기화
        float turnDuration = globalConfig.TurnDurationSeconds; // 턴 지속 시간 설정에서 읽어오기
        int hitCount = command.hitCount; // 커맨드의 히트 카운트(연타 공격 여부 및 횟수)

        // 히트 컨텍스트 초기화
        CurrentHit = 0; // 현재 히트 인덱스 초기화
        CurrentController = controller; // 현재 컨트롤러 설정
        CurrentResult = result; // 현재 커맨드 결과 설정

        CombatStatusDisplay.Instance.ClearResults(); // 결과 표시 초기화
        
        CombatStatusDisplay.Instance.whosTurnText(currentIsPlayer); // 현재 턴 표시

        // 1.1. 커맨드 유효성 확인
        if (selectedCommandIndex < 0 || selectedCommandIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[{actor.Name}] 선택 인덱스가 유효하지 않습니다: {selectedCommandIndex}");
            yield break;  // 잘못된 인덱스면 턴 건너뜀
        }

        CombatStatusDisplay.Instance.ShowCommandStart(currentIsPlayer, command.commandName); // 3. 커맨드 시작 표시


        // 2. 타이밍 윈도우 등록 및 입력 수신 시작
        timingInputHandler.LoadTimingWindows(command.perfectTimings); // 커맨드의 완벽 타이밍 윈도우를 로드
        CombatStatusDisplay.Instance.ShowInputPrompt(currentIsPlayer, "입력 대기"); // 5. 입력 프롬프트 표시
        
        
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
                float inputAvailableStart = GlobalConfig.Instance.InputBufferStartSeconds;
                float perfectWindowStart = perfectWindow.start;
                float perfectWindowEnd = perfectWindow.start + perfectWindow.duration;
                float inputAvailableEnd = GetInputDeadline();

                Debug.Log($"[{actor.Name}] 현재 히트: {CurrentHit + 1}/{hitCount}, 윈도우: {perfectWindowStart} ~ {perfectWindowEnd}");

                // 윈도우 오픈: prompt 한 번만 띄우기
                if (!windowPrompted && elapsed >= inputAvailableStart)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} 오픈");
                    windowPrompted = true;
                    CurrentHitResultShown = false; // 히트 결과가 아직 표시되지 않았음
                    CombatStatusDisplay.Instance.ShowInputPrompt(currentIsPlayer, "입력 가능!");
                    CurrentController = controller;
                    CurrentResult = result;
                    timingInputHandler.RegisterHitTiming(perfectWindow);
                }

                if (currentIsPlayer)
                {
                    if (elapsed >= perfectWindowStart && elapsed < perfectWindowEnd && !CurrentHitResultShown)
                    {
                        CombatStatusDisplay.Instance.ShowInputPrompt(currentIsPlayer, "지금이닷!"); // 강조 애니메이션 등
                    }
                    // 실패 fallback 처리
                    if (windowPrompted && elapsed >= perfectWindowEnd && !CurrentHitResultShown)
                    {
                        Debug.Log($"[PerformTurn] 히트 {CurrentHit} fallback");
                        if (currentIsPlayer) // 
                            timingInputHandler.NotifyWindowClosed(currentIsPlayer);
                        else
                            ResolveInput(currentIsPlayer, currentIsDefenseMode, false, CurrentHit, controller, result);
                    }
                    if (elapsed >= perfectWindowEnd && elapsed < inputAvailableEnd && !CurrentHitResultShown)
                    {
                        CombatStatusDisplay.Instance.ShowInputPrompt(currentIsPlayer, "늦었지만 가능...");
                    }

                }
                else
                {
                    if (windowPrompted && elapsed >= perfectWindowStart && !CurrentHitResultShown)
                    {
                        // 예시: AI 확률 기반 완벽 입력
                        bool isPerfect = Random.value < globalConfig.NpcActionPerfectRate;
                        ResolveInput(currentIsPlayer, currentIsDefenseMode, isPerfect, CurrentHit, controller, result);
                    }
                }
                // 히트 전환
                if (CurrentHitResultShown && windowPrompted)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} 완료 → 전환");
                    CurrentHitResultShown = false; // 히트 결과 표시 초기화
                    windowPrompted = false;
                    CurrentHit++;
                }
            }            
            yield return null;
        }
          
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");  // 최종 결과 로그
        controller.ReceiveCommandResult(result);    // 커맨드 결과를 컨트롤러에 전달
        timingInputHandler.EndListening();
    }

    public void OnInputReceivedFromHandler(TimingInputHandler handler)
    {
        currentIsPlayer = CurrentController is PlayerController;
        currentIsDefenseMode = false;
        bool isPerfect = handler.HasPerfectInput();

        Debug.Log($"[CombatManager] 입력 발생! currentIsPlayer={currentIsPlayer} currentIsDefenseMode={currentIsDefenseMode} isPerfect={isPerfect}");

        ResolveInput(
            currentIsPlayer,
            currentIsDefenseMode,
            isPerfect,
            CurrentHit,
            CurrentController,
            CurrentResult
        );
    }


    // 플레이어 또는 적이 입력을 성공적으로 처리했을 때 호출 (TimingInputHandler에서 호출됨)
    public void ResolveInput(
        bool currentIsPlayer,
        bool currentIsDefenseMode,
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
        Debug.Log($"[ResolveInput] via fallback | hit={CurrentHit}, perfect={isPerfect}");
        CombatStatusDisplay.Instance.ShowInputPrompt(currentIsPlayer, isPerfect ? "성공!" : "실패!");

        CurrentHitResultShown = true; // 💡 이 변수 필요: 전역으로 선언!
        
        Debug.Log($"[ResolveInput] 히트 {CurrentHit} 결과 처리됨 | 성공여부: {isPerfect} → CurrentHit: {CurrentHit + 1}");
    }


    private bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();        
    }

    public static void SetCurrentHitResultShown(bool currentHitResultShown)
    {
        CombatManager.Instance.CurrentHitResultShown = currentHitResultShown;
    }
}
