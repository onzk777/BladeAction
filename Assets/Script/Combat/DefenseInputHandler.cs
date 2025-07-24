using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefenseInputHandler : BaseInputHandler
{
    private float perfectStart; // �������� �Ϻ� Ÿ�̹� ����
    private float perfectEnd;   // �������� �Ϻ� Ÿ�̹� ����

    // �������� Ŀ�ǵ� �����͸� �޾� Ÿ�̹� �����츦 ����
    public void LoadFromOpponentCommand(ActionCommandData opponentCommand)
    {
        // opponentCommand�� ������ Ÿ�̹� ������ ����Ʈ�� ������ �θ��� �޼���� ����
        List<PerfectTimingWindow> copiedTimings = opponentCommand.perfectTimings;
        base.LoadTimingWindows(copiedTimings);
    }

    public override void RegisterHitTiming(PerfectTimingWindow timing)
    {
        loadedTimings = new List<PerfectTimingWindow> { timing };
    }
    
    /// /////////////////////////////////////////////////////////////////�ϴ� ������� ������
    public override void NotifyWindowClosed(bool isPlayer) 
    {
        if (!lastInputTime.HasValue)
        {
            Debug.Log("������ ���� �� �Է� ����, ���� ó��");
            CombatManager.Instance.ResolveInput(false);
        }
    }
    
    protected override void OnTimingInput(InputAction.CallbackContext ctx)
    {
        base.OnTimingInput(ctx); // �⺻ �Է� ó�� ȣ��
        Debug.Log($"[DefenseInputHandler] OnTimingInput ȣ��: {lastInputTime}");
    }
    public override void EnableInput()
    {
        base.EnableInput();
        Debug.Log("[DefenseInputHandler] EnableInput() ȣ���");
    }
}
