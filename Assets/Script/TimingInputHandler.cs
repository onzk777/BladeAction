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
        // PlayerInput 컴포넌트 확보
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[TimingInputHandler] PlayerInput 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        // 액션맵 "Combat" 내 PerfectInput 액션을 가져옴
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

    private void OnTimingInput(InputAction.CallbackContext ctx)
    {
        //Debug.Log("[TimingInputHandler] PerfectInput 입력 감지");
        OnPerfectInput?.Invoke();
    }
}
