using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }
    public event Action<SwordArtStyleData> OnStyleEquipped;
    public event Action<SwordArtStyleData> OnStyleUnequipped;
    // ��Ÿ�� �����ͷκ��� ������ Ŀ�ǵ� ���
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Combatant(string name)
    {
        Name = name;
    }

    public abstract CommandSelection ChooseCommand();

    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        _availableCommands.Clear(); // ���� Ŀ�ǵ� ��� �ʱ�ȭ
        if (styleData != null)
        {
            // ��Ÿ�Ͽ� ������ �׼� Ŀ�ǵ带 ����Ʈ�� ����
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