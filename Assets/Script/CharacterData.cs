using System;
using UnityEngine;

public class CharacterData
{
    public string characterName { get; private set; }
    public int HP { get; private set; }
    public int MaxHP { get; private set; }
    public int ATK { get; private set; }
    public int DR { get; private set; }
    public int Crit { get; private set; }
    public int CritRatio { get; private set; }
    public int CurrentPoise { get; private set; }
    public int MaxPoise { get; private set; }
    public int ParryPoiseDamage { get; private set; }
    public bool IsDefeated => HP <= 0;
    public bool IsInterrupted => CurrentPoise <= 0;

    public event Action<CharacterData> OnStatsChanged;
    public event Action<int, int> OnHPChanged;
    public event Action<int, int> OnPoiseChanged;

    public CharacterData(string name, int maxHp = 100, int atk = 20, int dr = 0, int crit = 0, int critRatio = 150, int maxPoise = 100, int parryPoiseDamage = 25)
    {
        characterName = name;
        MaxHP = maxHp;
        HP = MaxHP;
        ATK = atk;
        DR = dr;
        Crit = crit;
        CritRatio = critRatio;
        MaxPoise = maxPoise;
        CurrentPoise = MaxPoise;
        ParryPoiseDamage = parryPoiseDamage;
    }

    public void Heal(int amount)
    {
        int oldHP = HP;
        HP = Mathf.Min(MaxHP, HP + amount);
        Debug.Log($"[{characterName}] HP 회복: {oldHP} → {HP} (회복량: {amount})");
        OnHPChanged?.Invoke(oldHP, HP);
        OnStatsChanged?.Invoke(this);
    }

    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - DR);
        int oldHP = HP;
        HP = Mathf.Max(0, HP - actualDamage);
        Debug.Log($"[{characterName}] 피해 받음: {oldHP} → {HP} (원래 피해: {damage}, DR 적용 후: {actualDamage})");
        OnHPChanged?.Invoke(oldHP, HP);
        OnStatsChanged?.Invoke(this);
        if (IsDefeated) Debug.LogWarning($"[{characterName}] HP 소진! 패배!");
    }

    public bool IsCriticalHit()
    {
        return UnityEngine.Random.Range(0, 100) < Crit;
    }

    public int CalculateCriticalDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * CritRatio / 100f);
    }

    public void RestorePoise()
    {
        int oldPoise = CurrentPoise;
        CurrentPoise = MaxPoise;
        Debug.Log($"[{characterName}] Poise 회복: {oldPoise} → {CurrentPoise}");
        OnPoiseChanged?.Invoke(oldPoise, CurrentPoise);
        OnStatsChanged?.Invoke(this);
    }

    public void LosePoise(int amount)
    {
        int oldPoise = CurrentPoise;
        CurrentPoise = Mathf.Max(0, CurrentPoise - amount);
        Debug.Log($"[{characterName}] Poise 감소: {oldPoise} → {CurrentPoise} (감소량: {amount})");
        OnPoiseChanged?.Invoke(oldPoise, CurrentPoise);
        OnStatsChanged?.Invoke(this);
        if (IsInterrupted) Debug.LogWarning($"[{characterName}] Poise 소진! 중단 발생!");
    }

    public string GetHPStatus() => $"{HP}/{MaxHP}";
    public string GetPoiseStatus() => $"{CurrentPoise}/{MaxPoise}";
}
