using System;
using System.Collections.Generic;
using System.Linq;
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
    
    // 검술 관리 (인벤토리와 독립)
    private List<ActionCommandData> acquiredActions = new List<ActionCommandData>();
    private ActionCommandData[] equippedActions = new ActionCommandData[4];
    
    // 장신구 슬롯 관리
    private int currentAccessorySlots = 3;
    private int maxAccessorySlots = 5;
    
    /// <summary>
    /// 현재 장신구 슬롯 개수
    /// </summary>
    public int CurrentAccessorySlots => currentAccessorySlots;
    
    /// <summary>
    /// 최대 장신구 슬롯 개수
    /// </summary>
    public int MaxAccessorySlots => maxAccessorySlots;
    
    /// <summary>
    /// 사용 가능한 검술 목록 (장착된 4개)
    /// </summary>
    public List<ActionCommandData> AvailableCommands
    {
        get
        {
            return equippedActions
                .Where(action => action != null)
                .ToList();
        }
    }

    public Character(CharacterData characterData)
    {
        CharacterData = characterData;
        
        // 장신구 슬롯 설정 초기화
        if (characterData != null)
        {
            currentAccessorySlots = characterData.initialAccessorySlots;
            maxAccessorySlots = characterData.maxAccessorySlots;
        }
        
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
    
    #region 검술 관리 시스템
    
    /// <summary>
    /// 검술 획득
    /// </summary>
    public bool AcquireAction(ActionCommandData action)
    {
        if (action == null)
            return false;
        
        // 중복 확인 (유파 검술과 습득 검술이 겹칠 수 있으므로 중복 허용)
        if (acquiredActions.Contains(action))
        {
            // 이미 보유 중이므로 추가하지 않음 (경고 없음)
            return true;
        }
        
        acquiredActions.Add(action);
        Debug.Log($"[Character] {Name}이(가) '{action.commandName}' 검술을 획득했습니다.");
        return true;
    }
    
    /// <summary>
    /// 검술 보유 확인
    /// </summary>
    public bool HasAction(ActionCommandData action)
    {
        return action != null && acquiredActions.Contains(action);
    }
    
    /// <summary>
    /// 검술 장착 (슬롯 0~3)
    /// </summary>
    public bool EquipAction(ActionCommandData action, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4)
        {
            Debug.LogWarning($"[Character] 잘못된 슬롯 인덱스: {slotIndex} (0~3만 가능)");
            return false;
        }
        
        if (action == null)
        {
            Debug.LogWarning($"[Character] null 검술은 장착할 수 없습니다.");
            return false;
        }
        
        // 이미 다른 슬롯에 장착되어 있는지 확인
        for (int i = 0; i < 4; i++)
        {
            if (equippedActions[i] == action)
            {
                Debug.LogWarning($"[Character] '{action.commandName}' 검술은 이미 슬롯 {i}에 장착되어 있습니다.");
                return false;
            }
        }
        
        // 기존 장착 검술 해제 (있다면)
        if (equippedActions[slotIndex] != null)
        {
            var previousAction = equippedActions[slotIndex];
            // 해제된 검술을 습득 목록에 다시 추가
            if (!acquiredActions.Contains(previousAction))
            {
                acquiredActions.Add(previousAction);
            }
            Debug.Log($"[Character] 슬롯 {slotIndex}의 '{previousAction.commandName}' 검술이 해제되어 습득 목록으로 돌아갑니다.");
        }
        
        // 새 검술 장착
        equippedActions[slotIndex] = action;
        
        // 습득 목록에서 제거
        acquiredActions.Remove(action);
        
        Debug.Log($"[Character] {Name}이(가) '{action.commandName}' 검술을 슬롯 {slotIndex}에 장착했습니다.");
        return true;
    }
    
    /// <summary>
    /// 검술 해제
    /// </summary>
    public bool UnequipAction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4)
        {
            Debug.LogWarning($"[Character] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }
        
        if (equippedActions[slotIndex] == null)
        {
            Debug.LogWarning($"[Character] 슬롯 {slotIndex}이(가) 비어있습니다.");
            return false;
        }
        
        var unequipped = equippedActions[slotIndex];
        equippedActions[slotIndex] = null;
        
        // 습득 목록에 다시 추가
        if (!acquiredActions.Contains(unequipped))
        {
            acquiredActions.Add(unequipped);
        }
        
        Debug.Log($"[Character] {Name}이(가) '{unequipped.commandName}' 검술을 슬롯 {slotIndex}에서 해제했습니다. 습득 목록으로 돌아갑니다.");
        return true;
    }
    
    /// <summary>
    /// 습득 검술 목록 반환
    /// </summary>
    public List<ActionCommandData> GetAcquiredActions()
    {
        return new List<ActionCommandData>(acquiredActions);
    }
    
    /// <summary>
    /// 특정 슬롯의 검술 반환
    /// </summary>
    public ActionCommandData GetEquippedAction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4)
            return null;
        
        return equippedActions[slotIndex];
    }
    
    /// <summary>
    /// 장착된 유파의 검술 목록 반환
    /// </summary>
    public List<ActionCommandData> GetStyleActions()
    {
        var styleItem = Inventory?.GetEquippedItem(BladeAction.Item.EquipmentSlotType.SwordArtStyle);
        if (styleItem == null)
            return new List<ActionCommandData>();
        
        // swordArtStyleData 직접 참조 또는 Key로 조회
        var styleData = styleItem.swordArtStyleData;
        
        if (styleData == null && !string.IsNullOrEmpty(styleItem.swordArtStyleKey))
        {
            // Key로 조회
            var styleDb = SwordArtStyleDatabase.Instance;
            if (styleDb != null)
            {
                styleData = styleDb.GetStyle(styleItem.swordArtStyleKey);
            }
        }
        
        if (styleData == null)
            return new List<ActionCommandData>();
        
        return styleData.GetActionCommands();
    }
    
    /// <summary>
    /// 유파 해제 시 유파 검술 자동 해제
    /// </summary>
    public void UnequipAllStyleActions()
    {
        var styleActions = GetStyleActions();
        if (styleActions == null || styleActions.Count == 0)
            return;
        
        int unequippedCount = 0;
        for (int i = 0; i < 4; i++)
        {
            if (equippedActions[i] != null && styleActions.Contains(equippedActions[i]))
            {
                var unequipped = equippedActions[i];
                equippedActions[i] = null;
                unequippedCount++;
                Debug.Log($"[Character] 유파 해제로 인한 검술 자동 해제: '{unequipped.commandName}' (슬롯 {i})");
            }
        }
        
        if (unequippedCount > 0)
        {
            Debug.Log($"[Character] 유파 해제로 인해 {unequippedCount}개 검술 자동 해제");
        }
    }
    
    #endregion
    
    #region 장신구 슬롯 관리
    
    /// <summary>
    /// 장신구 슬롯 추가 (성장/아이템 효과)
    /// </summary>
    /// <returns>슬롯 추가 성공 여부</returns>
    public bool AddAccessorySlot()
    {
        if (currentAccessorySlots >= maxAccessorySlots)
        {
            Debug.LogWarning($"[Character] {Name}의 장신구 슬롯이 최대치입니다: {currentAccessorySlots}/{maxAccessorySlots}");
            return false;
        }
        
        currentAccessorySlots++;
        
        // Inventory에도 슬롯 추가
        if (Inventory != null)
        {
            Inventory.AddAccessorySlot();
            Debug.Log($"[Character] {Name}의 장신구 슬롯 추가: {currentAccessorySlots - 1} → {currentAccessorySlots}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 장신구 슬롯 설정 (디버그/치트용)
    /// </summary>
    public void SetAccessorySlots(int count)
    {
        count = Mathf.Clamp(count, 1, maxAccessorySlots);
        
        if (count == currentAccessorySlots)
            return;
        
        int oldCount = currentAccessorySlots;
        currentAccessorySlots = count;
        
        // Inventory 재초기화 필요
        if (Inventory != null)
        {
            Inventory.ReinitializeEquipmentSlots(currentAccessorySlots);
            Debug.Log($"[Character] {Name}의 장신구 슬롯 설정: {oldCount} → {currentAccessorySlots}");
        }
    }
    
    #endregion

    /// <summary>
    /// 유파 장착 (하위 호환용 메서드, 실제 장착은 Inventory.EquipItem으로 처리)
    /// 이벤트만 발행합니다.
    /// </summary>
    public void EquipSwordArtStyle(SwordArtStyleData styleData)
    {
        EquippedStyle = styleData;
        OnStyleEquipped?.Invoke(styleData);
    }
        
    /// <summary>
    /// 유파 해제 (하위 호환용 메서드, 실제 해제는 Inventory.UnequipItem으로 처리)
    /// 이벤트만 발행합니다.
    /// </summary>
    public void UnequipStyle()
    {
        var old = EquippedStyle;
        if (old != null)
        {
            EquippedStyle = null;
            OnStyleUnequipped?.Invoke(old);
        }
    }
}

