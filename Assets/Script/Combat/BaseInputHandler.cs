using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseInputHandler : MonoBehaviour
{
    protected InputAction perfectAction; // PerfectInput 액션
    protected bool isListening = false; // 입력 리스닝 상태

    protected List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>(); // 로드된 타이밍 윈도우 목록
    protected List<PerfectTimingWindow> currentTimings; // 현재 턴의 타이밍 윈도우 목록

    protected PerfectTimingWindow currentTiming; // 현재 타격의 PerfectTimingWindow
    protected float? lastInputTime = null; // 마지막 입력 시간
    protected float nextAllowedInputTime = 0f; // 다음 입력이 허용되는 시간 (쿨타임 관련)

    protected virtual void Awake()
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
    // ⬇️ 입력 콜백 등록
    protected virtual void OnEnable()
    {
        EnableInput();
    }

    protected virtual void OnDisable()
    {
        DisableInput();
    }
    public virtual void LoadTimingWindows(List<PerfectTimingWindow> timings)
    // 타이밍 윈도우 목록을 로드하고 저장
    {
        loadedTimings = timings;
        currentTimings = new List<PerfectTimingWindow>(timings);
        lastInputTime = null; // 초기화
    }
    protected bool IsWithinPerfectWindow(int hitIndex, float time) // 주어진 시간(time)이 특정 타격(hitIndex)의 PerfectTimingWindow 내에 있는지 확인
    {
        if (hitIndex < 0 || hitIndex >= loadedTimings.Count)
        {
            Debug.LogWarning($"Invalid hit index: {hitIndex}");
            return false;
        }

        float start = loadedTimings[hitIndex].start;
        float end = loadedTimings[hitIndex].End;
        return time >= start && time <= end;
    }
    protected virtual void RegisterInputCallbacks() { } // 입력 콜백 등록

    protected virtual void UnregisterInputCallbacks() { } // 입력 콜백 해제

    public virtual void EnableInput()
    {
        RegisterInputCallbacks();
        perfectAction?.Enable();
        isListening = true;
    }

    public virtual void DisableInput()
    {
        UnregisterInputCallbacks();
        perfectAction?.Disable();
        isListening = false;
    }
    public virtual bool IsInBufferPeriod() // 현재 입력이 버퍼 구간에 있는지 확인
    {
        float relativeTime = Time.time - CombatManager.CombatStartTime;

        float turnDuration = GlobalConfig.Instance.TurnDurationSeconds;

        bool inStartBuffer = relativeTime <= GlobalConfig.Instance.InputBufferStartSeconds;
        bool inEndBuffer = relativeTime >= (turnDuration - GlobalConfig.Instance.InputBufferEndSeconds);

        return inStartBuffer || inEndBuffer;
    }

    public virtual bool HasPerfectInput(PerfectTimingWindow timing)
    // 현재 턴의 PerfectTimingWindow을 사용하여 입력이 완벽한지 확인
    {
        //////////////////////////// 비정상적인 상황 체크 ////////////////////////////
        if (timing == null) // null 체크
        {
            Debug.LogError("[HasPerfectInput] timing is null!"); return false;
        }
        if (!lastInputTime.HasValue) // 마지막 입력 시간이 없을 경우
        {
            Debug.Log("[HasPerfectInput] lastInputTime 없음"); return false;
        }
        if (CombatManager.CombatStartTime == 0f) // CombatManager.CombatStartTime이 0일 경우
        {
            Debug.LogError("[HasPerfectInput] CombatManager.CombatStartTime is 0!"); return false;
        }
        ////////////////////////////////////////////////////////////////////////////

        float relativeTime = lastInputTime.Value - CombatManager.CombatStartTime; // 현재 턴의 상대 시간 계산
        bool isContain = timing.Contains(relativeTime); // 입력 시간이 PerfectTimingWindow에 포함되는지 확인

        Debug.Log($"[HasPerfectInput] CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[HasPerfectInput] TURN-relative={relativeTime}, Window=({timing.start}~{timing.start + timing.duration}) → Contains={isContain}");

        return isContain;
    }
    public virtual bool HasPerfectInput()
    // 매개변수를 받지 않는 형태로 오버로드(currentTiming 사용)
    {
        //////////////////////////// 비정상적인 상황 체크 ////////////////////////////
        if (!lastInputTime.HasValue) // 마지막 입력 시간이 없을 경우
        {
            Debug.Log("[HasPerfectInput] lastInputTime 없음"); return false;
        }
        if (CombatManager.CombatStartTime == 0f) // CombatManager.CombatStartTime이 0일 경우
        {
            Debug.LogError("[HasPerfectInput] CombatManager.CombatStartTime is 0!"); return false;
        }
        ////////////////////////////////////////////////////////////////////////////

        if (currentTiming == null)
        {
            Debug.LogError("[HasPerfectInput] currentTiming is null!");
            return false;
        }
        return HasPerfectInput(currentTiming); // currentTiming 을 매개변수에 전달하여 오버로드된 메서드 호출
    }
    protected virtual void OnTimingInput(InputAction.CallbackContext ctx) // 입력 이벤트 핸들러
    {
        //Debug.Log($"[OnTimingInput] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        //Debug.Log($"[OnTimingInput] fired! isListening={isListening}");
        //Debug.Log($"[OnTimingInput] Time: {Time.time}, lastInputTime: {lastInputTime}, CombatStartTime: {CombatManager.CombatStartTime}");
        Debug.LogWarning($"[입력 감지됨] Handler={this.GetType().Name}, isListening={isListening}, Time={Time.time}, InputSource={ctx.control.device.name}");
        CombatManager manager = FindAnyObjectByType<CombatManager>();
        bool isAtkPerfect = manager.AttackerPerfectInput;
        bool isDefPerfect = manager.DefenderPerfectInput;
        bool isPlayerAttacker = manager.IsPlayerAttacker;
        Debug.Log($"[OnTimingInput] 누구?:{isPlayerAttacker} , 공격자:{isAtkPerfect} , 방어자:{isDefPerfect}");
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

    public abstract void RegisterHitTiming(PerfectTimingWindow timing); // 현재 턴의 PerfectTimingWindow 등록
    public abstract void NotifyWindowClosed(bool isPlayer); // 윈도우가 닫혔을 때 알림
    public virtual void ResetInputState()
    {
        lastInputTime = null;
    }

}