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
    public CharacterHitSystem GetCharacterHitSystemForDefender()
    {
        return defenderInputHandler != null ? defenderInputHandler.CharacterHitSystem : null;
    }
    
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
    

    
    // 발사체 발사 상태 추적
    private bool[] projectileLaunched; // 각 히트별 발사 상태
    
    // 🆕 히트당 판정 한 번만 발생하도록 추적
    private bool[] hitJudgmentCompleted; // 각 히트별 판정 완료 상태
    
    // 🆕 중복 판정 추적을 위한 카운터
    private int[] hitJudgmentCount; // 각 히트별 판정 발생 횟수
    
    // ❌ 제거: 턴 종료 플래그들 (PerformTurn에서 직접 처리)
    // private bool turnEndRequested = false;
    // private bool isWaitingForTurnEnd = false;
    
    // 🆕 발사체 완료 카운팅
    // ❌ 제거: 발사체 완료 추적 변수 (시간 기반 턴 종료로 변경)
    // private int completedProjectiles = 0;
    // private int totalProjectiles = 0;

    // 현재 턴 지속 시간 (전역 접근 가능)
    public float CurrentTurnDuration { get; private set; } = 0f;
    
    // 현재 턴 번호 (BT에서 사용)
    public int CurrentTurnNumber { get; private set; } = 1;
    
    // 공격 턴 여부 (BT에서 사용)
    public bool IsNPCAttackTurn => !isPlayerAttacker;
    public bool IsPlayerAttackTurn => isPlayerAttacker;

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

        // 🆕 전투 시작 시 CombatStartDelay 적용
        yield return new WaitForSeconds(GlobalConfig.Instance.CombatStartDelay);
        
        // 🆕 BT 상태 리셋 (새 전투 시작 시)
        ResetBehaviorTreeStates();
        
        // ❌ 제거: 턴 종료 대기 중 체크 (PerformTurn에서 직접 처리)
        // if (isWaitingForTurnEnd)
        // {
        //     Debug.Log("[RunCombat] 턴 종료 대기 중 - 새로운 턴 시작 차단");
        //     yield break;
        // }
        
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
            CurrentTurnNumber++; // 턴 번호 증가
            Debug.Log($"[RunCombat] 턴 {CurrentTurnNumber} 시작 - 플레이어 턴");
            // ❌ 제거: CombatStartDelay는 전투 시작 시에만 적용되어야 함
            // yield return new WaitForSeconds(GlobalConfig.Instance.CombatStartDelay); // 전투 시작 후 대기 시간
            yield return StartCoroutine(PerformTurn(playerController));
            
            // 플레이어 턴 후 전투 종료 체크
            if (isBattleEnded)
            {
                Debug.Log("[RunCombat] 플레이어 턴 후 전투 종료 감지");
                break;
            }
            
            // 적 턴 (애니메이션 대기 없이 즉시 시작)
            Debug.Log($"[RunCombat] 턴 {CurrentTurnNumber} 계속 - 적 턴");
            yield return StartCoroutine(PerformTurn(enemyController));
            
            // 적 턴 종료 후 NPC 확률 리셋 (BT 효과 제거)
            ResetNPCProbabilities();
            
            // 적 턴 종료 후 선택 캐시 초기화 (PerformTurn 시작 시에도 리셋하므로 이중 안전장치)
            // if (enemyController != null)
            // {
            //     enemyController.ResetSelectionCache();
            // }
            
            // 적 턴 후 전투 종료 체크
            if (isBattleEnded)
            {
                Debug.Log("[RunCombat] 적 턴 후 전투 종료 감지");
                break;
            }
            
            // 🆕 디버그: 턴 완료 확인
            Debug.Log($"[RunCombat] ========== 턴 {CurrentTurnNumber} 완료 - 다음 턴으로 ==========");
            Debug.Log($"[RunCombat] 플레이어 HP: {CharacterManager.Instance.PlayerCombatant.HP}, 적 HP: {CharacterManager.Instance.EnemyCombatant.HP}");
            Debug.Log($"[RunCombat] isBattleEnded: {isBattleEnded}");
        }
        Debug.Log("전투 종료!");
    }
    
    /// <summary>
    /// 모든 캐릭터의 BT 실행 상태를 리셋 (새 전투 시작 시)
    /// 
    /// 블랙보드 패턴:
    /// - Combatant의 Blackboard.ResetCombat() 호출
    /// - BT 자체는 상태가 없으므로 리셋 불필요
    /// </summary>
    private void ResetBehaviorTreeStates()
    {
        if (CharacterManager.Instance != null)
        {
            // 플레이어 블랙보드 리셋
            var playerCombatant = CharacterManager.Instance.PlayerCombatant as PlayerCombatant;
            if (playerCombatant != null)
            {
                playerCombatant.ResetBlackboard();
                Debug.Log("[CombatManager] 플레이어 블랙보드 리셋 완료");
            }
            
            // 적 블랙보드 리셋
            var enemyCombatant = CharacterManager.Instance.EnemyCombatant as EnemyCombatant;
            if (enemyCombatant != null)
            {
                enemyCombatant.ResetBlackboard();
                Debug.Log("[CombatManager] 적 블랙보드 리셋 완료");
            }
            
            Debug.Log("[CombatManager] 모든 BT 블랙보드 리셋 완료");
        }
        else
        {
            Debug.LogWarning("[CombatManager] CharacterManager.Instance가 null입니다 - BT 상태 리셋 건너뜀");
        }
    }
    
    /// <summary>
    /// NPC의 런타임 확률을 원본으로 리셋합니다 (턴 종료 시 호출)
    /// 
    /// 역할:
    /// - BT가 해당 턴에 적용한 확률 조정을 모두 제거
    /// - CharacterData의 원본 확률로 복원
    /// 
    /// 호출 시점:
    /// - 각 턴 종료 후 (RunCombat에서 호출)
    /// - 다음 턴 시작 시 BT가 다시 새롭게 확률을 조정함
    /// </summary>
    private void ResetNPCProbabilities()
    {
        if (CharacterManager.Instance != null)
        {
            // 적 Combatant의 확률 리셋
            var enemyCombatant = CharacterManager.Instance.EnemyCombatant as EnemyCombatant;
            if (enemyCombatant != null)
            {
                enemyCombatant.ResetProbabilities();
                Debug.Log("[CombatManager] 적 NPC 확률 리셋 완료");
            }
            
            // TODO: 플레이어도 NPC일 수 있다면 여기에 추가
            // var playerCombatant = CharacterManager.Instance.PlayerCombatant as EnemyCombatant;
            // if (playerCombatant != null)
            // {
            //     playerCombatant.ResetProbabilities();
            // }
        }
        else
        {
            Debug.LogWarning("[CombatManager] CharacterManager.Instance가 null입니다 - NPC 확률 리셋 건너뜀");
        }
    }

    private IEnumerator PerformTurn(ICombatController controller)
    {
        Debug.Log($"[턴 시작] PerformTurn 호출, currentCommandIndex 초기화");
        
        // 초기화 (순서 중요! isPlayerAttacker를 먼저 계산해야 defender가 올바름)
        Combatant actor = controller.Combatant; // 현재 턴을 수행하는 Combatant (공격자)
        isPlayerAttacker = (controller.Combatant == CharacterManager.Instance?.PlayerCombatant) ? true : false; // 플레이어 여부 (먼저 계산!)
        Combatant defender = isPlayerAttacker ? CharacterManager.Instance.EnemyCombatant : CharacterManager.Instance.PlayerCombatant; // 피격자 (isPlayerAttacker 사용)
        
        // 🆕 BT 평가 (공격자 + 방어자 모두!)
        // 왜 필요한가?
        // - BT Condition에서 isAttackTurn을 체크하려면 방어 턴에도 평가되어야 함
        // - 예: "방어 턴이면서 HP < 50%면 막기 확률 100%"
        
        Debug.Log($"[CombatManager] 🌳 BT 평가 시작 - 공격자: {actor.Name}, 방어자: {defender.Name}");
        
        // 1. 공격자 BT 평가 (isAttackTurn = true)
        if (actor is PlayerCombatant playerActor)
        {
            Debug.Log($"[CombatManager]   → Player 공격 턴 BT 평가");
            playerActor.ResetBTEvaluation();
            playerActor.ExecuteBehaviorTrees();
        }
        else if (actor is EnemyCombatant enemyActor)
        {
            Debug.Log($"[CombatManager]   → Enemy 공격 턴 BT 평가");
            enemyActor.ResetBTEvaluation();
            enemyActor.ExecuteBehaviorTrees();
        }
        
        // 2. 방어자 BT 평가 (isAttackTurn = false)
        if (defender is PlayerCombatant playerDefender)
        {
            Debug.Log($"[CombatManager]   → Player 방어 턴 BT 평가");
            playerDefender.ResetBTEvaluation();
            playerDefender.ExecuteBehaviorTrees();
        }
        else if (defender is EnemyCombatant enemyDefender)
        {
            Debug.Log($"[CombatManager]   → Enemy 방어 턴 BT 평가");
            enemyDefender.ResetBTEvaluation();
            enemyDefender.ExecuteBehaviorTrees();
        }
        
        // 3. Enemy 공격 턴이면 선택 캐시 리셋
        if (controller is EnemyController enemyCtrl)
        {
            enemyCtrl.ResetSelectionCache();
        }

        // 검술 선택 (공격자만 필요, BT는 이미 평가됨!)
        int selectedCommandIndex = controller.GetSelectedCommandIndex();
        ActionCommandData command = actor.AvailableCommands[selectedCommandIndex];
        
        // Enemy 턴일 때 UI 업데이트 (선택된 검술 표시)
        if (!isPlayerAttacker)
        {
            var enemyUI = FindFirstObjectByType<EnemyActionSelectUI>();
            if (enemyUI != null)
            {
                enemyUI.SetSelectedButton(selectedCommandIndex);
                Debug.Log($"[CombatManager] Enemy UI 업데이트 - 선택된 검술: {selectedCommandIndex}번");
            }
        }      
        CombatantCommandResult result = new CombatantCommandResult(command); // 커맨드 결과 객체 생성
        attackerInputHandler.SetIsPlayer(isPlayerAttacker); // 공격자 입력 핸들러 설정
        defenderInputHandler.SetIsPlayer(!isPlayerAttacker); // 방어자 입력 핸들러 설정
        Debug.Log($"[InputTrace][Turn] SetIsPlayer - attacker:{attackerInputHandler.IsPlayer} defender:{defenderInputHandler.IsPlayer} (isPlayerAttacker:{isPlayerAttacker}) Time:{Time.time:F4} Frame:{Time.frameCount}");
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
        
        // Perfect Timing 가이드 표시
        CombatStatusDisplay.Instance.ShowPerfectTimingGuides(command, turnDuration);

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
        
        // 🆕 방어자 입력도 항상 활성화 (AI 막기 시스템을 위해)
        // BaseInputHandler의 isListening 상태를 확인하기 위해 EnableInput을 다시 호출
        defenderInputHandler.EnableInput();
        Debug.Log("[CombatManager] 🆕 방어자 입력 추가 활성화 (AI 막기 시스템)");

        // 1.1. 커맨드 유효성 확인
        if (selectedCommandIndex < 0 || selectedCommandIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[{actor.Name}] 선택 인덱스가 유효하지 않습니다: {selectedCommandIndex}");
            yield break;  // 잘못된 인덱스면 턴 건너뜀
        }

        CombatStatusDisplay.Instance.ShowCommandStart(isPlayerAttacker, command.commandName); // 3. 커맨드 시작 표시
        CombatStatusDisplay.Instance.ShowInputPrompt("입력 대기"); // 입력 프롬프트 표시
        Debug.Log($"[InputTrace][Turn] PerformTurn Start - actor:{actor.Name}, defender:{defender.Name}, Time:{Time.time:F4}, Frame:{Time.frameCount}");
        
        // ❌ 제거: 턴 종료 플래그 초기화 (PerformTurn에서 직접 처리)
        // turnEndRequested = false;
        // isWaitingForTurnEnd = false;
        
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
        // ❌ 제거: 발사체 기반 시스템에서는 방어자가 공격자 커맨드 데이터를 로드할 필요 없음
        // defenderInputHandler.LoadFromOpponentCommand(command);
        
        // 🆕 발사체 발사 상태 배열 초기화
        projectileLaunched = new bool[command.hitCount];
        for (int i = 0; i < projectileLaunched.Length; i++)
        {
            projectileLaunched[i] = false;
        }
        
        // 🆕 히트 판정 완료 상태 배열 초기화
        hitJudgmentCompleted = new bool[command.hitCount];
        for (int i = 0; i < hitJudgmentCompleted.Length; i++)
        {
            hitJudgmentCompleted[i] = false;
        }
        
        // 🆕 히트 판정 횟수 배열 초기화
        hitJudgmentCount = new int[command.hitCount];
        for (int i = 0; i < hitJudgmentCount.Length; i++)
        {
            hitJudgmentCount[i] = 0;
        }


        bool hasLoggedBlockedReason = false; // 히트 전환 디버깅용, PerformTurn 지역 변수로 선언
        float turnDurationBuffer = 0.02f; // 턴 지속 시간 버퍼 (초 단위, 히트 윈도우가 끝나기 전에 턴이 종료되는 것을 방지하기 위한 용도)

        // 5. 메인 루프 시작
        while (TurnTimer.ElapsedTime < turnDuration + turnDurationBuffer)
        {
            float elapsed = TurnTimer.ElapsedTime; // 현재 경과 시간
            float remaining = turnDuration - elapsed; // 잔여 시간

            // 턴 타이머 UI 업데이트 (잔여 시간, 전체 시간)
            CombatStatusDisplay.Instance?.updateTurnInfo(remaining, turnDuration);
            
            // ❌ 제거: 턴 종료 플래그 확인 (PerformTurn에서 직접 처리)
            // if (turnEndRequested)
            // {
            //     Debug.Log("[PerformTurn] 턴 종료 요청됨 - 턴 종료");
            //     break;
            // }
            
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
                
                // 남은 히트 판정 강제 완료 (무한 대기 방지)
                ForceCompleteRemainingHits(CurrentHit, hitCount);
                
                break;
            }
            
            if (CheckInterruptCondition())
            {
                Debug.Log("턴이 중단되었습니다.");
                
                // 남은 히트 판정 강제 완료 (무한 대기 방지)
                ForceCompleteRemainingHits(CurrentHit, hitCount);
                
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
                float aiInputTime = perfectWindowStart; // AI 방어 시도 시간 (즉시)
                bool aiAttackSuccess = Random.value < globalConfig.NpcAttackPerfectRate; // AI 공격 성공 여부
                bool aiDefenseSuccess = Random.value < GlobalConfig.Instance.NpcParryPerfectRate; // AI 방어 성공 여부
                

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
                    // ❌ 제거: 발사체 기반 시스템에서는 방어자 타이밍 윈도우 등록 불필요
                    // defenderInputHandler.RegisterHitTiming(perfectWindow);
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
                            // 🆕 플레이어 공격자 실패 시에도 ResolveInput 호출
                            ResolveInput(attackerInputHandler, false);
                        }
                    }
                    else // AI 공격자 처리
                    {
                        if (elapsed >= aiInputTime)
                        {
                            attackerInputHandler.RecordAIInput(aiInputTime, aiAttackSuccess); // AI 입력 기록
                            // 🆕 AI 공격자 성공 시 ResolveInput 호출
                            ResolveInput(attackerInputHandler, aiAttackSuccess);
                        }
                        else if (elapsed >= perfectWindowEnd)
                        {
                            // 🆕 AI 공격자 실패 시 ResolveInput 호출
                            ResolveInput(attackerInputHandler, false);
                        }
                    }
                }
                
                // ❌ 제거: 발사체 기반 시스템에서는 방어자 입력 처리가 발사체 트리거로 대체됨
                // if (!CurrentDefenseResultShown && elapsed >= perfectWindowStart) // 방어자 입력 처리
                // {
                //     // 타이밍 윈도우 기반 방어자 입력 처리 로직 제거
                // }
                if(isPlayerAttacker && CurrentAttackResultShown)
                {
                    CombatStatusDisplay.Instance.ShowInputPrompt("V");
                }
                else if (!isPlayerAttacker && CurrentDefenseResultShown)
                {
                    CombatStatusDisplay.Instance.ShowInputPrompt("V");
                }
                

                // ❌ 제거: PerformTurn에서 발사체 발사 로직 제거 (ResolveInput에서 처리)
                // 발사체 발사는 ResolveInput을 통해 통합 처리

                if (elapsed >= perfectWindowEnd && windowPrompted && CurrentAttackResultShown && CurrentDefenseResultShown && !CurrentClashResultShown)
                {
                    Debug.Log($"[히트 전환 조건 통과] Hit={CurrentHit}, 결과 표시됨: 공격자={CurrentAttackResultShown}, 방어자={CurrentDefenseResultShown}, Clash={CurrentClashResultShown}");
                }
                else if (!hasLoggedBlockedReason)
                {
                    Debug.Log($"[히트 전환 BLOCKED] 조건 미충족 - 공격자={CurrentAttackResultShown}, 방어자={CurrentDefenseResultShown}, Clash={CurrentClashResultShown}, WindowEnd={perfectWindowEnd}, Elapsed={elapsed}");
                    hasLoggedBlockedReason = true;

                }

                // 🆕 발사체 기반 히트 전환 (액션 커맨드 타이밍에 따라)
                if (elapsed >= perfectWindowEnd && windowPrompted)
                {
                    // PerfectTiming 종료 시점에 FloatingText 생성
                    if (FloatingTextManager.Instance != null)
                    {
                        Vector3 textPosition = GetFloatingTextPosition(isPlayerAttacker);
                        FloatingTextManager.Instance.ShowPerfectTimingEnd(textPosition, CurrentHit + 1, perfectWindow);
                    }
                    
                    Debug.Log($"[PerformTurn] 🆕 발사체 기반 히트 {CurrentHit} 완료 → 전환, CurrentClashResultShown:{CurrentClashResultShown}");

                    CombatStatusDisplay.Instance.ShowInputPrompt("");
                    CurrentAttackResultShown = false; // 히트 결과 표시 초기화
                    CurrentDefenseResultShown = false; // 히트 결과 표시 초기화
                    CurrentClashResultShown = false; // 판정 결과 표시 초기화
                    floatingTextShown = false; // FloatingText 생성 상태 초기화

                    Debug.LogWarning($"[DEBUG] 🆕 발사체 기반 히트 {CurrentHit} 완료 조건 만족 - windowPrompted false로 전환됨");
                    windowPrompted = false;
                    CurrentHit++;
                    
                    // 모든 히트가 완료되었는지 확인
                    if (CurrentHit >= command.hitCount)
                    {
                        Debug.Log($"[PerformTurn] 모든 히트 완료! CurrentHit={CurrentHit}, hitCount={command.hitCount} - 마지막 히트 판정 확인");
                        
                        // 🆕 마지막 히트의 판정이 발생했는지 확인 (발사체 기반)
                        if (hitJudgmentCompleted[CurrentHit - 1]) // 마지막 히트의 판정 완료 확인
                        {
                            Debug.Log($"[PerformTurn] 마지막 히트 발사체 기반 판정 완료 - 턴 종료 대기 시작");
                            yield return new WaitForSeconds(GlobalConfig.Instance.TurnEndBuffer);
                            Debug.Log($"[PerformTurn] 턴 종료 대기 완료 - 턴 종료");
                            break; // 턴 종료
                        }
                        else
                        {
                            Debug.Log($"[PerformTurn] 마지막 히트 발사체 기반 판정 대기 중...");
                        }
                    }
                }
            }
            yield return null;
        }
          
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");  // 최종 결과 로그
        Debug.Log($"[InputTrace][Turn] PerformTurn End - actor:{actor.Name}, Time:{Time.time:F4}, Frame:{Time.frameCount}");
        Debug.Log($"[PerformTurn] 🔵 메인 루프 종료 - {actor.Name} 턴 완료");
        controller.ReceiveCommandResult(result);    // 커맨드 결과를 컨트롤러에 전달

        // 1) 모든 히트에 대한 최종 적중 판정이 완료될 때까지 대기
        yield return StartCoroutine(EnsureAllHitJudgmentsCompleted(command.hitCount));
        Debug.Log("[CombatManager] 🆕 EnsureAllHitJudgmentsCompleted 완료 - 턴 종료 버퍼 대기 시작");

        // 3) 턴 종료 버퍼 시간 대기
        float turnEndBuffer = GlobalConfig.Instance.TurnEndBuffer;
        if (turnEndBuffer > 0f)
        {
            Debug.Log($"[InputTrace][Turn] Waiting TurnEndBuffer - duration:{turnEndBuffer:F4}s, time:{Time.time:F4}");
            yield return new WaitForSeconds(turnEndBuffer);
        }
        Debug.Log("[CombatManager] 🆕 턴 종료 버퍼 대기 완료 - 입력 비활성화 시작");

        // 2) 입력 비활성화 및 상태 초기화
        Debug.Log($"[CombatManager] 🆕 턴 종료 - 입력 비활성화 시작 (isPlayerAttacker:{isPlayerAttacker})");
        
        // 🆕 턴 종료 시 공격자와 방어자 모두 비활성화
        Debug.Log("[CombatManager] 🆕 공격자 입력 비활성화");
        attackerInputHandler.DisableInput();
        
        Debug.Log("[CombatManager] 🆕 방어자 입력 비활성화");
        defenderInputHandler.DisableInput();

        attackerInputHandler.ResetInputState();
        defenderInputHandler.ResetInputState();

        // 4) 애니메이션 완료 대기
        yield return StartCoroutine(WaitForAnimationsComplete(actor, defender));
    }

    private IEnumerator EnsureAllHitJudgmentsCompleted(int hitCount)
    {
        // hitCount가 0이면 즉시 반환
        if (hitCount <= 0)
        {
            Debug.Log($"[CombatManager] 🔍 hitCount가 0 이하 - 대기 생략");
            yield break;
        }
        
        float waitStart = Time.time;
        Debug.Log($"[CombatManager] 🔍 === Hit 판정 완료 대기 시작 === hitCount:{hitCount}");
        
        // 초기 상태 확인
        Debug.Log($"[CombatManager] 🔍 초기 상태 체크:");
        for (int i = 0; i < hitCount; i++)
        {
            bool isCompleted = (i < hitJudgmentCompleted.Length) && hitJudgmentCompleted[i];
            Debug.Log($"  - Hit {i}: {(isCompleted ? "✅ 이미 완료" : "⏳ 대기 중")}");
        }
        
        float lastLogTime = waitStart;
        int frameCount = 0;
        
        while (!AreAllHitJudgmentsCompleted(hitCount))
        {
            frameCount++;
            float waited = Time.time - waitStart;
            
            // 1초마다 상태 로그
            if (Time.time - lastLogTime >= 1.0f)
            {
                Debug.Log($"[CombatManager] 🔍 대기 중... 경과: {waited:F2}초, 프레임: {frameCount}");
                for (int i = 0; i < hitCount; i++)
                {
                    if (i >= hitJudgmentCompleted.Length || !hitJudgmentCompleted[i])
                    {
                        Debug.Log($"  ⏳ Hit {i}: 미완료 (발사체 충돌 대기)");
                    }
                }
                lastLogTime = Time.time;
            }
            
            yield return null;
        }
        
        float finalWait = Time.time - waitStart;
        Debug.Log($"[CombatManager] 🔍 === Hit 판정 완료 대기 종료 === 대기 시간: {finalWait:F4}초, 프레임: {frameCount}");
    }

    private bool AreAllHitJudgmentsCompleted(int hitCount)
    {
        if (hitJudgmentCompleted == null)
        {
            Debug.Log($"[CombatManager] 🆕 hitJudgmentCompleted 배열이 null - hitCount:{hitCount}");
            return false;
        }
        
        for (int i = 0; i < hitCount; i++)
        {
            if (i >= hitJudgmentCompleted.Length || !hitJudgmentCompleted[i])
            {
                Debug.Log($"[CombatManager] 🆕 Hit {i} 판정 미완료 - 배열길이:{hitJudgmentCompleted.Length}, 완료상태:{hitJudgmentCompleted[i]}");
                return false;
            }
        }
        
        Debug.Log($"[CombatManager] 🆕 모든 Hit 판정 완료 확인 - hitCount:{hitCount}");
        return true;
    }
    
    /// <summary>
    /// 남은 히트 판정을 강제로 완료 처리합니다.
    /// 
    /// 역할:
    /// - 중단 발생 시 더 이상 발사되지 않을 히트들을 "완료"로 표시
    /// - EnsureAllHitJudgmentsCompleted()의 무한 대기를 방지
    /// 
    /// 호출 시점:
    /// - isInterrupted = true일 때
    /// - CheckInterruptCondition()이 true일 때
    /// 
    /// 예시:
    /// - hitCount = 3, currentHit = 1 (2번째 히트 중단)
    /// - hitJudgmentCompleted[1], [2]를 강제로 true로 설정
    /// </summary>
    /// <param name="currentHit">현재 히트 인덱스</param>
    /// <param name="totalHits">총 히트 수</param>
    private void ForceCompleteRemainingHits(int currentHit, int totalHits)
    {
        if (hitJudgmentCompleted == null)
        {
            Debug.LogWarning("[중단] hitJudgmentCompleted 배열이 null - 강제 완료 생략");
            return;
        }
        
        Debug.Log($"[중단] 남은 히트 판정 강제 완료 시작: Hit {currentHit} ~ {totalHits - 1}");
        
        int completedCount = 0;
        for (int i = currentHit; i < totalHits; i++)
        {
            if (i < hitJudgmentCompleted.Length)
            {
                if (!hitJudgmentCompleted[i])
                {
                    hitJudgmentCompleted[i] = true;
                    completedCount++;
                    Debug.Log($"  - Hit {i}: 강제 완료 처리");
                }
                else
                {
                    Debug.Log($"  - Hit {i}: 이미 완료됨");
                }
            }
        }
        
        Debug.Log($"[중단] 강제 완료 처리 완료 - {completedCount}개 히트 처리됨");
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
            // 🆕 발사체 기반 방어자 입력 처리
            defenderPerfectInput = isPerfect; // 방어자 입력 처리
            Debug.Log($"[CombatManager] 발사체 기반 방어자 입력 수신: {isPerfect}");
            
            // 🆕 발사체 기반에서는 즉시 판정하지 않고, 발사체 충돌 시에만 판정
            // ResolveInput 호출 제거 (발사체 충돌 시에만 판정 발생)
            // 방어자 입력은 발사체 기반으로만 처리됨
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
            {
                playerController.OnHitResult(CurrentHit, isPerfect);
            }
            else // 공격자 : AI
            {
                enemyController.OnHitResult(CurrentHit, isPerfect);
            }

            CurrentAttackResultShown = true; // 히트 결과가 표시되었음을 설정
            
            // 🆕 공격자 입력 처리 시 발사체 발사 (성공/실패 무관)
            Debug.Log($"[CombatManager] 공격자 입력 처리 완료 - 발사체 발사: 히트 {CurrentHit}, 완벽 입력: {isPerfect}");
            CreateProjectileForCurrentHit(isPerfect);
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

    /// <summary>
    /// 전투 판정을 처리하는 메서드
    /// </summary>
    /// <param name="hitIndex">히트 인덱스 (발사체에서 전달받은 값)</param>
    private void EvaluateClashResult(int hitIndex)
    {
        bool atkPerfect = attackerPerfectInput ?? false;
        float atkTime = attackerInputTime ?? float.MaxValue;

        bool defPerfect = defenderPerfectInput ?? false;
        float defTime = defenderInputTime ?? float.MaxValue;

        // 방어 커맨드 여부 설정 - 실제 막기 상태 사용
        bool guard = defenderInputHandler.IsGuardActive;

        var ivr = new InputVersusResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성
        var resultVersus = ivr.GetResult(atkPerfect, atkTime, defPerfect, defTime, guard); // 입력 결과 생성

        Debug.Log($"[CombatManager] 판정 결과: {resultVersus} (공격자 완벽: {atkPerfect}, 방어자 완벽: {defPerfect}, 막기: {guard}) - 히트 {hitIndex}");

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
        
        // 피해량 계산 및 적용 - 올바른 히트 인덱스 사용
        ProcessDamageCalculation(attacker, defender, currentCommand, resultVersus, hitIndex);
        
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

        ivr.OnHitVersusResult(hitIndex, resultVersus); // 히트 결과 UI에 표시 - 올바른 히트 인덱스 사용
        
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
    /// 현재 히트에 대한 발사체 생성 및 발사
    /// </summary>
    /// <param name="isPerfect">완벽 입력 여부</param>
    private void CreateProjectileForCurrentHit(bool isPerfect = false)
    {
        Debug.Log($"[CombatManager] 발사체 생성 시도 - 히트 {CurrentHit}, 이미 발사됨: {projectileLaunched[CurrentHit]}");
        
        if (projectileLaunched[CurrentHit]) 
        {
            Debug.Log($"[CombatManager] 히트 {CurrentHit}는 이미 발사됨 - 중복 발사 방지");
            return; // 중복 발사 방지
        }
        
        // ActionCommandData에서 발사체 정보 가져오기
        var command = CurrentResult.Command;
        
        // 🆕 완벽 입력 여부에 따른 발사체 프리팹 선택
        Debug.Log($"[PROJECTILE] 조건 확인: isPerfect={isPerfect}, perfectProjectilePrefab={command.perfectProjectilePrefab != null}");
        
        GameObject projectilePrefab;
        if (isPerfect && command.perfectProjectilePrefab != null)
        {
            projectilePrefab = command.perfectProjectilePrefab;
            Debug.Log($"[PROJECTILE] ✅ Perfect 발사체 선택: {projectilePrefab.name}");
        }
        else
        {
            projectilePrefab = command.normalProjectilePrefab;
            Debug.Log($"[PROJECTILE] ❌ 일반 발사체 선택: {projectilePrefab.name}");
        }
        
        if (projectilePrefab == null)
        {
            Debug.LogError($"[CombatManager] {command.commandName}에 발사체 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        // ❌ 제거: 발사체 카운팅 초기화 (시간 기반 턴 종료로 변경)
        // if (CurrentHit == 0)
        // {
        //     completedProjectiles = 0;
        //     totalProjectiles = command.hitCount;
        //     Debug.Log($"[CombatManager] 발사체 카운팅 초기화: 총 {totalProjectiles}개");
        // }
        
        // 🆕 ProjectileManager 싱글톤을 통해 발사체 가져오기
        if (ProjectileManager.Instance == null)
        {
            Debug.LogError("[CombatManager] ProjectileManager.Instance가 null입니다!");
            return;
        }
        
        Projectile projectile = ProjectileManager.Instance.GetProjectile(projectilePrefab);
        projectile.Initialize(command, CurrentHit, isPlayerAttacker, isPerfect, this);
        
        // ❌ 제거: 중복된 hitIndex 설정 (Initialize에서 이미 설정됨)
        // projectile.hitIndex = CurrentHit;
        
        // Controller 기반으로 위치 가져오기
        Vector3 attackerPos, defenderPos;
        
        if (isPlayerAttacker)
        {
            attackerPos = playerController.transform.position;
            defenderPos = enemyController.transform.position;
        }
        else
        {
            attackerPos = enemyController.transform.position;
            defenderPos = playerController.transform.position;
        }
        
        // 🆕 ProjectileManager를 통해 발사체 생성 위치 계산
        Vector3 spawnPosition = ProjectileManager.Instance.CalculateSpawnPosition(attackerPos, defenderPos);
        projectile.transform.position = spawnPosition;
        
        // 발사 방향 계산
        Vector3 direction = (defenderPos - attackerPos).normalized;
        
        // 🆕 디버그 로그 추가
        Debug.Log($"[CombatManager] 발사체 생성: attackerPos={attackerPos}, defenderPos={defenderPos}, spawnPos={spawnPosition}, direction={direction}");
        
        // 🆕 발사체 이벤트 구독
        projectile.OnProjectileHit += OnProjectileHit;
        projectile.OnProjectileCompleted += OnProjectileCompleted;
        
        // 발사체 발사
        projectile.Launch(direction, projectile.baseSpeed);
        
        // 발사 상태 기록
        projectileLaunched[CurrentHit] = true;
        
        Debug.Log($"[CombatManager] 히트 {CurrentHit} 발사체 발사 완료");
    }
    
    /// <summary>
    /// 발사체 충돌 시 호출되는 메서드 (기존 방식 - 호환성 유지)
    /// </summary>
    public void OnProjectileHit(Projectile projectile)
    {
        // 🆕 발사체의 히트 인덱스 사용
        int hitIdx = projectile.hitIndex;
        
        // 🆕 히트 인덱스 범위 체크
        if (hitIdx < 0 || hitIdx >= hitJudgmentCount.Length)
        {
            Debug.LogError($"[CombatManager] 🚨 히트 인덱스 범위 초과! hitIdx={hitIdx}, 배열 길이={hitJudgmentCount.Length} - 판정 무시");
            return;
        }
        
        // 🆕 판정 발생 횟수 카운트
        hitJudgmentCount[hitIdx]++;
        int currentCount = hitJudgmentCount[hitIdx];
        
        Debug.Log($"[CombatManager] 🚨 OnProjectileHit 호출 - 히트 {hitIdx}, 호출 횟수: {currentCount}, 배열 길이: {hitJudgmentCompleted.Length}");
        
        // 🆕 중복 판정 방지: 이미 판정이 완료된 히트는 무시
        if (hitJudgmentCompleted[hitIdx])
        {
            Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 이미 판정 완료됨 - 중복 판정 방지 (총 {currentCount}번 호출됨)");
            return;
        }
        
        Debug.Log($"[CombatManager] 🚨 발사체 충돌 - 즉시 판정 발생 (히트 {hitIdx}, {currentCount}번째 호출)");
        
        // 발사체 충돌 시 즉시 판정 발생 - 올바른 히트 인덱스 전달
        EvaluateClashResult(hitIdx);
        
        // 🆕 히트 판정 완료 상태 기록
        hitJudgmentCompleted[hitIdx] = true;
        Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 판정 완료 상태 설정됨");
    }
    
    /// <summary>
    /// 발사체 기반 최종 판정을 처리합니다
    /// </summary>
    /// <param name="projectile">충돌한 발사체</param>
    /// <param name="defenderPerfectSuccess">방어자 완벽 입력 성공 여부</param>
    public void TriggerProjectileBasedFinalJudgment(Projectile projectile, bool defenderPerfectSuccess)
    {
        Debug.Log($"[CombatManager] 🚨 발사체 기반 최종 판정 시작 - 히트 {projectile.hitIndex}, 공격자 완벽: {projectile.attackerPerfectInput}, 방어자 완벽: {defenderPerfectSuccess}");
        
        // 🆕 발사체의 히트 인덱스 사용
        int hitIdx = projectile.hitIndex;
        
        // 🆕 히트 인덱스 범위 체크
        if (hitIdx < 0 || hitIdx >= hitJudgmentCount.Length)
        {
            Debug.LogError($"[CombatManager] 🚨 히트 인덱스 범위 초과! hitIdx={hitIdx}, 배열 길이={hitJudgmentCount.Length} - 판정 무시");
            return;
        }
        
        // 🆕 중복 판정 방지: 이미 판정이 완료된 히트는 무시
        if (hitJudgmentCompleted[hitIdx])
        {
            Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 이미 판정 완료됨 - 중복 판정 방지");
            return;
        }
        
        // 🆕 공격자와 방어자 입력 판정 설정
        attackerPerfectInput = projectile.attackerPerfectInput;
        defenderPerfectInput = defenderPerfectSuccess;
        
        // 🆕 입력 시간 설정 (현재 시간 사용)
        attackerInputTime = Time.time;
        defenderInputTime = Time.time;
        
        Debug.Log($"[CombatManager] 🚨 발사체 기반 판정 정보 설정 - 공격자: {attackerPerfectInput}, 방어자: {defenderPerfectInput}");
        
        // 🆕 최종 판정 실행 - 올바른 히트 인덱스 전달
        EvaluateClashResult(hitIdx);
        
        // 🆕 방어자 완벽 입력 성공 시 발사체 제거 (연출 개선)
        if (defenderPerfectSuccess)
        {
            Debug.Log($"[CombatManager] 🚨 방어자 완벽 입력 성공 - 발사체 제거 (히트 {hitIdx})");
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
        
        // 🆕 히트 판정 완료 상태 기록
        hitJudgmentCompleted[hitIdx] = true;
        Debug.Log($"[CombatManager] 🚨 히트 {hitIdx} 발사체 기반 판정 완료");
        
        // 🆕 AI 막기 해제는 턴 종료 버퍼 이후에 DisableInput()에서 처리됨
    }
    
    // ❌ 제거: WaitForTurnEnd 코루틴 (PerformTurn에서 직접 처리)
    // private IEnumerator WaitForTurnEnd()
    // {
    //     Debug.Log($"[CombatManager] 턴 종료 대기 시작 - TurnEndBuffer: {GlobalConfig.Instance.TurnEndBuffer}초");
    //     isWaitingForTurnEnd = true;
    //     
    //     yield return new WaitForSeconds(GlobalConfig.Instance.TurnEndBuffer);
    //     
    //     Debug.Log($"[CombatManager] 턴 종료 대기 완료 - 턴 종료");
    //     // 🆕 턴 종료 플래그 설정
    //     turnEndRequested = true;
    //     isWaitingForTurnEnd = false;
    // }
    
    /// <summary>
    /// 발사체 완료 시 호출되는 메서드
    /// </summary>
    private void OnProjectileCompleted(Projectile projectile)
    {
        Debug.Log($"[CombatManager] 발사체 완료: {projectile.name}");
        
        // 🆕 발사체 완료는 히트 전환과 턴 종료에 영향 없음
        // 히트 전환과 턴 종료는 모두 시간 기반으로 처리
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
