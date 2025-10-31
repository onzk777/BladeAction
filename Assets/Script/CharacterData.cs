using System;
using System.Collections.Generic;
using UnityEngine;
using BladeAction.Combat;
using BladeAction.Item;

/// <summary>
/// 초기 인벤토리 아이템 항목
/// </summary>
[System.Serializable]
public class InitialItemEntry
{
    [Tooltip("아이템 ID")]
    public string itemId;
    
    [Tooltip("수량")]
    [Min(1)]
    public int quantity = 1;
}

/// <summary>
/// 초기 장착 장비 항목
/// </summary>
[System.Serializable]
public class InitialEquipmentEntry
{
    [Tooltip("장비 슬롯")]
    public EquipmentSlotType slotType;
    
    [Tooltip("아이템 ID")]
    public string itemId;
}

/// <summary>
/// 초기 습득 검술 항목
/// </summary>
[System.Serializable]
public class InitialActionEntry
{
    [Tooltip("검술 키")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string actionKey;
}

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
    
    [Header("장신구 슬롯 설정")]
    [Tooltip("초기 장신구 슬롯 개수 (게임 시작 시)")]
    [Range(1, 10)]
    public int initialAccessorySlots = 3;
    
    [Tooltip("최대 장신구 슬롯 개수 (성장 한계)")]
    [Range(1, 10)]
    public int maxAccessorySlots = 5;
    
    [Header("초기 인벤토리")]
    [Tooltip("캐릭터 생성 시 보유할 아이템 목록")]
    public List<InitialItemEntry> initialItems = new List<InitialItemEntry>();
    
    [Tooltip("캐릭터 생성 시 장착할 장비 (슬롯별)")]
    public List<InitialEquipmentEntry> initialEquipment = new List<InitialEquipmentEntry>();
    
    [Header("초기 검술")]
    [Tooltip("캐릭터 생성 시 습득한 검술 목록")]
    public List<InitialActionEntry> initialAcquiredActions = new List<InitialActionEntry>();
    
    [Tooltip("캐릭터 생성 시 장착된 검술 슬롯 1 (ActionCommandDatabase의 Key)")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string equippedActionSlot1 = "";
    
    [Tooltip("캐릭터 생성 시 장착된 검술 슬롯 2 (ActionCommandDatabase의 Key)")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string equippedActionSlot2 = "";
    
    [Tooltip("캐릭터 생성 시 장착된 검술 슬롯 3 (ActionCommandDatabase의 Key)")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string equippedActionSlot3 = "";
    
    [Tooltip("캐릭터 생성 시 장착된 검술 슬롯 4 (ActionCommandDatabase의 Key)")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string equippedActionSlot4 = "";
    
    // 편의 프로퍼티들 (baseStats 접근자)
    public float MaxHP => baseStats.maxHP;
    public int ATK => (int)baseStats.attack;
    public int DR => (int)baseStats.defenseDR;
    public float CritChance => baseStats.critChance;
    public float CritMultiplier => baseStats.critMultiplier;
    public float MaxPoise => baseStats.maxPoise;
    public int ParryPoiseDamage => (int)baseStats.parryPoiseDamage;

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
            // BT는 선택사항이므로 경고 제거
            // Debug.LogWarning($"[CharacterData] {characterName} - Behavior Tree가 설정되지 않았습니다!");
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
