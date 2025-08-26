using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Combatant
{
    public string Name { get; protected set; }
    public SwordArtStyleData EquippedStyle { get; protected set; }
    public event Action<SwordArtStyleData> OnStyleEquipped;
    public event Action<SwordArtStyleData> OnStyleUnequipped;
    
    // 자세 포인트 시스템
    public float CurrentPosturePoints { get; private set; }
    public float MaxPosturePoints { get; private set; }
    public bool IsInterrupted => CurrentPosturePoints <= 0f;
    
    // 스타일 데이터로부터 가져온 커맨드 목록
    public IReadOnlyList<ActionCommandData> AvailableCommands => _availableCommands;
    private List<ActionCommandData> _availableCommands = new List<ActionCommandData>();

    public Combatant(string name)
    {
        Name = name;
        InitializePosturePoints();
    }
    
    /// <summary>
    /// 자세 포인트 초기화
    /// </summary>
    private void InitializePosturePoints()
    {
        MaxPosturePoints = GlobalConfig.Instance.PosturePointsMax;
        CurrentPosturePoints = MaxPosturePoints;
        Debug.Log($"[{Name}] 자세 포인트 초기화: {CurrentPosturePoints}/{MaxPosturePoints}");
    }
    
    /// <summary>
    /// 공격 턴 시작 시 자세 포인트 회복
    /// </summary>
    public void ResetPosturePoints()
    {
        CurrentPosturePoints = MaxPosturePoints;
        Debug.Log($"[{Name}] 자세 포인트 회복: {CurrentPosturePoints}/{MaxPosturePoints}");
    }
    
    /// <summary>
    /// 쳐내기 당했을 때 자세 포인트 감소
    /// </summary>
    /// <param name="amount">감소할 자세 포인트 양</param>
    public void LosePosturePoints(float amount)
    {
        float oldPosture = CurrentPosturePoints;
        CurrentPosturePoints = Mathf.Max(0f, CurrentPosturePoints - amount);
        
        Debug.Log($"[{Name}] 자세 포인트 감소: {oldPosture} → {CurrentPosturePoints} (감소량: {amount})");
        
        if (IsInterrupted)
        {
            Debug.LogWarning($"[{Name}] 자세 포인트 소진! 중단 발생!");
        }
    }
    
    /// <summary>
    /// 현재 자세 포인트 상태를 문자열로 반환
    /// </summary>
    public string GetPostureStatus()
    {
        return $"{CurrentPosturePoints:F0}/{MaxPosturePoints:F0}";
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