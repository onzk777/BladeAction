using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class AttackerInputHandler : BaseInputHandler
{
    protected override void RegisterInputCallbacks() // 입력 콜백 등록
    {
        if (perfectAction != null)
        {
            perfectAction.performed += OnTimingInput;
            perfectAction.Enable();
        }
    }
    protected override void UnregisterInputCallbacks() // 입력 콜백 해제
    {
        if (perfectAction != null)
        {
            perfectAction.performed -= OnTimingInput;
            perfectAction.Disable();
        }
    }
    public override void RegisterHitTiming(PerfectTimingWindow timing) // 현재 턴의 PerfectTimingWindow을 등록                                                                       
    {
        Debug.Log($"[RegisterHitTiming] Handler InstanceID: {this.GetInstanceID()} CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[RegisterHitTiming] Timing: {timing.start}~{timing.start + timing.duration}, CombatManager.CombatStartTime={CombatManager.CombatStartTime}");
        Debug.Log($"[PerfectTimingWindow] start={timing.start} duration={timing.duration}");

        currentTiming = timing;
    }


    public override void NotifyWindowClosed(bool isPlayer) // 윈도우가 닫혔을 때 호출되는 메서드
    {
        Debug.Log($"[NotifyWindowClosed] currentTiming: {currentTiming}, lastInputTime: {lastInputTime}");
        if (!lastInputTime.HasValue)
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.ResolveInput(false);
        }                  
    }
}
