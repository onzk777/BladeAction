using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; } // CombatManager의 싱글톤 인스턴스

    [SerializeField] private PlayerController playerController; // 플레이어 컨트롤러
    [SerializeField] private EnemyController enemyController; // 적 컨트롤러
    [SerializeField] private AttackerInputHandler attackerInputHandler; // 공격자 타이밍 입력 핸들러
    [SerializeField] private DefenderInputHandler defenderInputHandler; // 방어자 타이밍 입력 핸들러
    private bool isPlayerAttacker; // 현재 턴이 플레이어인지 여부
    private bool? attackerPerfectInput = null;
    private bool? defenderPerfectInput = null;
    private float? attackerInputTime;
    private float? defenderInputTime;
    public bool IsPlayerAttacker
    {
        get { return isPlayerAttacker; }
        set { isPlayerAttacker = value; }
    }
    public bool AttackerPerfectInput
    {
        get { return attackerPerfectInput.HasValue ? attackerPerfectInput.Value : false; }
        set { attackerPerfectInput = value; }
    }
    public bool DefenderPerfectInput
    {
        get { return defenderPerfectInput.HasValue ? defenderPerfectInput.Value : false; }
        set { defenderPerfectInput = value; }
    }

    [Header("전역 설정")]
    [SerializeField] private GlobalConfig globalConfig;

    private PlayerCombatant playerCombatant; // 플레이어 Combatant 인스턴스
    private EnemyCombatant enemyCombatant; // 적 Combatant 인스턴스

    // 현재 히트 컨텍스트 전역화
    public int CurrentHit { get; private set; } // 현재 히트 인덱스. (연타 공격일 경우 체크용)
    public bool CurrentAttackResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    public bool CurrentDefenseResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    private bool CurrentClashResultShown = false; // 현재 클래시 결과가 표시되었는지 여부
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
        Debug.Log($"[RunCombat] HandlerInstance: {attackerInputHandler.GetInstanceID()}");
        Debug.Log($"[RunCombat] timingInputHandler InstanceID: {attackerInputHandler.GetInstanceID()}");
        ////////////////////////////////////////////////////////////////

        bool isCombatOver = false; // 테스트용, 전투 완료 여부
        while (!isCombatOver)
        {
            CombatStartTime = Time.time;
            yield return new WaitForSeconds(0.2f); // 첫 턴 시작 전에 살짝 
            yield return StartCoroutine(PerformTurn(playerController));
            yield return new WaitForSeconds(3.0f); // 테스트 용 결과 확인 대기 시간
            yield return StartCoroutine(PerformTurn(enemyController));
            yield return new WaitForSeconds(3.0f); // 테스트 용 결과 확인 대기 시간
        }
        Debug.Log("전투 종료!");
    }

    private IEnumerator PerformTurn(ICombatController controller)
    {
        Debug.Log($"[턴 시작] PerformTurn 호출, currentCommandIndex 초기화");

        // 초기화        
        Combatant actor = controller.Combatant; // 현재 턴을 수행하는 Combatant
        int selectedCommandIndex = controller.GetSelectedCommandIndex(); // 선택된 커맨드 인덱스
        ActionCommandData command = actor.AvailableCommands[selectedCommandIndex];
        isPlayerAttacker = (controller.Combatant == playerController.Combatant) ? true : false; // 플레이어 여부      
        CombatantCommandResult result = new CombatantCommandResult(command); // 커맨드 결과 객체 생성
        attackerInputHandler.SetIsPlayer(isPlayerAttacker); // 공격자 입력 핸들러 설정
        defenderInputHandler.SetIsPlayer(!isPlayerAttacker); // 방어자 입력 핸들러 설정
        TurnTimer.Reset(); // 턴 시작 시각 초기화        
        float turnDuration = globalConfig.TurnDurationSeconds; // 턴 지속 시간 설정에서 읽어오기
        int hitCount = command.hitCount; // 커맨드의 히트 카운트(연타 공격 여부 및 횟수)
        attackerPerfectInput = null; // 공격자 완벽 입력 여부 초기화
        defenderPerfectInput = null; // 방어자 완벽 입력 여부 초기화
        attackerInputTime = null; // 공격자 입력 시간 초기화
        defenderInputTime = null; // 방어자 입력 시간 초기화
        CurrentAttackResultShown = false; // 현재 공격자 결과 표시 여부 초기화
        CurrentDefenseResultShown = false; // 현재 방어자 결과 표시 여부 초기화
        CurrentClashResultShown = false; // 현재 타격 판정 결과 표시 여부 초기화
        windowPrompted = false; // 히트 윈도우가 열렸는지 여부 초기화
        attackerInputHandler.ResetCooldown(); // 공격자 입력 핸들러 쿨다운 초기화
        defenderInputHandler.ResetCooldown(); // 방어자 입력 핸들러 쿨다운 초기화
        CurrentHit = 0; // 현재 히트 인덱스 초기화
        CurrentController = controller; // 현재 컨트롤러 설정
        CurrentResult = result; // 현재 커맨드 결과 설정
        CombatStatusDisplay.Instance.ClearResults(); // 결과 표시 초기화        
        CombatStatusDisplay.Instance.whosTurnText(isPlayerAttacker); // 현재 턴 표시

        if (isPlayerAttacker)
        {
            attackerInputHandler.EnableInput(); // 공격자 입력 리스닝 시작
            Debug.Log("[CombatManager] 공격자 입력 허용됨");
        }            
        else 
        {
            defenderInputHandler.EnableInput(); // 방어자 입력 리스닝 시작
            Debug.Log("[CombatManager] 방어자 입력 허용됨");
        }

        // 1.1. 커맨드 유효성 확인
        if (selectedCommandIndex < 0 || selectedCommandIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[{actor.Name}] 선택 인덱스가 유효하지 않습니다: {selectedCommandIndex}");
            yield break;  // 잘못된 인덱스면 턴 건너뜀
        }

        CombatStatusDisplay.Instance.ShowCommandStart(isPlayerAttacker, command.commandName); // 3. 커맨드 시작 표시
        CombatStatusDisplay.Instance.ShowInputPrompt("입력 대기"); // 입력 프롬프트 표시
        
        // 타이밍 윈도우 등록 및 입력 수신 시작
        attackerInputHandler.LoadTimingWindows(command.perfectTimings); // 커맨드의 완벽 타이밍 윈도우를 로드        
        defenderInputHandler.LoadFromOpponentCommand(command); // 적의 커맨드 데이터를 방어자 핸들러에 로드


        bool hasLoggedBlockedReason = false; // 히트 전환 디버깅용, PerformTurn 지역 변수로 선언
        float turnDurationBuffer = 0.02f; // 턴 지속 시간 버퍼 (초 단위, 히트 윈도우가 끝나기 전에 턴이 종료되는 것을 방지하기 위한 용도)

        // 5. 메인 루프 시작
        while (TurnTimer.ElapsedTime < turnDuration + turnDurationBuffer)
        {
            float elapsed = TurnTimer.ElapsedTime; // 현재 경과 시간
            Debug.Log($"[디버그] Hit={CurrentHit}, windowPrompted={windowPrompted}, elapsed={elapsed:F3}");
            Debug.Log($"[히트={CurrentHit}] 조건 점검 → elapsed={elapsed}, windowPrompted={windowPrompted}, clashShown={CurrentClashResultShown}, atkResult={CurrentAttackResultShown}, defResult={CurrentDefenseResultShown}");

            CombatStatusDisplay.Instance?.updateTurnInfo(turnDuration - elapsed);
            if (CheckInterruptCondition())
            {
                Debug.Log("턴이 중단되었습니다.");
                break;
            }
            // 초기화

            if (CurrentHit < hitCount)  // 현재 히트가 유효한 경우
            {   
                // 1) 이번 히트 윈도우 정의
                var perfectWindow = command.perfectTimings[CurrentHit];
                float inputAvailableStart = GlobalConfig.Instance.InputBufferStartSeconds;
                float perfectWindowStart = perfectWindow.start;
                float perfectWindowEnd = perfectWindow.start + perfectWindow.duration;
                float inputAvailableEnd = GetInputDeadline();
                float aiInputTime = perfectWindowStart + perfectWindow.duration * globalConfig.NpcInputDifficulty; // AI 방어 시도 시간 (예시: 윈도우 시작 70% 지점)
                bool aiAttackSuccess = Random.value < globalConfig.NpcActionPerfectRate; // AI 공격 성공 여부
                bool aiDefenseSuccess = Random.value < GlobalConfig.Instance.NpcDefensePerfectRate; // AI 방어 성공 여부
                

                Debug.Log($"[UI표시:지금이닷!] 히트 {CurrentHit + 1}, elapsed={elapsed:F5}, 타이밍창=({perfectWindow.start:F5} ~ {perfectWindow.End:F5})");

                // 윈도우 오픈: prompt 한 번만 띄우기
                if (!windowPrompted && elapsed >= inputAvailableStart)
                {
                    Debug.Log($"[PerformTurn] 히트 {CurrentHit} 오픈");
                    windowPrompted = true; // 히트 윈도우가 열렸음을 설정
                    CurrentAttackResultShown = false;
                    CurrentDefenseResultShown = false;
                    CurrentClashResultShown = false;
                    attackerInputHandler.ResetInputState(); // 👈 히트마다 입력 기록 초기화
                    defenderInputHandler.ResetInputState(); // 👈 히트마다 입력 기록 초기화
                    CombatStatusDisplay.Instance.ShowInputPrompt("입력 가능!");
                    CurrentController = controller;
                    CurrentResult = result;
                    attackerInputHandler.RegisterHitTiming(perfectWindow);
                    defenderInputHandler.RegisterHitTiming(perfectWindow);
                }

                if (!CurrentAttackResultShown && elapsed >= perfectWindowStart) // 히트 윈도우 시작 시점
                {
                    if (attackerInputHandler.IsPlayer)
                    {
                        // 플레이어 입력 대기 UI 표시
                        if (elapsed < perfectWindowEnd)
                        {
                            CombatStatusDisplay.Instance.ShowInputPrompt("지금이닷!");
                            Debug.Log($"[UI표시:막아!] 히트 {CurrentHit + 1}, elapsed={elapsed:F5}, 타이밍창=({perfectWindow.start:F5} ~ {perfectWindow.End:F5})");
                        }
                        else if (elapsed >= perfectWindowEnd)
                        {
                            Debug.Log($"[PerformTurn] 히트 {CurrentHit} fallback");
                            attackerInputHandler.NotifyWindowClosed(true); // 공격자 입력 실패 처리
                        }
                    }
                    else // AI 공격자 처리
                    {
                        if (elapsed >= aiInputTime)
                        {
                            attackerInputHandler.RecordAIInput(aiInputTime, aiAttackSuccess); // AI 입력 기록                            
                        }
                    }
                }
                if (!CurrentDefenseResultShown && elapsed >= perfectWindowStart)
                {
                    if (defenderInputHandler.IsPlayer)
                    {
                        // 플레이어 입력 대기 UI 표시
                        if (elapsed < perfectWindowEnd)
                        {                            
                            CombatStatusDisplay.Instance.ShowInputPrompt("막아!");
                            Debug.Log($"[UI표시:막아!] 히트 {CurrentHit + 1}, elapsed={elapsed:F5}, 타이밍창=({perfectWindow.start:F5} ~ {perfectWindow.End:F5})");
                        }
                        else
                        {
                            defenderInputHandler.NotifyWindowClosed(true);
                            //CurrentDefenseResultShown = true;
                        }
                    }
                    else // AI 방어자 처리
                    {
                        if (elapsed >= aiInputTime)
                        {
                            defenderInputHandler.RecordAIInput(aiInputTime, aiDefenseSuccess); // AI 입력 기록 
                            //CurrentDefenseResultShown = true;
                        }
                    }
                }
                if(isPlayerAttacker && CurrentAttackResultShown)
                {
                    CombatStatusDisplay.Instance.ShowInputPrompt("V");
                }
                else if (!isPlayerAttacker && CurrentDefenseResultShown)
                {
                    CombatStatusDisplay.Instance.ShowInputPrompt("V");
                }
                

                if (elapsed >= perfectWindowEnd && windowPrompted && CurrentAttackResultShown && CurrentDefenseResultShown && !CurrentClashResultShown)
                {

                    ///////////////////////// 판정 구간 진입 /////////////////////////
                    Debug.Log("[판정 구간 진입] 판정 결과 표시 중...");
                    EvaluateClashResult(); // 클래시 결과 평가                    
                }

                if (elapsed >= perfectWindowEnd && windowPrompted && CurrentAttackResultShown && CurrentDefenseResultShown && !CurrentClashResultShown)
                {
                    Debug.Log($"[히트 전환 조건 통과] Hit={CurrentHit}, 결과 표시됨: 공격자={CurrentAttackResultShown}, 방어자={CurrentDefenseResultShown}, Clash={CurrentClashResultShown}");
                }
                else if (!hasLoggedBlockedReason)
                {
                    Debug.Log($"[히트 전환 BLOCKED] 조건 미충족 - 공격자={CurrentAttackResultShown}, 방어자={CurrentDefenseResultShown}, Clash={CurrentClashResultShown}, WindowEnd={perfectWindowEnd}, Elapsed={elapsed}");
                    hasLoggedBlockedReason = true;

                }

                // 히트 전환
                if (elapsed >= perfectWindowEnd && windowPrompted & CurrentClashResultShown)
                {
                    Debug.Log($"[PerformTurn] isPlayerAttacker:{isPlayerAttacker}, 히트 {CurrentHit} 완료 → 전환, perfectWindowEnd:{perfectWindowEnd}, CurrentClashResultShown:{CurrentClashResultShown}");

                    CombatStatusDisplay.Instance.ShowInputPrompt("");
                    CurrentAttackResultShown = false; // 히트 결과 표시 초기화
                    CurrentDefenseResultShown = false; // 히트 결과 표시 초기화
                    CurrentClashResultShown = false; // 판정 결과 표시 초기화

                    Debug.LogWarning($"[DEBUG] 히트 {CurrentHit} 완료 조건 만족 - windowPrompted false로 전환됨");
                    windowPrompted = false;
                    CurrentHit++;
                }
            }
            yield return null;
        }
          
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");  // 최종 결과 로그
        controller.ReceiveCommandResult(result);    // 커맨드 결과를 컨트롤러에 전달
        if(isPlayerAttacker)
            attackerInputHandler.DisableInput(); // 플레이어 입력 핸들러 비활성화
        else
            defenderInputHandler.DisableInput(); // 적 입력 핸들러 비활성화

        attackerInputHandler.ResetInputState();
        defenderInputHandler.ResetInputState();
    }

    public void OnInputReceivedFromHandler(BaseInputHandler handler)
    {
        bool isPerfect = handler.HasPerfectInput();
        Debug.Log($"[CombatManager] OnInputReceivedFromHandler: handler={handler.GetType().Name}, isPerfect={isPerfect}");

        if (handler == attackerInputHandler)
        {
            attackerPerfectInput = isPerfect; // 플레이어 공격자 입력 처리
            Debug.Log($"[CombatManager] 공격자 입력 수신: {isPerfect}");
            if (!CurrentAttackResultShown)
            {
                ResolveInput(handler, isPerfect);
            }
        }
        else if (handler == defenderInputHandler)
        {
            defenderPerfectInput = isPerfect; // 방어자 입력 처리
            Debug.Log($"[CombatManager] 방어자 입력 수신: {isPerfect}");
            if (!CurrentDefenseResultShown)
            {
                ResolveInput(handler, isPerfect);
            }
        }
    }

    // 플레이어 또는 적이 입력을 성공적으로 처리했을 때 호출 (TimingInputHandler에서 호출됨)
    public void ResolveInput(BaseInputHandler handler, bool isPerfect)
    {
        Debug.Log($"[ResolveInput] 호출됨! handler={handler}, isPerfect ={isPerfect}");

        if (!attackerPerfectInput.HasValue) attackerPerfectInput = false;
        if (!defenderPerfectInput.HasValue) defenderPerfectInput = false;

        bool atk = attackerPerfectInput.Value;
        bool def = defenderPerfectInput.Value;
        
        CurrentResult.SetHitResult(CurrentHit, (bool)attackerPerfectInput); // 공격자만 저장. 용도 애매함.

        // 컨트롤러에 결과 전달
        if (handler == attackerInputHandler) // 공격자 입장(핸들러)
        {
            if (CurrentAttackResultShown)
            {
                Debug.LogWarning("[ResolveInput] 공격자 입력 이미 처리됨 → 무시");
                return;
            }

            attackerInputTime = attackerInputHandler.GetLastInputTime();  // ✅ 공격자 입력 시간 저장
            if (attackerInputHandler.IsPlayer) // 공격자 : 플레이어
                playerController.OnHitResult(CurrentHit, isPerfect);
            
            else // 공격자 : AI
                enemyController.OnHitResult(CurrentHit, isPerfect);

            CurrentAttackResultShown = true; // 히트 결과가 표시되었음을 설정   
        }

        if (handler == defenderInputHandler) // 방어자 입장(핸들러)
        {
            if (CurrentDefenseResultShown)
            {
                Debug.LogWarning("[ResolveInput] 방어자 입력 이미 처리됨 → 무시");
                return;
            }

            defenderInputTime = defenderInputHandler.GetLastInputTime();  // ✅ 방어자 입력 시간 저장
            Debug.Log($"[ResolveInput] defenderInputTime = {defenderInputTime}");
            if (defenderInputHandler.IsPlayer)
                playerController.OnHitResult(CurrentHit, isPerfect);
            else
                enemyController.OnHitResult(CurrentHit, isPerfect);

            CurrentDefenseResultShown = true; // 방어자 결과가 표시되었음을 설정 
        }
    }

    private void EvaluateClashResult()
    {
        bool atkPerfect = attackerPerfectInput ?? false;
        float atkTime = attackerInputTime ?? float.MaxValue;

        bool defPerfect = defenderPerfectInput ?? false;
        float defTime = defenderInputTime ?? float.MaxValue;

        // 방어 커맨드 여부 설정
        bool guard = true; // 이건 해당 히트가 방어 커맨드인 경우만 true로 처리하면 됨.

        var ivr = new InputVersusResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성
        var resultVersus = ivr.GetResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성

        ivr.OnHitVersusResult(CurrentHit, resultVersus); // 히트 결과 UI에 표시
                                                         //////////////////////// 판정 구간 종료 /////////////////////////
        Debug.Log("<==[판정 정보]==>");
        Debug.Log($"[판정 정보]공격자 완벽 입력: {atkPerfect}, 입력 시간: {atkTime} / 방어자 완벽 입력: {defPerfect}, 입력 시간: {defTime}, 방어 커맨드: {guard}");
        Debug.Log($"[판정 정보]InputVersusResult 생성됨: new InputVersusResult({atkPerfect}, {atkTime}, {defPerfect}, {defTime}, {guard})");

        CurrentClashResultShown = true; // 판정 결과가 표시되었음을 설정
        
        // 초기화
        attackerPerfectInput = null;
        defenderPerfectInput = null;
        Debug.Log("[판정 구간 종료] 판정 결과 표시 및 초기화 완료");
    }
    private bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();        
    }

    public void Update()
    {
        CombatStatusDisplay.Instance?.SetPlayerActionInputCooldown(attackerInputHandler.NextAllowedInputTime - Time.time);
        Debug.Log($"[windowPrompted]:{windowPrompted}");
    }
}
