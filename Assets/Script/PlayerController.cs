using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public SwordArtStyleData equippedStyle; // ✅ 인스펙터에서 연결 가능
    public PlayerCombatant combatant; // 내부 상태 (MonoBehaviour 아님)

    [SerializeField]
    public List<global::CommandSelection> selectedCommandIndices = new();
    

    void Awake()
    {
        combatant = new PlayerCombatant("Player");
        combatant.EquipSwordArtStyle(equippedStyle); // 검술 

        var resultList = new List<CombatantCommandResult>();
        foreach (var sel in selectedCommandIndices)
        {
            if(equippedStyle.commandSet != null && sel.index < equippedStyle.commandSet.Length)
            {
                var cmd = equippedStyle.commandSet[sel.index];
                resultList.Add(new CombatantCommandResult(cmd));
            }
        }
        combatant.SelectedCommandResults = resultList;
    }
    
    void Start()
    {        
        
    }
}