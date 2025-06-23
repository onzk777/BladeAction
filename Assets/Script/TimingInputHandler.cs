using UnityEngine;
using UnityEngine.InputSystem;

public class TimingInputHandler : MonoBehaviour
{
    public InputAction timingInput;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private CombatManager manager;

    private void Awake()
    {
        if (manager == null)
            manager = FindAnyObjectByType<CombatManager>();

        var combatMap = inputActions.FindActionMap("Combat");
        timingInput = combatMap.FindAction("TimingInput");
    }



    private void OnEnable()
    {
        timingInput.Enable();
        timingInput.performed += OnTimingInput;
    }

    private void OnDisable()
    {
        timingInput.Disable();
        timingInput.performed -= OnTimingInput;
    }

    private void OnTimingInput(InputAction.CallbackContext context)
    {
        if (manager == null || !manager.IsTimingWindowOpen)
            return; // 타이밍 윈도우가 열려있지 않으면 무시

        //Debug.Log("타이밍 입력 성공!");
        manager?.RegisterPerfectInput();
    }
}
