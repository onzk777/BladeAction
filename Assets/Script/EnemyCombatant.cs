using System.Collections.Generic;
using UnityEngine;
using BladeAction.BT;

public class EnemyCombatant : Combatant
{
    // ========================================
    // 필드 (Fields)
    // ========================================
    
    private EnemyController controller; // EnemyController 참조
    private BladeAction.BT.BehaviorTreeContext currentBTContext; // 현재 BT 컨텍스트
    
    /// <summary>
    /// 런타임 확률 관리 인스턴스
    /// BT의 확률 Override 결과를 적용하는 확률 복사본
    /// 
    /// 왜 필요한가?
    /// - CharacterData.npcBehavior는 원본 에셋이므로 직접 수정하면 안 됨
    /// - 이 인스턴스는 각 NPC마다 독립적으로 생성됨
    /// - 턴마다 확률을 조정하고 턴 종료 시 리셋 가능
    /// </summary>
    private NPCRuntimeProbabilities runtimeProbabilities;
    
    /// <summary>
    /// BT 블랙보드 - 개체별 BT 실행 상태 저장소
    /// 
    /// 역할:
    /// - executeOncePerCombat 같은 BT 실행 상태를 개체별로 관리
    /// - 같은 BT를 사용하는 다른 NPC에게 영향을 주지 않음
    /// 
    /// 예시:
    /// - Goblin A, B가 모두 BT_Goblin.asset 사용
    /// - 하지만 각자 독립적인 blackboard 소유
    /// - A가 궁극기를 써도 B는 쓸 수 있음
    /// </summary>
    private BladeAction.BT.BTBlackboard btBlackboard;
    
    /// <summary>
    /// 이번 턴에 BT를 이미 평가했는지 여부 (중복 평가 방지)
    /// CombatManager에서 턴 시작 시 ResetBTEvaluation()으로 리셋
    /// </summary>
    private bool btEvaluatedThisTurn = false;
    
    
    // ========================================
    // 생성자 (Constructor)
    // ========================================
    
    /// <summary>
    /// EnemyCombatant를 생성합니다.
    /// </summary>
    /// <param name="data">캐릭터 데이터 (원본 확률 포함)</param>
    /// <param name="controller">EnemyController 참조</param>
    public EnemyCombatant(CharacterData data, EnemyController controller) : base(data)
    {
        this.controller = controller;
        
        // NPCRuntimeProbabilities 인스턴스 생성
        // CharacterData의 npcBehavior를 복사하여 독립적인 확률 관리
        if (data != null && data.npcBehavior != null)
        {
            runtimeProbabilities = new NPCRuntimeProbabilities(data.npcBehavior);
            Debug.Log($"[EnemyCombatant] {Name} 런타임 확률 초기화 완료");
        }
        else
        {
            Debug.LogWarning($"[EnemyCombatant] {Name} CharacterData 또는 npcBehavior가 null입니다!");
        }
        
        // BTBlackboard 인스턴스 생성
        // 개체별 BT 실행 상태 관리 (executeOncePerCombat 등)
        btBlackboard = new BladeAction.BT.BTBlackboard(data?.characterName ?? "Unknown");
        Debug.Log($"[EnemyCombatant] {Name} BT 블랙보드 초기화 완료");
    }

    public void SetController(EnemyController newController)
    {
        controller = newController;
    }
    
    // ========================================
    // Public 프로퍼티 (Properties)
    // ========================================
    
    /// <summary>
    /// 런타임 확률 접근 (AI Defense Decision Maker가 사용)
    /// </summary>
    public NPCRuntimeProbabilities RuntimeProbabilities => runtimeProbabilities;

    public override CommandSelection ChooseCommand()
    {
        // BT 실행 및 결과 적용 (이미 평가되었으면 스킵)
        if (!btEvaluatedThisTurn)
        {
            ExecuteBehaviorTrees();
        }
        
        // BT 결과에 따른 검술 선택
        int selectedIndex = GetSelectedCommandFromBT();
        
        Debug.Log($"[EnemyCombatant] ✅ {Name} 검술 선택: {selectedIndex}번");
        
        return new CommandSelection { selectedIndex = selectedIndex };
    }
    
