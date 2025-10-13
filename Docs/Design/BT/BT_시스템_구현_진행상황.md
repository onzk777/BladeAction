# BT 시스템 구현 진행상황

## 개요
Behavior Tree 시스템의 구현 진행상황을 추적하는 문서입니다.  
각 Phase별 세부 작업의 완료 상태를 실시간으로 업데이트합니다.

---

## 전체 진행률: 88% (Phase 2 완료, Phase 3 거의 완료)

| Phase | 상태 | 진행률 | 완료일 |
|-------|------|--------|--------|
| Phase 1 | ✅ 완료 | 100% | 2025-10-01 |
| Phase 2 | ✅ 완료 | 100% | 2025-10-02 |
| Phase 3 | 🔄 진행 중 | 90% | - |
| Phase 4 | ⏳ 대기 | 0% | - |
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

## Phase 3: BT 실행 및 AI 연동 🔄 (90%)

| 작업 | 상태 | 비고 |
|------|------|------|
| 3.1 BT Executor 구현 | ✅ 완료 | BT 평가 및 실행 로직 |
| 3.2 EnemyController BT 연동 | ✅ 완료 | BT 실행 흐름 구현 |
| 3.3 확률 Override 시스템 | ✅ 완료 | NPCRuntimeProbabilities, 턴별 리셋 |
| 3.4 검술 선택 로직 수정 | ✅ 완료 | BT 결과에 따른 검술 선택 |
| 3.5 버그 수정 | 🔄 진행 중 | EnemyController BT 선택, Poise 중단, UI 표시 |

---

## Phase 4: 디버깅 및 최적화 ⏳ (0%)

| 작업 | 상태 | 비고 |
|------|------|------|
| 4.1 BT 실행 로그 시스템 | ⏳ 대기 (0%) | 디버깅용 로그 출력 |
| 4.2 Inspector 커스텀 에디터 | ⏳ 대기 (0%) | BT 편집 도구 |
| 4.3 런타임 확률 모니터링 | ⏳ 대기 (0%) | 실시간 확률 변화 추적 |

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

### ✅ 완료된 기능
- BT Core 시스템 (모든 노드 타입)
- BT 인스턴스화 시스템
- BT 실행 흐름 (기본 구조)
- 검술 선택 로직
- 전투 시작 시 BT 상태 리셋
- **NPCRuntimeProbabilities 시스템** (2025-10-13)
- **확률 Override 및 턴별 리셋** (2025-10-13)
- **턴 타이머 UI 개선** (2025-10-13)

### 🔴 발견된 버그 (수정 대기)
1. **EnemyController BT 선택 무시**: UseTestMode=false여도 testCommandIndex 사용
2. **Poise 중단 시 무한 대기**: hitJudgmentCompleted 미완료로 턴 진행 안 됨
3. **Enemy UI 미표시**: 검술 선택이 UI에 반영 안 됨

### ⏳ 다음 작업
- 🔴 CRITICAL 버그 3개 수정
- BT 에셋 생성 (공격형/방어형/특수 패턴)
- Unity 플레이 테스트 및 검증

---

## 예상 완료 일정

| Phase | 예상 완료일 | 비고 |
|-------|-------------|------|
| Phase 3 | 2025-10-13 | 버그 수정 후 완료 |
| Phase 4 | 2025-10-14 | 디버깅 도구 완성 |
| Phase 5 | 추후 | 낮은 우선순위 |

---

## 최근 업데이트 (2025-10-13)

### 완료된 작업
- NPCRuntimeProbabilities 클래스 구현
- EnemyCombatant 확률 Override 적용
- CombatManager 턴별 확률 리셋 연동
- 턴 타이머 UI 개선 (잔여/전체 시간, 진행률 바)

### 발견된 이슈
- EnemyController.GetSelectedCommandIndex() 버그
- Poise 중단 시 무한 대기 버그
- Enemy UI 표시 누락

**상세 내용**: DevJournal_20251013.md 참조

---

**문서 버전**: 1.1  
**최종 업데이트**: 2025년 10월 13일  
**다음 업데이트**: 버그 수정 완료 시
