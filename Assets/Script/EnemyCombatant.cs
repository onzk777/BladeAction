using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatant : Combatant
{
    private EnemyController controller; // EnemyController 참조
    private BladeAction.BT.BehaviorTreeContext currentBTContext; // 현재 BT 컨텍스트
    
    public EnemyCombatant(CharacterData data, EnemyController controller) : base(data)
    {
        this.controller = controller;
    }

    public void SetController(EnemyController newController)
    {
        controller = newController;
    }

    public override CommandSelection ChooseCommand()
    {
        // BT 실행 및 결과 적용
        ExecuteBehaviorTrees();
        
        // BT 결과에 따른 검술 선택
        int selectedIndex = GetSelectedCommandFromBT();
        
        return new CommandSelection { selectedIndex = selectedIndex };
    }
    
    /// <summary>
    /// Behavior Tree를 실행하고 결과를 적용합니다.
    /// </summary>
    private void ExecuteBehaviorTrees()
    {
        if (CharacterData?.behaviorTrees == null || CharacterData.behaviorTrees.Count == 0)
        {
            Debug.LogWarning("[EnemyCombatant] BT가 설정되지 않았습니다.");
            return;
        }
        
        // BT 실행 컨텍스트 생성
        var playerCombatant = CharacterManager.Instance?.PlayerCombatant;
        if (playerCombatant == null)
        {
            Debug.LogWarning("[EnemyCombatant] PlayerCombatant를 찾을 수 없습니다.");
            return;
        }
        
        int currentTurn = CombatManager.Instance != null ? CombatManager.Instance.CurrentTurnNumber : 1;
        bool isAttackTurn = CombatManager.Instance != null ? CombatManager.Instance.IsNPCAttackTurn : false;
        
        // BT 실행
        currentBTContext = BladeAction.BT.BehaviorTreeExecutor.EvaluateMultipleTrees(
            CharacterData.behaviorTrees,
            this,
            playerCombatant,
            currentTurn,
            isAttackTurn
        );
        
        // BT 결과 로그
        BladeAction.BT.BehaviorTreeExecutor.LogExecutionResult(currentBTContext);
        
        // BT 결과를 실제 확률에 적용
        ApplyBehaviorTreeResults();
    }
    
    /// <summary>
    /// BT 결과를 실제 NPC 확률에 적용합니다.
    /// </summary>
    private void ApplyBehaviorTreeResults()
    {
        if (currentBTContext == null) return;
        
        // 확률 Override 적용
        foreach (var kvp in currentBTContext.probabilityOverrides)
        {
            Debug.Log($"[EnemyCombatant] BT 확률 Override: {kvp.Key} = {kvp.Value:F2}");
            // TODO: 실제 확률 적용 로직 구현 필요
        }
    }
    
    /// <summary>
    /// BT 결과에서 검술을 선택합니다.
    /// </summary>
    private int GetSelectedCommandFromBT()
    {
        if (currentBTContext == null)
        {
            // BT 결과가 없으면 랜덤 선택
            return Random.Range(0, AvailableCommands.Count);
        }
        
        // BT에서 검술 인덱스가 지정된 경우
        if (currentBTContext.selectedCommandIndex.HasValue)
        {
            int idx = Mathf.Clamp(currentBTContext.selectedCommandIndex.Value, 0, AvailableCommands.Count - 1);
            Debug.Log($"[EnemyCombatant] BT 검술 인덱스 선택: {idx}");
            return idx;
        }
        
        // BT에서 검술 태그가 지정된 경우
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
                Debug.Log($"[EnemyCombatant] BT 태그 검술 선택: '{currentBTContext.selectedCommandTag}' -> 인덱스 {idx}");
                return idx >= 0 ? idx : Random.Range(0, AvailableCommands.Count);
            }
        }
        
        // BT에서 검술 선택이 없으면 랜덤 선택
        int randomIdx = Random.Range(0, AvailableCommands.Count);
        Debug.Log($"[EnemyCombatant] BT 검술 선택 없음 -> 랜덤 선택: {randomIdx}");
        return randomIdx;
    }
}
