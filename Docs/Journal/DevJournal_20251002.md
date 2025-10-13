# 개발 일지 - 2025년 10월 2일

## 작업 개요
- **주제**: BT 시스템 Phase 2 완료 및 Phase 3 사전 작업
- **목표**: BT Core 시스템 구현 완료 + Phase 3 기반 작업

---

## 진행 사항

### 1. BT 시스템 Phase 2 완료 ✅

#### 1.1 BT Core 클래스 시스템 구현
**구현된 파일들**:
- `Assets/Script/BT/Core/BTNode.cs`: 모든 BT 노드의 기본 클래스
- `Assets/Script/BT/Core/BTConditionNode.cs`: 조건 노드 기본 클래스
- `Assets/Script/BT/Core/BTActionNode.cs`: 액션 노드 기본 클래스
- `Assets/Script/BT/Core/BehaviorTreeContext.cs`: BT 실행 컨텍스트
- `Assets/Script/BT/Core/BTCompositeNode.cs`: 복합 노드 기본 클래스

**주요 기능**:
- `executeOncePerCombat`: 전투당 한 번만 실행 시스템
- `invertResult`: 조건 결과 반전 기능
- `priority`: 액션 우선순위 시스템

#### 1.2 Composite Node 구현
**구현된 파일들**:
- `Assets/Script/BT/Core/BTComposite_Sequence.cs`: AND 조건 (모든 자식이 true)
- `Assets/Script/BT/Core/BTComposite_Selector.cs`: OR 조건 (하나라도 true)

**특징**:
- Short-circuit 평가 지원
- 자동 설명 생성 (OnValidate)
- Inspector 친화적 설정

#### 1.3 Condition Node 구현
**구현된 파일들**:
- `Assets/Script/BT/Conditions/BTCondition_HPComparison.cs`: HP 비교 조건
- `Assets/Script/BT/Conditions/BTCondition_PoiseComparison.cs`: 자세 비교 조건
- `Assets/Script/BT/Conditions/BTCondition_TurnType.cs`: 턴 타입 조건
- `Assets/Script/BT/Conditions/BTCondition_TurnCount.cs`: 턴 수 조건

**지원 기능**:
- 6가지 비교 연산자 (>, <, >=, <=, ==, !=)
- 절대값/비율값 선택
- 자신/상대 대상 선택
- 자동 설명 생성

#### 1.4 Action Node 구현
**구현된 파일들**:
- `Assets/Script/BT/Actions/BTAction_ProbabilityAdjustment.cs`: 확률 조정 액션
- `Assets/Script/BT/Actions/BTAction_ForceBehavior.cs`: 강제 행동 액션
- `Assets/Script/BT/Actions/BTAction_CommandSelection.cs`: 검술 선택 액션
- `Assets/Script/BT/Actions/BTAction_DisableBehavior.cs`: 행동 비활성화 액션

**개선 사항**:
- `disableEnabled`, `forceEnabled` 변수 제거 (BTNode의 `isEnabled` 활용)
- `executeOnce` → `executeOncePerCombat` 명확한 변수명 변경
- 전투당 한 번만 실행 기준 명확화

#### 1.5 BehaviorTreeData 시스템
**구현된 파일들**:
- `Assets/Script/BT/BehaviorTreeData.cs`: BT ScriptableObject
- `Assets/Script/BT/BehaviorTreeExecutor.cs`: BT 실행기

**주요 기능**:
- BT Entry 리스트 관리 (인덱스 = 우선순위)
- 다중 BT 순차 평가
- Priority 기반 액션 실행
- 실행 결과 로깅

### 2. BT 인스턴스화 시스템 구현 ✅

#### 2.1 개체별 독립적인 BT 상태 관리
**구현 내용**:
- `CharacterData.originalBehaviorTrees`: 에디터용 원본 BT
- `CharacterData.behaviorTrees`: 런타임 인스턴스 BT
- `CharacterData.InstantiateBehaviorTrees()`: BT 인스턴스화 메서드

**블랙보드 개념 적용**:
- 각 NPC가 완전히 독립적인 BT 상태 보유
- 같은 BT를 사용하는 다른 개체가 서로 영향을 주지 않음
- 전투별로 BT 상태가 격리됨

#### 2.2 전투 시작 시 BT 상태 리셋
**구현 내용**:
- `CharacterData.ResetBehaviorTreeExecutionStates()`: BT 상태 리셋
- `CombatManager.ResetBehaviorTreeStates()`: 전투 시작 시 호출
- `BTActionNode.ResetCombatExecution()`: 개별 노드 상태 리셋

**리셋 타이밍**:
- 새 전투 시작 시 자동으로 모든 BT 상태 초기화
- `executeOncePerCombat` 상태도 함께 리셋

### 3. Phase 3 사전 작업 (미리 구현) ⚠️

#### 3.1 EnemyCombatant BT 실행 시스템
**구현된 파일**: `Assets/Script/EnemyCombatant.cs`

