# BT 시스템 구현 진행상황

## 개요
Behavior Tree 시스템의 구현 진행상황을 추적하는 문서입니다.  
각 Phase별 세부 작업의 완료 상태를 실시간으로 업데이트합니다.

---

## 전체 진행률: 100% (Phase 1~4 완료!)

| Phase | 상태 | 진행률 | 완료일 |
|-------|------|--------|--------|
| Phase 1 | ✅ 완료 | 100% | 2025-10-01 |
| Phase 2 | ✅ 완료 | 100% | 2025-10-02 |
| Phase 3 | ✅ 완료 | 100% | 2025-10-13 |
| Phase 4 | ✅ 완료 | 100% | 2025-10-14 |
| Phase 5 | ⏳ 대기 | 0% | - |

---

## Phase 1: 데이터 구조 확장 ✅

| 작업 | 상태 | 비고 |
|------|------|------|
| 1.1 CharacterData 확장 | ✅ 완료 | NPC 행동 확률 + BT 리스트 |
| 1.2 ActionCommandTag 시스템 | ✅ 완료 | ScriptableObject 기반 태그 관리 |
| 1.3 GlobalConfig 확장 | ✅ 완료 | Default BT 참조 추가 |

---

## Phase 2: BT Core 시스템 구현 ✅

| 작업 | 상태 | 비고 |
|------|------|------|
| 2.1 BT 기반 클래스 구현 | ✅ 완료 | BTNode, BTConditionNode, BTActionNode, BehaviorTreeContext |
| 2.2 Condition Node 구현 | ✅ 완료 | HP 비교, 자세 비교, 턴 타입, 턴 수 |
| 2.3 Action Node 구현 | ✅ 완료 | 확률 조정, 강제 행동, 검술 선택, 행동 비활성화 |
| 2.4 Composite Node 구현 | ✅ 완료 | Sequence (AND), Selector (OR) |
| 2.5 BehaviorTreeData 구현 | ✅ 완료 | ScriptableObject + BT Executor |

---

## Phase 3: BT 실행 및 AI 연동 ✅ (100%)

| 작업 | 상태 | 비고 |
|------|------|------|
| 3.1 BT Executor 구현 | ✅ 완료 | BT 평가 및 실행 로직 |
| 3.2 EnemyController BT 연동 | ✅ 완료 | BT 실행 흐름 구현 |
| 3.3 확률 Override 시스템 | ✅ 완료 | NPCRuntimeProbabilities, 턴별 리셋 |
| 3.4 검술 선택 로직 수정 | ✅ 완료 | BT 결과에 따른 검술 선택 |
| 3.5 CRITICAL 버그 수정 | ✅ 완료 | BT 선택, Poise 중단, UI 표시, 평가 타이밍 |
| 3.6 BT 평가 타이밍 개선 | ✅ 완료 | 공격/방어 턴 모두 평가, isAttackTurn 조건 의미화 |
| 3.7 Player/Enemy 구조 통일 | ✅ 완료 | 동일한 BT 시스템, 향후 자동 전투 대비 |
| 3.8 AI 확률 우선순위 수정 | ✅ 완료 | RuntimeProbabilities 우선 참조 |
| 3.9 DoParryWhileGuarding 노드 분리 | ✅ 완료 | 단일 책임 원칙, 직관적 UI |
| 3.10 테스트 BT 에셋 생성 | ⏳ 선택 | 공격형/방어형/특수 패턴 (Phase 4에서 진행 가능) |
| 3.11 통합 테스트 및 검증 | ⏳ 선택 | Unity 플레이 테스트 (Phase 4에서 진행 가능) |

---

## Phase 4: 디버깅 및 최적화 ✅ (100%)

| 작업 | 상태 | 비고 |
|------|------|------|
| 4.1 BTLogger 클래스 구현 | ✅ 완료 | 체계적인 로깅 (Console + 히스토리 저장) |
| 4.2 BTLogHistory 구현 | ✅ 완료 | BT 실행 기록 데이터 저장 (최대 50개) |
| 4.3 BT 실행 로그 시스템 | ✅ 완료 | BehaviorTreeExecutor 로그 강화 |
| 4.4 Condition 노드 로그 | ✅ 완료 | 자동 평가 로그 |
| 4.5 Action 노드 로그 | ✅ 완료 | 실행 로그 및 중복 제거 |
| 4.6 Combatant 확률 로그 | ✅ 완료 | 확률 변경/리셋 추적 |
| 4.7 BTMonitorUI | ✅ 완료 | 기본 모니터링 UI |
| 4.8 BTDebugPanel | ✅ 완료 | 고급 디버그 패널 (히스토리, 필터, 제어) |
| 4.9 DebugPanelController 확장 | ✅ 완료 | 패널 전환 기능 |
| 4.10 UI 개선 | ✅ 완료 | 색상 조정, 특수 문자 → 일반 문자 |

---

