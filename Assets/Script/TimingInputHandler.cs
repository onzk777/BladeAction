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
            return; // Ÿ�̹� �����찡 �������� ������ ����

        //Debug.Log("Ÿ�̹� �Է� ����!");
        manager?.RegisterPerfectInput();
    }
}
