using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefenderInputHandler : BaseInputHandler
{
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
#if UNITY_EDITOR
        Debug.Log("[DefenseInputHandler] EnableInput() 호출됨");
#endif
    }
    protected override void RegisterInputCallbacks()
    {
        if (perfectAction != null)
        {
            perfectAction.performed += OnTimingInput;
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
}
