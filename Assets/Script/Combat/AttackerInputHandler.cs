using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class AttackerInputHandler : BaseInputHandler
{
    protected override void RegisterInputCallbacks() // 입력 콜백 등록
    {
        if (!IsPlayer) return; // 플레이어가 아닌 경우 입력 콜백 등록하지 않음
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
        currentTiming = timing;
    }
    
    public override void NotifyWindowClosed(bool isPlayer)
    {
        if (!lastInputTime.HasValue)
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.ResolveInput(this, false);
        }
    }
    
}
