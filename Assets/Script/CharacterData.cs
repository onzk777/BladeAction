using System;
using UnityEngine;
using BladeAction.Combat;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/CharacterData", order = 1)]
public class CharacterData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    public string characterName = "Unknown";
    
    [Header("기본 전투 스탯")]
    [Tooltip("캐릭터의 기본 전투 스탯 (맨몸 상태)")]
    public CombatStats baseStats = new CombatStats
    {
        maxHP = 100,
        attack = 20,
        defenseDR = 0,
        critChance = 0.1f,
        critMultiplier = 1.5f,
        maxPoise = 100,
        parryPoiseDamage = 25,
        guardDamageReduction = 0.5f,
        guardDRBonus = 5
    };
    
    // ===== 이하 필드는 구형 (호환용) - 삭제 예정 =====
    [Header("구형 스탯 필드 (Deprecated)")]
    [System.Obsolete("Use baseStats.maxHP instead")]
    [Tooltip("최대 체력 - Deprecated: baseStats.maxHP 사용")]
    public int baseMaxHP = 100;
    
    [System.Obsolete("Use baseStats.attack instead")]
    [Tooltip("기본 공격력 - Deprecated: baseStats.attack 사용")]
    public int baseATK = 20;
    
    [System.Obsolete("Use baseStats.defenseDR instead")]
    [Tooltip("기본 방어력 - Deprecated: baseStats.defenseDR 사용")]
    public int baseDR = 0;
    
    [System.Obsolete("Use baseStats.critChance instead")]
    [Tooltip("치명타 확률 (0~1) - Deprecated: baseStats.critChance 사용")]
    [Range(0f, 1f)]
    public float baseCritChance = 0f;
    
    [System.Obsolete("Use baseStats.critMultiplier instead")]
    [Tooltip("치명타 배율 (배수, 예: 1.5 = 150%) - Deprecated: baseStats.critMultiplier 사용")]
    public float baseCritMultiplier = 1.5f;
    
    // 이하 필드는 구형(호환용) - 마이그레이션 후 사용하지 않음
    [System.Obsolete("Use baseCritChance (0~1) instead")]
    [Tooltip("치명타 확률 (%) - Deprecated: baseCritChance 사용")]
    public int baseCrit = 0;
    
    [System.Obsolete("Use baseCritMultiplier (float multiplier) instead")]
    [Tooltip("치명타 배율 (%) - Deprecated: baseCritMultiplier 사용")]
    public int baseCritRatio = 150;
    
    [System.Obsolete("Use baseStats.maxPoise instead")]
    [Tooltip("최대 포이즈 - Deprecated: baseStats.maxPoise 사용")]
    public int baseMaxPoise = 100;
    
    [System.Obsolete("Use baseStats.parryPoiseDamage instead")]
    [Tooltip("패링 시 포이즈 피해량 - Deprecated: baseStats.parryPoiseDamage 사용")]
    public int baseParryPoiseDamage = 25;
    
    [System.Obsolete("막기 관련 스탯은 baseStats에 포함됨")]
    [Header("막기 관련 스테이터스 (Deprecated)")]
    public int guardDRBonus = 5;
    
    [System.Obsolete("Use baseStats.guardDamageReduction instead")]
    public float guardDamageReduction = 0.5f;
    
    [System.Obsolete("Use baseStats.tempDRBonus instead")]
    [Header("임시 스탯 보너스 (Deprecated)")]
    public int tempDRBonus = 0;
    
    // 1차 스탯 프로퍼티들 (호환성을 위해 baseStats 참조)
    public int MaxHP => (int)baseStats.maxHP;
    public int ATK => (int)baseStats.attack;
    public int DR => (int)baseStats.defenseDR;
    public float CritChance => baseStats.critChance;
    public float CritMultiplier => baseStats.critMultiplier;
    public int MaxPoise => (int)baseStats.maxPoise;
    public int ParryPoiseDamage => (int)baseStats.parryPoiseDamage;

    /// <summary>
    /// 1차 스탯 데이터를 반환합니다 (기초 데이터)
    /// [DEPRECATED] 구형 스탯 구조체를 사용합니다. 직접 프로퍼티를 사용하세요.
    /// </summary>
    [System.Obsolete("Use properties directly (ATK, MaxHP, CritChance, CritMultiplier, etc.) instead")]
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

    [Header("NPC AI 설정")]
    [Tooltip("NPC 행동 확률 설정 (AI 전용)")]
    public NPCBehaviorProbabilities npcBehavior = new NPCBehaviorProbabilities();
    
    [Header("Behavior Tree 설정")]
    [Tooltip("이 캐릭터가 사용할 Behavior Tree 리스트 (BT 에셋을 여기에 할당하세요)")]
    public System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData> behaviorTrees = new System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData>();
    
    /// <summary>
    /// Behavior Tree 인스턴스화 메서드
    /// 
    /// 변경 사항 (블랙보드 패턴):
    /// - BT 복사를 하지 않음 (BT는 순수 로직, 공유 가능)
    /// - 상태는 BTBlackboard에서 개체별로 관리
    /// - 이 메서드는 하위 호환성을 위해 유지 (아무것도 하지 않음)
    /// </summary>
    public void InstantiateBehaviorTrees()
    {
        // 블랙보드 패턴으로 변경되어 BT 복사가 불필요함
        // behaviorTrees를 그대로 사용 (읽기 전용)
        
        if (behaviorTrees == null || behaviorTrees.Count == 0)
        {
            Debug.LogWarning($"[CharacterData] {characterName} - Behavior Tree가 설정되지 않았습니다!");
        }
        else
        {
            Debug.Log($"[CharacterData] {characterName} - Behavior Tree {behaviorTrees.Count}개 확인 (블랙보드 패턴, 복사 불필요)");
        }
    }
    
    /// <summary>
    /// 모든 BT 노드의 전투 실행 상태를 리셋
    /// 
    /// 변경 사항 (블랙보드 패턴):
    /// - BT 자체는 상태가 없으므로 리셋 불필요
    /// - Blackboard.ResetCombat()을 대신 호출해야 함
    /// - 이 메서드는 하위 호환성을 위해 유지 (아무것도 하지 않음)
    /// </summary>
    public void ResetBehaviorTreeExecutionStates()
    {
        // 블랙보드 패턴으로 변경되어 BT 상태 리셋이 불필요함
        // 각 Combatant의 Blackboard.ResetCombat()을 호출해야 함
        Debug.Log($"[CharacterData] {characterName} - BT 상태 리셋 (블랙보드 패턴, BT 리셋 불필요)");
    }
}

/// <summary>
/// NPC 행동 확률 데이터
/// </summary>
[System.Serializable]
public class NPCBehaviorProbabilities
{
    [Tooltip("공격 성공률 (0~1)")]
    [Range(0f, 1f)]
    public float attackPerfectRate = 0f;
    
    [Tooltip("쳐내기 성공률 (0~1)")]
    [Range(0f, 1f)]
    public float parryPerfectRate = 0f;
    
    [Tooltip("막기 시도 확률 (0~1)")]
    [Range(0f, 1f)]
    public float guardAttemptRate = 0f;
    
    [Tooltip("막기 중 쳐내기 시도 여부")]
    public bool parryWhileGuarding = false;
    
    [Tooltip("막기 중 쳐내기 성공률 (0~1)")]
    [Range(0f, 1f)]
    public float parryWhileGuardingRate = 0f;
}
