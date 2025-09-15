using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ActionButton : MonoBehaviour
{
    [Header("UI References")]
    public Toggle toggle; // Button 대신 Toggle 사용
    public TextMeshProUGUI buttonText;

    private ActionCommandData commandData;
    private int buttonIndex;

    public event Action<int> OnButtonClicked;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn) // Toggle이 선택되었을 때만 이벤트 발생
        {
            Debug.Log($"[ActionButton] Toggle 선택됨: {buttonIndex}");
            OnButtonClicked?.Invoke(buttonIndex);
        }
    }

    public void Initialize(ActionCommandData data, int index)
    {
        commandData = data;
        buttonIndex = index;

        if (buttonText != null)
        {
            buttonText.text = data?.commandName ?? $"검술 {index + 1}";
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (toggle != null)
        {
            toggle.interactable = interactable;
        }
    }

    public void SetSelected(bool selected)
    {
        if (toggle != null)
        {
            toggle.isOn = selected;
        }
    }

    public bool IsSelected()
    {
        return toggle != null && toggle.isOn;
    }

    public ActionCommandData GetCommandData()
    {
        return commandData;
    }

    public int GetButtonIndex()
    {
        return buttonIndex;
    }
}