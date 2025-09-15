using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Combatant
{
    public CharacterData CharacterData { get; protected set; }
    public string Name => CharacterData?.characterName ?? "Unknown";
    public SwordArtStyleData EquippedStyle { get; protected set; }
    public event Action<SwordArtStyleData> OnStyleEquipped;
    public event Action<SwordArtStyleData> OnStyleUnequipped;
    
    // CharacterData를 통한 스테이터스 접근
    public int HP => CharacterData?.HP ?? 0;
    public int MaxHP => CharacterData?.MaxHP ?? 0;
    public int ATK => CharacterData?.ATK ?? 0;
    public int DR => CharacterData?.DR ?? 0;
    public int Crit => CharacterData?.Crit ?? 0;
    public int CritRatio => CharacterData?.CritRatio ?? 100;
    public int CurrentPoise => CharacterData?.CurrentPoise ?? 0;
    public int MaxPoise => CharacterData?.MaxPoise ?? 0;
    public int ParryPoiseDamage => CharacterData?.ParryPoiseDamage ?? 25;
    public bool IsDefeated => CharacterData?.IsDefeated ?? true;
    public bool IsInterrupted => CharacterData?.IsInterrupted ?? true;
    
    // 스타일 데이터로부터 가져온 커맨드 목록
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Combatant(CharacterData characterData)
    {
        CharacterData = characterData;
    }
    
    /// <summary>
    /// 공격 턴 시작 시 Poise 회복
    /// </summary>
    public void ResetPoise()
    {
        CharacterData?.RestorePoise();
    }
    
    /// <summary>
    /// 쳐내기 당했을 때 Poise 감소
    /// </summary>
    /// <param name="amount">감소할 Poise 양</param>
    public void LosePoise(int amount)
    {
        CharacterData?.LosePoise(amount);
    }
    
    /// <summary>
    /// 현재 Poise 상태를 문자열로 반환
    /// </summary>
    public string GetPoiseStatus()
    {
        return CharacterData?.GetPoiseStatus() ?? "0/0";
    }
    
    /// <summary>
    /// 현재 HP 상태를 문자열로 반환
    /// </summary>
    public string GetHPStatus()
    {
        return CharacterData?.GetHPStatus() ?? "0/0";
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