    /// <summary>
    /// Behavior Tree를 실행하고 결과를 적용합니다.
    /// 
    /// 호출 시점:
    /// 1. CombatManager.PerformTurn() - 턴 시작 시 (공격/방어 무관)
    /// 2. EnemyCombatant.ChooseCommand() - 검술 선택 시 (중복 방지됨)
    /// 
    /// 중복 방지:
    /// - btEvaluatedThisTurn 플래그로 한 턴에 한 번만 실행
    /// - ResetBTEvaluation()으로 턴 시작 시 리셋
    /// </summary>
    public void ExecuteBehaviorTrees()
    {
        Debug.Log($"[EnemyCombatant] 🔍 ExecuteBehaviorTrees 호출");
        Debug.Log($"  - CharacterData: {CharacterData?.name ?? "null"}");
        Debug.Log($"  - BT 수: {CharacterData?.behaviorTrees?.Count ?? 0}");
        
        if (CharacterData?.behaviorTrees == null || CharacterData.behaviorTrees.Count == 0)
        {
            Debug.LogWarning("[EnemyCombatant] ⚠️ BT가 설정되지 않았습니다!");
            Debug.LogWarning($"  - CharacterData: {(CharacterData == null ? "null" : CharacterData.name)}");
            Debug.LogWarning($"  - behaviorTrees: {(CharacterData?.behaviorTrees == null ? "null" : $"Count={CharacterData.behaviorTrees.Count}")}");
            Debug.LogError("  → Unity Inspector에서 'Behavior Tree 설정'에 BT 에셋을 할당하세요!");
            return;
        }
        
        Debug.Log($"[EnemyCombatant] ✅ BT 발견: {CharacterData.behaviorTrees.Count}개");
        
        // BT 실행 컨텍스트 생성
        var playerCombatant = CharacterManager.Instance?.PlayerCombatant;
        if (playerCombatant == null)
        {
            Debug.LogWarning("[EnemyCombatant] PlayerCombatant를 찾을 수 없습니다.");
            return;
        }
        
        int currentTurn = CombatManager.Instance != null ? CombatManager.Instance.CurrentTurnNumber : 1;
        bool isAttackTurn = CombatManager.Instance != null ? CombatManager.Instance.IsNPCAttackTurn : false;
        
        // BT 실행 (블랙보드 패턴: 원본 BT 사용, 상태는 blackboard에 저장)
        currentBTContext = BladeAction.BT.BehaviorTreeExecutor.EvaluateMultipleTrees(
            CharacterData.behaviorTrees,
            this,
            playerCombatant,
            currentTurn,
            isAttackTurn,
            btBlackboard  // ← 블랙보드 전달!
        );
        
        // BT 결과 로그
        BladeAction.BT.BehaviorTreeExecutor.LogExecutionResult(currentBTContext);
        
        // BT 결과를 실제 확률에 적용
        ApplyBehaviorTreeResults();
        
        // 평가 완료 플래그 설정 (이번 턴에는 재평가 안 함)
        btEvaluatedThisTurn = true;
        Debug.Log($"[EnemyCombatant] 🎯 BT 평가 완료 - 이번 턴에는 재평가 안 함");
    }
    
    /// <summary>
    /// BT 결과를 실제 NPC 확률에 적용합니다.
    /// 
    /// 작동 흐름:
    /// 1. currentBTContext가 null이면 아무것도 하지 않음
    /// 2. runtimeProbabilities.ApplyOverrides()로 확률 적용
    /// 
    /// 중요:
    /// - 확률 적용은 runtimeProbabilities에만 영향을 줌
    /// - CharacterData의 원본 확률은 절대 변경되지 않음
    /// - 턴 종료 시 ResetProbabilities()로 원본 확률로 복원
    /// </summary>
    private void ApplyBehaviorTreeResults()
    {
        if (currentBTContext == null)
        {
            Debug.Log("[EnemyCombatant] BT Context가 없음 - 확률 적용 생략");
            return;
        }
        
        // runtimeProbabilities가 초기화되지 않았으면 생략
        if (runtimeProbabilities == null)
        {
            Debug.LogWarning("[EnemyCombatant] runtimeProbabilities가 null - 확률 적용 불가");
            return;
        }
        
        // BT의 확률 Override를 runtimeProbabilities에 적용
        // 예: {"AttackPerfectRate": 0.8} → runtimeProbabilities.attackPerfectRate = 0.8
        Debug.Log("[EnemyCombatant] === BT 확률 적용 시작 ===");
        runtimeProbabilities.ApplyOverrides(currentBTContext.probabilityOverrides);
        Debug.Log("[EnemyCombatant] === BT 확률 적용 완료 ===");
        
        // 적용된 확률 로그 (디버깅용)
        LogCurrentProbabilities();
    }
    
    /// <summary>
    /// 현재 확률 상태를 로그로 출력합니다. (디버깅용)
    /// </summary>
    private void LogCurrentProbabilities()
    {
        if (runtimeProbabilities == null) return;
        
        Debug.Log($"[EnemyCombatant] 현재 확률 상태:");
        Debug.Log($"  - 공격 성공률: {runtimeProbabilities.AttackPerfectRate:P0}");
        Debug.Log($"  - 쳐내기 성공률: {runtimeProbabilities.ParryPerfectRate:P0}");
        Debug.Log($"  - 막기 시도율: {runtimeProbabilities.GuardAttemptRate:P0}");
        Debug.Log($"  - 막기 중 쳐내기 시도 여부: {runtimeProbabilities.ParryWhileGuarding}");
        Debug.Log($"  - 막기 중 쳐내기 성공률: {runtimeProbabilities.ParryWhileGuardingRate:P0}");    
    }
    
