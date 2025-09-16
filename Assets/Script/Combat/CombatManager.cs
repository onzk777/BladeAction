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
    
    // UI에서 접근할 수 있도록 public 프로퍼티 추가
    public PlayerController PlayerController => playerController;
    public EnemyController EnemyController => enemyController;
    
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

    // 현재 턴 지속 시간 (전역 접근 가능)
    public float CurrentTurnDuration { get; private set; } = 0f;

    // CharacterManager를 통해 Combatant 인스턴스 접근

    // 현재 히트 컨텍스트 전역화
    public int CurrentHit { get; private set; } // 현재 히트 인덱스. (연타 공격일 경우 체크용)
    public bool CurrentAttackResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    public bool CurrentDefenseResultShown { get; private set; } = false; // 히트 결과가 표시되었는지 여부
    private bool CurrentClashResultShown = false; // 현재 클래시 결과가 표시되었는지 여부
    public bool windowPrompted { get; private set; } = false; // 히트 윈도우가 열렸는지 여부
    
    // 중단 상태 추적
    private bool isInterrupted = false; // 현재 턴에서 중단이 발생했는지 여부
    
    // 전투 종료 상태 추적
    private bool isBattleEnded = false; // 전투가 종료되었는지 여부
    private BattleResult battleResult; // 전투 결과
    public event System.Action<BattleResult> OnBattleEnded; // 전투 종료 이벤트
    
    // FloatingText 생성 상태 추적 (입력 처리 결과와 분리)
    private bool floatingTextShown = false; // 공격자 FloatingText 생성 여부
    public ICombatController CurrentController { get; private set; } // player/enemy 컨트롤러의 인터페이스
    public CombatantCommandResult CurrentResult { get; private set; } // 현재 커맨드 결과
    public static float CombatStartTime { get; private set; } // 전투 시작 시간 (초 단위 f.)
    public float GetInputDeadline() // 입력 마감 시간 계산
    {
        return CombatStartTime + CurrentTurnDuration - GlobalConfig.Instance.InputBufferEndSeconds;
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
        // EventSystem 상태 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[CombatManager] Start에서 EventSystem이 없습니다!");
        }
        else
        {
            Debug.Log($"[CombatManager] Start에서 EventSystem 활성화 상태: {eventSystem.enabled}");
        }
        
        // 전투 결과 초기화
        battleResult = new BattleResult();
        battleResult.InitializeBattle();
        
        // CharacterManager 초기화 대기 후 Controller 연결
        StartCoroutine(WaitForCharacterManagerAndConnect());
    }

    private System.Collections.IEnumerator WaitForCharacterManagerAndConnect()
    {
        // CharacterManager가 초기화될 때까지 대기
        while (CharacterManager.Instance == null)
        {
            yield return null;
        }

        // Controller 연결
        ConnectControllers();
        
        // 전투 시작
        StartCoroutine(RunCombat());
    }

    private void ConnectControllers() // Controller 연결
    {
        // CharacterManager를 통해 Controller 연결
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.ConnectController(CharacterType.Player, playerController);
            CharacterManager.Instance.ConnectController(CharacterType.Enemy, enemyController);
            Debug.Log("[CombatManager] Controller 연결 완료");
        }
        else
        {
            Debug.LogError("[CombatManager] CharacterManager.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 검술 기반 턴 지속 시간 계산
    /// </summary>
    /// <param name="command">사용할 검술 커맨드</param>
    /// <returns>계산된 턴 지속 시간 (초)</returns>
    private float CalculateTurnDuration(ActionCommandData command)
    {
        if (command == null || command.perfectTimings == null || command.perfectTimings.Count == 0)
        {
            Debug.LogWarning("[CombatManager] 유효하지 않은 커맨드로 기본 턴 지속 시간 사용");
            return 1.0f; // 기본값 1초 사용 (마지막 히트 완료 시간이 없으므로)
        }
        
        // 마지막 히트의 완료 시간 계산
        var lastTiming = command.perfectTimings[command.perfectTimings.Count - 1];
        float lastHitEndTime = lastTiming.start + lastTiming.duration;
        
        // 추가 시간 (기본 0초, GlobalConfig에서 설정 가능)
        float additionalTime = GlobalConfig.Instance.AdditionalTurnDuration; // 기본값 0초
        
        float totalDuration = lastHitEndTime + additionalTime;
        
        Debug.Log($"[CombatManager] 턴 지속 시간 계산 - 마지막 히트 완료: {lastHitEndTime}초, 추가 시간: {additionalTime}초, 총 지속 시간: {totalDuration}초");
        
        return totalDuration;
    }
    
    /// <summary>
    /// 애니메이션 완료 대기 후 다음 턴 시작
    /// </summary>
    /// <param name="nextController">다음 턴을 수행할 컨트롤러</param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationAndStartNextTurn(ICombatController nextController)
    {
        // 기본 턴 전환 대기 시간
        float baseWaitTime = GlobalConfig.Instance.TurnEndBuffer;
        
        // 애니메이션 완료를 위한 추가 대기 시간 (피격 애니메이션 등)
        float animationWaitTime = GlobalConfig.Instance.AnimationWaitTime;
        
        // 총 대기 시간
        float totalWaitTime = baseWaitTime + animationWaitTime;
        
        Debug.Log($"[CombatManager] 턴 전환 대기 - 기본: {baseWaitTime}초, 애니메이션: {animationWaitTime}초, 총: {totalWaitTime}초");
        
        yield return new WaitForSeconds(totalWaitTime);
        
        // 다음 턴 시작
        yield return StartCoroutine(PerformTurn(nextController));
    }


    private IEnumerator RunCombat()
    {
        /////////////////////// 점검용 디버그 로그 ///////////////////////
        Debug.Log($"[RunCombat] CombatStartTime 세팅됨: {CombatStartTime}");
        Debug.Log($"[RunCombat] HandlerInstance: {attackerInputHandler.GetInstanceID()}");
        Debug.Log($"[RunCombat] timingInputHandler InstanceID: {attackerInputHandler.GetInstanceID()}");
        ////////////////////////////////////////////////////////////////

        // 전투 시작 시 첫 번째 검술 버튼에 포커스 설정
        var playerActionSelectUI = FindFirstObjectByType<PlayerActionSelectUI>();
        if (playerActionSelectUI != null)
        {
            Debug.Log("[CombatManager] PlayerActionSelectUI 찾음 - 초기화 후 포커스 설정 시도");
            playerActionSelectUI.Initialize(); // 먼저 초기화 (내부에서 코루틴으로 포커스 설정됨)
        }
        else
        {
            Debug.LogWarning("[CombatManager] PlayerActionSelectUI를 찾을 수 없습니다!");
        }

        while (!isBattleEnded)
        {
            // 전투 종료 조건 체크
            if (isBattleEnded)
            {
                Debug.Log("[RunCombat] 전투가 종료되어 루프를 중단합니다.");
                break;
            }
            
            // 플레이어 턴
            CombatStartTime = Time.time;
            yield return new WaitForSeconds(0.2f); // 첫 턴 시작 전에 살짝 
            yield return StartCoroutine(PerformTurn(playerController));
            
            // 플레이어 턴 후 전투 종료 체크
            if (isBattleEnded)
            {
                Debug.Log("[RunCombat] 플레이어 턴 후 전투 종료 감지");
                break;
            }
            
            // 적 턴 (애니메이션 대기 없이 즉시 시작)
            yield return StartCoroutine(PerformTurn(enemyController));
            
            // 적 턴 후 전투 종료 체크
            if (isBattleEnded)
            {
                Debug.Log("[RunCombat] 적 턴 후 전투 종료 감지");
                break;
            }
        }
        Debug.Log("전투 종료!");
    }

    private IEnumerator PerformTurn(ICombatController controller)
    {
        Debug.Log($"[턴 시작] PerformTurn 호출, currentCommandIndex 초기화");

        // 초기화        
        Combatant actor = controller.Combatant; // 현재 턴을 수행하는 Combatant
        Combatant defender = isPlayerAttacker ? CharacterManager.Instance.EnemyCombatant : CharacterManager.Instance.PlayerCombatant; // 피격자
        int selectedCommandIndex = controller.GetSelectedCommandIndex(); // 선택된 커맨드 인덱스
        ActionCommandData command = actor.AvailableCommands[selectedCommandIndex];
        isPlayerAttacker = (controller.Combatant == CharacterManager.Instance?.PlayerCombatant) ? true : false; // 플레이어 여부      
        CombatantCommandResult result = new CombatantCommandResult(command); // 커맨드 결과 객체 생성
        attackerInputHandler.SetIsPlayer(isPlayerAttacker); // 공격자 입력 핸들러 설정
        defenderInputHandler.SetIsPlayer(!isPlayerAttacker); // 방어자 입력 핸들러 설정
        TurnTimer.Reset(); // 턴 시작 시각 초기화        
        float turnDuration = CalculateTurnDuration(command); // 검술 기반 턴 지속 시간 계산
        CurrentTurnDuration = turnDuration; // 전역 접근 가능하도록 설정
        int hitCount = command.hitCount; // 커맨드의 히트 카운트(연타 공격일 경우 체크용)
        attackerPerfectInput = null; // 공격자 완벽 입력 여부 초기화
        defenderPerfectInput = null; // 방어자 완벽 입력 여부 초기화
        attackerInputTime = null; // 공격자 입력 시간 초기화
        defenderInputTime = null; // 방어자 입력 시간 초기화
        CurrentAttackResultShown = false; // 현재 공격자 결과 표시 여부 초기화
        CurrentDefenseResultShown = false; // 현재 방어자 결과 표시 여부 초기화
        CurrentClashResultShown = false; // 현재 타격 판정 결과 표시 여부 초기화
        windowPrompted = false; // 히트 윈도우가 열렸는지 여부 초기화
        floatingTextShown = false; // 공격자 FloatingText 생성 여부 초기화
        attackerInputHandler.ResetCooldown(); // 공격자 입력 핸들러 쿨다운 초기화
        defenderInputHandler.ResetCooldown(); // 방어자 입력 핸들러 쿨다운 초기화
        CurrentHit = 0; // 현재 히트 인덱스 초기화
        CurrentController = controller; // 현재 컨트롤러 설정
        CurrentResult = result; // 현재 커맨드 결과 설정
        
        // Poise 회복 및 중단 상태 초기화
        actor.ResetPoise(); // 공격 턴 시작 시 Poise 회복
        isInterrupted = false; // 중단 상태 초기화
        
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
        
        // Spine 애니메이션 연동: 공격 턴 시작 시 애니메이션 재생
        if (isPlayerAttacker && playerController != null)
        {
            playerController.OnPlayActionCommand();
        }
        else if (!isPlayerAttacker && enemyController != null)
        {
            enemyController.OnPlayActionCommand();
        }
        
        // 타이밍 윈도우 등록 및 입력 수신 시작
        attackerInputHandler.LoadTimingWindows(command.perfectTimings); // 커맨드의 완벽 타이밍 윈도우를 로드        
        defenderInputHandler.LoadFromOpponentCommand(command); // 적의 커맨드 데이터를 방어자 핸들러에 로드


        bool hasLoggedBlockedReason = false; // 히트 전환 디버깅용, PerformTurn 지역 변수로 선언
        float turnDurationBuffer = 0.02f; // 턴 지속 시간 버퍼 (초 단위, 히트 윈도우가 끝나기 전에 턴이 종료되는 것을 방지하기 위한 용도)

        // 5. 메인 루프 시작
        while (TurnTimer.ElapsedTime < turnDuration + turnDurationBuffer)
        {
            float elapsed = TurnTimer.ElapsedTime; // 현재 경과 시간

            CombatStatusDisplay.Instance?.updateTurnInfo(turnDuration - elapsed);
            
            // 전투 종료 조건 체크 (HP가 0이 되었는지 확인)
            if (isBattleEnded)
            {
                Debug.LogWarning("[PerformTurn] 전투가 종료되어 턴을 중단합니다.");
                break;
            }
            
            // 중단 발생 시 턴 조기 종료
            if (isInterrupted)
            {
                Debug.LogWarning("[PerformTurn] 중단 발생으로 턴이 조기 종료됩니다.");
                break;
            }
            
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

                if (!floatingTextShown && elapsed >= perfectWindowStart) // 공격자 FloatingText 생성
                {
                    // PerfectTiming 시작 시점에 공격자에게만 FloatingText 생성
                    if (FloatingTextManager.Instance != null)
                    {
                        Vector3 textPosition = GetFloatingTextPosition(isPlayerAttacker);
                        FloatingTextManager.Instance.ShowPerfectTimingStart(textPosition, CurrentHit + 1, perfectWindow);
                    }
                    
                    // FloatingText 생성 후 플래그 설정하여 중복 생성 방지
                    floatingTextShown = true;
                }
                
                if (!CurrentAttackResultShown && elapsed >= perfectWindowStart) // 공격자 입력 처리
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
                            // AI 애니메이션은 이미 Perfect Window 시작 시점에 재생됨
                        }
                    }
                }
                
                if (!CurrentDefenseResultShown && elapsed >= perfectWindowStart) // 방어자 입력 처리
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
                        }
                    }
                    else // AI 방어자 처리
                    {
                        if (elapsed >= aiInputTime)
                        {
                            defenderInputHandler.RecordAIInput(aiInputTime, aiDefenseSuccess); // AI 입력 기록 
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
                if (elapsed >= perfectWindowEnd && windowPrompted && CurrentClashResultShown)
                {
                    // PerfectTiming 종료 시점에 FloatingText 생성
                    if (FloatingTextManager.Instance != null)
                    {
                        Vector3 textPosition = GetFloatingTextPosition(isPlayerAttacker);
                        FloatingTextManager.Instance.ShowPerfectTimingEnd(textPosition, CurrentHit + 1, perfectWindow);
                    }
                    
                    Debug.Log($"[PerformTurn] isPlayerAttacker:{isPlayerAttacker}, 히트 {CurrentHit} 완료 → 전환, perfectWindowEnd:{perfectWindowEnd}, CurrentClashResultShown:{CurrentClashResultShown}");

                    CombatStatusDisplay.Instance.ShowInputPrompt("");
                    CurrentAttackResultShown = false; // 히트 결과 표시 초기화
                    CurrentDefenseResultShown = false; // 히트 결과 표시 초기화
                    CurrentClashResultShown = false; // 판정 결과 표시 초기화
                    floatingTextShown = false; // FloatingText 생성 상태 초기화

                    Debug.LogWarning($"[DEBUG] 히트 {CurrentHit} 완료 조건 만족 - windowPrompted false로 전환됨");
                    windowPrompted = false;
                    CurrentHit++;
                    
                    // 모든 히트가 완료되었는지 확인 (5초 턴 지속 시간은 보장)
                    if (CurrentHit >= hitCount)
                    {
                        Debug.Log($"[PerformTurn] 모든 히트 완료! CurrentHit={CurrentHit}, hitCount={hitCount} - 추가 입력 차단, 5초 턴 지속 시간 대기");
                        // break 제거: 턴은 5초까지 지속되어야 함
                    }
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
        
        // 턴 종료 후 애니메이션 완료 대기
        yield return StartCoroutine(WaitForAnimationsComplete(actor, defender));
    }
    
    /// <summary>
    /// 공격자와 피격자의 애니메이션이 모두 완료될 때까지 대기
    /// </summary>
    private IEnumerator WaitForAnimationsComplete(Combatant attacker, Combatant target)
    {
        Debug.Log($"[CombatManager] 애니메이션 완료 대기 시작 - 공격자: {attacker.Name}, 피격자: {target.Name}");
        
        // 공격자와 피격자의 컨트롤러 가져오기
        ICombatController attackerController = isPlayerAttacker ? playerController : enemyController;
        ICombatController defenderController = isPlayerAttacker ? enemyController : playerController;
        
        // 최대 대기 시간 (안전장치)
        float maxWaitTime = 10f;
        float startTime = Time.time;
        
        // 공격자 애니메이션 완료 대기
        yield return StartCoroutine(WaitForControllerAnimationComplete(attackerController, "공격자", maxWaitTime));
        
        // 피격자 애니메이션 완료 대기
        yield return StartCoroutine(WaitForControllerAnimationComplete(defenderController, "피격자", maxWaitTime));
        
        float totalWaitTime = Time.time - startTime;
        Debug.Log($"[CombatManager] 모든 애니메이션 완료 대기 완료 - 총 대기 시간: {totalWaitTime:F2}초");
    }
    
    /// <summary>
    /// 특정 컨트롤러의 애니메이션이 완료될 때까지 대기
    /// </summary>
    private IEnumerator WaitForControllerAnimationComplete(ICombatController controller, string role, float maxWaitTime)
    {
        if (controller == null)
        {
            Debug.LogWarning($"[CombatManager] {role} 컨트롤러가 null입니다. 애니메이션 대기를 건너뜁니다.");
            yield break;
        }
        
        float startTime = Time.time;
        
        // 기본 애니메이션 대기 시간 (GlobalConfig에서 설정)
        float baseWaitTime = GlobalConfig.Instance.AnimationWaitTime;
        
        // 최소 대기 시간 적용
        if (baseWaitTime > 0)
        {
            Debug.Log($"[CombatManager] {role} 기본 애니메이션 대기: {baseWaitTime}초");
            yield return new WaitForSeconds(baseWaitTime);
        }
        
        // 실제 애니메이션 상태 확인을 통한 완료 대기
        yield return StartCoroutine(WaitForActualAnimationComplete(controller, role, maxWaitTime));
        
        float totalWaitTime = Time.time - startTime;
        Debug.Log($"[CombatManager] {role} 애니메이션 대기 완료 - 총 대기 시간: {totalWaitTime:F2}초");
    }
    
    /// <summary>
    /// 실제 Animator 상태를 확인하여 애니메이션 완료를 감지
    /// </summary>
    private IEnumerator WaitForActualAnimationComplete(ICombatController controller, string role, float maxWaitTime)
    {
        // Controller에서 Animator 컴포넌트 가져오기
        Animator animator = GetControllerAnimator(controller);
        if (animator == null)
        {
            Debug.LogWarning($"[CombatManager] {role} Animator를 찾을 수 없습니다. 기본 대기 시간을 사용합니다.");
            yield return new WaitForSeconds(1f); // 기본 1초 대기
            yield break;
        }
        
        float startTime = Time.time;
        float lastAnimationTime = 0f;
        int stableFrameCount = 0;
        const int requiredStableFrames = 3; // 3프레임 동안 안정적이어야 완료로 간주
        
        Debug.Log($"[CombatManager] {role} 실제 애니메이션 상태 모니터링 시작");
        
        while (Time.time - startTime < maxWaitTime)
        {
            // 현재 애니메이션 상태 정보 가져오기
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float currentAnimationTime = stateInfo.normalizedTime;
            
            // 애니메이션이 루프되지 않는 경우 (normalizedTime이 1.0 이상이면 완료)
            if (stateInfo.normalizedTime >= 1.0f)
            {
                Debug.Log($"[CombatManager] {role} 애니메이션 완료 감지 (normalizedTime: {stateInfo.normalizedTime:F2})");
                break;
            }
            
            // 애니메이션 시간이 변하지 않는 경우 (안정 상태)
            if (Mathf.Approximately(currentAnimationTime, lastAnimationTime))
            {
                stableFrameCount++;
                if (stableFrameCount >= requiredStableFrames)
                {
                    Debug.Log($"[CombatManager] {role} 애니메이션 안정 상태 감지 (stableFrames: {stableFrameCount})");
                    break;
                }
            }
            else
            {
                stableFrameCount = 0; // 리셋
            }
            
            lastAnimationTime = currentAnimationTime;
            yield return null; // 다음 프레임까지 대기
        }
        
        // 추가 안전 대기 (애니메이션이 완전히 끝날 때까지)
        float additionalWaitTime = 0.2f;
        Debug.Log($"[CombatManager] {role} 추가 안전 대기: {additionalWaitTime}초");
        yield return new WaitForSeconds(additionalWaitTime);
    }
    
    /// <summary>
    /// Controller에서 Animator 컴포넌트를 가져옵니다
    /// </summary>
    private Animator GetControllerAnimator(ICombatController controller)
    {
        if (controller is PlayerController playerCtrl)
        {
            return playerCtrl.CombatAnimationObject?.GetComponent<Animator>();
        }
        else if (controller is EnemyController enemyCtrl)
        {
            return enemyCtrl.CombatAnimationObject?.GetComponent<Animator>();
        }
        
        return null;
    }
    
    /// <summary>
    /// FloatingText 위치 계산 (2D 프로젝트용)
    /// </summary>
    /// <param name="isPlayerAttacker">플레이어가 공격자인지 여부</param>
    /// <returns>FloatingText가 표시될 월드 위치</returns>
    private Vector3 GetFloatingTextPosition(bool isPlayerAttacker)
    {
        Vector3 basePosition;
        
        // 1. 캐릭터 위치를 기준으로 설정
        if (isPlayerAttacker)
        {
            // 플레이어가 공격자인 경우: 플레이어 위치 근처
            if (playerController != null)
            {
                basePosition = playerController.transform.position;
                Debug.Log($"[FloatingText 위치] 플레이어 공격자 기준 위치: {basePosition}");
            }
            else
            {
                basePosition = Vector3.zero;
                Debug.LogWarning("[FloatingText 위치] playerController가 null입니다!");
            }
        }
        else
        {
            // AI가 공격자인 경우: AI 위치 근처
            if (enemyController != null)
            {
                basePosition = enemyController.transform.position;
                Debug.Log($"[FloatingText 위치] AI 공격자 기준 위치: {basePosition}");
            }
            else
            {
                // AI 컨트롤러가 null인 경우 대체 방법 시도
                basePosition = GetAIPositionFallback();
                Debug.LogWarning("[FloatingText 위치] enemyController가 null입니다! 대체 위치 사용: " + basePosition);
            }
        }
        
        // 2. 캐릭터 위쪽에 오프셋 추가 (기존 2f에서 1.5f로 감소)
        Vector3 finalPosition = basePosition + Vector3.up * 1.5f;
        Debug.Log($"[FloatingText 위치] 오프셋 적용 후: {finalPosition}");
        
        // 3. 화면 밖으로 나가지 않도록 제한
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(finalPosition);
            
            // 화면 경계 내로 제한 (여백 100픽셀)
            screenPos.x = Mathf.Clamp(screenPos.x, 100, Screen.width - 100);
            screenPos.y = Mathf.Clamp(screenPos.y, 100, Screen.height - 100);
            
            // 다시 월드 좌표로 변환
            finalPosition = Camera.main.ScreenToWorldPoint(screenPos);
            
            Debug.Log($"[FloatingText 위치] 화면 경계 제한 후: {finalPosition}");
        }
        
        Debug.Log($"[FloatingText 위치] 최종 위치: {finalPosition}");
        return finalPosition;
    }
    
    /// <summary>
    /// AI 위치를 가져오는 대체 방법
    /// </summary>
    /// <returns>AI의 대략적인 위치</returns>
    private Vector3 GetAIPositionFallback()
    {
        // 1. 씬에서 EnemyController를 찾아보기
        EnemyController[] enemyControllers = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        if (enemyControllers.Length > 0)
        {
            Debug.Log($"[FloatingText 위치] FindObjectsOfType으로 AI 위치 찾음: {enemyControllers[0].transform.position}");
            return enemyControllers[0].transform.position;
        }
        
        // 2. "Enemy" 태그를 가진 GameObject 찾기
        GameObject enemyObject = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyObject != null)
        {
            Debug.Log($"[FloatingText 위치] Enemy 태그로 AI 위치 찾음: {enemyObject.transform.position}");
            return enemyObject.transform.position;
        }
        
        // 3. 플레이어 반대편에 대략적인 위치 설정
        if (playerController != null)
        {
            Vector3 playerPos = playerController.transform.position;
            Vector3 fallbackPos = new Vector3(-playerPos.x, playerPos.y, playerPos.z);
            Debug.Log($"[FloatingText 위치] 플레이어 반대편 대체 위치 사용: {fallbackPos}");
            return fallbackPos;
        }
        
        // 4. 최후의 수단: 화면 중앙
        Vector3 centerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        Debug.Log($"[FloatingText 위치] 화면 중앙 대체 위치 사용: {centerPos}");
        return centerPos;
    }

    public void OnInputReceivedFromHandler(BaseInputHandler handler)
    {
        // 모든 히트가 완료된 경우 입력 무시
        if (CurrentHit >= CurrentResult?.HitCount)
        {
            Debug.Log($"[CombatManager] 모든 히트 완료! CurrentHit={CurrentHit}, HitCount={CurrentResult?.HitCount} - 입력 무시");
            return;
        }

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
        
        // 안전한 범위 체크 추가
        if (CurrentResult != null && CurrentHit >= 0 && CurrentHit < CurrentResult.HitCount)
        {
            CurrentResult.SetHitResult(CurrentHit, (bool)attackerPerfectInput);
        }
        else
        {
            Debug.LogWarning($"[CombatManager] SetHitResult 실패: CurrentHit={CurrentHit}, HitCount={CurrentResult?.HitCount ?? 0}");
        }

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

        // 방어 커맨드 여부 설정 - 실제 막기 상태 사용
        bool guard = defenderInputHandler.IsGuardActive;

        var ivr = new InputVersusResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성
        var resultVersus = ivr.GetResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성

        Debug.Log($"[CombatManager] 판정 결과: {resultVersus} (공격자 완벽: {atkPerfect}, 방어자 완벽: {defPerfect}, 막기: {guard})");

        // 현재 공격자와 방어자 Combatant 찾기
        Combatant attacker = isPlayerAttacker ? CharacterManager.Instance.PlayerCombatant : CharacterManager.Instance.EnemyCombatant;
        Combatant defender = isPlayerAttacker ? CharacterManager.Instance.EnemyCombatant : CharacterManager.Instance.PlayerCombatant;
        
        // 현재 사용된 검술 커맨드 가져오기
        var currentCommand = CurrentResult?.Command;
        if (currentCommand == null)
        {
            Debug.LogError("[CombatManager] 현재 커맨드를 찾을 수 없습니다!");
            return;
        }
        
        // 피해량 계산 및 적용
        ProcessDamageCalculation(attacker, defender, currentCommand, resultVersus, CurrentHit);
        
        // 쳐내기 판정 시 공격자 자세 포인트 감소
        if (resultVersus == InputVersusResult.ResultType.Parry || resultVersus == InputVersusResult.ResultType.HalfParry)
        {
            int poiseDamage = defender.CharacterData.ParryPoiseDamage;
            
            Debug.Log($"[CombatManager] {resultVersus} 판정! {attacker.Name}의 Poise 감소 시작 (현재: {attacker.GetPoiseStatus()}, {defender.Name}의 ParryPoiseDamage: {poiseDamage})");
            
            attacker.LosePoise(poiseDamage); // 쳐내기 당했을 때 Poise 감소
            
            Debug.Log($"[CombatManager] {attacker.Name}의 Poise 감소 완료 (감소 후: {attacker.GetPoiseStatus()})");
            
            // 중단 발생 확인
            if (attacker.IsInterrupted)
            {
                Debug.LogWarning($"[CombatManager] {attacker.Name}의 공격이 중단되었습니다!");
                TriggerInterrupt();
            }
        }
        else
        {
            Debug.Log($"[CombatManager] {resultVersus} 판정 - Poise 감소 없음");
        }

        ivr.OnHitVersusResult(CurrentHit, resultVersus); // 히트 결과 UI에 표시
        
        // 판정 결과에 따른 애니메이션 호출
        HandleClashResultAnimation(resultVersus);
        
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
    
    /// <summary>
    /// 중단 발생 시 처리
    /// </summary>
    private void TriggerInterrupt()
    {
        // 중단 상태 설정
        isInterrupted = true;
        
        // 중단 애니메이션 재생
        if (isPlayerAttacker && playerController != null)
        {
            playerController.OnInterrupted();
        }
        else if (!isPlayerAttacker && enemyController != null)
        {
            enemyController.OnInterrupted();
        }
        
        Debug.LogWarning("[CombatManager] 중단 발생! 턴이 조기 종료됩니다.");
    }
    
    /// <summary>
    /// 클래시 결과에 따른 애니메이션 처리
    /// </summary>
    /// <param name="resultType">판정 결과 타입</param>
    private void HandleClashResultAnimation(InputVersusResult.ResultType resultType)
    {
        switch (resultType)
        {
            case InputVersusResult.ResultType.Hit:
            case InputVersusResult.ResultType.PerfectAttack:
            case InputVersusResult.ResultType.GuardBreak:
                // 방어자가 피격된 경우 - 방어자에게 OnBeHitted 호출
                if (isPlayerAttacker)
                {
                    // 플레이어가 공격자, AI가 방어자인 경우
                    if (enemyController != null)
                    {
                        enemyController.OnBeHitted();
                        Debug.Log("[CombatManager] AI 방어자 피격 애니메이션 호출");
                    }
                }
                else
                {
                    // AI가 공격자, 플레이어가 방어자인 경우
                    if (playerController != null)
                    {
                        playerController.OnBeHitted();
                        Debug.Log("[CombatManager] 플레이어 방어자 피격 애니메이션 호출");
                    }
                }
                break;
                
            case InputVersusResult.ResultType.Parry:
            case InputVersusResult.ResultType.HalfParry:
                // 방어자가 쳐내기 성공한 경우 - 방어자에게 OnSuccessParry 호출
                if (isPlayerAttacker)
                {
                    // 플레이어가 공격자, AI가 방어자인 경우
                    if (enemyController != null)
                    {
                        enemyController.OnSuccessParry();
                        Debug.Log("[CombatManager] AI 방어자 쳐내기 성공 애니메이션 호출");
                    }
                }
                else
                {
                    // AI가 공격자, 플레이어가 방어자인 경우
                    if (playerController != null)
                    {
                        playerController.OnSuccessParry();
                        Debug.Log("[CombatManager] 플레이어 방어자 쳐내기 성공 애니메이션 호출");
                    }
                }
                break;
                
            case InputVersusResult.ResultType.Guard:
                // 방어자가 막아낸 경우 - 방어자에게 OnPlayDefence 호출
                if (isPlayerAttacker)
                {
                    // 플레이어가 공격자, AI가 방어자인 경우
                    if (enemyController != null)
                    {
                        enemyController.OnPlayDefence();
                        Debug.Log("[CombatManager] AI 방어자 방어 애니메이션 호출");
                    }
                }
                else
                {
                    // AI가 공격자, 플레이어가 방어자인 경우
                    if (playerController != null)
                    {
                        playerController.OnPlayDefence();
                        Debug.Log("[CombatManager] 플레이어 방어자 방어 애니메이션 호출");
                    }
                }
                break;
        }
    }
    
    private bool CheckInterruptCondition()
    {
        return InterruptManager.IsInterrupted();        
    }

    /// <summary>
    /// 피해량 계산 및 적용
    /// </summary>
    private void ProcessDamageCalculation(Combatant attacker, Combatant defender, ActionCommandData command, InputVersusResult.ResultType resultType, int hitIndex = 0)
    {
        // ========== 피해량 계산 시작 ==========
        Debug.Log($"\n[피해량 계산] ========== {attacker.Name} → {defender.Name} ==========");
        Debug.Log($"[피해량 계산] 판정: {resultType}, 히트: {hitIndex + 1}");
        
        // 히트별 damageRatio 사용
        float currentHitDamageRatio = command.GetDamageRatio(hitIndex);
        int baseDamage = Mathf.RoundToInt(attacker.ATK * currentHitDamageRatio);
        
        Debug.Log($"[피해량 계산] 기본 피해량: {attacker.ATK} × {currentHitDamageRatio} = {baseDamage}");
        
        // 치명타 판정
        bool isCritical = attacker.IsCriticalHit();
        if (isCritical)
        {
            int criticalDamage = attacker.CalculateCriticalDamage(baseDamage);
            Debug.Log($"[피해량 계산] 치명타 발생! {baseDamage} → {criticalDamage}");
            baseDamage = criticalDamage;
        }
        else
        {
            Debug.Log($"[피해량 계산] 치명타 없음");
        }
        
        // 판정 결과에 따른 피해량 감소 적용
        float damageReduction = GetDamageReduction(resultType);
        int damageAfterReduction = Mathf.RoundToInt(baseDamage * damageReduction);
        Debug.Log($"[피해량 계산] 판정 감소: {baseDamage} × {damageReduction} = {damageAfterReduction}");
        
        // DR 적용 (막기 상태에 따라 다른 DR 사용)
        int effectiveDR;
        if (defenderInputHandler.IsGuardActive)
        {
            effectiveDR = defender.GetGuardEffectiveDR();
        }
        else
        {
            effectiveDR = defender.GetEffectiveDR();
        }
        
        int damageAfterDR = ApplyDefenseReduction(damageAfterReduction, effectiveDR);
        
        // DR 적용 결과 로그
        if (defenderInputHandler.IsGuardActive)
        {
            Debug.Log($"[피해량 계산] 막기 상태 - 막기 DR 적용: {damageAfterReduction} - {effectiveDR} = {damageAfterDR} (기본 DR: {defender.DR}, 막기 보너스: {defender.CharacterData.guardDRBonus}, 임시 보너스: {defender.tempDRBonus})");
        }
        else
        {
            Debug.Log($"[피해량 계산] 일반 상태 - 일반 DR 적용: {damageAfterReduction} - {effectiveDR} = {damageAfterDR} (기본 DR: {defender.DR}, 임시 보너스: {defender.tempDRBonus})");
        }
        
        // 피해량이 0보다 크면 HP 감소 적용
        if (damageAfterDR > 0)
        {
            int oldHP = defender.HP;
            defender.TakeDamage(damageAfterDR);
            int newHP = defender.HP;
            int actualDamage = oldHP - newHP;
            
            Debug.Log($"[피해량 계산] HP 감소: {oldHP} → {newHP} (실제 감소량: {actualDamage})");
            Debug.Log($"[피해량 계산] 최종 결과: {defender.Name}이 {actualDamage} 피해를 받았습니다!");
            
            // HP 0 체크 및 전투 종료 처리 (즉시 체크)
            if (defender.IsDefeated)
            {
                Debug.LogWarning($"[피해량 계산] {defender.Name}이 패배했습니다! (HP: {defender.GetHPStatus()})");
                EndBattle(defender == CharacterManager.Instance.PlayerCombatant ? BattleResult.BattleEndReason.PlayerDefeated : BattleResult.BattleEndReason.EnemyDefeated);
                return; // 피해 처리 후 즉시 종료
            }
        }
        else
        {
            Debug.Log($"[피해량 계산] 피해량이 0이므로 HP 감소 없음");
        }
        
        Debug.Log($"[피해량 계산] ========== 계산 완료 ==========\n");
    }
    
    /// <summary>
    /// 판정 결과에 따른 피해량 감소 비율 반환
    /// </summary>
    private float GetDamageReduction(InputVersusResult.ResultType resultType)
    {
        float reduction;
        
        if (GameRule.Instance == null)
        {
            // GameRule이 없으면 기본값 사용
            switch (resultType)
            {
                case InputVersusResult.ResultType.Parry:
                    reduction = 0f; // 패리: 100% 감소 (완전 무효화)
                    break;
                case InputVersusResult.ResultType.HalfParry:
                    reduction = 0.5f; // 하프패리: 50% 감소
                    break;
                case InputVersusResult.ResultType.Guard:
                    reduction = 0.5f; // 막기: 50% 감소
                    break;
                case InputVersusResult.ResultType.GuardBreak:
                case InputVersusResult.ResultType.PerfectAttack:
                case InputVersusResult.ResultType.Hit:
                default:
                    reduction = 1f; // 일반 명중: 감소 없음
                    break;
            }
            Debug.Log($"[피해량 계산] 기본값 사용 - {resultType}: {reduction} (GameRule 없음)");
        }
        else
        {
            // GameRule에서 피해량 감소 비율 가져오기
            switch (resultType)
            {
                case InputVersusResult.ResultType.Parry:
                    reduction = GameRule.Instance.CalculateParryDamageReduction();
                    break;
                case InputVersusResult.ResultType.HalfParry:
                    reduction = GameRule.Instance.CalculateHalfParryDamageReduction();
                    break;
                case InputVersusResult.ResultType.Guard:
                    reduction = GameRule.Instance.CalculateGuardDamageReduction();
                    break;
                case InputVersusResult.ResultType.GuardBreak:
                    reduction = GameRule.Instance.CalculateGuardBreakDamageReduction();
                    break;
                case InputVersusResult.ResultType.PerfectAttack:
                case InputVersusResult.ResultType.Hit:
                default:
                    reduction = 1f; // 일반 명중: 감소 없음
                    break;
            }
            Debug.Log($"[피해량 계산] GameRule 사용 - {resultType}: {reduction}");
        }
        
        return reduction;
    }
    
    /// <summary>
    /// 막기 시 피해량 감소 비율 반환
    /// </summary>
    private float GetGuardDamageReduction()
    {
        if (GameRule.Instance != null)
        {
            return GameRule.Instance.CalculateGuardDamageReduction();
        }
        
        // GameRule이 없으면 기본값 사용
        return 0.5f; // 50% 감소
    }
    
    /// <summary>
    /// 방어력(DR) 적용하여 최종 피해량 계산
    /// </summary>
    private int ApplyDefenseReduction(int damage, int defenderDR)
    {
        int finalDamage;
        
        if (GameRule.Instance != null)
        {
            finalDamage = GameRule.Instance.CalculateFinalDamage(damage, defenderDR);
            Debug.Log($"[피해량 계산] GameRule DR 적용: {damage} - {defenderDR} = {finalDamage}");
        }
        else
        {
            // GameRule이 없으면 기본 계산
            int minimumDamage = 1; // 기본 최소 피해량
            finalDamage = Mathf.Max(minimumDamage, damage - defenderDR);
            Debug.Log($"[피해량 계산] 기본 DR 적용: {damage} - {defenderDR} = {finalDamage} (최소: {minimumDamage})");
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// 전투 종료 처리 (새로운 방식)
    /// </summary>
    private void EndBattle(BattleResult.BattleEndReason reason)
    {
        if (isBattleEnded) return; // 이미 전투가 종료된 경우 무시
        
        isBattleEnded = true;
        battleResult.EndReason = reason;
        battleResult.EndTime = Time.time;
        
        // 승리자와 패배자 결정
        Combatant winner = null;
        Combatant loser = null;
        string winnerName = "";
        
        if (reason == BattleResult.BattleEndReason.PlayerDefeated)
        {
            winner = CharacterManager.Instance.EnemyCombatant;
            loser = CharacterManager.Instance.PlayerCombatant;
            winnerName = "적";
        }
        else if (reason == BattleResult.BattleEndReason.EnemyDefeated)
        {
            winner = CharacterManager.Instance.PlayerCombatant;
            loser = CharacterManager.Instance.EnemyCombatant;
            winnerName = "플레이어";
        }
        
        // 전투 결과 저장
        battleResult.winner = winner;
        battleResult.loser = loser;
        
        // 캐릭터 비활성화 처리
        DisableCharacters();
        
        // UI에 전투 종료 및 승리자 표시
        string resultMessage = "승리!"; // 승리자에게는 항상 승리 메시지
        CombatStatusDisplay.Instance?.ShowBattleEndResult(winnerName, resultMessage);
        
        // 전투 종료 이벤트 발생
        OnBattleEnded?.Invoke(battleResult);
        
        Debug.Log($"[CombatManager] 전투 종료 - 사유: {reason}, 승리자: {winnerName}, 결과: {resultMessage}");
    }
    
    /// <summary>
    /// 전투 종료 시 캐릭터들 비활성화
    /// </summary>
    private void DisableCharacters()
    {
        // 플레이어 Controller 비활성화
        if (playerController != null)
        {
            playerController.gameObject.SetActive(false);
            Debug.Log("[CombatManager] 플레이어 캐릭터 비활성화");
        }
        
        // 적 Controller 비활성화
        if (enemyController != null)
        {
            enemyController.gameObject.SetActive(false);
            Debug.Log("[CombatManager] 적 캐릭터 비활성화");
        }
        
        // 입력 핸들러 비활성화
        if (attackerInputHandler != null)
        {
            attackerInputHandler.DisableInput();
        }
        
        if (defenderInputHandler != null)
        {
            defenderInputHandler.DisableInput();
        }
    }
    
    /// <summary>
    /// 전투 재시작 시 캐릭터들 재활성화
    /// </summary>
    private void EnableCharacters()
    {
        // 플레이어 Controller 재활성화
        if (playerController != null)
        {
            playerController.gameObject.SetActive(true);
            Debug.Log("[CombatManager] 플레이어 캐릭터 재활성화");
        }
        
        // 적 Controller 재활성화
        if (enemyController != null)
        {
            enemyController.gameObject.SetActive(true);
            Debug.Log("[CombatManager] 적 캐릭터 재활성화");
        }
    }
    
    /// <summary>
    /// 전투 처음부터 다시 시작 (UI 버튼에서 호출)
    /// </summary>
    public void RestartBattle()
    {
        Debug.Log("[CombatManager] 재시작 버튼 클릭됨!");
        Debug.Log("[CombatManager] 전투 다시 시작 요청");
        
        // EventSystem 상태 확인
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("[CombatManager] EventSystem이 없습니다!");
        }
        else
        {
            Debug.Log($"[CombatManager] EventSystem 활성화 상태: {eventSystem.enabled}");
        }
        
        // 1. 전투 상태 초기화
        isBattleEnded = false;
        isInterrupted = false;
        CurrentHit = 0;
        CurrentAttackResultShown = false;
        CurrentDefenseResultShown = false;
        CurrentClashResultShown = false;
        windowPrompted = false;
        floatingTextShown = false;
        
        // 2. 입력 핸들러 초기화
        attackerInputHandler?.ResetInputState();
        defenderInputHandler?.ResetInputState();
        attackerInputHandler?.DisableInput();
        defenderInputHandler?.DisableInput();
        
        // 3. 캐릭터 재활성화 및 스테이터스 초기화
        EnableCharacters();
        
        if (CharacterManager.Instance != null)
        {
            // 플레이어 스테이터스 초기화
            var playerCombatant = CharacterManager.Instance.PlayerCombatant;
            if (playerCombatant != null)
            {
                playerCombatant.InitializeRuntimeStats();
                Debug.Log($"[CombatManager] 플레이어 스테이터스 초기화 - HP: {playerCombatant.GetHPStatus()}, Poise: {playerCombatant.GetPoiseStatus()}");
            }
            
            // 적 스테이터스 초기화
            var enemyCombatant = CharacterManager.Instance.EnemyCombatant;
            if (enemyCombatant != null)
            {
                enemyCombatant.InitializeRuntimeStats();
                Debug.Log($"[CombatManager] 적 스테이터스 초기화 - HP: {enemyCombatant.GetHPStatus()}, Poise: {enemyCombatant.GetPoiseStatus()}");
            }
        }
        
        // 4. UI 초기화
        CombatStatusDisplay.Instance?.ClearResults();
        CombatStatusDisplay.Instance?.ShowInputPrompt("전투 다시 시작!");
        
        // 5. 전투 결과 초기화
        battleResult = new BattleResult();
        battleResult.InitializeBattle();
        
        // 6. 전투 재시작
        StopAllCoroutines();
        StartCoroutine(RunCombat());
        
        Debug.Log("[CombatManager] 전투 다시 시작 완료");
    }

    public void Update()
    {
        CombatStatusDisplay.Instance?.SetPlayerActionInputCooldown(attackerInputHandler.NextAllowedInputTime - Time.time);
    }
    
    /// <summary>
    /// 플레이어 컨트롤러를 반환합니다
    /// </summary>
    public PlayerController GetPlayerController()
    {
        return playerController;
    }
    
    /// <summary>
    /// 적 컨트롤러를 반환합니다
    /// </summary>
    public EnemyController GetEnemyController()
    {
        return enemyController;
    }
}
