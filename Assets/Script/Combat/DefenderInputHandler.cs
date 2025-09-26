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
    
    // 막기 상태 프로퍼티
    public bool IsGuardActive => isGuardActive;
    
    [Header("발사체 기반 입력 시스템")]
    private CharacterHitSystem characterHitSystem; // 🆕 CharacterHitSystem 참조 (자동 참조)
    private bool isPerfectInputAvailable = false; // 🆕 완벽 입력 가능 상태
    private bool isHitTiming = false; // 🆕 피격 타이밍 상태
    private bool hasPerfectInputSucceeded = false; // 🆕 완벽 입력 성공 여부 추적
    private Projectile currentProjectile = null; // 🆕 현재 처리 중인 발사체
    private bool isProjectileInPerfectZone = false; // 🆕 PerfectInputZone 내 발사체 존재 여부
    private bool isProjectileInHitZone = false; // 🆕 CharacterHitBox 진입 여부
    private float perfectZoneEnterTime = -1f; // 🆕 PerfectZone 진입 시각
    private float hitZoneEnterTime = -1f; // 🆕 HitZone 진입 시각
    
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
        // 상태 2: PerfectInputArea 진입 (start 타이밍)
        isPerfectInputAvailable = true;
        isHitTiming = false;
        hasPerfectInputSucceeded = false; // 완벽 입력 성공 플래그 초기화
        isProjectileInPerfectZone = true;
        isProjectileInHitZone = false;
        perfectZoneEnterTime = Time.time;
        hitZoneEnterTime = -1f;
        
        // 🆕 현재 발사체 정보 저장 (최종 판정 시 사용)
        currentProjectile = projectile;
        
        Debug.Log($"[InputTrace][Defender] Projectile Enter PerfectZone - hitIndex:{projectile.hitIndex}, projectile:{projectile.name}, time:{perfectZoneEnterTime:F4}");
    }
    
    private void OnProjectileEnterHitZone(Projectile projectile)
    {
        // 상태 3: CharacterHitBox 진입 (end 타이밍)
        isPerfectInputAvailable = false;
        isHitTiming = true;
        isProjectileInPerfectZone = false;
        isProjectileInHitZone = true;
        hitZoneEnterTime = Time.time;
        Debug.Log($"[InputTrace][Defender] Projectile Enter HitZone - hitIndex:{projectile.hitIndex}, projectile:{projectile.name}, time:{hitZoneEnterTime:F4}");
        
        // 🆕 CharacterHitBox 충돌 시 최종 판정 발생
        // 방어자 완벽 입력이 실패했거나 입력하지 않은 경우에만 실행
        if (!hasPerfectInputSucceeded)
        {
            Debug.Log($"[InputTrace][Defender] 방어자 완벽 입력 실패/무입력 - CharacterHitBox 충돌 시 최종 판정 발생");
            TriggerFinalJudgment(projectile, false);
        }
        else
        {
            Debug.Log($"[InputTrace][Defender] 방어자 완벽 입력 성공으로 이미 최종 판정 완료됨");
        }
    }
    
    private void OnProjectileExitZones(Projectile projectile)
    {
        // 상태 1: 충돌 없음
        isPerfectInputAvailable = false;
        isHitTiming = false;
        hasPerfectInputSucceeded = false; // 완벽 입력 성공 플래그 초기화
        isProjectileInPerfectZone = false;
        isProjectileInHitZone = false;
        if (currentProjectile == projectile)
        {
            currentProjectile = null;
        }
        Debug.Log($"[InputTrace][Defender] Projectile Exit Zones - hitIndex:{projectile.hitIndex}, projectile:{projectile.name}, time:{Time.time:F4}");
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
        // 발사체 기반 판정
        return hasPerfectInputSucceeded;
    }
    
    // 🆕 기존 타이밍 윈도우 로직 완전 차단
    public override bool HasPerfectInput(PerfectTimingWindow timing)
    {
        // 발사체 기반 판정만 사용 (기존 타이밍 윈도우 로직 무시)
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

        bool success = EvaluatePerfectInputWindow();
        hasPerfectInputSucceeded = success;

        if (success)
        {
            Debug.Log("[InputTrace][Defender] PerfectInput 성공 판정");
        }
        else
        {
            Debug.Log("[InputTrace][Defender] PerfectInput 실패 판정 (히트 또는 윈도우 외 입력)");
        }

        RecordPerfectInput();

        if (success && currentProjectile != null)
        {
            Debug.Log("[InputTrace][Defender] PerfectInput 성공 → 즉시 최종 판정 트리거");
            TriggerFinalJudgment(currentProjectile, true);
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
        base.EnableInput();
        // 막기 상태 초기화
        ResetGuardState();
        ResetDefenseState();
#if UNITY_EDITOR
        Debug.Log("[DefenseInputHandler] EnableInput() 호출됨");
#endif
    }
    
    public override void DisableInput()
    {
        base.DisableInput();
        // 막기 상태 초기화
        ResetGuardState();
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
    /// 막기 상태를 초기화합니다
    /// </summary>
    public void ResetGuardState()
    {
        if (isGuardActive)
        {
            StopGuardAnimation();
        }
        isGuardActive = false;
        isGuardInputHeld = false;
        guardHoldStartTime = 0f;
        Debug.Log("[DefenderInputHandler] 막기 상태 초기화");
    }
    
    /// <summary>
    /// 방어 상태를 초기화합니다
    /// </summary>
    public void ResetDefenseState()
    {
        ResetGuardState();
        
        // 🆕 발사체 기반 상태 초기화
        isPerfectInputAvailable = false;
        isHitTiming = false;
        hasPerfectInputSucceeded = false;
        currentProjectile = null; // 🆕 현재 발사체 초기화
        
        Debug.Log("[DefenderInputHandler] ResetDefenseState 호출됨");
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
        if (CombatManager.Instance != null)
        {
            if (IsPlayer)
            {
                var playerController = CombatManager.Instance.GetPlayerController();
                playerController?.OnPlayDefence();
            }
            else
            {
                var enemyController = CombatManager.Instance.GetEnemyController();
                enemyController?.OnPlayDefence();
            }
        }
    }
    
    /// <summary>
    /// 막기 애니메이션을 중단합니다
    /// </summary>
    private void StopGuardAnimation()
    {
        if (CombatManager.Instance != null)
        {
            if (IsPlayer)
            {
                var playerController = CombatManager.Instance.GetPlayerController();
                // 애니메이션 중단 로직이 필요하면 여기에 추가
            }
            else
            {
                var enemyController = CombatManager.Instance.GetEnemyController();
                // 애니메이션 중단 로직이 필요하면 여기에 추가
            }
        }
    }
}
