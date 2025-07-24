using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefenseInputHandler : BaseInputHandler
{
    private float perfectStart; // 공격자의 완벽 타이밍 시작
    private float perfectEnd;   // 공격자의 완벽 타이밍 종료

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
    }
    
    /// /////////////////////////////////////////////////////////////////일단 여기까지 진행함
    public override void NotifyWindowClosed(bool isPlayer) 
    {
        if (!lastInputTime.HasValue)
        {
            Debug.Log("윈도우 종료 → 입력 없음, 실패 처리");
            CombatManager.Instance.ResolveInput(false);
        }
    }
    
    protected override void OnTimingInput(InputAction.CallbackContext ctx)
    {
        base.OnTimingInput(ctx); // 기본 입력 처리 호출
        Debug.Log($"[DefenseInputHandler] OnTimingInput 호출: {lastInputTime}");
    }
    public override void EnableInput()
    {
        base.EnableInput();
        Debug.Log("[DefenseInputHandler] EnableInput() 호출됨");
    }
}
