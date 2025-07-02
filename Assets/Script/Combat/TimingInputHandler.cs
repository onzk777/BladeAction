using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TimingInputHandler : MonoBehaviour
{
    private InputAction perfectAction;

    private bool isListening = false;
    private List<PerfectTimingWindow> currentTimings;

    //private float timingWindowStartTime; // 현재 턴의 시작 시간
    private float? lastInputTime = null; // 마지막 입력 시간

    private float nextAllowedInputTime = 0f;

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
            perfectAction.performed += OnTimingInput;
            perfectAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (perfectAction != null)
        {
            perfectAction.performed -= OnTimingInput;
            perfectAction.Disable();
        }
    }

    
    public void BeginListening()
    {
        isListening = true;
        lastInputTime = null;
    }

    public void EndListening()
    {
        isListening = false;
    }
    private bool ShouldIgnoreInput()
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
    private void OnTimingInput(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[OnTimingInput] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[OnTimingInput] fired! isListening={isListening}");
        Debug.Log($"[OnTimingInput] Time: {Time.time}, lastInputTime: {lastInputTime}, CombatStartTime: {CombatManager.CombatStartTime}");

        if (ShouldIgnoreInput()) return;
        
        lastInputTime = Time.time;
        bool isPerfect = HasPerfectInput(currentTiming);

        if (!CombatManager.Instance.windowPrompted)
        {
            Debug.Log("윈도우 열리기 전 → 쿨다운만 적용, 판정 X");
            if (!isPerfect)
            {
                nextAllowedInputTime = Time.time + GlobalConfig.Instance.ActionInputCooldown;
                Debug.Log($"쿨다운 발동! {GlobalConfig.Instance.ActionInputCooldown}초");
            }
            return; // ❗ ResolveInput 호출 금지!
        }

        /*
        if (!isPerfect)
        {
            nextAllowedInputTime = Time.time + GlobalConfig.Instance.ActionInputCooldown;
            Debug.Log($"윈도우 밖 입력 → 쿨다운 적용! {GlobalConfig.Instance.ActionInputCooldown}초");
        }
        */
        bool isPlayer = CombatManager.Instance.CurrentController is PlayerController;
        CombatManager.Instance.ResolveInput(
            isPlayer,
            isPerfect,
            CombatManager.Instance.CurrentHit,
            CombatManager.Instance.CurrentController,
            CombatManager.Instance.CurrentResult
        );

        //if (isPerfect) EndListening(); // 중복 입력 방지 위해 즉시 리스너 종료
    }

    public bool EvaluateInput(PerfectTimingWindow timing)
    {
        // 최근 입력 시간이 timing 구간 안에 들어가는지 확인
        if (!lastInputTime.HasValue) return false;

        float relativeTime = lastInputTime.Value - CombatManager.CombatStartTime;
        return timing.Contains(relativeTime);
    }

    public bool HasPerfectInput(PerfectTimingWindow timing)
    {
        if (timing == null)
        {
            Debug.LogError("[HasPerfectInput] timing is null!");
            return false;
        }
        if (!lastInputTime.HasValue)
        {
            Debug.Log("[HasPerfectInput] lastInputTime 없음");
            return false;
        }
        if (CombatManager.CombatStartTime == 0f)
        {
            Debug.LogError("[HasPerfectInput] CombatManager.CombatStartTime is 0!");
            return false;
        }
        
        float relativeTime = lastInputTime.Value - CombatManager.CombatStartTime;
        bool isIn = timing.Contains(relativeTime);

        Debug.Log($"[HasPerfectInput] CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[HasPerfectInput] TURN-relative={relativeTime}, Window=({timing.start}~{timing.start + timing.duration}) → Contains={isIn}");

        return isIn;
    }


    private List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>();
    public void LoadTimingWindows(List<PerfectTimingWindow> timings)
    {
        currentTimings = timings;
        loadedTimings = timings;
        lastInputTime = -1f; // 초기화
    }

    private PerfectTimingWindow currentTiming;

    public void RegisterHitTiming(PerfectTimingWindow timing)
    {
        Debug.Log($"[RegisterHitTiming] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[RegisterHitTiming] Timing: {timing.start}~{timing.start + timing.duration}, CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[PerfectTimingWindow] start={timing.start} duration={timing.duration}");

        currentTiming = timing;
    }

    public void NotifyWindowClosed(bool isPlayer)
    {
        if (!CombatManager.Instance.CurrentHitResultShown)
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.ResolveInput(
                isPlayer,
                false,
                CombatManager.Instance.CurrentHit,
                CombatManager.Instance.CurrentController,
                CombatManager.Instance.CurrentResult
            );
            CombatManager.MarkCurrentHitResultShown();
            EndListening();
        }        
    }

    private bool IsInBufferPeriod()
    {
        float relativeTime = Time.time - CombatManager.CombatStartTime;

        float turnDuration = GlobalConfig.Instance.TurnDurationSeconds;

        bool inStartBuffer = relativeTime <= GlobalConfig.Instance.InputBufferStartSeconds;
        bool inEndBuffer = relativeTime >= (turnDuration - GlobalConfig.Instance.InputBufferEndSeconds);

        return inStartBuffer || inEndBuffer;
    }

    //public float CombatManager.CombatStartTime { get; set; }

}
