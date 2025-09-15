using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ActionButton : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public TextMeshProUGUI buttonText;
    
    private ActionCommandData commandData;
    private int buttonIndex;
    private bool isFocused = false;
    
    public event Action<int> OnButtonClicked;
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (button == null)
            button = GetComponent<Button>();
        
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        
        // 버튼 클릭 이벤트 연결
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
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
    
    public void OnButtonClick()
    {
        OnButtonClicked?.Invoke(buttonIndex);
    }
    
    public void SetFocused(bool focused)
    {
        isFocused = focused;
        
        if (button != null)
        {
            // Unity Button의 기본 포커스 시스템 사용
            if (focused)
            {
                // Unity EventSystem에서 이 버튼을 선택된 상태로 설정
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }
    }
    
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
    
    public ActionCommandData GetCommandData()
    {
        return commandData;
    }
    
    public int GetButtonIndex()
    {
        return buttonIndex;
    }
    
    public bool IsFocused()
    {
        return isFocused;
    }
}
