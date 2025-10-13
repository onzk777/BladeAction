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

    [Header("NPC AI 설정")]
    [Tooltip("NPC 행동 확률 설정 (AI 전용)")]
    public NPCBehaviorProbabilities npcBehavior = new NPCBehaviorProbabilities();
    
    [Header("Behavior Tree")]
    [Tooltip("이 캐릭터가 사용할 Behavior Tree 리스트 (런타임 인스턴스)")]
    public System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData> behaviorTrees = new System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData>();
    
    [Header("Behavior Tree 원본 (에디터용)")]
    [Tooltip("에디터에서 설정할 원본 Behavior Tree 리스트")]
    public System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData> originalBehaviorTrees = new System.Collections.Generic.List<BladeAction.BT.BehaviorTreeData>();
    
    /// <summary>
    /// Behavior Tree를 인스턴스화하여 개체별 독립적인 BT 생성
    /// </summary>
    public void InstantiateBehaviorTrees()
    {
        behaviorTrees.Clear();
        
        foreach (var originalTree in originalBehaviorTrees)
        {
            if (originalTree != null)
            {
                var instantiatedTree = UnityEngine.Object.Instantiate(originalTree);
                behaviorTrees.Add(instantiatedTree);
                Debug.Log($"[CharacterData] BT 인스턴스화 완료: {originalTree.name} → {instantiatedTree.name}");
            }
        }
        
        Debug.Log($"[CharacterData] {characterName} BT 인스턴스화 완료 - 총 {behaviorTrees.Count}개");
    }
    
    /// <summary>
    /// 모든 BT 노드의 전투 실행 상태를 리셋
    /// </summary>
    public void ResetBehaviorTreeExecutionStates()
    {
        foreach (var tree in behaviorTrees)
        {
            if (tree != null)
            {
                ResetTreeExecutionStates(tree);
            }
        }
    }
    
    /// <summary>
    /// 특정 BT의 모든 노드 실행 상태 리셋
    /// </summary>
    private void ResetTreeExecutionStates(BladeAction.BT.BehaviorTreeData tree)
    {
        if (tree == null) return;
        
        foreach (var entry in tree.entries)
        {
            if (entry == null) continue;
            
            // 조건 노드 리셋
            if (entry.condition != null)
            {
                ResetNodeExecutionState(entry.condition);
            }
            
            // 액션 노드들 리셋
            foreach (var action in entry.actions)
            {
                if (action != null)
                {
                    ResetNodeExecutionState(action);
                }
            }
        }
    }
    
    /// <summary>
    /// 개별 노드의 실행 상태 리셋
    /// </summary>
    private void ResetNodeExecutionState(BladeAction.BT.BTNode node)
    {
        if (node is BladeAction.BT.BTActionNode actionNode)
        {
            actionNode.ResetCombatExecution();
        }
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
