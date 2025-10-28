using System;
using System.Collections.Generic;
using UnityEngine;
using BladeAction.Item;
using BladeAction.Combat;

public abstract class Character
{
    public CharacterData CharacterData { get; protected set; }
    public string Name => CharacterData?.characterName ?? "Unknown";
    public SwordArtStyleData EquippedStyle { get; protected set; }
    public event Action<SwordArtStyleData> OnStyleEquipped;
    public event Action<SwordArtStyleData> OnStyleUnequipped;
    
    // 인벤토리 (장비 스탯 적용용)
    public CharacterInventory Inventory { get; set; }
    
    // 런타임 전투 스탯 (StatsCalculationManager가 업데이트)
    public CombatStats stats = new CombatStats();
    
    // 이벤트들
    public event Action<Character> OnStatsChanged;
    public event Action<int, int> OnHPChanged;
    public event Action<int, int> OnPoiseChanged;
    public event Action<Character> OnDefeated;
    
    // 편의 프로퍼티들 (stats 필드로 리다이렉트)
    public float MaxHP => stats.maxHP;
    public float CurrentHP { get => stats.currentHP; set => stats.currentHP = value; }
    public float MaxPoise => stats.maxPoise;
    public float CurrentPoise { get => stats.currentPoise; set => stats.currentPoise = value; }
    public int ATK => (int)stats.attack;
    public int DR => (int)stats.defenseDR;
    public float CritChance => stats.critChance;
    public float CritMultiplier => stats.critMultiplier;
    public int ParryPoiseDamage => (int)stats.parryPoiseDamage;
    public int TempDRBonus { get => stats.tempDRBonus; set => stats.tempDRBonus = value; }
    
    // 하위 호환 프로퍼티
    public int HP { get => (int)stats.currentHP; set => stats.currentHP = value; }
    public int currentHP { get => (int)stats.currentHP; set => stats.currentHP = value; }
    public int currentPoise { get => (int)stats.currentPoise; set => stats.currentPoise = value; }
    public int tempDRBonus { get => stats.tempDRBonus; set => stats.tempDRBonus = value; }
    
    // 상태 확인
    public bool IsDefeated => stats.currentHP <= 0;
    public bool IsInterrupted => stats.currentPoise <= 0;
    
    // 스타일 데이터로부터 가져온 커맨드 목록
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Character(CharacterData characterData)
    {
        CharacterData = characterData;
        InitializeRuntimeStats();
    }
    
    /// <summary>
    /// 런타임 스테이터스를 초기화합니다 (전투 시작 시 호출)
    /// CharacterData.baseStats를 기반으로 stats를 초기화합니다.
    /// </summary>
    public void InitializeRuntimeStats()
    {
        if (CharacterData != null)
        {
            // CharacterData의 baseStats를 복사
            stats = CharacterData.baseStats;
            
            // currentHP/currentPoise를 maxHP/maxPoise로 초기화
            stats.currentHP = stats.maxHP;
            stats.currentPoise = stats.maxPoise;
            
            Debug.Log($"[Character] {Name} 런타임 스테이터스 초기화 - HP: {stats.currentHP}/{stats.maxHP}, Poise: {stats.currentPoise}/{stats.maxPoise}");
        }
    }
    
