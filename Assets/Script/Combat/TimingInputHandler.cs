using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TimingInputHandler : MonoBehaviour
{
    private InputAction perfectAction; // PerfectInput 액션
    private bool isListening = false; // 입력 리스닝 상태
    
    private PerfectTimingWindow currentTiming; // 현재 턴의 PerfectTimingWindow
    private List<PerfectTimingWindow> currentTimings; // 현재 턴의 타이밍 윈도우 목록

    private List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>(); // 로드된 타이밍 윈도우 목록

    //private float timingWindowStartTime; // 현재 턴의 시작 시간
    private float? lastInputTime = null; // 마지막 입력 시간
    private float nextAllowedInputTime = 0f; // 다음 입력이 허용되는 시간 


    private void Awake()
    {
        // PlayerInput 컴포넌트 확보
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[TimingInputHandler] PlayerInput 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 액션맵 "Combat" 내 PerfectInput 액션을 가져옴
        playerInput.SwitchCurrentActionMap("Combat");
        perfectAction = playerInput.actions["PerfectInput"];
        if (perfectAction == null)
        {
            Debug.LogError("[TimingInputHandler] 'PerfectInput' 액션을 찾을 수 없습니다.");
        }
    }
    private void OnEnable()
    {
        if (perfectAction != null)
        {
            perfectAction.performed += OnTimingInput; // 입력 이벤트 핸들러 등록
            perfectAction.Enable();
        }
    }
    private void OnDisable()
    {
        if (perfectAction != null)
        {
            perfectAction.performed -= OnTimingInput; // 입력 이벤트 핸들러 해제
            perfectAction.Disable();
        }
    }

    
    public void BeginListening(bool defenseMode = false) // 입력 리스닝 시작
    {
        isListening = true;
        lastInputTime = null;
    }

    public void EndListening() // 입력 리스닝 종료
    {
        isListening = false;
    }
    
    public void LoadTimingWindows(List<PerfectTimingWindow> timings) 
        // 타이밍 윈도우 목록을 timings[]에 저장
    {
        currentTimings = timings;
        loadedTimings = timings;
        lastInputTime = -1f; // 초기화
    }
    public void RegisterHitTiming(PerfectTimingWindow timing) 
        // 현재 턴의 PerfectTimingWindow을 등록
    {
        Debug.Log($"[RegisterHitTiming] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[RegisterHitTiming] Timing: {timing.start}~{timing.start + timing.duration}, CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[PerfectTimingWindow] start={timing.start} duration={timing.duration}");

        currentTiming = timing;
    }
    private bool ShouldIgnoreInput() // 입력을 무시해야 하는지 확인
    {
        if (!isListening) return true;   // 입력 리스닝 중이 아닐 경우 무시
        if (IsInBufferPeriod()) // 버퍼 구간에 있는 경우 입력 무시
        {
            Debug.Log($"[OnTimingInput] 버퍼 구간 → 입력 무시");
            return true;
        }
        if (Time.time < nextAllowedInputTime)
        {
            Debug.Log("[OnTimingInput] 쿨다운 중! 입력 무시");
            return true;
        }
        return false;
    }
    /* 이 메서드는 쓰이지 않고 있으므로 추후 정리해야 할 수 있습니다.
    public bool EvaluateInput(PerfectTimingWindow timing) 
        // 입력이 timing 구간 안에 있는지 확인
    {
        if (!lastInputTime.HasValue) return false;

        float relativeTime = lastInputTime.Value - CombatManager.CombatStartTime;
        return timing.Contains(relativeTime);
    }
    */

    public bool HasPerfectInput(PerfectTimingWindow timing) 
        // 현재 턴의 PerfectTimingWindow을 사용하여 입력이 완벽한지 확인
    {
        ////////////////////// 비정상적인 상황 체크 //////////////////////        
        if (timing == null) // null 체크
        {
            Debug.LogError("[HasPerfectInput] timing is null!");
            return false;
        }
        if (!lastInputTime.HasValue) // 마지막 입력 시간이 없을 경우
        {
            Debug.Log("[HasPerfectInput] lastInputTime 없음");
            return false;
        }
        if (CombatManager.CombatStartTime == 0f) // CombatManager.CombatStartTime이 0일 경우
        {
            Debug.LogError("[HasPerfectInput] CombatManager.CombatStartTime is 0!");
            return false;
        }
        ////////////////////////////////////////////////////////////////

        float relativeTime = lastInputTime.Value - CombatManager.CombatStartTime; // 현재 턴의 상대 시간 계산
        bool isContain = timing.Contains(relativeTime); // 입력 시간이 PerfectTimingWindow에 포함되는지 확인

        Debug.Log($"[HasPerfectInput] CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[HasPerfectInput] TURN-relative={relativeTime}, Window=({timing.start}~{timing.start + timing.duration}) → Contains={isContain}");

        return isContain;
    }
    public bool HasPerfectInput() 
        // 매개변수를 받지 않는 형태로 오버로드(currentTiming 사용)
    {
        if (currentTiming == null)
        {
            Debug.LogError("[HasPerfectInput] currentTiming is null!");
            return false;
        }
        return HasPerfectInput(currentTiming); // currentTiming 을 매개변수에 전달하여 오버로드된 메서드 호출
    }
    private void OnTimingInput(InputAction.CallbackContext ctx) // 입력 이벤트 핸들러
    {
        Debug.Log($"[OnTimingInput] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[OnTimingInput] fired! isListening={isListening}");
        Debug.Log($"[OnTimingInput] Time: {Time.time}, lastInputTime: {lastInputTime}, CombatStartTime: {CombatManager.CombatStartTime}");

        if (ShouldIgnoreInput()) return; // 입력 무시 여부 확인

        lastInputTime = Time.time;
        bool isPerfect = HasPerfectInput(currentTiming);

        if (!CombatManager.Instance.windowPrompted) // 윈도우가 열리지 않은 상태이면
        {
            Debug.Log("윈도우 열리기 전 → 쿨다운만 적용, 판정 X");
            if (!isPerfect) // 완벽 입력이 아니면 쿨다운 적용
            {
                nextAllowedInputTime = Time.time + GlobalConfig.Instance.ActionInputCooldown;
                Debug.Log($"쿨다운 발동! {GlobalConfig.Instance.ActionInputCooldown}초");
            }
            return; // ❗ ResolveInput 호출 금지!
        }
        CombatManager.Instance.OnInputReceivedFromHandler(this);


        //if (isPerfect) EndListening(); // 중복 입력 방지 위해 즉시 리스너 종료
    }

    public void NotifyWindowClosed(bool isPlayer) // 윈도우가 닫혔을 때 호출되는 메서드
    {
        bool currentHitResultShown;
        if (currentHitResultShown = !CombatManager.Instance.CurrentHitResultShown) // 현재 히트 결과가 표시되었는지 확인
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.OnInputReceivedFromHandler(this);
            CombatManager.SetCurrentHitResultShown(currentHitResultShown);
            EndListening();
        }        
    }

    private bool IsInBufferPeriod() // 현재 입력이 버퍼 구간에 있는지 확인
    {
        float relativeTime = Time.time - CombatManager.CombatStartTime;

        float turnDuration = GlobalConfig.Instance.TurnDurationSeconds;

        bool inStartBuffer = relativeTime <= GlobalConfig.Instance.InputBufferStartSeconds;
        bool inEndBuffer = relativeTime >= (turnDuration - GlobalConfig.Instance.InputBufferEndSeconds);

        return inStartBuffer || inEndBuffer;
    }
}
