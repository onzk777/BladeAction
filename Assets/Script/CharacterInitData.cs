using System;
using System.Collections.Generic;
using UnityEngine;
using BladeAction.Combat;
using BladeAction.Item;

/// <summary>
/// 캐릭터 타입 구분
/// </summary>
public enum CharacterType { Player, Enemy }

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
/// 초기 습득 검술 항목
/// </summary>
[System.Serializable]
public class InitialActionEntry
{
    [Tooltip("검술 키")]
    [DatabaseKey(typeof(ActionCommandDatabase), "actions", "key")]
    public string actionKey;
}

/// <summary>
/// 캐릭터 초기화 데이터 (템플릿)
/// Character 인스턴스를 생성할 때 사용하는 초기 값들을 정의합니다.
/// 여러 Character 인스턴스가 동일한 CharacterInitData를 참조할 수 있습니다.
/// </summary>
[CreateAssetMenu(fileName = "CharacterInitData", menuName = "Character/CharacterInitData", order = 1)]
public class CharacterInitData : ScriptableObject
{
    [Header("캐릭터 기본 정보")]
    [Tooltip("초기화 데이터 Key (템플릿 식별자)")] 
    public string key = ""; // 예: "player_default", "goblin_warrior", "orc_shaman"
    
    [Tooltip("캐릭터 이름")]
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
        guardDRBonus = 5,
        blockEfficiency = 0.5f,
        blockPoiseConsumption = 10f,
        parryEfficiency = 0.9f,
        parryPoiseConsumption = 5f,
        poiseGain = 1.0f
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
    
    [Header("초기 장비 (슬롯별)")]
    [Tooltip("무기 슬롯 초기 장비 (Item ID)")]
    public string weaponSlot = "";
    
    [Tooltip("갑옷 슬롯 초기 장비 (Item ID)")]
    public string armorSlot = "";
    
    [Tooltip("검술 유파 슬롯 초기 장비 (Item ID)")]
    public string swordArtStyleSlot = "";
    
    [Tooltip("장신구 슬롯 초기 장비 (Item ID 배열, Initial Accessory Slots 값에 따라 자동 조정)")]
    public string[] accessorySlots = new string[3]; // 기본값: 3개
    
#if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 값 변경 시 호출 (Editor 전용)
    /// initialAccessorySlots 값에 맞춰 accessorySlots 배열 크기 자동 조정
    /// </summary>
    private void OnValidate()
    {
        // accessorySlots 배열 크기를 initialAccessorySlots 값에 맞춤
        if (accessorySlots == null || accessorySlots.Length != initialAccessorySlots)
        {
            int oldSize = accessorySlots?.Length ?? 0;
            int newSize = Mathf.Clamp(initialAccessorySlots, 1, maxAccessorySlots);
            
            // 배열 크기 변경
            System.Array.Resize(ref accessorySlots, newSize);
            
            // 새로 추가된 슬롯은 빈 문자열로 초기화
            for (int i = oldSize; i < newSize; i++)
            {
                if (accessorySlots[i] == null)
                    accessorySlots[i] = "";
            }
            
            Debug.Log($"[CharacterInitData] {characterName} - 장신구 슬롯 크기 조정: {oldSize} → {newSize}");
        }
    }
#endif
    
    /// <summary>
    /// 실제 사용할 장신구 슬롯 (배열 그대로 반환)
    /// </summary>
    public string[] GetAccessorySlots()
    {
        return accessorySlots ?? new string[0];
    }
    
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
            // Debug.LogWarning($"[CharacterInitData] {characterName} - Behavior Tree가 설정되지 않았습니다!");
        }
        else
        {
            Debug.Log($"[CharacterInitData] {characterName} - Behavior Tree {behaviorTrees.Count}개 확인 (블랙보드 패턴, 복사 불필요)");
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
        Debug.Log($"[CharacterInitData] {characterName} - BT 상태 리셋 (블랙보드 패턴, BT 리셋 불필요)");
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

