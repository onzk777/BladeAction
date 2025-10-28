using System.Collections.Generic;
using UnityEngine;

namespace BladeAction.BT
{
    /// <summary>
    /// Behavior Tree 실행 컨텍스트
    /// BT 평가 및 실행에 필요한 모든 정보를 담고 있습니다.
    /// </summary>
    public class BehaviorTreeContext
    {
        [Header("전투 참가자")]
        [Tooltip("NPC 자신 (BT를 실행하는 캐릭터)")]
        public Character self;
        
        [Tooltip("상대방 (플레이어)")]
        public Character target;
        
        [Header("턴 정보")]
        [Tooltip("현재 턴 번호")]
        public int currentTurn;
        
        [Tooltip("공격 턴 여부 (true: 공격 턴, false: 방어 턴)")]
        public bool isAttackTurn;
        
        [Header("블랙보드 (상태 저장소)")]
        [Tooltip("개체별 BT 실행 상태 저장 (executeOncePerCombat 등)")]
        public BTBlackboard blackboard;
        
        [Header("실행 결과 저장")]
        [Tooltip("확률 Override 딕셔너리 (키: 확률 타입, 값: 새로운 확률)")]
        public Dictionary<string, float> probabilityOverrides = new Dictionary<string, float>();
        
        [Tooltip("선택된 검술 인덱스 (null이면 BT에서 지정하지 않음)")]
        public int? selectedCommandIndex = null;
        
        [Tooltip("선택된 검술 태그 (null이면 BT에서 지정하지 않음)")]
        public string selectedCommandTag = null;
        
        [Tooltip("강제 행동 타입 (null이면 BT에서 지정하지 않음)")]
        public string forcedBehavior = null;
        
        /// <summary>
        /// 컨텍스트 초기화
        /// </summary>
        /// <param name="selfCombatant">NPC 자신</param>
        /// <param name="targetCombatant">상대방</param>
        /// <param name="turnNumber">현재 턴 번호</param>
        /// <param name="attackTurn">공격 턴 여부</param>
        /// <param name="btBlackboard">개체별 상태 저장소 (null이면 임시 생성)</param>
        public void Initialize(Character selfCombatant, Character targetCombatant, int turnNumber, bool attackTurn, BTBlackboard btBlackboard = null)
        {
            self = selfCombatant;
            target = targetCombatant;
            currentTurn = turnNumber;
            isAttackTurn = attackTurn;
            
            // 블랙보드 설정 (제공되지 않으면 임시 생성)
            if (btBlackboard != null)
            {
                blackboard = btBlackboard;
            }
            else
            {
                // 임시 블랙보드 생성 (테스트용)
                blackboard = new BTBlackboard(selfCombatant?.Name ?? "Temp");
                Debug.LogWarning($"[BehaviorTreeContext] 블랙보드가 제공되지 않아 임시 생성함");
            }
            
            // 결과 저장소 초기화
            probabilityOverrides.Clear();
            selectedCommandIndex = null;
            selectedCommandTag = null;
            forcedBehavior = null;
        }
        
        /// <summary>
        /// 확률 Override 적용
        /// </summary>
        public void SetProbabilityOverride(string key, float value)
        {
            probabilityOverrides[key] = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// 확률 Override 가져오기
        /// </summary>
        public float GetProbabilityOverride(string key, float defaultValue = 0f)
        {
            return probabilityOverrides.ContainsKey(key) ? probabilityOverrides[key] : defaultValue;
        }
        
        /// <summary>
        /// 컨텍스트 병합 (다른 컨텍스트의 결과를 현재 컨텍스트에 병합)
        /// </summary>
        public void MergeFrom(BehaviorTreeContext other)
        {
            if (other == null) return;
            
            // 확률 Override 병합 (나중에 온 것이 우선)
            foreach (var kvp in other.probabilityOverrides)
            {
                probabilityOverrides[kvp.Key] = kvp.Value;
            }
            
            // 검술 선택 병합 (나중에 온 것이 우선)
            if (other.selectedCommandIndex.HasValue)
            {
                selectedCommandIndex = other.selectedCommandIndex;
            }
            
            if (!string.IsNullOrEmpty(other.selectedCommandTag))
            {
                selectedCommandTag = other.selectedCommandTag;
            }
            
            // 강제 행동 병합 (나중에 온 것이 우선)
            if (!string.IsNullOrEmpty(other.forcedBehavior))
            {
                forcedBehavior = other.forcedBehavior;
            }
        }
    }
}

