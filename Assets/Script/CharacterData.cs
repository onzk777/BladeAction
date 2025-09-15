using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName = "Unknown";
    
    [Header("1차 스탯 (맨몸 상태 - 기초 데이터)")]
    [Tooltip("최대 체력")]
    public int baseMaxHP = 100;
    [Tooltip("기본 공격력")]
    public int baseATK = 20;
    [Tooltip("기본 방어력")]
    public int baseDR = 0;
    [Tooltip("치명타 확률 (%)")]
    public int baseCrit = 0;
    [Tooltip("치명타 배율 (%)")]
    public int baseCritRatio = 150;
    [Tooltip("최대 포이즈")]
    public int baseMaxPoise = 100;
    [Tooltip("패링 시 포이즈 피해량")]
    public int baseParryPoiseDamage = 25;
    
    [Header("막기 관련 스테이터스")]
    public int guardDRBonus = 5; // 막기 시 DR 보너스
    public float guardDamageReduction = 0.5f; // 막기 시 피해 감소 비율 (0.5 = 50% 감소)
    
    [Header("임시 스탯 보너스")]
    public int tempDRBonus = 0; // 임시 DR 보너스 (기타 상태 효과 등)
    
    // 1차 스탯 프로퍼티들 (기존 코드와의 호환성을 위해 유지)
    public int MaxHP => baseMaxHP;
    public int ATK => baseATK;
    public int DR => baseDR;
    public int Crit => baseCrit;
    public int CritRatio => baseCritRatio;
    public int MaxPoise => baseMaxPoise;
    public int ParryPoiseDamage => baseParryPoiseDamage;

    /// <summary>
    /// 1차 스탯 데이터를 반환합니다 (기초 데이터)
    /// </summary>
    public CharacterBaseStats GetBaseStats()
    {
        return new CharacterBaseStats
        {
            maxHP = baseMaxHP,
            atk = baseATK,
            dr = baseDR,
            crit = baseCrit,
            critRatio = baseCritRatio,
            maxPoise = baseMaxPoise,
            parryPoiseDamage = baseParryPoiseDamage,
            guardDRBonus = guardDRBonus,
            guardDamageReduction = guardDamageReduction
        };
    }
    
    [System.Serializable]
    public struct CharacterBaseStats
    {
        public int maxHP;
        public int atk;
        public int dr;
        public int crit;
        public int critRatio;
        public int maxPoise;
        public int parryPoiseDamage;
        public int guardDRBonus;
        public float guardDamageReduction;
    }

}
