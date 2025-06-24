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
            Debug.LogError("Combat 맵을 찾을 수 없습니다.");
            return;
        }
        timingInput = combatMap.FindAction("TimingInput");
        if (timingInput == null)
        {
            Debug.LogError("TimingInput 액션을 찾을 수 없습니다.");
            return;
        }
        Debug.Log("TimingInput 액션 연결 완료");
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
        //Debug.Log("타이밍 입력 시도됨!");
        if (manager == null || !manager.IsTimingWindowOpen)
            return; // 타이밍 윈도우가 열려있지 않으면 무시

        //Debug.Log("타이밍 입력 성공!");
        manager?.RegisterPerfectInput();
        
    }
}
