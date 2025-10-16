using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefenderInputHandler : BaseInputHandler
{
    [Header("막기 시스템")]
    [SerializeField] private float guardHoldThreshold = 0.5f; // 막기 활성화에 필요한 홀드 시간 (초)
    
    private bool isGuardActive = false; // 현재 막기 상태
    private float guardHoldStartTime = 0f; // 막기 홀드 시작 시간
    private bool isGuardInputHeld = false; // 막기 입력이 홀드되고 있는지 여부
    
    // 막기 상태 프로퍼티 (플레이어 + AI 통합)
    public bool IsGuardActive => isGuardActive || aiIsGuarding;
    
    // 🆕 AI 막기 상태 프로퍼티 (분리)
    public bool IsAIGuardActive => aiIsGuarding;
    
    [Header("발사체 기반 입력 시스템")]
    private CharacterHitSystem characterHitSystem; // 🆕 CharacterHitSystem 참조 (자동 참조)
    public CharacterHitSystem CharacterHitSystem => characterHitSystem;
    private bool isPerfectInputAvailable = false; // 🆕 완벽 입력 가능 상태
    private bool isHitTiming = false; // 🆕 피격 타이밍 상태
    private bool hasPerfectInputSucceeded = false; // 🆕 완벽 입력 성공 여부 추적
    private Projectile currentProjectile = null; // 🆕 현재 처리 중인 발사체
    private bool isProjectileInPerfectZone = false; // 🆕 PerfectInputZone 내 발사체 존재 여부
    private bool isProjectileInHitZone = false; // 🆕 CharacterHitBox 진입 여부
    private float perfectZoneEnterTime = -1f; // 🆕 PerfectZone 진입 시각
    private float hitZoneEnterTime = -1f; // 🆕 HitZone 진입 시각
    
    // 🆕 연타 공격 대응: Hit Index별 발사체 추적
    private Dictionary<int, Projectile> projectilesByHitIndex = new Dictionary<int, Projectile>();
    private Dictionary<int, bool> perfectInputSucceededByHitIndex = new Dictionary<int, bool>();
    private Dictionary<int, bool> projectileInPerfectZoneByHitIndex = new Dictionary<int, bool>();
    private Dictionary<int, bool> projectileInHitZoneByHitIndex = new Dictionary<int, bool>();
    
    // 🆕 AI 방어 입력 처리
    private Dictionary<int, bool> aiDefenseAttemptedByHitIndex = new Dictionary<int, bool>();
    private Dictionary<int, float> aiDefenseTimeByHitIndex = new Dictionary<int, float>();
    
    // 🆕 AI 의사결정 시스템
    private IAIDefenseDecisionMaker aiDefenseDecisionMaker;
    
    // 🆕 AI 막기 시스템
    private bool aiWillGuard = false; // AI가 막기를 시도할지 여부
    private bool aiIsGuarding = false; // AI가 현재 막기 중인지 여부
    private Coroutine aiGuardCoroutine = null; // AI 막기 코루틴
    
    // ❌ 제거: 기존 타이밍 윈도우 복제 방식
    // public void LoadFromOpponentCommand(ActionCommandData opponentCommand)
    // {
    //     // opponentCommand가 포함한 타이밍 윈도우 리스트를 추출해 부모의 메서드로 전달
    //     List<PerfectTimingWindow> copiedTimings = opponentCommand.perfectTimings;
    //     base.LoadTimingWindows(copiedTimings);
    // }

    // 🆕 발사체 기반 입력 처리 메서드들
    private void OnProjectileEnterPerfectZone(Projectile projectile)
    {
        int hitIndex = projectile.hitIndex;
        
        // 🆕 Hit Index별 발사체 추적
        projectilesByHitIndex[hitIndex] = projectile;
        perfectInputSucceededByHitIndex[hitIndex] = false; // 완벽 입력 성공 플래그 초기화
        projectileInPerfectZoneByHitIndex[hitIndex] = true;
        projectileInHitZoneByHitIndex[hitIndex] = false;
        
        // 🆕 AI 방어 입력 초기화
        aiDefenseAttemptedByHitIndex[hitIndex] = false;
        aiDefenseTimeByHitIndex[hitIndex] = -1f;
        
        // 🆕 현재 발사체 정보 저장 (최종 판정 시 사용) - 가장 최근 발사체
        currentProjectile = projectile;
        
        // 🆕 전역 상태 업데이트 (기존 호환성 유지)
        isPerfectInputAvailable = true;
        isHitTiming = false;
        hasPerfectInputSucceeded = false;
        isProjectileInPerfectZone = true;
        isProjectileInHitZone = false;
        perfectZoneEnterTime = Time.time;
        hitZoneEnterTime = -1f;
        
        Debug.Log($"[InputTrace][Defender] 🆕 Projectile Enter PerfectZone - hitIndex:{hitIndex}, projectile:{projectile.name}, time:{perfectZoneEnterTime:F4}");
        Debug.Log($"[InputTrace][Defender] 🆕 현재 추적 중인 발사체 수: {projectilesByHitIndex.Count}");
        
        // 🆕 AI 방어자 처리: PerfectZone 진입 시 AI 방어 입력 시도
        if (!IsPlayer)
        {
            StartCoroutine(AttemptAIDefenseInput(projectile));
        }
    }
    
    private void OnProjectileEnterHitZone(Projectile projectile)
    {
        int hitIndex = projectile.hitIndex;
        
        // 🆕 Hit Index별 상태 업데이트
        projectileInPerfectZoneByHitIndex[hitIndex] = false;
        projectileInHitZoneByHitIndex[hitIndex] = true;
        
        // 🆕 전역 상태 업데이트 (기존 호환성 유지)
        isPerfectInputAvailable = false;
        isHitTiming = true;
        isProjectileInPerfectZone = false;
        isProjectileInHitZone = true;
        hitZoneEnterTime = Time.time;
        
        Debug.Log($"[InputTrace][Defender] 🆕 Projectile Enter HitZone - hitIndex:{hitIndex}, projectile:{projectile.name}, time:{hitZoneEnterTime:F4}");
        
        // 🆕 CharacterHitBox 충돌 시 최종 판정 발생
        // 방어자 완벽 입력이 실패했거나 입력하지 않은 경우에만 실행
        bool hitIndexPerfectSucceeded = perfectInputSucceededByHitIndex.ContainsKey(hitIndex) && perfectInputSucceededByHitIndex[hitIndex];
        
        if (!hitIndexPerfectSucceeded)
        {
            Debug.Log($"[InputTrace][Defender] 🆕 방어자 완벽 입력 실패/무입력 - CharacterHitBox 충돌 시 최종 판정 발생 (hitIndex:{hitIndex})");
            TriggerFinalJudgment(projectile, false);
        }
        else
        {
            Debug.Log($"[InputTrace][Defender] 🆕 방어자 완벽 입력 성공으로 이미 최종 판정 완료됨 (hitIndex:{hitIndex})");
        }
    }
    
    private void OnProjectileExitZones(Projectile projectile)
    {
        int hitIndex = projectile.hitIndex;
        
        // 🆕 Hit Index별 상태 정리
        if (projectilesByHitIndex.ContainsKey(hitIndex))
        {
            projectilesByHitIndex.Remove(hitIndex);
        }
        if (perfectInputSucceededByHitIndex.ContainsKey(hitIndex))
        {
            perfectInputSucceededByHitIndex.Remove(hitIndex);
        }
        if (projectileInPerfectZoneByHitIndex.ContainsKey(hitIndex))
        {
            projectileInPerfectZoneByHitIndex.Remove(hitIndex);
        }
        if (projectileInHitZoneByHitIndex.ContainsKey(hitIndex))
        {
            projectileInHitZoneByHitIndex.Remove(hitIndex);
        }
        
        // 🆕 AI 방어 입력 상태 정리
        if (aiDefenseAttemptedByHitIndex.ContainsKey(hitIndex))
        {
            aiDefenseAttemptedByHitIndex.Remove(hitIndex);
        }
        if (aiDefenseTimeByHitIndex.ContainsKey(hitIndex))
        {
            aiDefenseTimeByHitIndex.Remove(hitIndex);
        }
        
        // 🆕 현재 발사체가 나간 경우 정리
        if (currentProjectile == projectile)
        {
            currentProjectile = null;
        }
        
        // 🆕 전역 상태 업데이트 (기존 호환성 유지)
        isPerfectInputAvailable = false;
        isHitTiming = false;
        hasPerfectInputSucceeded = false;
        isProjectileInPerfectZone = false;
        isProjectileInHitZone = false;
        
        Debug.Log($"[InputTrace][Defender] 🆕 Projectile Exit Zones - hitIndex:{hitIndex}, projectile:{projectile.name}, time:{Time.time:F4}");
        Debug.Log($"[InputTrace][Defender] 🆕 현재 추적 중인 발사체 수: {projectilesByHitIndex.Count}");
    }
    
    // 🆕 CharacterHitSystem 이벤트 구독
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake() 호출
        
        // 🆕 같은 오브젝트에서 CharacterHitSystem 자동 참조
        characterHitSystem = GetComponent<CharacterHitSystem>();
        if (characterHitSystem == null)
        {
            Debug.LogError($"[DefenderInputHandler] {gameObject.name}에 CharacterHitSystem 컴포넌트가 없습니다!");
            return;
        }
        
        Debug.Log($"[DefenderInputHandler] CharacterHitSystem 자동 참조 성공: {characterHitSystem.name}");
        SubscribeToHitSystemEvents();
        
        // 🆕 AI 의사결정 시스템 초기화
        InitializeAIDecisionSystem();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromHitSystemEvents();
    }
    
    private void SubscribeToHitSystemEvents()
    {
        if (characterHitSystem != null)
        {
            characterHitSystem.OnProjectileEnterPerfectZone += OnProjectileEnterPerfectZone;
            characterHitSystem.OnProjectileEnterHitZone += OnProjectileEnterHitZone;
        }
    }
    
    private void UnsubscribeFromHitSystemEvents()
    {
        if (characterHitSystem != null)
        {
            characterHitSystem.OnProjectileEnterPerfectZone -= OnProjectileEnterPerfectZone;
            characterHitSystem.OnProjectileEnterHitZone -= OnProjectileEnterHitZone;
        }
    }
    
    public override void RegisterHitTiming(PerfectTimingWindow timing)
    {
        loadedTimings = new List<PerfectTimingWindow> { timing };
        currentTiming = timing; // ← 반드시 필요
#if UNITY_EDITOR
        Debug.Log($"[DefenderInputHandler] Registered Timing: start={timing.start}, duration={timing.duration}");
#endif    
    }
    
    // 🆕 발사체 기반 HasPerfectInput 재정의
    public override bool HasPerfectInput()
    {
        // 🆕 연타 공격 대응: 모든 Hit Index 중 하나라도 완벽 입력 성공하면 true
        foreach (var kvp in perfectInputSucceededByHitIndex)
        {
            if (kvp.Value)
            {
                Debug.Log($"[InputTrace][Defender] 🆕 HasPerfectInput - hitIndex:{kvp.Key}에서 완벽 입력 성공");
                return true;
            }
        }
        
        // 기존 호환성 유지
        return hasPerfectInputSucceeded;
    }
    
    // 🆕 기존 타이밍 윈도우 로직 완전 차단
    public override bool HasPerfectInput(PerfectTimingWindow timing)
    {
        // 🆕 연타 공격 대응: 모든 Hit Index 중 하나라도 완벽 입력 성공하면 true
        foreach (var kvp in perfectInputSucceededByHitIndex)
        {
            if (kvp.Value)
            {
                Debug.Log($"[InputTrace][Defender] 🆕 HasPerfectInput(timing) - hitIndex:{kvp.Key}에서 완벽 입력 성공");
                return true;
            }
        }
        
        // 기존 호환성 유지
        return hasPerfectInputSucceeded;
    }

    protected override void OnTimingInput(InputAction.CallbackContext ctx)
    {
        // 🆕 발사체 기반 입력 처리 (기존 타이밍 윈도우 로직 완전 제거)
        Debug.Log($"[InputTrace][Defender] OnTimingInput - phase:{ctx.phase}, isPerfectAvailable:{isPerfectInputAvailable}, isHitTiming:{isHitTiming}, isListening:{isListening}, time:{Time.time:F4}, frame:{Time.frameCount}");

        if (!IsPlayer)
        {
            Debug.Log("[InputTrace][Defender] 플레이어가 아니므로 입력 처리 생략");
            return;
        }

        HandleGuardInput(ctx);

        // 입력 이벤트 기록 (최근 입력 시각)
        lastInputTime = Time.time;
        Debug.Log($"[InputTrace][Defender] 입력 시각 기록 - lastInputTime:{lastInputTime:F4}, frame:{Time.frameCount}");

        if (ctx.phase != InputActionPhase.Performed)
        {
            Debug.Log($"[InputTrace][Defender] 입력 phase={ctx.phase} → 판정 처리 생략");
            Debug.Log($"[InputTrace][Defender] 기존 타이밍 윈도우 로직 차단");
            return;
        }

        // 🆕 연타 공격 대응: 모든 PerfectZone 내 발사체에 대해 입력 평가
        bool anySuccess = false;
        Projectile successfulProjectile = null;
        
        foreach (var kvp in projectileInPerfectZoneByHitIndex)
        {
            int hitIndex = kvp.Key;
            bool inPerfectZone = kvp.Value;
            
            if (inPerfectZone && projectilesByHitIndex.ContainsKey(hitIndex))
            {
                Projectile projectile = projectilesByHitIndex[hitIndex];
                bool success = EvaluatePerfectInputWindowForProjectile(projectile);

        if (success)
                {
                    Debug.Log($"[InputTrace][Defender] 🆕 PerfectInput 성공 판정 - hitIndex:{hitIndex}");
                    perfectInputSucceededByHitIndex[hitIndex] = true;
                    anySuccess = true;
                    successfulProjectile = projectile;
                    break; // 첫 번째 성공한 발사체만 처리
                }
            }
        }
        
        // 🆕 기존 호환성 유지
        hasPerfectInputSucceeded = anySuccess;

        if (anySuccess)
        {
            Debug.Log("[InputTrace][Defender] 🆕 PerfectInput 성공 판정 (연타 대응)");
        }
        else
        {
            Debug.Log("[InputTrace][Defender] 🆕 PerfectInput 실패 판정 (히트 또는 윈도우 외 입력)");
        }

        RecordPerfectInput();

        if (anySuccess && successfulProjectile != null)
        {
            Debug.Log($"[InputTrace][Defender] 🆕 PerfectInput 성공 → 즉시 최종 판정 트리거 (hitIndex:{successfulProjectile.hitIndex})");
            TriggerFinalJudgment(successfulProjectile, true);
            
            // 🆕 해당 Hit Index의 상태 업데이트
            int hitIndex = successfulProjectile.hitIndex;
            projectileInPerfectZoneByHitIndex[hitIndex] = false;
            projectileInHitZoneByHitIndex[hitIndex] = true;
            
            // 🆕 전역 상태 업데이트 (기존 호환성 유지)
            isProjectileInPerfectZone = false;
            isProjectileInHitZone = true;
        }

        Debug.Log($"[InputTrace][Defender] 기존 타이밍 윈도우 로직 차단");
    }

    /// <summary>
    /// 현재 발사체 위치 상태를 기반으로 완벽 입력 가능 여부를 평가합니다.
    /// </summary>
    private bool EvaluatePerfectInputWindow()
    {
        if (currentProjectile == null)
        {
            Debug.Log($"[InputTrace][Defender] 평가 실패 - currentProjectile null (lastInput:{lastInputTime:F4}, perfectEnter:{perfectZoneEnterTime:F4}, hitEnter:{hitZoneEnterTime:F4})");
            return false;
        }

        if (!isProjectileInPerfectZone)
        {
            Debug.Log($"[InputTrace][Defender] 평가 실패 - PerfectZone 내에 있지 않음 (lastInput:{lastInputTime:F4}, perfectEnter:{perfectZoneEnterTime:F4}, hitEnter:{hitZoneEnterTime:F4})");
            return false;
        }

        if (isProjectileInHitZone)
        {
            Debug.Log($"[InputTrace][Defender] 평가 실패 - 이미 HitZone 진입 (lastInput:{lastInputTime:F4}, perfectEnter:{perfectZoneEnterTime:F4}, hitEnter:{hitZoneEnterTime:F4})");
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// 🆕 특정 발사체에 대해 완벽 입력 가능 여부를 평가합니다 (연타 공격 대응)
    /// </summary>
    private bool EvaluatePerfectInputWindowForProjectile(Projectile projectile)
    {
        if (projectile == null)
        {
            Debug.Log($"[InputTrace][Defender] 🆕 평가 실패 - projectile null");
            return false;
        }

        int hitIndex = projectile.hitIndex;
        
        if (!projectileInPerfectZoneByHitIndex.ContainsKey(hitIndex) || !projectileInPerfectZoneByHitIndex[hitIndex])
        {
            Debug.Log($"[InputTrace][Defender] 🆕 평가 실패 - hitIndex:{hitIndex} PerfectZone 내에 있지 않음");
            return false;
        }

        if (projectileInHitZoneByHitIndex.ContainsKey(hitIndex) && projectileInHitZoneByHitIndex[hitIndex])
        {
            Debug.Log($"[InputTrace][Defender] 🆕 평가 실패 - hitIndex:{hitIndex} 이미 HitZone 진입");
            return false;
        }

        Debug.Log($"[InputTrace][Defender] 🆕 평가 성공 - hitIndex:{hitIndex} PerfectZone 내에서 입력 가능");
        return true;
    }
    
    // 🆕 발사체 기반 완벽 입력 기록 (기존 OnInputReceivedFromHandler 활용)
    private void RecordPerfectInput()
    {
        lastInputTime = Time.time;
        Debug.Log($"[DefenderInputHandler] 발사체 기반 완벽 입력 기록: {hasPerfectInputSucceeded}, 시간: {lastInputTime}");
        
        // 🆕 기존 OnInputReceivedFromHandler 메서드 활용
        CombatManager.Instance.OnInputReceivedFromHandler(this);
    }
    
    /// <summary>
    /// 발사체 기반 최종 판정을 발생시킵니다
    /// </summary>
    /// <param name="projectile">충돌한 발사체</param>
    /// <param name="defenderPerfectSuccess">방어자 완벽 입력 성공 여부</param>
    private void TriggerFinalJudgment(Projectile projectile, bool defenderPerfectSuccess)
    {
        Debug.Log($"[DefenderInputHandler] 🚨 최종 판정 발생 - 발사체: {projectile.name}, 공격자 완벽: {projectile.attackerPerfectInput}, 방어자 완벽: {defenderPerfectSuccess}");
        
        // 🆕 쳐내기 성공 시 막기 자동 해제 (Player/Enemy 공통)
        if (defenderPerfectSuccess)
        {
            // Player 막기 해제
            if (isGuardActive)
            {
                isGuardActive = false;
                isGuardInputHeld = false;
                StopGuardAnimation();
                Debug.Log("[DefenderInputHandler] 🆕 쳐내기 성공 - Player 막기 해제");
            }
            
            // Enemy 막기 해제
            if (aiIsGuarding)
            {
                aiIsGuarding = false;
                StopGuardAnimation();
                Debug.Log("[DefenderInputHandler] 🆕 쳐내기 성공 - Enemy 막기 해제");
            }
        }
        
        // 🆕 CombatManager에 발사체 기반 최종 판정 요청
        CombatManager.Instance.TriggerProjectileBasedFinalJudgment(projectile, defenderPerfectSuccess);
    }
    
    /// <summary>
    /// 막기 입력을 처리합니다
    /// </summary>
    private void HandleGuardInput(InputAction.CallbackContext ctx)
    {
        // 자세 포인트가 0인 상태로 방어 턴이 되면 막기 불가
        if (IsInterrupted())
        {
            Debug.Log("[DefenderInputHandler] 자세 포인트 소진으로 막기를 수행할 수 없습니다.");
            return;
        }
        
        if (ctx.performed)
        {
            // 입력 시작 - 막기 홀드 시작
            isGuardInputHeld = true;
            guardHoldStartTime = Time.time;
            Debug.Log("[DefenderInputHandler] 막기 입력 시작");
        }
        else         if (ctx.canceled)
        {
            // 입력 해제 - 막기 즉시 해제
            isGuardInputHeld = false;
            if (isGuardActive)
            {
                StopGuardAnimation();
            }
            isGuardActive = false;
            Debug.Log("[DefenderInputHandler] 막기 입력 해제 - 막기 OFF");
        }
    }
    
    /// <summary>
    /// 막기 상태를 업데이트합니다 (매 프레임 호출)
    /// </summary>
    private void UpdateGuardState()
    {
        // 자세 포인트가 0인 상태로 방어 턴이 되면 막기 불가
        if (IsInterrupted())
        {
            isGuardInputHeld = false;
            isGuardActive = false;
            return;
        }
        
        if (isGuardInputHeld && !isGuardActive)
        {
            // 홀드 임계값을 넘으면 막기 활성화
            if (Time.time - guardHoldStartTime >= guardHoldThreshold)
            {
                isGuardActive = true;
                PlayGuardAnimation();
            }
        }
        else if (!isGuardInputHeld && isGuardActive)
        {
            // 막기 입력이 해제되면 막기 비활성화
            isGuardActive = false;
            StopGuardAnimation();
        }
    }
    
    public override void NotifyWindowClosed(bool isPlayer)
    {
        if (!lastInputTime.HasValue)
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.ResolveInput(this, false);
        }
    }
    
    public override void EnableInput()
    {
        Debug.Log($"[DefenderInputHandler] 🆕 EnableInput 호출됨 - IsPlayer:{IsPlayer}");
        
        base.EnableInput();
        // 턴 상태 초기화
        ResetTurnState();
        ResetDefenseState();
        
        // 🆕 AI 의사결정 시스템 초기화
        InitializeAIDecisionSystem();
        
        // 🆕 AI 막기 의사결정 수행
        if (!IsPlayer)
        {
            Debug.Log("[DefenderInputHandler] 🆕 AI 방어자이므로 막기 의사결정 시작");
            StartAIGuardDecision();
        }
        else
        {
            Debug.Log("[DefenderInputHandler] 🆕 플레이어 방어자이므로 AI 막기 의사결정 건너뜀");
        }
        
#if UNITY_EDITOR
        Debug.Log("[DefenseInputHandler] EnableInput() 호출됨");
#endif
    }
    
    public override void DisableInput()
    {
        Debug.Log($"[DefenderInputHandler] 🆕 DisableInput 호출됨 - IsPlayer:{IsPlayer}, aiIsGuarding:{aiIsGuarding}");
        
        base.DisableInput();
        
        // 🆕 입력 해제 시 지속되고 있던 모든 상태 해제
        ResetTurnState();
        
        Debug.Log("[DefenderInputHandler] 🆕 DisableInput 완료 - 모든 지속 상태 해제됨");
    }
    
    protected override void RegisterInputCallbacks()
    {
        Debug.Log($"[InputTrace][Defender] RegisterInputCallbacks - perfectActionNull:{perfectAction == null}");
        if (perfectAction != null)
        {
            perfectAction.performed += OnTimingInput;
            perfectAction.canceled += OnTimingInput; // canceled 이벤트 추가
            perfectAction.Enable();
            Debug.Log("[InputTrace][Defender] perfectAction.Enable 호출");
        }
        else
        {
            Debug.LogWarning("[InputTrace][Defender] perfectAction이 null - 콜백 등록 불가");
        }
    }
    
    public override bool IsInBufferPeriod()
    {
        // 방어자(플레이어) 입력은 버퍼 구간을 무시한다
        if (IsPlayer)
        {
            return false;
        }
        return base.IsInBufferPeriod();
    }
    
    /// <summary>
    /// 턴 진행에 따라 초기화해야 하는 모든 상태를 초기화합니다
    /// </summary>
    public void ResetTurnState()
    {
        // 1. 플레이어 막기 상태 해제
        if (isGuardActive)
        {
            StopGuardAnimation();
        }
        isGuardActive = false;
        isGuardInputHeld = false;
        guardHoldStartTime = 0f;
        
        // 2. AI 막기 상태 해제
        if (aiIsGuarding)
        {
            StopGuardAnimation();
        }
        aiIsGuarding = false;
        aiWillGuard = false;
        
        // 3. AI 막기 코루틴 정리
        if (aiGuardCoroutine != null)
        {
            StopCoroutine(aiGuardCoroutine);
            aiGuardCoroutine = null;
        }
        
        Debug.Log("[DefenderInputHandler] 턴 상태 초기화 완료 (플레이어 + AI)");
    }
    
    /// <summary>
    /// 방어 상태를 초기화합니다
    /// </summary>
    public void ResetDefenseState()
    {
        ResetTurnState();
        
        // 🆕 발사체 기반 상태 초기화
        isPerfectInputAvailable = false;
        isHitTiming = false;
        hasPerfectInputSucceeded = false;
        currentProjectile = null; // 🆕 현재 발사체 초기화
        
        // 🆕 연타 공격 대응: Hit Index별 상태 초기화
        projectilesByHitIndex.Clear();
        perfectInputSucceededByHitIndex.Clear();
        projectileInPerfectZoneByHitIndex.Clear();
        projectileInHitZoneByHitIndex.Clear();
        
        // 🆕 AI 방어 입력 상태 초기화
        aiDefenseAttemptedByHitIndex.Clear();
        aiDefenseTimeByHitIndex.Clear();
        
        Debug.Log("[DefenderInputHandler] 🆕 ResetDefenseState 호출됨 - 연타 공격 상태 초기화");
    }
    
    /// <summary>
    /// 자세 포인트 소진으로 인한 중단 상태인지 확인
    /// </summary>
    private bool IsInterrupted()
    {
        // Combatant의 자세 포인트 상태 확인
        if (CombatManager.Instance != null)
        {
            var currentController = CombatManager.Instance.CurrentController;
            if (currentController != null && currentController.Combatant != null)
            {
                return currentController.Combatant.IsInterrupted;
            }
        }
        return false;
    }
    
    private void Update()
    {
        // 막기 상태 업데이트 (방어 턴일 때만)
        if (IsPlayer && isListening)
        {
            UpdateGuardState();

#if UNITY_EDITOR
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("[InputTrace][Defender] Keyboard.spaceKey.wasPressedThisFrame");
            }
            if (perfectAction != null && perfectAction.triggered)
            {
                Debug.Log("[InputTrace][Defender] perfectAction.triggered=true");
            }
#endif
        }
    }
    
    
    /// <summary>
    /// 막기 애니메이션을 재생합니다
    /// </summary>
    private void PlayGuardAnimation()
    {
        if (CombatManager.Instance == null)
        {
            Debug.LogError("[DefenderInputHandler] CombatManager.Instance가 null입니다!");
            return;
        }
        
        // 🆕 Animator 소유권 검증 강화
        if (!ValidateAnimatorOwnership())
        {
            Debug.LogError("[DefenderInputHandler] Animator 소유권 검증 실패 - 막기 애니메이션 재생 중단");
            return;
        }
        
            if (IsPlayer)
            {
                var playerController = CombatManager.Instance.GetPlayerController();
            if (playerController != null)
            {
                playerController.OnPlayDefence();
                Debug.Log("[DefenderInputHandler] 플레이어 막기 애니메이션 재생");
            }
            else
            {
                Debug.LogError("[DefenderInputHandler] PlayerController가 null입니다!");
            }
            }
            else
            {
                var enemyController = CombatManager.Instance.GetEnemyController();
            if (enemyController != null)
            {
                enemyController.OnPlayDefence();
                Debug.Log("[DefenderInputHandler] 적 막기 애니메이션 재생");
            }
            else
            {
                Debug.LogError("[DefenderInputHandler] EnemyController가 null입니다!");
            }
        }
    }
    
    /// <summary>
    /// 막기 애니메이션을 중단합니다
    /// </summary>
    private void StopGuardAnimation()
    {
        Debug.Log($"[DefenderInputHandler] 🆕 StopGuardAnimation 호출됨 - IsPlayer:{IsPlayer}");
        
        if (CombatManager.Instance == null)
        {
            Debug.LogError("[DefenderInputHandler] CombatManager.Instance가 null입니다!");
            return;
        }
        
        // 🆕 Animator 소유권 검증 강화
        if (!ValidateAnimatorOwnership())
        {
            Debug.LogError("[DefenderInputHandler] Animator 소유권 검증 실패 - 막기 애니메이션 중단 중단");
            return;
        }
        
            if (IsPlayer)
            {
                var playerController = CombatManager.Instance.GetPlayerController();
            if (playerController != null)
            {
                playerController.OnStopDefence();
                Debug.Log("[DefenderInputHandler] 플레이어 막기 애니메이션 중단");
            }
            else
            {
                Debug.LogError("[DefenderInputHandler] PlayerController가 null입니다!");
            }
            }
            else
            {
                var enemyController = CombatManager.Instance.GetEnemyController();
            if (enemyController != null)
            {
                Debug.Log("[DefenderInputHandler] 🆕 EnemyController.OnStopDefence() 호출 시작");
                enemyController.OnStopDefence();
                Debug.Log("[DefenderInputHandler] 🆕 EnemyController.OnStopDefence() 호출 완료");
            }
            else
            {
                Debug.LogError("[DefenderInputHandler] EnemyController가 null입니다!");
            }
        }
    }
    
    /// <summary>
    /// 🆕 AI 방어 입력 시도 코루틴 (확장 가능한 아키텍처)
    /// </summary>
    private System.Collections.IEnumerator AttemptAIDefenseInput(Projectile projectile)
    {
        int hitIndex = projectile.hitIndex;
        
        // 🆕 AI 방어 입력 시도 여부 확인
        if (aiDefenseAttemptedByHitIndex.ContainsKey(hitIndex) && aiDefenseAttemptedByHitIndex[hitIndex])
        {
            Debug.Log($"[DefenderInputHandler] 🆕 AI 방어 입력 이미 시도됨 - hitIndex:{hitIndex}");
            yield break;
        }
        
        // 🆕 AI 의사결정 시스템을 통한 방어 입력 결정
        var aiContext = CreateAIContext(projectile);
        Debug.Log($"[DefenderInputHandler] 🆕 AI 의사결정 시스템 호출 - hitIndex:{hitIndex}, aiDefenseDecisionMaker null:{aiDefenseDecisionMaker == null}");
        var defenseDecision = aiDefenseDecisionMaker.MakeDefenseDecision(projectile, aiContext);
        
        // 🆕 AI 반응 시간 시뮬레이션
        yield return new WaitForSeconds(defenseDecision.reactionTime);
        
        // 🆕 발사체가 여전히 PerfectZone에 있는지 확인
        if (!projectileInPerfectZoneByHitIndex.ContainsKey(hitIndex) || !projectileInPerfectZoneByHitIndex[hitIndex])
        {
            Debug.Log($"[DefenderInputHandler] 🆕 AI 방어 입력 취소 - 발사체가 PerfectZone을 벗어남 (hitIndex:{hitIndex})");
            yield break;
        }
        
        // 🆕 AI 방어 입력 시도 플래그 설정
        aiDefenseAttemptedByHitIndex[hitIndex] = true;
        aiDefenseTimeByHitIndex[hitIndex] = Time.time;
        
        Debug.Log($"[DefenderInputHandler] 🆕 AI 방어 입력 시도 - hitIndex:{hitIndex}, 시도:{defenseDecision.willAttempt}, 성공:{defenseDecision.willSucceed}");
        
        if (defenseDecision.willAttempt && defenseDecision.willSucceed)
        {
            // 🆕 AI 방어 성공 시 완벽 입력 성공 처리
            perfectInputSucceededByHitIndex[hitIndex] = true;
            hasPerfectInputSucceeded = true;
            
            // 🆕 AI 방어 성공 시 즉시 최종 판정 발생
            Debug.Log($"[DefenderInputHandler] 🆕 AI 방어 성공 → 즉시 최종 판정 트리거 (hitIndex:{hitIndex})");
            TriggerFinalJudgment(projectile, true);
            
            // 🆕 해당 Hit Index의 상태 업데이트
            projectileInPerfectZoneByHitIndex[hitIndex] = false;
            projectileInHitZoneByHitIndex[hitIndex] = true;
            
            // 🆕 전역 상태 업데이트
            isProjectileInPerfectZone = false;
            isProjectileInHitZone = true;
        }
        else
        {
            Debug.Log($"[DefenderInputHandler] 🆕 AI 방어 실패/무시 - CharacterHitBox 충돌 시 최종 판정 대기 (hitIndex:{hitIndex})");
        }
    }
    
    /// <summary>
    /// 🆕 AI 의사결정 시스템 초기화
    /// </summary>
    private void InitializeAIDecisionSystem()
    {
        // 🆕 기본 AI 의사결정 시스템 생성
        aiDefenseDecisionMaker = new DefaultAIDefenseDecisionMaker();
        
        Debug.Log("[DefenderInputHandler] 🆕 AI 의사결정 시스템 초기화 완료");
    }
    
    /// <summary>
    /// 🆕 AI 컨텍스트 생성
    /// </summary>
    private AIContext CreateAIContext(Projectile projectile)
    {
        // 🆕 CombatManager에서 현재 전투 상태 정보 가져오기
        var combatManager = CombatManager.Instance;
        if (combatManager == null)
        {
            Debug.LogError("[DefenderInputHandler] 🆕 CombatManager를 찾을 수 없습니다!");
            return new AIContext();
        }
        
        // 🆕 현재 턴 경과 시간 계산
        float turnElapsedTime = combatManager.CurrentTurnDuration;
        
        // 🆕 현재 자세 포인트 가져오기
        float posturePoints = 100f; // 기본값
        if (combatManager.CurrentController?.Combatant != null)
        {
            posturePoints = combatManager.CurrentController.Combatant.CurrentPoise;
        }
        
        // 🆕 중단 상태 확인
        bool isInterrupted = combatManager.CurrentController?.Combatant?.IsInterrupted ?? false;
        
        // 🆕 총 히트 수 가져오기
        int totalHitCount = combatManager.CurrentResult?.HitCount ?? 1;
        
        // 방어자 Combatant 가져오기 (BT 확률 참조용)
        Combatant defenderCombatant = combatManager.IsPlayerAttacker 
            ? CharacterManager.Instance?.EnemyCombatant 
            : CharacterManager.Instance?.PlayerCombatant;
        
        return new AIContext(
            projectile.hitIndex,
            turnElapsedTime,
            combatManager.IsPlayerAttacker,
            totalHitCount,
            posturePoints,
            isInterrupted,
            aiIsGuarding,
            defenderCombatant  // ← 방어자 Combatant 전달!
        );
    }
    
    /// <summary>
    /// 🆕 AI 의사결정 시스템 교체 (확장성)
    /// </summary>
    public void SetAIDecisionMaker(IAIDefenseDecisionMaker newDecisionMaker)
    {
        aiDefenseDecisionMaker = newDecisionMaker;
        Debug.Log("[DefenderInputHandler] 🆕 AI 의사결정 시스템 교체 완료");
    }
    
    /// <summary>
    /// 🆕 AI 방어 의사결정 메서드 (임시 구현 - 추후 제거 예정)
    /// </summary>
    private AIDefenseDecision MakeAIDefenseDecision(Projectile projectile)
    {
        // 🆕 임시 구현 - AI 의사결정 시스템이 완전히 통합되면 제거
        float aiDefenseSuccessRate = GlobalConfig.Instance.NpcParryPerfectRate;
        
        bool willAttempt = true;
        bool willSucceed = Random.value < aiDefenseSuccessRate;
        float reactionTime = 0f; // 즉시 반응
        
        return new AIDefenseDecision(willAttempt, willSucceed, reactionTime);
    }
    
    /// <summary>
    /// 🆕 AI 막기 의사결정 시작
    /// </summary>
    private void StartAIGuardDecision()
    {
        Debug.Log("[DefenderInputHandler] 🆕 StartAIGuardDecision 호출됨");
        
        if (aiDefenseDecisionMaker == null)
        {
            Debug.LogError("[DefenderInputHandler] AI 의사결정 시스템이 초기화되지 않았습니다!");
            return;
        }
        
        // 🆕 AI 막기 의사결정 수행
        var aiContext = CreateAIContextForGuard();
        Debug.Log($"[DefenderInputHandler] 🆕 AI 컨텍스트 생성 완료 - isInterrupted:{aiContext.isInterrupted}");
        
        aiWillGuard = aiDefenseDecisionMaker.MakeGuardDecision(aiContext);
        Debug.Log($"[DefenderInputHandler] 🆕 AI 막기 의사결정 결과: {aiWillGuard}");
        
        if (aiWillGuard)
        {
            Debug.Log("[DefenderInputHandler] 🆕 AI가 막기를 시도하기로 결정");
            // 🆕 첫 번째 Hit 타이밍까지 대기 후 막기 시작
            aiGuardCoroutine = StartCoroutine(WaitForFirstHitAndStartGuard());
        }
        else
        {
            Debug.Log("[DefenderInputHandler] 🆕 AI가 막기를 시도하지 않기로 결정");
        }
    }
    
    /// <summary>
    /// 🆕 막기 의사결정용 AI 컨텍스트 생성
    /// </summary>
    private AIContext CreateAIContextForGuard()
    {
        var combatManager = CombatManager.Instance;
        if (combatManager == null)
        {
            Debug.LogError("[DefenderInputHandler] CombatManager를 찾을 수 없습니다!");
            return new AIContext(0, 0f, false, 1, 100f, false, false);
        }
        
        // 🆕 턴 경과 시간 계산
        float turnElapsedTime = TurnTimer.ElapsedTime;
        
        // 🆕 자세 포인트 가져오기
        float posturePoints = combatManager.CurrentController?.Combatant?.CurrentPoise ?? 100f;
        
        // 🆕 중단 상태 확인
        bool isInterrupted = combatManager.CurrentController?.Combatant?.IsInterrupted ?? false;
        
        // 🆕 총 히트 수 가져오기
        int totalHitCount = combatManager.CurrentResult?.HitCount ?? 1;
        
        // 방어자 Combatant 가져오기 (BT 확률 참조용)
        Combatant defenderCombatant = combatManager.IsPlayerAttacker 
            ? CharacterManager.Instance?.EnemyCombatant 
            : CharacterManager.Instance?.PlayerCombatant;
        
        return new AIContext(
            0, // 막기 의사결정 시에는 hitIndex 0 사용
            turnElapsedTime,
            combatManager.IsPlayerAttacker,
            totalHitCount,
            posturePoints,
            isInterrupted,
            false, // 막기 의사결정 시에는 아직 막기 중이 아님
            defenderCombatant  // ← 방어자 Combatant 전달!
        );
    }
    
    /// <summary>
    /// 🆕 첫 번째 Hit 타이밍까지 대기 후 막기 시작
    /// </summary>
    private System.Collections.IEnumerator WaitForFirstHitAndStartGuard()
    {
        Debug.Log("[DefenderInputHandler] 🆕 WaitForFirstHitAndStartGuard 코루틴 시작");
        
        var combatManager = CombatManager.Instance;
        if (combatManager == null || combatManager.CurrentResult == null)
        {
            Debug.LogError("[DefenderInputHandler] CombatManager 또는 CurrentResult가 null입니다!");
            yield break;
        }
        
        // 🆕 첫 번째 Hit 타이밍 계산
        var command = combatManager.CurrentResult.Command;
        if (command == null || command.perfectTimings == null || command.perfectTimings.Count == 0)
        {
            Debug.LogError("[DefenderInputHandler] Command 또는 perfectTimings가 null입니다!");
            yield break;
        }
        
        var firstHitTiming = command.perfectTimings[0];
        float firstHitTime = firstHitTiming.start;
        
        Debug.Log($"[DefenderInputHandler] 🆕 첫 번째 Hit 타이밍까지 대기: {firstHitTime:F2}초");
        
        // 🆕 첫 번째 Hit 타이밍까지 대기
        yield return new WaitForSeconds(firstHitTime);
        
        Debug.Log($"[DefenderInputHandler] 🆕 첫 번째 Hit 타이밍 도달 - aiWillGuard:{aiWillGuard}, aiIsGuarding:{aiIsGuarding}");
        
        // 🆕 막기 시작
        if (aiWillGuard && !aiIsGuarding)
        {
            StartAIGuard();
        }
        else
        {
            Debug.Log($"[DefenderInputHandler] 🆕 막기 시작 조건 불만족 - aiWillGuard:{aiWillGuard}, aiIsGuarding:{aiIsGuarding}");
        }
    }
    
    /// <summary>
    /// 🆕 AI 막기 시작
    /// </summary>
    private void StartAIGuard()
    {
        aiIsGuarding = true;
        PlayGuardAnimation();
        Debug.Log("[DefenderInputHandler] 🆕 AI 막기 시작");
    }
    
    /// <summary>
    /// 🆕 AI 막기 중지 (내부에서만 사용)
    /// </summary>
    private void StopAIGuard()
    {
        if (aiIsGuarding)
        {
            aiIsGuarding = false;
            StopGuardAnimation();
            Debug.Log("[DefenderInputHandler] 🆕 AI 막기 중지 완료");
        }
        else
        {
            Debug.Log("[DefenderInputHandler] 🆕 AI 막기 상태가 이미 해제됨");
        }
    }
    
    /// <summary>
    /// 🆕 Animator 소유권 검증 강화
    /// </summary>
    private bool ValidateAnimatorOwnership()
    {
        if (CombatManager.Instance == null)
        {
            Debug.LogError("[DefenderInputHandler] CombatManager.Instance가 null입니다!");
            return false;
        }
        
        // 🆕 현재 DefenderInputHandler가 올바른 캐릭터의 Animator를 제어하는지 검증
        if (IsPlayer)
        {
            // 플레이어 DefenderInputHandler는 플레이어의 Animator만 제어해야 함
            var playerController = CombatManager.Instance.GetPlayerController();
            if (playerController == null)
            {
                Debug.LogError("[DefenderInputHandler] PlayerController가 null입니다!");
                return false;
            }
            
            // 🆕 현재 턴이 플레이어 공격 턴인지 확인 (플레이어가 방어자여야 함)
            if (CombatManager.Instance.IsPlayerAttacker)
            {
                Debug.LogError("[DefenderInputHandler] 플레이어가 공격자 턴인데 플레이어 DefenderInputHandler가 실행됨!");
                return false;
            }
            
            Debug.Log("[DefenderInputHandler] ✅ 플레이어 Animator 소유권 검증 성공");
            return true;
        }
        else
        {
            // 적 DefenderInputHandler는 적의 Animator만 제어해야 함
            var enemyController = CombatManager.Instance.GetEnemyController();
            if (enemyController == null)
            {
                Debug.LogError("[DefenderInputHandler] EnemyController가 null입니다!");
                return false;
            }
            
            // 🆕 현재 턴이 적 공격 턴인지 확인 (적이 방어자여야 함)
            if (!CombatManager.Instance.IsPlayerAttacker)
            {
                Debug.LogError("[DefenderInputHandler] 적이 공격자 턴인데 적 DefenderInputHandler가 실행됨!");
                return false;
            }
            
            Debug.Log("[DefenderInputHandler] ✅ 적 Animator 소유권 검증 성공");
            return true;
        }
    }
}
