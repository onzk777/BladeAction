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
    
    // 2차 스탯 (런타임 상태)
    [Header("2차 스탯 (런타임 상태)")]
    public int currentHP;
    public int currentPoise;
    public int tempDRBonus = 0; // 임시 DR 보너스
    
    // 이벤트들
    public event Action<Combatant> OnStatsChanged;
    public event Action<int, int> OnHPChanged;
    public event Action<int, int> OnPoiseChanged;
    public event Action<Combatant> OnDefeated;
    
    // 1차 스탯 프로퍼티들 (CharacterData에서 가져옴)
    public int MaxHP => CharacterData?.MaxHP ?? 0;
    public int ATK => CharacterData?.ATK ?? 0;
    public int DR => CharacterData?.DR ?? 0;
    public int Crit => CharacterData?.Crit ?? 0;
    public int CritRatio => CharacterData?.CritRatio ?? 100;
    public int MaxPoise => CharacterData?.MaxPoise ?? 0;
    public int ParryPoiseDamage => CharacterData?.ParryPoiseDamage ?? 25;
    
    // 2차 스탯 프로퍼티들 (런타임 상태)
    public int HP { get => currentHP; set => currentHP = value; }
    public int CurrentPoise { get => currentPoise; set => currentPoise = value; }
    public bool IsDefeated => currentHP <= 0;
    public bool IsInterrupted => currentPoise <= 0;
    
    // 스타일 데이터로부터 가져온 커맨드 목록
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Combatant(CharacterData characterData)
    {
        CharacterData = characterData;
        InitializeRuntimeStats();
    }
    
    /// <summary>
    /// 런타임 스테이터스를 초기화합니다 (전투 시작 시 호출)
    /// </summary>
    public void InitializeRuntimeStats()
    {
        if (CharacterData != null)
        {
            currentHP = CharacterData.MaxHP;
            currentPoise = CharacterData.MaxPoise;
            tempDRBonus = 0;
            Debug.Log($"[Combatant] {Name} 런타임 스테이터스 초기화 - HP: {currentHP}/{MaxHP}, Poise: {currentPoise}/{MaxPoise}");
        }
    }
    
    /// <summary>
    /// 공격 턴 시작 시 Poise 회복
    /// </summary>
    public void ResetPoise()
    {
        int oldPoise = currentPoise;
        currentPoise = MaxPoise;
        Debug.Log($"[Combatant] {Name} Poise 회복: {oldPoise} → {currentPoise}");
        OnPoiseChanged?.Invoke(oldPoise, currentPoise);
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 쳐내기 당했을 때 Poise 감소
    /// </summary>
    /// <param name="amount">감소할 Poise 양</param>
    public void LosePoise(int amount)
    {
        int oldPoise = currentPoise;
        currentPoise = Mathf.Max(0, currentPoise - amount);
        Debug.Log($"[Combatant] {Name} Poise 감소: {oldPoise} → {currentPoise} (감소량: {amount})");
        OnPoiseChanged?.Invoke(oldPoise, currentPoise);
        OnStatsChanged?.Invoke(this);
        if (IsInterrupted) Debug.LogWarning($"[Combatant] {Name} Poise 소진! 중단 발생!");
    }
    
    /// <summary>
    /// 현재 Poise 상태를 문자열로 반환
    /// </summary>
    public string GetPoiseStatus()
    {
        return $"{currentPoise}/{MaxPoise}";
    }
    
    /// <summary>
    /// 현재 HP 상태를 문자열로 반환
    /// </summary>
    public string GetHPStatus()
    {
        return $"{currentHP}/{MaxHP}";
    }
    
    /// <summary>
    /// HP 회복
    /// </summary>
    public void Heal(int amount)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
        Debug.Log($"[Combatant] {Name} HP 회복: {oldHP} → {currentHP} (회복량: {amount})");
        OnHPChanged?.Invoke(oldHP, currentHP);
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 피해 받기
    /// </summary>
    public void TakeDamage(int finalDamage)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Max(0, currentHP - finalDamage);
        Debug.Log($"[Combatant] {Name} 피해 받음: {oldHP} → {currentHP} (최종 피해: {finalDamage})");
        OnHPChanged?.Invoke(oldHP, currentHP);
        OnStatsChanged?.Invoke(this);
        
        if (IsDefeated)
        {
            Debug.LogWarning($"[Combatant] {Name} HP 소진! 패배!");
            OnDefeated?.Invoke(this);
        }
    }
    
    /// <summary>
    /// 치명타 여부 확인
    /// </summary>
    public bool IsCriticalHit()
    {
        return UnityEngine.Random.Range(0, 100) < Crit;
    }
    
    /// <summary>
    /// 치명타 피해 계산
    /// </summary>
    public int CalculateCriticalDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * CritRatio / 100f);
    }
    
    /// <summary>
    /// 유효 DR 반환 (기본 DR + 임시 보너스)
    /// </summary>
    public int GetEffectiveDR()
    {
        return DR + tempDRBonus;
    }
    
    /// <summary>
    /// 막기 시 유효 DR 반환 (기본 DR + 막기 보너스 + 임시 보너스)
    /// </summary>
    public int GetGuardEffectiveDR()
    {
        if (CharacterData != null)
        {
            return DR + CharacterData.guardDRBonus + tempDRBonus;
        }
        return DR + tempDRBonus;
    }
    
    /// <summary>
    /// 막기 시 피해 감소 비율 반환
    /// </summary>
    public float GetGuardDamageReduction()
    {
        return CharacterData?.guardDamageReduction ?? 0.5f;
    }
    
    /// <summary>
    /// 임시 DR 보너스 설정
    /// </summary>
    public void SetTempDRBonus(int bonus)
    {
        tempDRBonus = bonus;
        Debug.Log($"[Combatant] {Name} 임시 DR 보너스 설정: {bonus} (총 DR: {GetEffectiveDR()})");
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 임시 DR 보너스 제거
    /// </summary>
    public void ClearTempDRBonus()
    {
        tempDRBonus = 0;
        Debug.Log($"[Combatant] {Name} 임시 DR 보너스 제거 (총 DR: {GetEffectiveDR()})");
        OnStatsChanged?.Invoke(this);
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