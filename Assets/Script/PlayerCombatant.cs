using System.Collections.Generic;
using UnityEngine;
using BladeAction.BT;

public class PlayerCombatant : Combatant
{
    public int selectedIndex = 0; // 인스펙터에서 지정하는 테스트용 인덱스
    public bool useTestMode;  // true면 테스트 모드로 동작

    private PlayerController controller; // PlayerController 인스턴스 참조
    private BehaviorTreeContext currentBTContext; // 현재 BT 컨텍스트
    
    /// <summary>
    /// BT 블랙보드 - 개체별 BT 실행 상태 저장소
    /// 
    /// 역할:
    /// - executeOncePerCombat 같은 BT 실행 상태를 개체별로 관리
    /// - 향후 플레이어 자동 전투 시스템 추가 시 사용
    /// 
    /// 현재 상태:
    /// - 플레이어는 UI 기반이므로 BT를 사용하지 않음
    /// - 하지만 구조적으로 준비되어 있음 (향후 확장 가능)
    /// </summary>
    private BTBlackboard btBlackboard;
    
    /// <summary>
    /// 이번 턴에 BT를 이미 평가했는지 여부 (중복 평가 방지)
    /// CombatManager에서 턴 시작 시 ResetBTEvaluation()으로 리셋
    /// </summary>
    private bool btEvaluatedThisTurn = false;
    
    public PlayerCombatant(CharacterData data, PlayerController controller) : base(data)
    {
        this.controller = controller;
        
        // BTBlackboard 인스턴스 생성
        // 현재는 사용하지 않지만, 향후 자동 전투 시스템 추가 시 사용
        btBlackboard = new BTBlackboard(data?.characterName ?? "Player");
        Debug.Log($"[PlayerCombatant] {Name} BT 블랙보드 초기화 완료 (향후 자동 전투 대비)");
    }
    
    public void SetController(PlayerController newController)
    {
        controller = newController;
    }
    
    /// <summary>
    /// 플레이어의 검술을 선택합니다.
    /// 
    /// 현재: UI에서 선택된 버튼 인덱스 사용
    /// 향후: BT 시스템 추가 가능 (자동 전투 모드 등)
    /// 
    /// 중요:
    /// - Controller.GetSelectedCommandIndex()를 호출하면 순환 참조 발생!
    /// - 직접 UI에서 선택을 가져와야 함
    /// </summary>
    public override CommandSelection ChooseCommand()
    {
        // 현재: UI 기반 선택
        // 향후: BT 기반 선택 추가 가능 (useAutoBattle 플래그 등)
        
        // UI에서 직접 선택된 버튼 인덱스 가져오기
        var playerActionSelectUI = UnityEngine.Object.FindFirstObjectByType<PlayerActionSelectUI>();
        
        int idx = 0;
        if (playerActionSelectUI != null)
        {
            idx = playerActionSelectUI.GetCurrentSelectedButtonIndex();
            Debug.Log($"[PlayerCombatant] UI에서 선택된 인덱스: {idx}");
        }
        else
        {
            Debug.LogWarning("[PlayerCombatant] PlayerActionSelectUI를 찾을 수 없음 - 기본값 0 사용");
            idx = 0;
        }
        
        // 범위 체크
        idx = UnityEngine.Mathf.Clamp(idx, 0, AvailableCommands.Count - 1);
        
        return new CommandSelection { selectedIndex = idx };
    }
    
    /// <summary>
    /// Behavior Tree를 실행하고 결과를 적용합니다.
    /// 
    /// 호출 시점:
    /// 1. CombatManager.PerformTurn() - 턴 시작 시 (공격/방어 무관)
    /// 
    /// 중복 방지:
    /// - btEvaluatedThisTurn 플래그로 한 턴에 한 번만 실행
    /// - ResetBTEvaluation()으로 턴 시작 시 리셋
    /// 
    /// 현재 상태:
    /// - CharacterData에 BT가 설정되어 있으면 평가 실행
    /// - 없으면 로그만 남기고 스킵
    /// </summary>
    public void ExecuteBehaviorTrees()
    {
        Debug.Log($"[PlayerCombatant] 🔍 ExecuteBehaviorTrees 호출");
        
        if (CharacterData?.behaviorTrees == null || CharacterData.behaviorTrees.Count == 0)
        {
            Debug.Log("[PlayerCombatant] BT가 설정되지 않음 - 스킵 (정상, Player는 UI 기반)");
            btEvaluatedThisTurn = true; // 평가 완료 처리
            return;
        }
        
        Debug.Log($"[PlayerCombatant] ✅ BT 발견: {CharacterData.behaviorTrees.Count}개");
        
        // BT 실행 컨텍스트 생성
        var enemyCombatant = CharacterManager.Instance?.EnemyCombatant;
        if (enemyCombatant == null)
        {
            Debug.LogWarning("[PlayerCombatant] EnemyCombatant를 찾을 수 없습니다.");
            btEvaluatedThisTurn = true;
            return;
        }
        
        int currentTurn = CombatManager.Instance != null ? CombatManager.Instance.CurrentTurnNumber : 1;
        bool isAttackTurn = CombatManager.Instance != null ? CombatManager.Instance.IsPlayerAttackTurn : false;
        
        // BT 실행 (블랙보드 패턴: 원본 BT 사용, 상태는 blackboard에 저장)
        currentBTContext = BehaviorTreeExecutor.EvaluateMultipleTrees(
            CharacterData.behaviorTrees,
            this,
            enemyCombatant,
            currentTurn,
            isAttackTurn,
            btBlackboard
        );
        
        // BT 결과 로그
        BehaviorTreeExecutor.LogExecutionResult(currentBTContext);
        
        // 평가 완료 플래그 설정
        btEvaluatedThisTurn = true;
        Debug.Log($"[PlayerCombatant] 🎯 BT 평가 완료 - 이번 턴에는 재평가 안 함");
    }
    
    /// <summary>
    /// 블랙보드를 리셋합니다 (새 전투 시작 시 호출)
    /// 
    /// 사용 시점:
    /// - 새 전투 시작 시 (CombatManager에서 호출)
    /// 
    /// 효과:
    /// - executeOncePerCombat 상태가 모두 초기화됨
    /// - 향후 플레이어도 BT 사용 시 필요
    /// </summary>
    public void ResetBlackboard()
    {
        if (btBlackboard != null)
        {
            Debug.Log($"[PlayerCombatant] {Name} 블랙보드 리셋 호출");
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
        Debug.Log($"[PlayerCombatant] {Name} BT 평가 플래그 리셋 - 새 턴 준비 완료");
    }
}
