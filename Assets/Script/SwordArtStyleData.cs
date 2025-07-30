using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    [Header("검술 스타일 기본")]
    [Tooltip("에디터에 표시될 스타일 이름")]
    public string styleName = "New Style";

    [Header("사용 가능한 커맨드 목록")]
    [Tooltip("이 스타일에서 사용할 커맨드를 순서대로 나열")]
    [SerializeField]
    private List<ActionCommandData> commandSet = new List<ActionCommandData>();
    public List<ActionCommandData> CommandSet
    {
        get => commandSet;
        set
        {
            commandSet = value;
            commandSet.RemoveAll(item => item == null);
        }
    }

    public IReadOnlyList<ActionCommandData> ActionCommands => commandSet.AsReadOnly();

    public List<ActionCommandData> GetActionCommands() => new List<ActionCommandData>(commandSet);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(styleName))
            styleName = "New Style";
        if (commandSet != null)
            commandSet.RemoveAll(item => item == null);
    }
#endif

}

