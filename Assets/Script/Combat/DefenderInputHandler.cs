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
        Debug.Log($"[DefenderInputHandler] 🚨 PerfectInputArea 진입 - 완벽 입력 가능 (상태 2)");
    }
    
    private void OnProjectileEnterHitZone(Projectile projectile)
    {
        // 상태 3: CharacterHitBox 진입 (end 타이밍)
        isPerfectInputAvailable = false;
        isHitTiming = true;
        Debug.Log($"[DefenderInputHandler] 🚨 CharacterHitBox 진입 - 피격 판정 발생 (상태 3)");
        
        // end 타이밍에서 완벽 입력 성공 여부에 따른 판정 처리
        if (hasPerfectInputSucceeded)
        {
            // 완벽 입력 성공 (이미 입력됨)
            Debug.Log($"[DefenderInputHandler] end 타이밍 - 완벽 입력 성공 처리");
        }
        else
        {
            // 완벽 입력 실패 (입력 없음 또는 실패)
            Debug.Log($"[DefenderInputHandler] end 타이밍 - 완벽 입력 실패 처리");
        }
    }
    
    private void OnProjectileExitZones(Projectile projectile)
    {
        // 상태 1: 충돌 없음
        isPerfectInputAvailable = false;
        isHitTiming = false;
        hasPerfectInputSucceeded = false; // 완벽 입력 성공 플래그 초기화
        Debug.Log($"[DefenderInputHandler] 발사체 이탈 - 입력 상태 초기화 (상태 1)");
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
        if (IsPlayer)
        {
            HandleGuardInput(ctx);
            
            // 상태에 따른 판정 처리
            if (isPerfectInputAvailable && !isHitTiming)
            {
                // 상태 2: PerfectInputArea 진입 상태에서 입력 시 완벽 입력 성공
                hasPerfectInputSucceeded = true;
                Debug.Log($"[DefenderInputHandler] 완벽 입력 성공! (상태 2에서 입력)");
                
                // 🆕 발사체 기반 완벽 입력 성공 처리
                RecordPerfectInput();
            }
            else if (!isPerfectInputAvailable && !isHitTiming)
            {
                // 상태 1: 충돌 없음 상태에서 입력 시 완벽 입력 실패
                hasPerfectInputSucceeded = false;
                Debug.Log($"[DefenderInputHandler] 완벽 입력 실패! (상태 1에서 입력)");
                
                // 🆕 발사체 기반 완벽 입력 실패 처리
                RecordPerfectInput();
            }
            // 상태 3 (isHitTiming = true)에서는 입력 무시 (이미 판정 완료)
        }
        
        // ❌ 제거: base.OnTimingInput(ctx) 호출하지 않음 (기존 타이밍 윈도우 로직 제거)
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
        if (perfectAction != null)
        {
            perfectAction.performed += OnTimingInput;
            perfectAction.canceled += OnTimingInput; // canceled 이벤트 추가
            perfectAction.Enable();
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
