# BT 시스템 사용 메뉴얼

## 목차
1. [BT 시스템 개요](#bt-시스템-개요)
2. [노드 타입별 상세 가이드](#노드-타입별-상세-가이드)
3. [실제 사용 예시](#실제-사용-예시)
4. [고급 활용법](#고급-활용법)
5. [문제 해결 가이드](#문제-해결-가이드)

---

## BT 시스템 개요

### BT란?
Behavior Tree(행동 트리)는 NPC의 행동 패턴을 조건과 액션의 조합으로 제어하는 시스템입니다.

### 기본 구조
```
BT Entry (우선순위 순서)
├── 조건 (Condition): "언제 실행할 것인가?"
└── 액션들 (Actions): "무엇을 할 것인가?"
```

### 실행 순서
1. **턴 시작 시 1회 평가**
2. **Entry 순차 평가** (0번 → 1번 → 2번...)
3. **조건 만족 시 액션 실행**
4. **상위 조건 만족 시 하위 미체크** (우선순위 처리)

---

## 노드 타입별 상세 가이드

### 1. Condition Nodes (조건 노드)

#### 1.1 HP 비교 조건
**파일**: `BTCondition_HPComparison`

**설정 항목**:
- `target`: 비교 대상 (자신/상대)
- `comparisonOperator`: 비교 연산자 (>, <, >=, <=, ==, !=)
- `valueType`: 값 타입 (절대값/비율)
- `threshold`: 임계값

**사용 예시**:
```
자신의 HP가 30% 미만인가?
→ target: Self, operator: <, valueType: Percentage, threshold: 0.3

상대의 HP가 50 이상인가?
→ target: Target, operator: >=, valueType: Absolute, threshold: 50
```

#### 1.2 자세 비교 조건
**파일**: `BTCondition_PoiseComparison`

**설정 항목**: HP 비교와 동일

**사용 예시**:
```
상대의 자세가 80% 이상인가?
→ target: Target, operator: >=, valueType: Percentage, threshold: 0.8
```

#### 1.3 턴 타입 조건
**파일**: `BTCondition_TurnType`

**설정 항목**:
- `turnType`: 확인할 턴 타입 (공격/방어)

**사용 예시**:
```
현재가 공격 턴인가?
→ turnType: AttackTurn

현재가 방어 턴인가?
→ turnType: DefenseTurn
```

#### 1.4 턴 수 조건
**파일**: `BTCondition_TurnCount`

**설정 항목**:
- `comparisonOperator`: 비교 연산자
- `turnCount`: 비교할 턴 수

**사용 예시**:
```
현재 턴이 5턴 이상인가?
→ operator: >=, turnCount: 5
```

### 2. Composite Nodes (복합 노드)

#### 2.1 Sequence 노드 (AND)
**파일**: `BTComposite_Sequence`

**기능**: 모든 자식 조건이 true일 때만 true 반환

**사용 예시**:
```
조건: "자신 HP < 30% AND 상대 자세 > 70%"
→ Sequence 노드에 HP 조건과 자세 조건을 자식으로 추가
```

#### 2.2 Selector 노드 (OR)
**파일**: `BTComposite_Selector`

**기능**: 자식 조건 중 하나라도 true이면 true 반환

**사용 예시**:
```
조건: "자신 HP < 20% OR 자신 자세 < 30%"
→ Selector 노드에 HP 조건과 자세 조건을 자식으로 추가
```

### 3. Action Nodes (액션 노드)

#### 3.1 확률 조정 액션
**파일**: `BTAction_ProbabilityAdjustment`

**설정 항목**:
- `targetProbability`: 조정할 확률 타입
- `adjustmentType`: 조정 방식 (절대값/상대값)
- `value`: 조정할 값 (0~1)
- `priority`: 우선순위
- `executeOnce`: 한 번만 실행 여부

**사용 예시**:
```
공격 성공률을 80%로 설정
→ targetProbability: AttackPerfectRate, adjustmentType: Absolute, value: 0.8

막기 시도 확률을 +20% 증가
→ targetProbability: GuardAttemptRate, adjustmentType: Relative, value: 0.2
```

#### 3.2 강제 행동 액션
**파일**: `BTAction_ForceBehavior`

**설정 항목**:
- `behaviorType`: 강제할 행동 타입
- `forceEnabled`: 강제 활성화 여부

**사용 예시**:
```
이번 턴에 반드시 막기 시도
→ behaviorType: Guard, forceEnabled: true
```

#### 3.3 검술 선택 액션
**파일**: `BTAction_CommandSelection`

**설정 항목**:
- `selectionType`: 선택 방식 (인덱스/태그)
- `commandIndex`: 검술 인덱스 (ByIndex일 때)
- `requiredTag`: 검술 태그 (ByTag일 때)

**사용 예시**:
```
검술 인덱스 2 선택
→ selectionType: ByIndex, commandIndex: 2

'필살기' 태그 검술 중 랜덤 선택
→ selectionType: ByTag, requiredTag: "필살기"
```

#### 3.4 행동 비활성화 액션
**파일**: `BTAction_DisableBehavior`

**설정 항목**:
- `behaviorType`: 비활성화할 행동 타입
- `disableEnabled`: 비활성화 여부

**사용 예시**:
```
막기 중 쳐내기 비활성화
→ behaviorType: ParryWhileGuarding, disableEnabled: true
```

---

## 실제 사용 예시

### 예시 1: 공격형 NPC 패턴

**목표**: HP가 낮을수록 공격적으로, 높을 때는 방어적으로

```
BT Entry 0 (최우선):
├── 조건: "자신 HP < 30% AND 공격 턴"
└── 액션들:
    ├── "공격 성공률 100% 설정" (priority: 10)
    ├── "'필살기' 태그 검술 선택" (priority: 5)
    └── "막기 중 쳐내기 비활성화" (priority: 1)

BT Entry 1:
├── 조건: "자신 HP < 50% AND 공격 턴"
└── 액션들:
    ├── "공격 성공률 +30% 증가" (priority: 8)
    └── "'강공격' 태그 검술 선택" (priority: 3)

BT Entry 2:
├── 조건: "자신 HP > 70% AND 방어 턴"
└── 액션들:
    ├── "막기 시도 확률 +20% 증가" (priority: 5)
    └── "쳐내기 성공률 +15% 증가" (priority: 3)

BT Entry 3 (기본):
├── 조건: "항상"
└── 액션: "기본 확률 유지"
```

**동작 시나리오**:
- **HP 25%, 공격 턴**: 필살기 사용, 공격 성공률 100%
- **HP 40%, 공격 턴**: 강공격 사용, 공격 성공률 +30%
- **HP 80%, 방어 턴**: 막기 시도 +20%, 쳐내기 성공률 +15%
- **기타 상황**: 기본 확률로 랜덤 행동

### 예시 2: 방어형 NPC 패턴

**목표**: 상대가 강할수록 방어적으로, 약할 때는 공격적으로

```
BT Entry 0:
├── 조건: "상대 자세 > 80% AND 방어 턴"
└── 액션들:
    ├── "막기 시도 확률 100% 설정" (priority: 10)
    └── "쳐내기 성공률 +40% 증가" (priority: 5)

BT Entry 1:
├── 조건: "상대 자세 < 30% AND 공격 턴"
└── 액션들:
    ├── "공격 성공률 +25% 증가" (priority: 8)
    └── "'빠른공격' 태그 검술 선택" (priority: 3)

BT Entry 2 (기본):
├── 조건: "항상"
└── 액션: "기본 확률 유지"
```

### 예시 3: executeOnce 활용 패턴

**목표**: 특정 상황에서 한 번만 특별한 행동

```
BT Entry 0:
├── 조건: "현재 턴 == 1"
└── 액션: "공격 성공률 +50% 증가" (executeOnce: true, priority: 10)

BT Entry 1:
├── 조건: "자신 HP < 10%"
└── 액션: "공격 성공률 100% 설정" (executeOnce: true, priority: 15)

BT Entry 2:
├── 조건: "상대 자세 < 20%"
└── 액션: "쳐내기 성공률 +30% 증가" (executeOnce: false, priority: 5)
```

**동작 시나리오**:
- **1턴째**: 공격 성공률 +50% (한 번만)
- **HP 5%가 되는 순간**: 공격 성공률 100% (한 번만)
- **상대 자세 < 20%일 때마다**: 쳐내기 성공률 +30% (매번)

---

## 고급 활용법

### 1. 복잡한 조건 조합

```
조건: "(자신 HP < 30% OR 자신 자세 < 20%) AND 상대 HP > 50%"

구성:
Selector (OR)
├── HP 조건: "자신 HP < 30%"
└── 자세 조건: "자신 자세 < 20%"
AND
자세 조건: "상대 HP > 50%"
```

### 2. Priority 활용

```
액션들:
├── "공격 성공률 100% 설정" (priority: 15) ← 최우선
├── "공격 성공률 +20% 증가" (priority: 10) ← 무시됨 (같은 대상)
├── "쳐내기 성공률 +30% 증가" (priority: 8)
└── "막기 시도 확률 +15% 증가" (priority: 5)
```

### 3. 다중 BT 활용

```
CharacterData.behaviorTrees:
├── BT_기본패턴 (항상 실행)
├── BT_위험상황 (HP < 20%일 때)
└── BT_보스패턴 (턴 > 10일 때)
```

---

## 문제 해결 가이드

### Q1: BT가 실행되지 않아요
**A**: 다음을 확인하세요:
- BT Entry의 `isEnabled`가 true인가?
- 조건 노드가 올바르게 설정되었는가?
- BT가 CharacterData에 할당되었는가?

### Q2: 액션이 실행되지 않아요
**A**: 다음을 확인하세요:
- 액션 노드의 `isEnabled`가 true인가?
- `executeOnce`가 true인데 이미 실행되었는가?
- Priority가 너무 낮은가?

### Q3: 확률이 예상과 다르게 적용되어요
**A**: 다음을 확인하세요:
- Priority가 높은 액션이 나중에 실행되어 덮어쓰는가?
- `adjustmentType`이 올바른가? (Absolute vs Relative)
- 여러 BT에서 같은 확률을 조정하는가?

### Q4: 검술이 선택되지 않아요
**A**: 다음을 확인하세요:
- `selectedCommandTag`에 해당하는 태그가 검술에 있는가?
- `selectedCommandIndex`가 유효한 범위인가?
- 여러 액션에서 다른 방식으로 검술을 선택하는가?

---

**문서 버전**: 1.0  
**작성일**: 2025년 10월 2일  
**최종 수정일**: 2025년 10월 2일

