using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    public string styleName;
    [SerializeField] public List<ActionCommandData> commandSet = new List<ActionCommandData>();

    public List<ActionCommandData> GetActionCommands()
    {
        // ����Ʈ�� �ٷ� ��ȯ
        return commandSet;
    }
}