    /// <summary>
    /// 공격 턴 시작 시 Poise 회복
    /// </summary>
    public void ResetPoise()
    {
        float oldPoise = stats.currentPoise;
        stats.currentPoise = stats.maxPoise;
        Debug.Log($"[Character] {Name} Poise 회복: {oldPoise} → {stats.currentPoise}");
        OnPoiseChanged?.Invoke((int)oldPoise, (int)stats.currentPoise);
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 쳐내기 당했을 때 Poise 감소
    /// </summary>
    /// <param name="amount">감소할 Poise 양</param>
    public void LosePoise(int amount)
    {
        float oldPoise = stats.currentPoise;
        stats.currentPoise = Mathf.Max(0, stats.currentPoise - amount);
        Debug.Log($"[Character] {Name} Poise 감소: {oldPoise} → {stats.currentPoise} (감소량: {amount})");
        OnPoiseChanged?.Invoke((int)oldPoise, (int)stats.currentPoise);
        OnStatsChanged?.Invoke(this);
        if (IsInterrupted) Debug.LogWarning($"[Character] {Name} Poise 소진! 중단 발생!");
    }
    
    /// <summary>
    /// 현재 Poise 상태를 문자열로 반환
    /// </summary>
    public string GetPoiseStatus()
    {
        return $"{stats.currentPoise:F0}/{stats.maxPoise:F0}";
    }
    
    /// <summary>
    /// 현재 HP 상태를 문자열로 반환
    /// </summary>
    public string GetHPStatus()
    {
        return $"{stats.currentHP:F0}/{stats.maxHP:F0}";
    }
    
    /// <summary>
    /// HP 회복
    /// </summary>
    public void Heal(int amount)
    {
        float oldHP = stats.currentHP;
        stats.currentHP = Mathf.Min(stats.maxHP, stats.currentHP + amount);
        Debug.Log($"[Character] {Name} HP 회복: {oldHP} → {stats.currentHP} (회복량: {amount})");
        OnHPChanged?.Invoke((int)oldHP, (int)stats.currentHP);
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 피해 받기
    /// </summary>
    public void TakeDamage(int finalDamage)
    {
        float oldHP = stats.currentHP;
        stats.currentHP = Mathf.Max(0, stats.currentHP - finalDamage);
        Debug.Log($"[Character] {Name} 피해 받음: {oldHP} → {stats.currentHP} (최종 피해: {finalDamage})");
        OnHPChanged?.Invoke((int)oldHP, (int)stats.currentHP);
        OnStatsChanged?.Invoke(this);
        
        if (IsDefeated)
        {
            Debug.LogWarning($"[Character] {Name} HP 소진! 패배!");
            OnDefeated?.Invoke(this);
        }
    }
    
    /// <summary>
    /// 치명타 여부 확인
    /// </summary>
    public bool IsCriticalHit()
    {
        return UnityEngine.Random.value < stats.critChance;
    }
    
    /// <summary>
    /// 치명타 피해 계산
    /// </summary>
    public int CalculateCriticalDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * stats.critMultiplier);
    }
    
    /// <summary>
    /// 최종 DR 반환 (기본 DR + 임시 보너스)
    /// </summary>
    public int GetFinalDR()
    {
        return (int)stats.defenseDR + stats.tempDRBonus;
    }
    
    /// <summary>
    /// 막기 시 최종 DR 반환 (기본 DR + 막기 보너스 + 임시 보너스)
    /// </summary>
    public int GetGuardFinalDR()
    {
        return (int)stats.defenseDR + stats.guardDRBonus + stats.tempDRBonus;
    }
    
    /// <summary>
    /// 막기 시 피해 감소 비율 반환
    /// </summary>
    public float GetGuardDamageReduction()
    {
        return stats.guardDamageReduction;
    }
    
    /// <summary>
    /// 임시 DR 보너스 설정
    /// </summary>
    public void SetTempDRBonus(int bonus)
    {
        stats.tempDRBonus = bonus;
        Debug.Log($"[Character] {Name} 임시 DR 보너스 설정: {bonus} (총 DR: {GetFinalDR()})");
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 임시 DR 보너스 제거
    /// </summary>
    public void ClearTempDRBonus()
    {
        stats.tempDRBonus = 0;
        Debug.Log($"[Character] {Name} 임시 DR 보너스 제거 (총 DR: {GetFinalDR()})");
        OnStatsChanged?.Invoke(this);
    }
    
    /// <summary>
    /// 스탯 변경 이벤트를 발행합니다 (외부 시스템용)
    /// </summary>
    public void NotifyStatsChanged()
    {
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

