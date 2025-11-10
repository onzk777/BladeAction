using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 팀 기반 액션 선택 UI. 컨트롤러 타입에 따라 입력 가능 여부를 구분하고,
/// 각 캐릭터가 보유한 액션 커맨드를 동적으로 버튼으로 구성한다.
/// </summary>
[DisallowMultipleComponent]
public class TeamActionSelectUI : MonoBehaviour
{
    private const float FocusPollInterval = 0.1f;

    [Header("UI References")]
    [SerializeField] private GameObject actionButtonPrefab;
    [SerializeField] private Transform buttonContainerOverride;

    [Header("General Settings")]
    [SerializeField] private int maxButtons = 5;
    [SerializeField] private bool autoRegisterToManager = true;
    [SerializeField] private bool autoAssignController = true;
    [SerializeField] private bool maintainFocusWhenInteractive = true;
    [SerializeField] private bool allowSpectatorHighlight = true;

    [Header("Team Settings")]
    [SerializeField] private CombatCharacterManager.CombatTeam team = CombatCharacterManager.CombatTeam.TeamA;
    private readonly List<ActionButton> actionButtons = new List<ActionButton>();
    private CanvasGroup canvasGroup;
    private Transform actionButtonContainer;
    private bool isInitialized;
    private ICombatController assignedController;
    private Coroutine focusCoroutine;
    private int lastSpectatorHighlightIndex = -1;

    public CombatCharacterManager.CombatTeam Team => team;

    protected virtual bool ShouldAutoRegister => autoRegisterToManager;
    protected virtual bool ShouldAutoAssignController => autoAssignController;
    protected virtual bool ShouldMaintainFocus => maintainFocusWhenInteractive;
    protected virtual bool AllowSpectatorHighlight => allowSpectatorHighlight;

    protected virtual bool AllowInteractionForController(ICombatController controller)
    {
        return controller is PlayerController;
    }

    protected virtual void Reset()
    {
        maxButtons = Mathf.Max(1, maxButtons);
        team = CombatCharacterManager.CombatTeam.TeamA;
    }

    protected virtual void Awake()
    {
        EnsureCanvasGroup();
        EnsureButtonContainer();

        if (ShouldAutoRegister)
        {
            RegisterToSelectionManager();
        }

        if (ShouldAutoAssignController)
        {
            TryAutoAssignController();
        }
    }

    protected virtual void OnEnable()
    {
        if (assignedController == null && ShouldAutoAssignController)
        {
            StartCoroutine(WaitForControllerAndInitialize());
        }
        else
        {
            Initialize();
        }
    }

    protected virtual void OnDisable()
    {
        StopFocusCoroutine();
    }

    protected virtual void OnDestroy()
    {
        StopFocusCoroutine();
        CleanupButtonEventHandlers();
    }

    public void AssignController(ICombatController controller)
    {
        assignedController = controller;
        Initialize();
    }

    public void RefreshButtons()
    {
        Initialize(true);
    }

    public void SetSelectedButton(int index)
    {
        if (!AllowSpectatorHighlight)
        {
            return;
        }

        if (actionButtons.Count == 0)
        {
            Debug.LogWarning("[TeamActionSelectUI] 버튼이 생성되지 않아 선택 상태를 표시할 수 없습니다.");
            return;
        }

        if (index < 0 || index >= actionButtons.Count)
        {
            Debug.LogWarning($"[TeamActionSelectUI] 잘못된 하이라이트 인덱스 {index} (버튼 수: {actionButtons.Count})");
            return;
        }

        lastSpectatorHighlightIndex = index;

        for (int i = 0; i < actionButtons.Count; i++)
        {
            HighlightButton(actionButtons[i], i == index);
        }
    }

    public int GetCurrentSelectedButtonIndex()
    {
        if (assignedController == null)
        {
            return 0;
        }

        if (AllowInteractionForController(assignedController))
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
            {
                for (int i = 0; i < actionButtons.Count; i++)
                {
                    if (actionButtons[i] != null && actionButtons[i].gameObject == eventSystem.currentSelectedGameObject)
                    {
                        return i;
                    }
                }
            }

            return Mathf.Max(0, lastSpectatorHighlightIndex);
        }

