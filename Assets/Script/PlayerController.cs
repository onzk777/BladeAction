using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public SwordArtStyleData equippedStyle; // ✅ 인스펙터에서 연결 가능
    public PlayerCombatant combatant; // 내부 상태 (MonoBehaviour 아님)
    [SerializeField]
    private List<ActionCommandData> selectedCommands;

    [SerializeField]
    public List<CommandSelection> selectedCommandIndices = new();


    void Awake()
    {
        combatant = new PlayerCombatant("Player");
        combatant.EquipSwordArtStyle(equippedStyle); // 검술 장비
        //combatant.SelectedCommands = selectedCommands;

        var list = new List<ActionCommandData>();
        foreach (var sel in selectedCommandIndices)
        {
            if(equippedStyle.commandSet != null && sel.index < equippedStyle.commandSet.Length)
            {
                list.Add(equippedStyle.commandSet[sel.index]);
            }
        }
        combatant.SelectedCommands = list;
    }
    void Start()
    {        
        
    }
}