using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerInput))]
public class TimingInputHandler : MonoBehaviour
{
    public event Action OnPerfectInput;

    private InputAction perfectAction;

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

    private void OnTimingInput(InputAction.CallbackContext ctx)
    {
        //Debug.Log("[TimingInputHandler] PerfectInput �Է� ����");
        OnPerfectInput?.Invoke();
    }
}
