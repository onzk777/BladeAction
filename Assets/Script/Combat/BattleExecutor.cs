using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 실행 로직을 담당하는 클래스
/// 
/// 역할:
/// - 전투 루프 실행 (RunBattle)
/// - 턴 수행 (PerformTurn)
/// - 히트 판정 완료 대기 (EnsureAllHitJudgmentsCompleted)
/// - 피해량 계산 (ProcessDamageCalculation)
/// - AI 막기 처리 (공격과 동일한 레벨에서 처리)
/// 
/// 설계:
/// - CombatManager보다 하위 계층으로, 전투 실행 로직만 담당
/// - CombatManager의 internal 멤버에 접근하여 전투 상태 관리
/// </summary>
public class BattleExecutor
{
    private readonly CombatManager manager;
    
    // 🆕 AI 막기 시스템 (공격과 동일한 레벨에서 처리)
    private IAIDefenseDecisionMaker aiDefenseDecisionMaker;
    private bool aiWillGuard = false;
    private bool aiIsGuarding = false;
    private CombatTurnContext currentAIGuardTurnContext = null; // 현재 AI 막기 턴 컨텍스트

    public BattleExecutor(CombatManager manager)
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public IEnumerator RunBattle()
    {
        Debug.Log($"[RunCombat] CombatStartTime 세팅됨: {CombatManager.CombatStartTime}");
        if (manager.attackerInputHandler != null)
        {
            Debug.Log($"[RunCombat] HandlerInstance: {manager.attackerInputHandler.GetInstanceID()}");
            Debug.Log($"[RunCombat] timingInputHandler InstanceID: {manager.attackerInputHandler.GetInstanceID()}");
        }
        else
        {
            Debug.LogWarning("[RunCombat] attackerInputHandler가 null입니다 (NPC vs NPC 시나리오일 수 있습니다)");
        }

        yield return new WaitForSeconds(GlobalConfig.Instance.CombatStartDelay);

        manager.ResetBehaviorTreeStates();

        // TeamA와 TeamB 모두 ActionSelectUI 초기화
        var selectionManager = ActionCommandSelectionManager.Instance;
        var teamAActionUI = selectionManager?.GetTeamActionUI(CombatCharacterManager.CombatTeam.TeamA);
        if (teamAActionUI != null)
        {
            Debug.Log("[BattleExecutor] TeamA ActionSelectUI 초기화 요청");
            teamAActionUI.RefreshButtons();
        }
        else
        {
            Debug.LogWarning("[BattleExecutor] TeamA ActionSelectUI를 찾을 수 없습니다!");
        }

        var teamBActionUI = selectionManager?.GetTeamActionUI(CombatCharacterManager.CombatTeam.TeamB);
        if (teamBActionUI != null)
        {
            Debug.Log("[BattleExecutor] TeamB ActionSelectUI 초기화 요청");
            teamBActionUI.RefreshButtons();
        }
        else
        {
            Debug.LogWarning("[BattleExecutor] TeamB ActionSelectUI를 찾을 수 없습니다!");
        }

        while (!manager.isBattleEnded)
        {
            if (manager.isBattleEnded)
            {
                Debug.Log("[RunCombat] 전투가 종료되어 루프를 중단합니다.");
                break;
            }

            var teamAContext = PrepareTeamTurn(CombatCharacterManager.CombatTeam.TeamA);
            if (teamAContext == null)
            {
                yield break;
            }

            yield return PerformTurn(teamAContext);

            if (manager.isBattleEnded)
            {
                Debug.Log("[RunCombat] TeamA 턴 후 전투 종료 감지");
                break;
            }

            var teamBContext = PrepareTeamTurn(CombatCharacterManager.CombatTeam.TeamB);
            if (teamBContext == null)
            {
                yield break;
            }

            yield return PerformTurn(teamBContext);

            // 한 턴(TeamA + TeamB) 완료 후 모든 NPC 확률 리셋
            // 다음 턴 시작 시 BT가 다시 새롭게 확률을 조정함
            manager.ResetNPCProbabilities();

            if (manager.isBattleEnded)
            {
                Debug.Log("[RunCombat] 적 턴 후 전투 종료 감지");
                break;
            }

            Debug.Log($"[RunCombat] ========== 턴 {manager.CurrentTurnNumber} 완료 - 다음 턴으로 ==========");
            var characterManager = CombatCharacterManager.Instance;
            int? teamALeaderHp = characterManager?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamA)?.Character?.HP;
            int? teamBLeaderHp = characterManager?.GetLeaderSlot(CombatCharacterManager.CombatTeam.TeamB)?.Character?.HP;
            Debug.Log($"[RunCombat] TeamA Leader HP: {(teamALeaderHp.HasValue ? teamALeaderHp.Value.ToString() : "N/A")}, TeamB Leader HP: {(teamBLeaderHp.HasValue ? teamBLeaderHp.Value.ToString() : "N/A")}");
            Debug.Log($"[RunCombat] isBattleEnded: {manager.isBattleEnded}");
        }

