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
    
    // 공격자의 커맨드 데이터를 받아 타이밍 윈도우를 복사
    public void LoadFromOpponentCommand(ActionCommandData opponentCommand)
    {
        // opponentCommand가 포함한 타이밍 윈도우 리스트를 추출해 부모의 메서드로 전달
        List<PerfectTimingWindow> copiedTimings = opponentCommand.perfectTimings;
        base.LoadTimingWindows(copiedTimings);
    }

    public override void RegisterHitTiming(PerfectTimingWindow timing)
    {
        loadedTimings = new List<PerfectTimingWindow> { timing };
        currentTiming = timing; // ← 반드시 필요
#if UNITY_EDITOR
        Debug.Log($"[DefenderInputHandler] Registered Timing: start={timing.start}, duration={timing.duration}");
#endif    
    }

    protected override void OnTimingInput(InputAction.CallbackContext ctx)
    {
        // 막기 입력 처리 (방어 턴일 때만)
        if (IsPlayer)
        {
            HandleGuardInput(ctx);
        }
        
        base.OnTimingInput(ctx); // 기본 입력 처리 호출
#if UNITY_EDITOR
        Debug.Log($"[DefenseInputHandler] OnTimingInput 호출: {lastInputTime}");
        if (currentTiming != null)
        {
            Debug.Log($"[DefenseInputHandler] currentTiming: start={currentTiming.start}, duration={currentTiming.duration}");
        }
        else
        {
            Debug.LogError("[DefenseInputHandler] currentTiming is NULL!");
        }
#endif
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
        else if (ctx.canceled)
        {
            // 입력 해제 - 막기 즉시 해제
            isGuardInputHeld = false;
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
                Debug.Log($"[DefenderInputHandler] 막기 활성화! 홀드 시간: {Time.time - guardHoldStartTime:F2}초");
            }
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
}
