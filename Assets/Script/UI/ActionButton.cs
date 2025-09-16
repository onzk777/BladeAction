using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ActionButton : MonoBehaviour
{
    [Header("UI References")]
    public Button button; // Button 사용
    public TextMeshProUGUI buttonText;

    private ActionCommandData commandData;
    private int buttonIndex;
    private bool isFocused = false; // 포커스 상태 저장

    public event Action<int> OnButtonClicked;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        Debug.Log($"[ActionButton] 버튼 클릭됨: {buttonIndex}");
        
        // UI 이벤트만 전달 (검술 실행은 PlayerActionSelectUI에서 처리)
        OnButtonClicked?.Invoke(buttonIndex);
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
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void SetFocused(bool focused)
    {
        // 포커스 상태 저장
        isFocused = focused;
        Debug.Log($"[ActionButton] 버튼 {buttonIndex} 포커스 상태: {focused}");
    }
    
    public bool IsFocused()
    {
        return isFocused;
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