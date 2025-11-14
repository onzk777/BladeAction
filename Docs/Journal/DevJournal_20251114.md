# 개발 일지 - 2025년 11월 14일

**작업 주제**: CombatSessionSystem 일반화 리팩토링 1단계  
**작업 시간**: 금일  
**상태**: 진행 중  

---

## 오늘 계획 (ToDo)
- [x] `CombatManager` 및 관련 매니저에서 `isPlayer` 전제 로직 전수 조사 → 슬롯/팀 기반 대체 설계
- [x] `PerformTurn` / `RunCombat`를 `CombatantSlot` 중심으로 리팩토링하여 공격자·방어자 컨텍스트 일원화
- [x] ControlType 도입 및 HUD/HP Panel 슬롯 기반 갱신
- [ ] 확률 리셋·입력·HUD 업데이트를 슬롯 기반 공통 API로 재정비하고 NPC 대 NPC 시나리오까지 검증
- [ ] TeamA=Player vs TeamB=NPC / 양쪽 NPC / TeamB=Player 시나리오 회귀 테스트 체크리스트 초안 작성

---

- 2025-11-08 일지 메모: `isPlayer` 의존 구간을 우선 추출하고, 턴 컨텍스트를 슬롯 기준으로 재구성할 것.
- 리팩토링 후 UI 이벤트 연결은 사용자 확인 절차를 기다릴 것.
- Spine 애니메이션 타임라인은 초 단위 기준 유지.
- `CombatCharacterManager.ControlType` (Player/AI) 도입 완료, 슬롯·컨트롤러 연결 로직 정비, HP Panel TeamA/TeamB 전환.
- Battle 상태를 `BattleState` 중첩 클래스로 정리 완료 (턴 컨텍스트·입력·히트 판정 상태 통합 관리). BattleExecutor로의 이전 기반 확보.

---

## 다음 세션 에이전트 안내
- **BattleExecutor 확장**: 현재 `RunBattle()`가 턴 수행을 대행하지만, 개별 턴 처리(`PerformTurn`, `EnsureAllHitJudgmentsCompleted`, `ProcessDamageCalculation`)는 아직 `CombatManager`에 남아 있습니다. 해당 메서드들을 `BattleExecutor`로 단계적으로 이전해주세요. 이때 필요한 상태는 `BattleState`를 통해 접근 가능합니다.
- **슬롯 기반 공통 로직 마무리**:
  - `ResetNPCProbabilities()`, `ResetBehaviorTreeStates()` 호출 위치가 Executor 흐름에 맞게 조정되어 있는지 재검토해 주세요.
  - 입력 핸들러 초기화와 HUD 업데이트(`CombatDebugDisplay`)가 TeamA/TeamB 슬롯 기반으로 빠짐없이 작동하는지 확인하고, 필요 시 공통 헬퍼로 묶어주세요.
- **회귀 테스트**: 아래 시나리오를 중심으로 빠르게 회귀 테스트 진행 부탁드립니다.
  1. TeamA(Player) vs TeamB(NPC)
  2. TeamA(NPC) vs TeamB(NPC)
  3. TeamA(Player) & TeamB(Player) (플레이어 제어 슬롯 전환 시나리오)
- **문서 업데이트**: 2단계 진행 상황이 BattleExecutor 분리까지 확대되면, `Docs/Design/CombatSessionSystem/전투_세션_시스템_구현_계획서.md`의 진행도를 다시 조정하고 주요 변화(메서드 분리 범위 등)를 기록해주세요.


