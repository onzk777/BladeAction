using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class TimingInputHandler : MonoBehaviour
{
    public event Action OnPerfectInput;
    private InputAction perfectAction;

    private bool isListening = false;
    private List<PerfectTimingWindow> currentTimings;

    private float timingWindowStartTime; // 현재 턴의 시작 시간
    private float? lastInputTime = null; // 마지막 입력 시간

    

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
        timingWindowStartTime = Time.time;
    }

    public void EndListening()
    {
        isListening = false;
    }

    private void OnTimingInput(InputAction.CallbackContext ctx)
    {
        if (!isListening) return;
        lastInputTime = Time.time;

#if UNITY_EDITOR
        Debug.Log("[TimingInputHandler] PerfectInput 입력 감지");
        Debug.Log($"[TimingInput] 입력 감지됨: {lastInputTime}");
#endif
        OnPerfectInput?.Invoke();
    }

    public bool EvaluateInput(PerfectTimingWindow timing)
    {
        // 최근 입력 시간이 timing 구간 안에 들어가는지 확인
        if (!lastInputTime.HasValue) return false;

        float relativeTime = lastInputTime.Value - timingWindowStartTime;
        return timing.Contains(relativeTime);
    }

    private List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>();
    public void LoadTimingWindows(List<PerfectTimingWindow> timings)
    {
        loadedTimings = timings;
        lastInputTime = -1f; // 초기화
    }

    private PerfectTimingWindow currentTiming;

    public void RegisterHitTiming(PerfectTimingWindow timing)
    {
        currentTiming = timing;
        lastInputTime = -1f; // 초기화
    }

}
