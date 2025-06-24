using NUnit.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSwordArtStyle", menuName = "Combat/SwordArtStyle")]
public class SwordArtStyleData : ScriptableObject
{
    public string styleName;
    public ActionCommandData[] commandSet = new ActionCommandData[5]; // 장착 시 자동 할당

    public ActionCommandData[] GetActionCommands()
    {
        return commandSet;
    }

    
}