    /// <summary>
    /// 런타임 확률을 원본으로 리셋합니다.
    /// 턴 종료 시 호출하여 BT 효과를 제거합니다.
    /// 
    /// 사용 시점:
    /// - 턴 종료 시 (CombatManager에서 호출)
    /// - 전투 종료 시
    /// 
    /// 효과:
    /// - 이전 턴의 모든 BT 확률 조정이 초기화됨
    /// - CharacterData의 원본 확률로 복원
    /// </summary>
    public void ResetProbabilities()
    {
        if (runtimeProbabilities != null)
        {
            Debug.Log($"[EnemyCombatant] {Name} 확률 리셋 호출");
            runtimeProbabilities.ResetToOriginal();
        }
    }
    
    /// <summary>
    /// 블랙보드를 리셋합니다 (새 전투 시작 시 호출)
    /// 
    /// 사용 시점:
    /// - 새 전투 시작 시 (CombatManager에서 호출)
    /// 
    /// 효과:
    /// - executeOncePerCombat 상태가 모두 초기화됨
    /// - 전투 시작 시 모든 BT 액션을 다시 사용 가능
    /// </summary>
    public void ResetBlackboard()
    {
        if (btBlackboard != null)
        {
            Debug.Log($"[EnemyCombatant] {Name} 블랙보드 리셋 호출");
            btBlackboard.ResetCombat();
        }
    }
    
    /// <summary>
    /// BT 평가 플래그를 리셋합니다 (새 턴 시작 시 호출)
    /// 
    /// 사용 시점:
    /// - CombatManager.PerformTurn() 시작 시
    /// 
    /// 효과:
    /// - 새 턴에서 BT를 다시 평가 가능하게 만듦
    /// - 중복 평가 방지 해제
    /// </summary>
    public void ResetBTEvaluation()
    {
        btEvaluatedThisTurn = false;
        Debug.Log($"[EnemyCombatant] {Name} BT 평가 플래그 리셋 - 새 턴 준비 완료");
    }
    
    /// <summary>
    /// BT 결과에서 검술을 선택합니다.
    /// 
    /// 우선순위:
    /// 1. selectedCommandIndex - 특정 인덱스 지정
    /// 2. selectedCommandTag - 특정 태그의 검술 중 선택
    /// 3. 랜덤 선택 (기본)
    /// 
    /// 참고:
    /// - forcedBehavior는 BTAction_ForceBehavior가 확률 조정용으로 사용
    /// - 검술 강제 선택은 selectedCommandIndex/Tag를 사용
    /// </summary>
    private int GetSelectedCommandFromBT()
    {
        if (currentBTContext == null)
        {
            // BT 결과가 없으면 랜덤 선택
            return Random.Range(0, AvailableCommands.Count);
        }
        
        // ========================================
        // 1. selectedCommandIndex 체크
        // ========================================
        if (currentBTContext.selectedCommandIndex.HasValue)
        {
            int idx = Mathf.Clamp(currentBTContext.selectedCommandIndex.Value, 0, AvailableCommands.Count - 1);
            Debug.Log($"[EnemyCombatant] BT 검술 인덱스 선택: {idx} ('{AvailableCommands[idx].commandName}')");
            return idx;
        }
        
        // ========================================
        // 3. selectedCommandTag 체크
        // ========================================
        if (!string.IsNullOrEmpty(currentBTContext.selectedCommandTag))
        {
            var filteredCommands = new List<ActionCommandData>();
            for (int i = 0; i < AvailableCommands.Count; i++)
            {
                if (AvailableCommands[i].HasTag(currentBTContext.selectedCommandTag))
                {
                    filteredCommands.Add(AvailableCommands[i]);
                }
            }
            
            if (filteredCommands.Count > 0)
            {
                var selectedCommand = filteredCommands[Random.Range(0, filteredCommands.Count)];
                // IReadOnlyList에는 IndexOf가 없으므로 직접 찾기
                int idx = -1;
                for (int i = 0; i < AvailableCommands.Count; i++)
                {
                    if (AvailableCommands[i] == selectedCommand)
                    {
                        idx = i;
                        break;
                    }
                }
                Debug.Log($"[EnemyCombatant] BT 태그 검술 선택: '{currentBTContext.selectedCommandTag}' -> '{selectedCommand.commandName}' (인덱스: {idx})");
                return idx >= 0 ? idx : Random.Range(0, AvailableCommands.Count);
            }
            else
            {
                Debug.LogWarning($"[EnemyCombatant] 태그 '{currentBTContext.selectedCommandTag}'를 가진 검술이 없음 - 랜덤 선택");
            }
        }
        
        // ========================================
        // 4. 랜덤 선택 (기본)
        // ========================================
        int randomIdx = Random.Range(0, AvailableCommands.Count);
        Debug.Log($"[EnemyCombatant] BT 검술 선택 없음 -> 랜덤 선택: {randomIdx} ('{AvailableCommands[randomIdx].commandName}')");
        return randomIdx;
    }
}
