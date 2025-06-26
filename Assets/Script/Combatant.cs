using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }
    public event Action<SwordArtStyleData> OnStyleEquipped;
    public event Action<SwordArtStyleData> OnStyleUnequipped;
    // 스타일 데이터로부터 가져온 커맨드 목록
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Combatant(string name)
    {
        Name = name;
    }

    public abstract CommandSelection ChooseCommand();

    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        _availableCommands.Clear(); // 기존 커맨드 목록 초기화
        if (styleData != null)
        {
            // 스타일에 설정된 액션 커맨드를 리스트로 복사
            _availableCommands.AddRange(styleData.GetActionCommands());
        }
    
        OnStyleEquipped?.Invoke(styleData);
    }
        
    public void UnequipStyle()
    {
        var old = EquippedStyle;
        if (old != null)
        {
            _availableCommands.Clear();
            EquippedStyle = null;
            OnStyleUnequipped?.Invoke(old);
        }
    }
}