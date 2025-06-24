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
        if (combatMap == null)
        {
            Debug.LogError("Combat ���� ã�� �� �����ϴ�.");
            return;
        }
        timingInput = combatMap.FindAction("TimingInput");
        if (timingInput == null)
        {
            Debug.LogError("TimingInput �׼��� ã�� �� �����ϴ�.");
            return;
        }
        Debug.Log("TimingInput �׼� ���� �Ϸ�");
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
        //Debug.Log("Ÿ�̹� �Է� �õ���!");
        if (manager == null || !manager.IsTimingWindowOpen)
            return; // Ÿ�̹� �����찡 �������� ������ ����

        //Debug.Log("Ÿ�̹� �Է� ����!");
        manager?.RegisterPerfectInput();
        
    }
}
