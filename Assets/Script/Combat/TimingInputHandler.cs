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

    private float timingWindowStartTime; // ���� ���� ���� �ð�
    private float? lastInputTime = null; // ������ �Է� �ð�

    

    private void Awake()
    {
        // PlayerInput ������Ʈ Ȯ��
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[TimingInputHandler] PlayerInput ������Ʈ�� ã�� �� �����ϴ�.");
            return;
        }

        // �׼Ǹ� "Combat" �� PerfectInput �׼��� ������
        playerInput.SwitchCurrentActionMap("Combat");
        perfectAction = playerInput.actions["PerfectInput"];
        if (perfectAction == null)
        {
            Debug.LogError("[TimingInputHandler] 'PerfectInput' �׼��� ã�� �� �����ϴ�.");
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
        Debug.Log("[TimingInputHandler] PerfectInput �Է� ����");
        Debug.Log($"[TimingInput] �Է� ������: {lastInputTime}");
#endif
        OnPerfectInput?.Invoke();
    }

    public bool EvaluateInput(PerfectTimingWindow timing)
    {
        // �ֱ� �Է� �ð��� timing ���� �ȿ� ������ Ȯ��
        if (!lastInputTime.HasValue) return false;

        float relativeTime = lastInputTime.Value - timingWindowStartTime;
        return timing.Contains(relativeTime);
    }

    private List<PerfectTimingWindow> loadedTimings = new List<PerfectTimingWindow>();
    public void LoadTimingWindows(List<PerfectTimingWindow> timings)
    {
        loadedTimings = timings;
        lastInputTime = -1f; // �ʱ�ȭ
    }

    private PerfectTimingWindow currentTiming;

    public void RegisterHitTiming(PerfectTimingWindow timing)
    {
        currentTiming = timing;
        lastInputTime = -1f; // �ʱ�ȭ
    }

}