## Phase 5: Additional Turn Duration ⏳ (0%)

| 작업 | 상태 | 비고 |
|------|------|------|
| 5.1 Duration 관리 시스템 | ⏳ 대기 (0%) | 낮은 우선순위 |
| 5.2 중첩 효과 처리 | ⏳ 대기 (0%) | 낮은 우선순위 |

---

## 추가 구현 사항

### BT 인스턴스화 시스템 ✅
- **개체별 독립성**: 각 NPC가 완전히 독립적인 BT 상태
- **블랙보드 개념**: 개체별 메모리 관리 최적화
- **전투당 한 번**: executeOncePerCombat 기준 명확화

### NPCRuntimeProbabilities 시스템 ✅ (2025-10-13)
- **확률 데이터 관리**: 원본 보호 및 복사본 수정
- **BT 결과 적용**: ApplyOverrides()로 확률 조정
- **턴별 리셋**: ResetToOriginal()로 원본 복원
- **CombatManager 연동**: 턴 종료 시 자동 리셋

### 턴 타이머 UI 개선 ✅ (2025-10-13)
- **상세 시간 표시**: 잔여/전체 시간 + 진행률(%)
- **프로그레스 바**: Image Fill Amount 지원
- **Inspector 연결**: UI 요소 드래그 앤 드롭 가능

### 문서화 ✅
- **BT 시스템 사용 메뉴얼**: 노드별 상세 가이드
- **BehaviorTreeContext 사용 예시**: 실제 시나리오별 동작 설명
- **개발 일지**: 2025-10-01, 2025-10-02, 2025-10-13

---

## 현재 상태 요약

### ✅ 완료된 핵심 기능
1. **BT Core 시스템** - 모든 노드 타입 (Condition, Action, Composite)
2. **BT 인스턴스화 & Blackboard 패턴** - 개체별 독립 상태 관리
3. **BT 실행 흐름** - Executor 및 컨텍스트 시스템
4. **검술 선택 로직** - BT 결과 기반 선택
5. **전투 시작/종료 BT 상태 리셋** - 완전 초기화
6. **NPCRuntimeProbabilities 시스템** - 확률 관리 (원본 보호)
7. **확률 Override 및 턴별 리셋** - BT 조정 적용 및 복원
8. **턴 타이머 UI 개선** - 잔여/전체 시간 + 진행률 바
9. **BT 평가 타이밍 시스템** - 공격/방어 턴 모두 평가
10. **Player/Enemy 구조 통일** - 동일한 BT 구조 (자동 전투 대비)
11. **AI 확률 우선순위 시스템** - RuntimeProbabilities > CustomSettings > GlobalConfig
12. **DoParryWhileGuarding 액션 노드** - 단일 책임 원칙, 직관적 체크박스 UI
13. **BTLogger 시스템** - 체계적인 로깅 (Console + 히스토리 데이터 저장)
14. **BTLogHistory** - BT 실행 기록 저장 (최대 50개, 조건/액션/확률)
15. **BTMonitorUI** - 기본 모니터링 UI (실시간 확률 표시)
16. **BTDebugPanel** - 고급 디버그 UI (히스토리, 상세 로그, 필터링, 제어) ✨
17. **DebugPanelController 패널 전환** - 전투 정보 ↔ BT 정보 전환

### ✅ 수정 완료된 CRITICAL 버그 (2025-10-13)
1. **EnemyController BT 선택 무시** - GetSelectedCommandIndex() 수정, 캐싱 구현
2. **Poise 중단 시 무한 대기** - ForceCompleteRemainingHits() 구현
3. **Enemy UI 미표시** - EnemyActionSelectUI.SetSelectedButton() 구현
4. **방어 턴 BT 미평가** - BT 평가와 검술 선택 분리, 양쪽 모두 평가
5. **확률 조정 미적용** - AI Defense가 RuntimeProbabilities 우선 참조
6. **ParryWhileGuarding 미적용** - 별도 액션 노드로 분리, 체크박스 UI

### ✅ Phase 3 완료!
- 모든 핵심 기능 구현 완료
- CRITICAL 버그 모두 수정
- 설계 개선 완료 (Blackboard 패턴, 단일 책임 원칙)
- BT 시스템 실전 사용 가능 상태

### 🎯 추가 개선 사항 (선택)
- 테스트 BT 에셋 생성 (공격형/방어형/특수 패턴)
- Unity 플레이 테스트 및 최종 검증
- Inspector 커스텀 에디터
- Phase 5 (Duration 시스템)

---

## 예상 완료 일정

| Phase | 예상 완료일 | 실제 완료일 | 비고 |
|-------|-------------|-------------|------|
| Phase 3 | 2025-10-13 | ✅ 2025-10-13 | 완료 |
| Phase 4 | 2025-10-14 | ✅ 2025-10-14 | 완료 |
| Phase 5 | 추후 | - | 낮은 우선순위 |

---