        return Mathf.Max(0, lastSpectatorHighlightIndex);
    }

    public void SetTeam(CombatCharacterManager.CombatTeam newTeam)
    {
        team = newTeam;
        if (ActionCommandSelectionManager.Instance != null)
        {
            ActionCommandSelectionManager.Instance.RegisterTeamActionUI(team, this);
        }
    }

    private void Initialize(bool forceRefresh = false)
    {
        if (assignedController == null)
        {
            if (!ShouldAutoAssignController)
            {
                Debug.LogWarning("[TeamActionSelectUI] Controller가 지정되지 않았습니다. AssignController 또는 autoAssign 옵션을 확인해주세요.");
            }
            return;
        }

        if (isInitialized && !forceRefresh)
        {
            return;
        }

        CreateActionButtons();
        UpdateInteractionState();

        isInitialized = true;

        if (AllowInteractionForController(assignedController) && ShouldMaintainFocus)
        {
            StartFocusCoroutine();
        }
        else
        {
            StopFocusCoroutine();
        }
    }

    private void CreateActionButtons()
    {
        CleanupButtonEventHandlers();

        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        actionButtons.Clear();
        lastSpectatorHighlightIndex = -1;

        if (actionButtonPrefab == null || actionButtonContainer == null)
        {
            Debug.LogError("[TeamActionSelectUI] 액션 버튼 프리팹 또는 컨테이너가 설정되지 않았습니다.");
            return;
        }

        var availableCommands = assignedController?.Character?.AvailableCommands;
        var playerController = assignedController as PlayerController;

        if (playerController != null && playerController.UseTestMode)
        {
            CreateActionButton(0, ResolveTestModeCommand(playerController));
        }
        else if (availableCommands != null && availableCommands.Count > 0)
        {
            int buttonCount = Mathf.Min(availableCommands.Count, maxButtons);
            for (int i = 0; i < buttonCount; i++)
            {
                CreateActionButton(i, availableCommands[i]);
            }
        }
        else
        {
            for (int i = 0; i < maxButtons; i++)
            {
                CreateActionButton(i, null);
            }
        }
    }

    private void CreateActionButton(int index, ActionCommandData commandData)
    {
        var buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        var actionButton = buttonObj.GetComponent<ActionButton>() ?? buttonObj.AddComponent<ActionButton>();

        actionButton.Initialize(commandData, index);
        actionButton.OnButtonClicked += HandleButtonClicked;

        if (!AllowInteractionForController(assignedController))
        {
            actionButton.SetInteractable(false);
        }

        actionButtons.Add(actionButton);

        if (index == 0)
        {
            if (!AllowInteractionForController(assignedController) && AllowSpectatorHighlight)
            {
                HighlightButton(actionButton, true);
            }
            lastSpectatorHighlightIndex = 0;
        }
    }

    private ActionCommandData ResolveTestModeCommand(PlayerController playerController)
    {
        var availableCommands = playerController.Character?.AvailableCommands;
        if (availableCommands != null && availableCommands.Count > 0)
        {
            int commandIndex = Mathf.Clamp(playerController.TestCommandIndex, 0, availableCommands.Count - 1);
            return availableCommands[commandIndex];
        }

        return null;
    }

    private void UpdateInteractionState()
    {
        bool interactable = assignedController != null && AllowInteractionForController(assignedController);

        foreach (var button in actionButtons)
        {
            button?.SetInteractable(interactable);
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
            canvasGroup.alpha = interactable ? 1f : canvasGroup.alpha;
        }
    }

    private void RegisterToSelectionManager()
    {
        var manager = ActionCommandSelectionManager.Instance ?? ActionCommandSelectionManager.EnsureInstance();
        manager?.RegisterTeamActionUI(team, this);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
    }

    private void EnsureButtonContainer()
    {
        if (buttonContainerOverride != null)
        {
            actionButtonContainer = buttonContainerOverride;
            if (actionButtonContainer == null || !actionButtonContainer.gameObject.scene.IsValid())
            {
                actionButtonContainer = transform;
            }
            return;
        }

        actionButtonContainer = transform;
    }

    private bool TryAutoAssignController()
    {
        var manager = CombatCharacterManager.Instance;
        if (manager == null)
        {
            return false;
        }

        var slot = manager.GetLeaderSlot(team);
        if (slot?.Controller != null)
        {
            AssignController(slot.Controller);
            return true;
        }

        return false;
    }

    private IEnumerator WaitForControllerAndInitialize()
    {
        while (assignedController == null)
        {
            if (!TryAutoAssignController())
            {
                yield return null;
                continue;
            }
        }

        Initialize();
    }

    private void HandleButtonClicked(int buttonIndex)
    {
        Debug.Log($"[TeamActionSelectUI] 버튼 클릭: {buttonIndex} (Team: {team})");
    }

    private void HighlightButton(ActionButton actionButton, bool highlight)
    {
        if (actionButton == null)
        {
            return;
        }

        var image = actionButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = highlight ? new Color(1f, 1f, 0.5f, 1f) : Color.white;
        }

        actionButton.SetFocused(highlight);
    }

    private void StartFocusCoroutine()
    {
        if (focusCoroutine != null)
        {
            return;
        }

        focusCoroutine = StartCoroutine(FocusRetentionRoutine());
    }

    private void StopFocusCoroutine()
    {
        if (focusCoroutine != null)
        {
            StopCoroutine(focusCoroutine);
            focusCoroutine = null;
        }
    }

    private IEnumerator FocusRetentionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(FocusPollInterval);

            if (assignedController == null || !AllowInteractionForController(assignedController))
            {
                continue;
            }

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                continue;
            }

            bool hasSelectedButton = false;
            foreach (var button in actionButtons)
            {
                if (button != null && button.gameObject == eventSystem.currentSelectedGameObject)
                {
                    hasSelectedButton = true;
                    break;
                }
            }

            if (!hasSelectedButton && actionButtons.Count > 0)
            {
                eventSystem.SetSelectedGameObject(actionButtons[0].gameObject);
                lastSpectatorHighlightIndex = 0;
                Debug.Log("[TeamActionSelectUI] 포커스를 첫 번째 버튼으로 복원했습니다.");
            }
        }
    }

    private void CleanupButtonEventHandlers()
    {
        foreach (var button in actionButtons)
        {
            if (button != null)
            {
                button.OnButtonClicked -= HandleButtonClicked;
            }
        }
    }
}