        Debug.Log("전투 종료!");
    }

    private CombatTurnContext PrepareTeamTurn(CombatCharacterManager.CombatTeam team)
    {
        var context = manager.BuildTurnContext(team);
        if (context == null || !context.IsValid)
        {
            Debug.LogError($"[RunCombat] {team} 턴 컨텍스트 생성 실패로 전투를 종료합니다.");
            manager.isBattleEnded = true;
            return null;
        }

        switch (team)
        {
            case CombatCharacterManager.CombatTeam.TeamA:
                CombatManager.CombatStartTime = Time.time;
                manager.CurrentTurnNumber++;
                Debug.Log($"[RunCombat] 턴 {manager.CurrentTurnNumber} 시작 - TeamA 리더 턴 ({context.AttackerCharacter?.Name} vs {context.DefenderCharacter?.Name})");
                break;
            case CombatCharacterManager.CombatTeam.TeamB:
                Debug.Log($"[RunCombat] 턴 {manager.CurrentTurnNumber} 계속 - TeamB 리더 턴 ({context.AttackerCharacter?.Name} vs {context.DefenderCharacter?.Name})");
                break;
        }

        return context;
    }

    private IEnumerator PerformTurn(CombatTurnContext turnContext)
    {
        if (turnContext == null)
        {
            Debug.LogError("[BattleExecutor] PerformTurn 호출 시 turnContext가 null입니다.");
            yield break;
        }

        if (!turnContext.IsValid)
        {
            Debug.LogError($"[BattleExecutor] PerformTurn - turnContext가 유효하지 않습니다. {turnContext}");
            yield break;
        }

        Debug.Log($"[턴 시작] PerformTurn 호출, currentCommandIndex 초기화");
        var characterManager = CombatCharacterManager.Instance;
        var actorSlot = turnContext.AttackerSlot ?? characterManager?.FindSlotByController(turnContext.AttackerController);
        var defenderSlot = turnContext.DefenderSlot ?? characterManager?.GetOpponentLeaderSlot(actorSlot != null ? actorSlot.Team : CombatCharacterManager.CombatTeam.TeamB);
        var controller = turnContext.AttackerController;

        manager.currentTurnContext = turnContext;
        manager.currentAttackerSlot = actorSlot;
        manager.currentDefenderSlot = defenderSlot;
        manager.currentAttackerController = controller;
        manager.currentDefenderController = turnContext.DefenderController ?? defenderSlot?.Controller;

        CombatDebugDisplay.Instance?.ForceUpdateUI();

        Character actor = turnContext.AttackerCharacter ?? actorSlot?.Character ?? controller?.Character;
        Character defender = turnContext.DefenderCharacter ?? defenderSlot?.Character;

        if (defender == null && actorSlot != null)
        {
            defender = characterManager?.GetOpponentLeaderSlot(actorSlot.Team)?.Character;
        }

        if (manager.currentDefenderController == null && defenderSlot?.Controller != null)
        {
            manager.currentDefenderController = defenderSlot.Controller;
        }

        if (manager.currentDefenderController == null && defender != null)
        {
            var charManager = CombatCharacterManager.Instance;
            var resolvedSlot = charManager?.FindSlotByCharacter(defender);
            if (resolvedSlot?.Controller != null)
            {
                manager.currentDefenderController = resolvedSlot.Controller;
            }
            else if (defender is PlayerCharacter && manager.playerController != null)
            {
                manager.currentDefenderController = manager.playerController;
            }
        }

        if (actor == null || defender == null)
        {
            Debug.LogError("[BattleExecutor] 공격자 또는 방어자 정보를 찾지 못했습니다.");
            yield break;
        }

        bool attackerIsPlayer = actorSlot != null ? actorSlot.IsPlayerControlledSlot : actor is PlayerCharacter;
        
        // 🆕 BT 평가 (공격자 + 방어자 모두!)
        Debug.Log($"[BattleExecutor] 🌳 BT 평가 시작 - 공격자: {actor.Name}, 방어자: {defender.Name}");
        
        // 1. 공격자 BT 평가 (isAttackTurn = true)
        if (actor is PlayerCharacter playerActor)
        {
            Debug.Log($"[BattleExecutor]   → Player 공격 턴 BT 평가");
            playerActor.ResetBTEvaluation();
            playerActor.ExecuteBehaviorTrees();
        }
        else if (actor is EnemyCharacter enemyActor)
        {
            Debug.Log($"[BattleExecutor]   → Enemy 공격 턴 BT 평가");
            enemyActor.ResetBTEvaluation();
            enemyActor.ExecuteBehaviorTrees();
        }
        
        // 2. 방어자 BT 평가 (isAttackTurn = false)
        if (defender is PlayerCharacter playerDefender)
        {
            Debug.Log($"[BattleExecutor]   → Player 방어 턴 BT 평가");
            playerDefender.ResetBTEvaluation();
            playerDefender.ExecuteBehaviorTrees();
        }
        else if (defender is EnemyCharacter enemyDefender)
        {
            Debug.Log($"[BattleExecutor]   → Enemy 방어 턴 BT 평가");
            enemyDefender.ResetBTEvaluation();
            enemyDefender.ExecuteBehaviorTrees();
        }
        
        // 3. Enemy 공격 턴이면 선택 캐시 리셋
        if (controller is AIController enemyCtrl)
        {
            enemyCtrl.ResetSelectionCache();
        }
        
        // 🆕 AI 방어자 막기 의사결정 (방어 턴일 때만, PerformTurn 루프에서 처리)
        if (defender is EnemyCharacter)
        {
            InitializeAIGuardSystem();
            currentAIGuardTurnContext = turnContext; // 현재 턴 컨텍스트 저장
            
            // AI 막기 의사결정 수행 (코루틴 없이 즉시 처리)
            var aiContext = CreateAIContextForGuard(turnContext);
            aiWillGuard = aiDefenseDecisionMaker.MakeGuardDecision(aiContext);
            Debug.Log($"[BattleExecutor] 🆕 AI 막기 의사결정 결과: {aiWillGuard}");
        }

        // 검술 선택 (공격자만 필요, BT는 이미 평가됨!)
        int selectedCommandIndex = controller.GetSelectedCommandIndex();
        
        // 유효성 검증 (방어 코드)
        if (actor.AvailableCommands == null || actor.AvailableCommands.Count == 0)
        {
            Debug.LogError($"[BattleExecutor] {actor.Name}에게 사용 가능한 검술이 없습니다! 턴 진행 중단.");
            yield break;
        }
        
        if (selectedCommandIndex < 0 || selectedCommandIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogError($"[BattleExecutor] 유효하지 않은 검술 인덱스: {selectedCommandIndex} (범위: 0~{actor.AvailableCommands.Count - 1})");
            yield break;
        }
        
        ActionCommandData command = actor.AvailableCommands[selectedCommandIndex];
        
        // Enemy 턴일 때 UI 업데이트 (선택된 검술 표시)
        if (!attackerIsPlayer && actorSlot != null)
        {
            var actionUI = ActionCommandSelectionManager.Instance?.GetTeamActionUI(actorSlot.Team);
            if (actionUI != null)
            {
                actionUI.SetSelectedButton(selectedCommandIndex);
                Debug.Log($"[BattleExecutor] {actorSlot.Team} ActionSelectUI 업데이트 - 선택된 커맨드 인덱스: {selectedCommandIndex}");
            }
        }
        CharacterCommandResult result = new CharacterCommandResult(command);
        manager.BindInputHandler(manager.attackerInputHandler, actorSlot);
        manager.BindInputHandler(manager.defenderInputHandler, defenderSlot);
        Debug.Log($"[InputTrace][Turn] BindToSlot - attacker:{manager.attackerInputHandler?.BoundSlot} defender:{manager.defenderInputHandler?.BoundSlot} (attackerIsPlayer:{attackerIsPlayer}) Time:{Time.time:F4} Frame:{Time.frameCount}");
        TurnTimer.Reset();
        float turnDuration = manager.CalculateTurnDuration(command);
        manager.CurrentTurnDuration = turnDuration;
        int hitCount = command.hitCount;
        manager.attackerPerfectInput = null;
        manager.defenderPerfectInput = null;
        manager.attackerInputTime = null;
        manager.defenderInputTime = null;
        manager.CurrentAttackResultShown = false;
        manager.CurrentDefenseResultShown = false;
        manager.CurrentClashResultShown = false;
        manager.windowPrompted = false;
        manager.floatingTextShown = false;
        manager.attackerInputHandler?.ResetCooldown();
        manager.defenderInputHandler?.ResetCooldown();
        manager.CurrentHit = 0;
        manager.CurrentController = controller;
        manager.CurrentResult = result;
        
        // Poise 회복 및 중단 상태 초기화
        actor.ResetPoise();
        manager.isInterrupted = false;
        
        // HUD 초기화
        CombatHUD.Instance?.ClearHUD();
        CombatHUD.Instance?.ShowPerfectTimingGuides(command, turnDuration);
        CombatDebugDisplay.Instance?.ClearDebugResults();

        if (attackerIsPlayer && manager.attackerInputHandler != null)
        {
            manager.attackerInputHandler.EnableInput();
            Debug.Log("[BattleExecutor] 공격자 입력 허용됨");
        }

        if (defender != null && manager.defenderInputHandler != null)
        {
            manager.defenderInputHandler.EnableInput();
            Debug.Log("[BattleExecutor] 방어자 입력 허용됨");
        }

        // 커맨드 유효성 확인
        if (selectedCommandIndex < 0 || selectedCommandIndex >= actor.AvailableCommands.Count)
        {
            Debug.LogWarning($"[BattleExecutor] 선택 인덱스가 유효하지 않습니다: {selectedCommandIndex}");
            yield break;
        }

        // 디버그 정보: 커맨드 시작 표시
        CombatDebugDisplay.Instance?.ShowCommandStart(attackerIsPlayer, command.commandName);
        CombatDebugDisplay.Instance?.ShowInputPrompt("입력 대기");
        Debug.Log($"[InputTrace][Turn] PerformTurn Start - actor:{actor.Name}, defender:{defender.Name}, Time:{Time.time:F4}, Frame:{Time.frameCount}");
        
        // Spine 애니메이션 연동: 공격 턴 시작 시 애니메이션 재생
        controller?.OnPlayActionCommand();
        
        // 타이밍 윈도우 등록 및 입력 수신 시작
        manager.attackerInputHandler?.LoadTimingWindows(command.perfectTimings);
        
        // 발사체/판정 상태 배열 초기화
        manager.ActiveBattle.EnsureHitArrays(command.hitCount);
        Array.Clear(manager.ActiveBattle.ProjectileLaunched, 0, manager.ActiveBattle.ProjectileLaunched.Length);
        Array.Clear(manager.ActiveBattle.HitJudgmentCompleted, 0, manager.ActiveBattle.HitJudgmentCompleted.Length);
        Array.Clear(manager.ActiveBattle.HitJudgmentCount, 0, manager.ActiveBattle.HitJudgmentCount.Length);

        float turnDurationBuffer = 0.02f;

        // 메인 루프 시작
        while (TurnTimer.ElapsedTime < turnDuration + turnDurationBuffer)
        {
            float elapsed = TurnTimer.ElapsedTime;
            float remaining = turnDuration - elapsed;

            // 턴 타이머 UI 업데이트
            CombatHUD.Instance?.UpdateTurnProgressBar(remaining, turnDuration);
            CombatDebugDisplay.Instance?.UpdateTurnInfo(remaining, turnDuration);
            
            // 전투 종료 조건 체크
            if (manager.isBattleEnded)
            {
                Debug.LogWarning("[BattleExecutor] 전투가 종료되어 턴을 중단합니다.");
                break;
            }
            
            // 중단 발생 시 턴 조기 종료
            if (manager.isInterrupted)
            {
                Debug.LogWarning("[BattleExecutor] 중단 발생으로 턴이 조기 종료됩니다.");
                manager.ForceCompleteRemainingHits(manager.CurrentHit, hitCount);
                break;
            }
            
            if (manager.CheckInterruptCondition())
            {
                Debug.Log("턴이 중단되었습니다.");
                manager.ForceCompleteRemainingHits(manager.CurrentHit, hitCount);
                break;
            }

            if (manager.CurrentHit < hitCount)
            {   
                // 이번 히트 윈도우 정의
                var perfectWindow = command.perfectTimings[manager.CurrentHit];
                float inputAvailableStart = GlobalConfig.Instance.InputBufferStartSeconds;
                float perfectWindowStart = perfectWindow.start;
                float perfectWindowEnd = perfectWindow.start + perfectWindow.duration;
                float inputAvailableEnd = manager.GetInputDeadline();
                float aiInputTime = perfectWindowStart;
                bool aiAttackSuccess = UnityEngine.Random.value < manager.globalConfig.NpcAttackPerfectRate;
                bool aiDefenseSuccess = UnityEngine.Random.value < GlobalConfig.Instance.NpcParryPerfectRate;

                Debug.Log($"[UI표시:지금이닷!] 히트 {manager.CurrentHit + 1}, elapsed={elapsed:F5}, 타이밍창=({perfectWindow.start:F5} ~ {perfectWindow.End:F5})");

                // 윈도우 오픈: prompt 한 번만 띄우기
                if (!manager.windowPrompted && elapsed >= inputAvailableStart)
                {
                    Debug.Log($"[BattleExecutor] 히트 {manager.CurrentHit} 오픈");
                    manager.windowPrompted = true;
                    manager.CurrentAttackResultShown = false;
                    manager.CurrentDefenseResultShown = false;
                    manager.CurrentClashResultShown = false;
                    manager.attackerInputHandler?.ResetInputState();
                    manager.defenderInputHandler?.ResetInputState();
                    CombatDebugDisplay.Instance?.ShowInputPrompt("입력 가능!");
                    manager.CurrentController = controller;
                    manager.CurrentResult = result;
                    manager.attackerInputHandler?.RegisterHitTiming(perfectWindow);
                }

                if (!manager.floatingTextShown && elapsed >= perfectWindowStart)
                {
                    if (FloatingTextManager.Instance != null)
                    {
                        Vector3 textPosition = manager.GetFloatingTextPosition(attackerIsPlayer);
                        FloatingTextManager.Instance.ShowPerfectTimingStart(textPosition, manager.CurrentHit + 1, perfectWindow);
                    }
                    manager.floatingTextShown = true;
                }
                
                if (!manager.CurrentAttackResultShown && elapsed >= perfectWindowStart)
                {
                    bool attackerHandlerIsPlayer = manager.attackerInputHandler != null && manager.attackerInputHandler.IsPlayer;
                    if (attackerHandlerIsPlayer)
                    {
                        if (elapsed < perfectWindowEnd)
                        {
                            CombatDebugDisplay.Instance?.ShowInputPrompt("지금이닷!");
                            Debug.Log($"[UI표시:막아!] 히트 {manager.CurrentHit + 1}, elapsed={elapsed:F5}, 타이밍창=({perfectWindow.start:F5} ~ {perfectWindow.End:F5})");
                        }
                        else if (elapsed >= perfectWindowEnd)
                        {
                            Debug.Log($"[BattleExecutor] 히트 {manager.CurrentHit} fallback");
                            manager.attackerInputHandler?.NotifyWindowClosed(true);
                            if (manager.attackerInputHandler != null)
                            {
                                manager.ResolveInput(manager.attackerInputHandler, false);
                            }
                        }
                    }
                    else
                    {
                        // ❌ 제거 예정: AI 공격자 입력 처리 (InputHandler 거치지 않고 직접 처리)
                        // AI는 InputHandler를 사용하지 않으므로 직접 CombatManager에 상태 설정
                        if (elapsed >= aiInputTime)
                        {
                            // manager.attackerInputHandler?.RecordAIInput(aiInputTime, aiAttackSuccess);
                            // if (manager.attackerInputHandler != null)
                            // {
                            //     manager.ResolveInput(manager.attackerInputHandler, aiAttackSuccess);
                            // }
                            
                            // 🆕 AI 공격자 입력 직접 처리 (InputHandler 없이)
                            manager.attackerPerfectInput = aiAttackSuccess;
                            manager.attackerInputTime = aiInputTime;
                            manager.ResolveAIInput(true, aiAttackSuccess); // 공격자, 완벽 입력 여부
                        }
                        else if (elapsed >= perfectWindowEnd)
                        {
                            // manager.attackerInputHandler가 null이 아닐 수도 있지만, AI는 InputHandler를 사용하지 않음
                            // if (manager.attackerInputHandler != null)
                            // {
                            //     manager.ResolveInput(manager.attackerInputHandler, false);
                            // }
                            
                            // 🆕 AI 공격자 입력 실패 처리
                            manager.attackerPerfectInput = false;
                            manager.attackerInputTime = perfectWindowEnd;
                            manager.ResolveAIInput(true, false); // 공격자, 실패
                        }
                    }
                }
                
                // 🆕 AI 방어자 막기 처리 (첫 번째 히트 타이밍에 도달하면 막기 시작)
                if (defender is EnemyCharacter && !aiIsGuarding && aiWillGuard && manager.CurrentHit == 0)
                {
                    // 첫 번째 히트의 perfectWindowStart에 도달하면 막기 시작
                    if (elapsed >= perfectWindowStart)
                    {
                        StartAIGuard(turnContext);
                        Debug.Log($"[BattleExecutor] 🆕 AI 막기 시작 - 첫 번째 히트 타이밍 도달 (elapsed:{elapsed:F3}, perfectWindowStart:{perfectWindowStart:F3})");
                    }
                }
                
                if(attackerIsPlayer && manager.CurrentAttackResultShown)
                {
                    CombatDebugDisplay.Instance?.ShowInputPrompt("V");
                }
                else if (!attackerIsPlayer && manager.CurrentDefenseResultShown)
                {
                    CombatDebugDisplay.Instance?.ShowInputPrompt("V");
                }

                // 발사체 기반 히트 전환 (액션 커맨드 타이밍에 따라)
                if (elapsed >= perfectWindowEnd && manager.windowPrompted)
                {
                    if (FloatingTextManager.Instance != null)
                    {
                        Vector3 textPosition = manager.GetFloatingTextPosition(attackerIsPlayer);
                        FloatingTextManager.Instance.ShowPerfectTimingEnd(textPosition, manager.CurrentHit + 1, perfectWindow);
                    }
                    
                    Debug.Log($"[BattleExecutor] 🆕 발사체 기반 히트 {manager.CurrentHit} 완료 → 전환, CurrentClashResultShown:{manager.CurrentClashResultShown}");

                    CombatDebugDisplay.Instance?.ShowInputPrompt("");
                    manager.CurrentAttackResultShown = false;
                    manager.CurrentDefenseResultShown = false;
                    manager.CurrentClashResultShown = false;
                    manager.floatingTextShown = false;

                    Debug.LogWarning($"[DEBUG] 🆕 발사체 기반 히트 {manager.CurrentHit} 완료 조건 만족 - windowPrompted false로 전환됨");
                    manager.windowPrompted = false;
                    manager.CurrentHit++;
                    
                    // 모든 히트가 완료되었는지 확인
                    if (manager.CurrentHit >= command.hitCount)
                    {
                        Debug.Log($"[BattleExecutor] 모든 히트 완료! CurrentHit={manager.CurrentHit}, hitCount={command.hitCount} - 마지막 히트 판정 확인");
                        
                        if (manager.hitJudgmentCompleted[manager.CurrentHit - 1])
                        {
                            Debug.Log($"[BattleExecutor] 마지막 히트 발사체 기반 판정 완료 - 턴 종료 대기 시작");
                            yield return new WaitForSeconds(GlobalConfig.Instance.TurnEndBuffer);
                            Debug.Log($"[BattleExecutor] 턴 종료 대기 완료 - 턴 종료");
                            break;
                        }
                        else
                        {
                            Debug.Log($"[BattleExecutor] 마지막 히트 발사체 기반 판정 대기 중...");
                        }
                    }
                }
            }
            yield return null;
        }
          
        Debug.Log($"[{actor.Name}] 커맨드 실행 완료: {command.commandName}");
        Debug.Log($"[InputTrace][Turn] PerformTurn End - actor:{actor.Name}, Time:{Time.time:F4}, Frame:{Time.frameCount}");
        Debug.Log($"[BattleExecutor] 🔵 메인 루프 종료 - {actor.Name} 턴 완료");
        controller.ReceiveCommandResult(result);

        // 모든 히트에 대한 최종 적중 판정이 완료될 때까지 대기
        yield return manager.StartCoroutine(EnsureAllHitJudgmentsCompleted(command.hitCount));
        Debug.Log("[BattleExecutor] 🆕 EnsureAllHitJudgmentsCompleted 완료 - 턴 종료 버퍼 대기 시작");

        // 턴 종료 버퍼 시간 대기
        float turnEndBuffer = GlobalConfig.Instance.TurnEndBuffer;
        if (turnEndBuffer > 0f)
        {
            Debug.Log($"[InputTrace][Turn] Waiting TurnEndBuffer - duration:{turnEndBuffer:F4}s, time:{Time.time:F4}");
            yield return new WaitForSeconds(turnEndBuffer);
        }
        Debug.Log("[BattleExecutor] 🆕 턴 종료 버퍼 대기 완료 - 입력 비활성화 시작");

        // 입력 비활성화 및 상태 초기화
        Debug.Log($"[BattleExecutor] 🆕 턴 종료 - 입력 비활성화 시작 (attackerIsPlayer:{attackerIsPlayer})");
        
        Debug.Log("[BattleExecutor] 🆕 공격자 입력 비활성화");
        manager.attackerInputHandler.DisableInput();
        
        Debug.Log("[BattleExecutor] 🆕 방어자 입력 비활성화");
        manager.defenderInputHandler.DisableInput();

        manager.attackerInputHandler.ResetInputState();
        manager.defenderInputHandler.ResetInputState();
        
        // 🆕 AI 막기 상태 리셋 및 애니메이션 중지
        ResetAIGuardState(turnContext);

        // 애니메이션 완료 대기
        yield return manager.StartCoroutine(manager.WaitForAnimationsComplete(actor, defender));
    }

    private IEnumerator EnsureAllHitJudgmentsCompleted(int hitCount)
    {
        if (hitCount <= 0)
        {
            Debug.Log($"[BattleExecutor] 🔍 hitCount가 0 이하 - 대기 생략");
            yield break;
        }
        
        float waitStart = Time.time;
        Debug.Log($"[BattleExecutor] 🔍 === Hit 판정 완료 대기 시작 === hitCount:{hitCount}");
        
        // 초기 상태 확인
        Debug.Log($"[BattleExecutor] 🔍 초기 상태 체크:");
        for (int i = 0; i < hitCount; i++)
        {
            bool isCompleted = (i < manager.hitJudgmentCompleted.Length) && manager.hitJudgmentCompleted[i];
            Debug.Log($"  - Hit {i}: {(isCompleted ? "✅ 이미 완료" : "⏳ 대기 중")}");
        }
        
        float lastLogTime = waitStart;
        int frameCount = 0;
        
        while (!manager.AreAllHitJudgmentsCompleted(hitCount))
        {
            frameCount++;
            float waited = Time.time - waitStart;
            
            // 1초마다 상태 로그
            if (Time.time - lastLogTime >= 1.0f)
            {
                Debug.Log($"[BattleExecutor] 🔍 대기 중... 경과: {waited:F2}초, 프레임: {frameCount}");
                for (int i = 0; i < hitCount; i++)
                {
                    if (i >= manager.hitJudgmentCompleted.Length || !manager.hitJudgmentCompleted[i])
                    {
                        Debug.Log($"  ⏳ Hit {i}: 미완료 (발사체 충돌 대기)");
                    }
                }
                lastLogTime = Time.time;
            }
            
            yield return null;
        }
        
        float finalWait = Time.time - waitStart;
        Debug.Log($"[BattleExecutor] 🔍 === Hit 판정 완료 대기 종료 === 대기 시간: {finalWait:F4}초, 프레임: {frameCount}");
    }

    internal void ProcessDamageCalculation(Character attacker, Character defender, ActionCommandData command, InputVersusResult.ResultType resultType, int hitIndex = 0)
    {
        Debug.Log($"\n[피해량 계산] ========== {attacker.Name} → {defender.Name} ==========");
        Debug.Log($"[피해량 계산] 판정: {resultType}, 히트: {hitIndex + 1}");
        
        float currentHitDamageRatio = command.GetDamageRatio(hitIndex);
        int attackerATK = BladeAction.Combat.StatsCalculationManager.Instance != null 
            ? BladeAction.Combat.StatsCalculationManager.Instance.GetFinalATK(attacker)
            : attacker.ATK;
        int baseDamage = Mathf.RoundToInt(attackerATK * currentHitDamageRatio);
        
        Debug.Log($"[피해량 계산] 기본 피해량: {attackerATK} × {currentHitDamageRatio} = {baseDamage}");
        
        // 치명타 판정
        bool isCritical = attacker.IsCriticalHit();
        if (isCritical)
        {
            int criticalDamage = attacker.CalculateCriticalDamage(baseDamage);
            Debug.Log($"[피해량 계산] 치명타 발생! {baseDamage} → {criticalDamage}");
            baseDamage = criticalDamage;
        }
        else
        {
            Debug.Log($"[피해량 계산] 치명타 없음");
        }
        
        // 판정 결과에 따른 피해량 감소 적용
        float damageReduction = manager.GetDamageReduction(resultType);
        int damageAfterReduction = Mathf.RoundToInt(baseDamage * damageReduction);
        Debug.Log($"[피해량 계산] 판정 감소: {baseDamage} × {damageReduction} = {damageAfterReduction}");
        
        // DR 적용 (막기 상태에 따라 다른 DR 사용) - 플레이어 + AI 막기 확인
        int effectiveDR;
        bool isGuardActive = manager.defenderInputHandler != null ? manager.defenderInputHandler.IsGuardActive : false;
        if (!isGuardActive)
        {
            isGuardActive = IsAIGuardActive();
        }
        if (isGuardActive)
        {
            effectiveDR = defender.GetGuardFinalDR();
        }
        else
        {
            effectiveDR = defender.GetFinalDR();
        }
        
        int damageAfterDR = manager.ApplyDefenseReduction(damageAfterReduction, effectiveDR);
        
        // DR 적용 결과 로그
        if (isGuardActive)
        {
            Debug.Log($"[피해량 계산] 막기 상태 - 막기 DR 적용: {damageAfterReduction} - {effectiveDR} = {damageAfterDR} (기본 DR: {defender.DR}, 막기 보너스: {defender.CharacterInitData.baseStats.guardDRBonus}, 임시 보너스: {defender.tempDRBonus})");
        }
        else
        {
            Debug.Log($"[피해량 계산] 일반 상태 - 일반 DR 적용: {damageAfterReduction} - {effectiveDR} = {damageAfterDR} (기본 DR: {defender.DR}, 임시 보너스: {defender.tempDRBonus})");
        }
        
        // 피해량이 0보다 크면 HP 감소 적용
        if (damageAfterDR > 0)
        {
            int oldHP = defender.HP;
            defender.TakeDamage(damageAfterDR);
            int newHP = defender.HP;
            int actualDamage = oldHP - newHP;
            
            Debug.Log($"[피해량 계산] HP 감소: {oldHP} → {newHP} (실제 감소량: {actualDamage})");
            Debug.Log($"[피해량 계산] 최종 결과: {defender.Name}이 {actualDamage} 피해를 받았습니다!");
            
            // HP 0 체크 및 전투 종료 처리 (즉시 체크)
            if (defender.IsDefeated)
            {
                Debug.LogWarning($"[피해량 계산] {defender.Name}이 패배했습니다! (HP: {defender.GetHPStatus()})");
                manager.EndBattle(defender == CombatCharacterManager.Instance.PlayerCharacter ? BattleResult.BattleEndReason.PlayerDefeated : BattleResult.BattleEndReason.EnemyDefeated);
                return;
            }
        }
        else
        {
            Debug.Log($"[피해량 계산] 피해량이 0이므로 HP 감소 없음");
        }
        
        Debug.Log($"[피해량 계산] ========== 계산 완료 ==========\n");
    }
    
    // ==================== AI 막기 시스템 ====================
    
    /// <summary>
    /// AI 막기 시스템 초기화
    /// </summary>
    private void InitializeAIGuardSystem()
    {
        if (aiDefenseDecisionMaker == null)
        {
            aiDefenseDecisionMaker = new DefaultAIDefenseDecisionMaker();
            Debug.Log("[BattleExecutor] 🆕 AI 막기 시스템 초기화 완료");
        }
    }
    
    /// <summary>
    /// 막기 의사결정용 AI 컨텍스트 생성
    /// </summary>
    private AIContext CreateAIContextForGuard(CombatTurnContext turnContext)
    {
        var defenderSlot = turnContext?.DefenderSlot;
        var attackerSlot = turnContext?.AttackerSlot;
        
        if (defenderSlot == null || attackerSlot == null)
        {
            Debug.LogError("[BattleExecutor] DefenderSlot 또는 AttackerSlot이 null입니다!");
            return new AIContext(0, 0f, false, 1, 100f, false, false);
        }
        
        Character defenderCharacter = defenderSlot.Character;
        float turnElapsedTime = TurnTimer.ElapsedTime;
        float posturePoints = defenderCharacter?.CurrentPoise ?? 100f;
        bool isInterrupted = defenderCharacter?.IsInterrupted ?? false;
        int totalHitCount = manager.CurrentResult?.HitCount ?? 1;
        bool attackerIsPlayer = attackerSlot.Character is PlayerCharacter;
        
        return new AIContext(
            0, // 막기 의사결정 시에는 hitIndex 0 사용
            turnElapsedTime,
            attackerIsPlayer,
            totalHitCount,
            posturePoints,
            isInterrupted,
            false, // 막기 의사결정 시에는 아직 막기 중이 아님
            defenderCharacter
        );
    }
    
    /// <summary>
    /// AI 막기 시작
    /// </summary>
    private void StartAIGuard(CombatTurnContext turnContext)
    {
        aiIsGuarding = true;
        
        var defenderController = turnContext?.DefenderController;
        if (defenderController != null)
        {
            defenderController.OnPlayDefence();
            Debug.Log("[BattleExecutor] 🆕 AI 막기 시작 - 애니메이션 재생");
        }
        else
        {
            Debug.LogError("[BattleExecutor] 방어자 컨트롤러를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// AI 막기 중지
    /// </summary>
    private void StopAIGuard(CombatTurnContext turnContext)
    {
        if (aiIsGuarding)
        {
            aiIsGuarding = false;
            
            var defenderController = turnContext?.DefenderController;
            if (defenderController != null)
            {
                defenderController.OnStopDefence();
                Debug.Log("[BattleExecutor] 🆕 AI 막기 중지 완료");
            }
        }
    }
    
    /// <summary>
    /// AI 막기 상태 확인 (CombatManager에서 호출)
    /// </summary>
    internal bool IsAIGuardActive()
    {
        return aiIsGuarding;
    }
    
    /// <summary>
    /// 턴 종료 시 AI 막기 상태 리셋 및 애니메이션 중지
    /// </summary>
    internal void ResetAIGuardState(CombatTurnContext turnContext)
    {
        // 막기 애니메이션 중지
        if (aiIsGuarding)
        {
            // turnContext가 없으면 현재 방어자 컨트롤러를 직접 가져옴
            if (turnContext == null)
            {
                turnContext = currentAIGuardTurnContext;
            }
            
            if (turnContext != null)
            {
                StopAIGuard(turnContext);
            }
            else
            {
                // turnContext가 없어도 현재 방어자 컨트롤러를 통해 중지 시도
                var defenderSlot = manager.CurrentDefenderSlot;
                if (defenderSlot?.Controller != null)
                {
                    defenderSlot.Controller.OnStopDefence();
                    Debug.Log("[BattleExecutor] 🆕 AI 막기 애니메이션 중지 (turnContext 없음)");
                }
            }
        }
        
        aiIsGuarding = false;
        aiWillGuard = false;
        currentAIGuardTurnContext = null;
    }
    
    /// <summary>
    /// 쳐내기 성공 시 AI 막기 해제 (CombatManager에서 호출)
    /// </summary>
    internal void StopAIGuardOnParry(CombatTurnContext turnContext)
    {
        if (aiIsGuarding && turnContext != null)
        {
            Debug.Log("[BattleExecutor] 🆕 쳐내기 성공 - AI 막기 해제");
            StopAIGuard(turnContext);
        }
    }
}