## 최근 업데이트 (2025-10-14)

### Phase 4 작업 완료 ✅ (실전 검증 완료!)

#### 디버깅 시스템 구현
- **BTLogger 클래스** (457줄)
  - Console 로그 + BTLogHistory 데이터 저장
  - 6개 로그 레벨 제어
  - 색상 코드 (밝은 배경 대응으로 어둡게 조정)
  - 컴파일 에러 수정 (Poise → CurrentPoise, threshold → turnCount)
  
- **BTLogHistory 클래스** (227줄)
  - BT 실행 기록 메모리 저장 (최대 50개)
  - 조건/액션/확률 모든 데이터 포함
  - 필터링/검색 기능 (턴별, Combatant별)
  
- **BTDebugPanel** (300줄) ✨ 핵심!
  - 요약 정보, 실행 히스토리, 상세 로그 (3개 영역)
  - 필터링 (Enemy/Player, 매칭만)
  - 제어 (클리어, 일시정지, 내보내기)
  - 특수 문자 → 일반 문자 변경 (폰트 의존성 제거)
  - 색상 최적화 (밝은 배경에서 가독성 향상)
  
- **BTMonitorUI** (312줄)
  - 기본 모니터링 UI (간단 버전)
  
- **DebugPanelController 확장** (219줄)
  - 패널 전환 기능 (전투 정보 ↔ BT 정보)
  - ShowCombatInfoPanel(), ShowBTInfoPanel()
  - 확장 가능한 구조

#### 통합 및 테스트
- BehaviorTreeExecutor Entry 정보 전달
- EnemyCombatant/PlayerCombatant 프로퍼티 추가
- 실전 동작 확인 완료 ✅

**상세 내용**: DevJournal_20251014.md 참조

---

## 이전 업데이트 (2025-10-13)

### 오전 작업 완료 ✅
- NPCRuntimeProbabilities 클래스 구현
- EnemyCombatant 확률 Override 적용
- CombatManager 턴별 확률 리셋 연동
- 턴 타이머 UI 개선 (잔여/전체 시간, 진행률 바)

### 저녁 작업 완료 ✅
- **CRITICAL 버그 6개 수정 완료**
  - EnemyController BT 선택 무시 버그
  - Poise 중단 시 무한 대기 버그
  - Enemy UI 미표시 문제
  - 방어 턴 BT 미평가 문제 (근본 설계 수정)
  - 확률 조정 미적용 문제 (AI 우선순위 수정)
  - ParryWhileGuarding 미적용 문제 (액션 노드 분리)

- **BT 평가 타이밍 시스템 개선**
  - 공격자/방어자 모두 BT 평가 (공격/방어 턴 무관)
  - isAttackTurn 조건이 의미 있게 작동
  - BT 평가와 검술 선택 분리

- **Player/Enemy 구조 통일**
  - PlayerCombatant에 BT 시스템 완전 구현
  - ExecuteBehaviorTrees(), ResetBTEvaluation() 추가
  - 향후 자동 전투 시스템 대비 완료

- **AI Defense 시스템 개선**
  - DefaultAIDefenseDecisionMaker가 RuntimeProbabilities 우선 참조
  - AIContext에 defenderCombatant 전달
  - BT 확률 조정이 막기/쳐내기에 실제 적용

- **DoParryWhileGuarding 액션 노드 분리**
  - BTAction_ProbabilityAdjustment는 float 확률만 조정
  - BTAction_DoParryWhileGuarding는 bool 행동 활성화 전용
  - 단일 책임 원칙 준수, 직관적인 체크박스 UI

**상세 내용**: DevJournal_20251013.md 참조

---

### ✅ 추가 개선 완료 (2025-10-14 오후)

#### 전투 시스템 개선
- **쳐내기 시 막기 자동 해제**
  - DefenderInputHandler.TriggerFinalJudgment() 수정
  - Player/Enemy 공통 로직
  - 막기/쳐내기 효과 중첩 방지

#### BT 구조 개선
- **ActionWrapper 시스템**
  - Entry별 액션 활성화/비활성화 제어
  - 같은 노드를 여러 BT에서 다르게 사용 가능
  - 노드 재사용성 향상
  
- **BTNode.isEnabled Obsolete 처리**
  - 노드 공유 문제 해결
  - IsValid()에서 체크 제거

#### 편의성 개선
- **BehaviorTreeData Custom Editor** (240줄)
  - Condition/Action 노드 인라인 편집
  - Composite 노드 재귀 표시
  - ActionWrapper 체크박스 UI
  - 한 화면에서 모든 설정 편집 가능

---

**문서 버전**: 4.0 (최종 완성)  
**최종 업데이트**: 2025년 10월 14일  
**현재 상태**: ✅ **Phase 1~4 완전 완료, 실전 사용 가능, 편의성 대폭 향상**  
**다음 단계**: 다른 시스템 개발 (BT 시스템 완성)
