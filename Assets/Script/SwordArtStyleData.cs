using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    public string styleName;
    public ActionCommandData[] commandSet = new ActionCommandData[5]; // ���� �� �ڵ� �Ҵ�

    public ActionCommandData[] GetActionCommands()
    {
        return commandSet;
    }

    
}

