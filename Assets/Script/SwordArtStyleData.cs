using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    [Header("�˼� ��Ÿ�� �⺻")]
    [Tooltip("�����Ϳ� ǥ�õ� ��Ÿ�� �̸�")]
    public string styleName = "New Style";

    [Header("��� ������ Ŀ�ǵ� ���")]
    [Tooltip("�� ��Ÿ�Ͽ��� ����� Ŀ�ǵ带 ������� ����")]
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