**구현 내용**:
- `ExecuteBehaviorTrees()`: BT 평가 및 컨텍스트 생성
- `ApplyBehaviorTreeResults()`: BT 결과를 실제 확률에 적용 (TODO)
- `GetSelectedCommandFromBT()`: BT 결과에 따른 검술 선택

**BT 실행 흐름**:
```
턴 시작 → ChooseCommand() → BT 실행 → 검술 선택 → 액션 실행
```

#### 3.2 CombatManager 연동
**구현된 파일**: `Assets/Script/Combat/CombatManager.cs`

**추가된 프로퍼티**:
- `CurrentTurnNumber`: 현재 턴 번호 (BT에서 사용)
- `IsNPCAttackTurn`: NPC 공격 턴 여부 (BT에서 사용)

**턴 관리**:
- 매 턴마다 턴 번호 자동 증가
- 플레이어/적 턴 구분 로직

### 4. 문서화 작업 ✅

#### 4.1 BT 시스템 사용 메뉴얼 작성
**파일**: `Docs/Design/BT/메뉴얼/BT_시스템_사용_메뉴얼.md`

**내용**:
- 노드 타입별 상세 가이드
- 실제 사용 예시 (공격형/방어형 NPC 패턴)
- executeOncePerCombat 사용법
- 문제 해결 가이드

#### 4.2 BehaviorTreeContext 사용 예시 작성
**파일**: `Docs/Design/BT/BehaviorTreeContext_사용_예시.md`

**내용**:
- Context 데이터 조합 과정 상세 설명
- 실제 시나리오별 동작 예시
- 디버깅 팁 및 로그 활용법

---

## 기술적 개선사항

### 1. 변수명 개선
- `executeOnce` → `executeOncePerCombat`: 명확한 기준 제시
- `disableEnabled`, `forceEnabled` 제거: BTNode의 `isEnabled` 활용

### 2. 아키텍처 개선
- BT 인스턴스화 시스템으로 개체별 독립성 확보
- 블랙보드 개념 도입으로 메모리 관리 최적화
- 전투당 한 번만 실행 기준 명확화

### 3. 사용성 개선
- Inspector 친화적인 노드 설정
- 자동 설명 생성 (OnValidate)
- 상세한 디버그 로그 시스템

---

## 이슈 및 해결

### 1. 컴파일 오류 해결
**문제**: `Combatant.Poise` 속성명 오류
**해결**: `Combatant.CurrentPoise`로 수정

**문제**: 네임스페이스 참조 오류
**해결**: `BladeAction.BT` 네임스페이스 명시

### 2. 설계 개선
**문제**: `disableEnabled` 변수명 혼란
**해결**: BTNode의 `isEnabled` 활용으로 통일

**문제**: "한 번만 실행" 기준 불명확
**해결**: "전투당 한 번" 기준으로 명확화

---

## Phase 2 완료 상태

### ✅ 완료된 기능
1. BT Core 클래스 시스템
2. Condition/Action/Composite 노드 구현
3. BT 인스턴스화 시스템
4. executeOncePerCombat 시스템
5. 전투 시작 시 BT 상태 리셋
6. BT 시스템 사용 메뉴얼

### ⚠️ Phase 3 사전 작업 (유지)
1. EnemyCombatant BT 실행 로직
2. CombatManager 연동 코드
3. BT 실행 흐름 구현

### 📋 Phase 3에서 완성할 내용
1. 확률 Override 실제 적용 로직
2. BT 실행 결과 검증 및 디버깅
3. 테스트 및 최적화

---

## 다음 작업 계획

### Phase 3: BT 실행 및 AI 연동
1. **확률 Override 실제 적용**: BT 결과를 NPC 확률에 반영
2. **BT 실행 검증**: 디버그 로그 및 테스트
3. **성능 최적화**: BT 실행 효율성 개선
4. **통합 테스트**: 전체 BT 시스템 동작 확인

### 예상 소요 시간
- Phase 3: 2-3일
- 테스트 및 디버깅: 1일

---

## 메모 및 참고사항

### BT 시스템 특징
- **개체별 독립성**: 각 NPC가 완전히 독립적인 BT 상태
- **전투당 한 번**: executeOncePerCombat 기준 명확화
- **블랙보드 개념**: 개체별 메모리 관리 최적화
- **Inspector 친화적**: Unity 에디터에서 쉽게 설정 가능

### 사용 가능한 BT 노드
- **조건 노드**: HP 비교, 자세 비교, 턴 타입, 턴 수
- **액션 노드**: 확률 조정, 강제 행동, 검술 선택, 행동 비활성화
- **복합 노드**: Sequence (AND), Selector (OR)

---

**작성자**: AI Assistant  
**작업 시간**: 
- Phase 2 구현: 약 3-4시간
- Phase 3 사전 작업: 약 1시간
- 문서화: 약 1시간
**완료 시간**: 2025년 10월 2일  
**Phase 2 상태**: ✅ 완료  
**다음 목표**: Phase 3 - BT 실행 및 AI 연동

