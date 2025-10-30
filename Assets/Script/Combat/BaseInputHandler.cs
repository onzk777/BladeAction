using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BaseInputHandler : MonoBehaviour
{
    protected InputAction perfectAction; // PerfectInput 액션
    protected bool isListening = false; // 입력 리스닝 상태
    public bool IsPlayer { get; private set; } = false; // 기본값 false
    protected List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>(); // 로드된 타이밍 윈도우 목록
    protected List<PerfectTimingWindow> currentTimings; // 현재 턴의 타이밍 윈도우 목록

    protected PerfectTimingWindow currentTiming; // 현재 타격의 PerfectTimingWindow
    protected float? lastInputTime = null; // 마지막 입력 시간
    protected float nextAllowedInputTime = 0f; // 다음 입력이 허용되는 시간 (쿨타임 관련)
    public float NextAllowedInputTime => nextAllowedInputTime; // 외부에서 접근할 수 있도록 프로퍼티로 공개
    
    // AI 입력의 완벽 입력 여부를 저장하는 필드
    protected bool? aiInputIsPerfect = null;

    protected virtual void Awake()
    {
        // PlayerInput 초기화는 Start로 지연 (GameInputManager Awake 대기)
    }
    
    protected virtual void Start()
    {
        InitializePlayerInput();
    }
    
    /// <summary>
    /// PlayerInput 초기화
    /// </summary>
    private void InitializePlayerInput()
    {
        // PlayerInput 컴포넌트 가져오기 (GameInputManager에서)
        PlayerInput playerInput = null;
        
        if (GameInputManager.Instance != null)
        {
            playerInput = GameInputManager.Instance.GetPlayerInput();
        }
        
        if (playerInput == null)
        {
            // Fallback: 같은 GameObject에서 찾기 (하위 호환)
            playerInput = GetComponent<PlayerInput>();
        }
        
        if (playerInput == null)
        {
            Debug.LogError("[TimingInputHandler] PlayerInput 컴포넌트를 찾을 수 없습니다. GameInputManager를 확인하세요.");
            return;
        }

        // Actions Asset이 연결되어 있는지 확인
        if (playerInput.actions == null)
        {
            Debug.LogError("[TimingInputHandler] PlayerInput에 Actions Asset이 연결되지 않았습니다.");
            return;
        }

        // 액션맵 "Combat" 내 PerfectInput 액션을 가져옴
        try
        {
            playerInput.SwitchCurrentActionMap("Combat");
            perfectAction = playerInput.actions["PerfectInput"];
            if (perfectAction == null)
            {
                Debug.LogError("[TimingInputHandler] 'PerfectInput' 액션을 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log($"[InputTrace][Start] handler:{GetType().Name} devices:{string.Join(",", playerInput.devices)} controls:{perfectAction.controls.Count}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TimingInputHandler] 액션 맵 전환 실패: {e.Message}");
        }
    }
    // ⬇️ 입력 콜백 등록
    protected virtual void OnEnable()
    {

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
        Debug.Log($"[InputTrace][InputLifecycle] Enable - handler:{GetType().Name} perfectActionNull:{perfectAction == null} isPlayer:{IsPlayer} time:{Time.time:F4} frame:{Time.frameCount}");
        RegisterInputCallbacks();
        if (perfectAction != null)
        {
            perfectAction.Enable();
            Debug.Log($"[InputTrace][InputLifecycle] perfectAction.Enable - handler:{GetType().Name} controls:{perfectAction.controls.Count} time:{Time.time:F4} frame:{Time.frameCount}");
            perfectAction.started -= OnTimingInput;
            perfectAction.performed -= OnTimingInput;
            perfectAction.canceled -= OnTimingInput;

            perfectAction.started += OnTimingInput;
            perfectAction.performed += OnTimingInput;
            perfectAction.canceled += OnTimingInput;

            Debug.Log($"[InputTrace][InputLifecycle] 이벤트 재등록 완료 - handler:{GetType().Name} time:{Time.time:F4} frame:{Time.frameCount}");
        }
        isListening = true;
        Debug.Log($"[InputTrace][InputLifecycle] isListening->{isListening} - handler:{GetType().Name} time:{Time.time:F4} frame:{Time.frameCount}");
    }

    public virtual void DisableInput()
    {
        Debug.Log($"[InputTrace][InputLifecycle] Disable - handler:{GetType().Name} time:{Time.time:F4} frame:{Time.frameCount}");
        UnregisterInputCallbacks();
        if (perfectAction != null)
        {
            perfectAction.started -= OnTimingInput;
            perfectAction.performed -= OnTimingInput;
            perfectAction.canceled -= OnTimingInput;
            perfectAction.Disable();
            Debug.Log($"[InputTrace][InputLifecycle] 이벤트 해제 - handler:{GetType().Name} time:{Time.time:F4} frame:{Time.frameCount}");
        }
        isListening = false;
        Debug.Log($"[InputTrace][InputLifecycle] isListening->{isListening} - handler:{GetType().Name} time:{Time.time:F4} frame:{Time.frameCount}");
    }
    public virtual bool IsInBufferPeriod() // 현재 입력이 버퍼 구간에 있는지 확인
    {
        float relativeTime = TurnTimer.ElapsedTime; // 현재 턴의 상대 시간

        // CombatManager에서 현재 턴 지속 시간을 가져옴
        float turnDuration = CombatManager.Instance?.CurrentTurnDuration ?? 1.0f;

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
        ////////////////////////////////////////////////////////////////////////////

        // AI 입력의 경우 전달받은 isPerfect 값을 우선적으로 사용
        if (!IsPlayer && aiInputIsPerfect.HasValue)
        {
            Debug.Log($"[HasPerfectInput] AI 입력 - 전달받은 값 사용: {aiInputIsPerfect.Value}");
            return aiInputIsPerfect.Value;
        }

        // 플레이어 입력의 경우 기존 로직 사용 (타이밍 윈도우 내에 있는지 확인)
        float relativeTime = lastInputTime.Value; // 현재 턴의 상대 시간 계산
        bool isContain = timing.Contains(relativeTime); // 입력 시간이 PerfectTimingWindow에 포함되는지 확인

        Debug.Log($"[HasPerfectInput] 플레이어 입력 - input={lastInputTime.Value:F5}, window=({timing.start:F5} ~ {timing.End:F5})");

        Debug.Log($"[HasPerfectInput] 결과: {isContain}");

        return isContain;
    }
    public virtual bool HasPerfectInput()
    // 매개변수를 받지 않는 형태로 오버로드(currentTiming 사용)
    {
        if (currentTiming == null)
        {
            Debug.LogError("[HasPerfectInput] currentTiming is null!");
            return false;
        }
        return HasPerfectInput(currentTiming); // currentTiming 을 매개변수에 전달하여 오버로드된 메서드 호출
    }
    // 입력 이벤트 핸들러
    protected virtual void OnTimingInput(InputAction.CallbackContext ctx) // 입력 이벤트 핸들러
    {
        // 🆕 DefenderInputHandler는 발사체 기반으로 완전 오버라이드됨
        if (this is DefenderInputHandler)
        {
            Debug.Log("[InputTrace][BaseOnTiming] DefenderInputHandler 전용 구현 - 차단");
            return;
        }
        
        Debug.LogWarning($"[InputTrace][OnTiming] handler:{GetType().Name} phase:{ctx.phase} isListening:{isListening} device:{ctx.control?.device?.name} time:{Time.time:F4} frame:{Time.frameCount}");

        CombatManager manager = FindAnyObjectByType<CombatManager>();
        bool isPlayerAttacker = manager.IsPlayerAttacker;
        if (ShouldIgnoreInput()) return; // 입력 무시 여부 확인
        Debug.Log("[OnTimingInput] ShouldIgnoreInput 통과함. 판정 루틴 진입 중.");
        lastInputTime = TurnTimer.ElapsedTime;
        bool isPerfect = HasPerfectInput(currentTiming);

        if (CombatManager.Instance.windowPrompted) // 입력 윈도우가 열린 상태라면
        {
            Debug.Log("윈도우가 열림, 입력 시 쿨다운 일괄 적용 (판정에 따라 차등)");
            float cooldown = isPerfect
            ? GlobalConfig.Instance.ActionInputCooldown_Perfect
            : GlobalConfig.Instance.ActionInputCooldown_Default;

            nextAllowedInputTime = TurnTimer.ElapsedTime + cooldown;
            Debug.Log($"쿨다운 발동! {GlobalConfig.Instance.ActionInputCooldown_Default}초");
        }
        CombatManager.Instance.OnInputReceivedFromHandler(this);
    }
    // AI 입력 기록 메서드
    public void RecordAIInput(float inputTime, bool isPerfect)
    {
        lastInputTime = inputTime;
        aiInputIsPerfect = isPerfect; // AI 입력의 완벽 입력 여부를 저장

        if (CombatManager.Instance.windowPrompted)
        {
            float cooldown = isPerfect
                ? GlobalConfig.Instance.ActionInputCooldown_Perfect
                : GlobalConfig.Instance.ActionInputCooldown_Default;

            nextAllowedInputTime = TurnTimer.ElapsedTime + cooldown;
            Debug.Log($"[AI 입력 기록] isPerfect={isPerfect}, inputTime={inputTime}, cooldown={cooldown}");
        }
        // AI 입력이 기록되면 CombatManager에 알림
        CombatManager.Instance.OnInputReceivedFromHandler(this);
    }


    private bool ShouldIgnoreInput() // 입력을 무시해야 하는지 확인
    {
        Debug.Log($"[InputTrace][ShouldIgnore] handler:{GetType().Name} isListening:{isListening} buffer:{IsInBufferPeriod()} cooldown:{TurnTimer.ElapsedTime < nextAllowedInputTime}");
        if (!isListening)
        {
            Debug.LogWarning($"[InputTrace][ShouldIgnore] handler:{GetType().Name} 입력 무시 - 리스닝 상태 아님");
            return true;
        }
        if (IsInBufferPeriod()) // 버퍼 구간에 있는 경우 입력 무시
        {
            Debug.Log($"[InputTrace][ShouldIgnore] handler:{GetType().Name} 입력 무시 - 버퍼 구간");
            return true;
        }
        if (TurnTimer.ElapsedTime < nextAllowedInputTime)
        {
            Debug.Log($"[InputTrace][ShouldIgnore] handler:{GetType().Name} 입력 무시 - 쿨다운 (Elapsed:{TurnTimer.ElapsedTime}, Next:{nextAllowedInputTime})");
            return true;
        }
        return false;
    }
    public float GetLastInputTime()
    {
        return lastInputTime ?? float.MaxValue; // 입력 안했으면 무조건 느린 것으로
    }
    public abstract void RegisterHitTiming(PerfectTimingWindow timing); // 현재 턴의 PerfectTimingWindow 등록
    public abstract void NotifyWindowClosed(bool isPlayer);

    public virtual void ResetInputState()
    {
        lastInputTime = null;
        aiInputIsPerfect = null; // AI 입력 완벽 입력 여부도 초기화
    }
    public void SetIsPlayer(bool isPlayer)
    {
        IsPlayer = isPlayer;
        Debug.Log($"[InputTrace][SetIsPlayer] handler:{GetType().Name} IsPlayer->{IsPlayer}");
    }

    public void ResetCooldown()
    {
        nextAllowedInputTime = 0f; // 쿨다운 초기화
        Debug.Log("[BaseInputHandler] 쿨다운 초기화됨");
    }

}