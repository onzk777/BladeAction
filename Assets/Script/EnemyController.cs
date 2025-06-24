using System;
using UnityEngine;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    public SwordArtStyleData equippedStyle; // 적의 검술 스타일
    public EnemyCombatant combatant;

    /*
    [SerializeField]
    public List<global::CommandSelection> selectedCommandIndices = new();
    */

    [SerializeField]
    public int[] selectedCommandIndices = new int[5];

    void Awake()
    {
        combatant = new EnemyCombatant("Monster");
        combatant.EquipSwordArtStyle(equippedStyle);

        var resultList = new List<CombatantCommandResult>();
        for (int i = 0; i < selectedCommandIndices.Length; i++)
        {
            int sel = selectedCommandIndices[i];
            if (equippedStyle.commandSet[sel] == null)
            {
                Debug.LogError($"[EnemyController] commandSet[{sel}] 는 null입니다.");
                continue; // 또는 return;
            }
            if (equippedStyle.commandSet != null && sel < equippedStyle.commandSet.Length)
            {
                var cmd = equippedStyle.commandSet[sel];
                resultList.Add(new CombatantCommandResult(cmd));
            }
        }
        combatant.SelectedCommandResults = resultList;

    }
}
